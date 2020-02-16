using Priority_Queue;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hertzole.HertzVox
{
    public partial class VoxelWorld
    {
        private Stack<MeshCollider> pooledColliders = new Stack<MeshCollider>();
        private Stack<MeshRenderer> pooledRenderers = new Stack<MeshRenderer>();

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
        private Dictionary<int3, MeshCollider> chunkColliders = new Dictionary<int3, MeshCollider>();
        private Dictionary<int3, MeshRenderer> chunkRenderers = new Dictionary<int3, MeshRenderer>();

        private const int MAX_FRAMES = 3;

        public int ChunkCount { get { return chunks.Count; } }

        public ICollection<Chunk> Chunks { get { return chunks.Values; } }

        private void AddToQueue(FastPriorityQueue<ChunkNode> queue, int3 position, float priority)
        {
            ChunkNode node = new ChunkNode(position);
#if DEBUG
            if (queue.Contains(node))
            {
                Debug.LogWarning("Wants to enqueue generator job but it already exists.");
                return;
            }
#endif
            if (queue.Count + 1 >= queue.MaxSize)
            {
                queue.Resize(queue.Count + 32);
            }

            queue.Enqueue(node, priority);
        }

        private void ProcessChunks()
        {
            foreach (Chunk chunk in chunks.Values)
            {
                if (chunk.dirty)
                {
                    chunk.dirty = false;

                    if (chunk.urgentUpdate)
                    {
                        AddChunkToRenderList(chunk, 0);
                    }
                    else
                    {
                        ChunkNode node = new ChunkNode(chunk.position);
                        if (!renderQueue.Contains(node))
                        {
                            renderQueue.Enqueue(node, 0);
                        }
                    }
                }
            }
        }

        private void ProcessGeneratorJobs(NativeList<int3> jobsToRemove)
        {
            NativeKeyValueArrays<int3, ChunkJobData> jobs = generateJobs.GetKeyValueArrays(Allocator.Temp);

            for (int i = 0; i < jobs.Keys.Length; i++)
            {
                ChunkJobData data = jobs.Values[i];

                if (data.urgent || data.job.IsCompleted || data.frameCounter >= MAX_FRAMES)
                {
                    data.job.Complete();
                    Chunk chunk = chunks[data.position];
                    chunk.CompleteGenerating();

                    if (!chunk.RequestedRemoval)
                    {
                        AddToQueue(renderQueue, data.position, data.priority);
                    }

                    jobsToRemove.Add(jobs.Keys[i]);
                }
                else
                {
                    data.frameCounter++;
                    generateJobs[jobs.Keys[i]] = data;
                }
            }

            jobs.Dispose();

            for (int i = 0; i < jobsToRemove.Length; i++)
            {
                generateJobs.Remove(jobsToRemove[i]);
            }
        }

        private void ProcessRenderJobs(NativeList<int3> jobsToRemove)
        {
            NativeKeyValueArrays<int3, ChunkJobData> jobs = renderJobs.GetKeyValueArrays(Allocator.Temp);

            for (int i = 0; i < jobs.Keys.Length; i++)
            {
                ChunkJobData data = jobs.Values[i];
                if (data.urgent || data.job.IsCompleted || data.frameCounter >= MAX_FRAMES)
                {
                    data.job.Complete();
                    Chunk chunk = chunks[data.position];

                    if (useRendererPrefab)
                    {
                        if (!chunkRenderers.TryGetValue(data.position, out MeshRenderer renderer))
                        {
                            renderer = GetChunkRenderer();
                            chunkRenderers.Add(data.position, renderer);
#if DEBUG
                            renderer.gameObject.name = "Renderer [" + data.position.x + "," + data.position.y + "," + data.position.z + "]";
#endif
                        }

                        MeshFilter filter = renderer.GetComponent<MeshFilter>();
                        Mesh originalMesh = filter.mesh;
                        filter.mesh = chunk.CompleteMeshUpdate(originalMesh);
                    }
                    else
                    {
                        chunk.CompleteMeshUpdate(chunk.mesh);
                    }

                    if (!chunk.RequestedRemoval)
                    {
                        AddToQueue(colliderQueue, data.position, data.priority);
                    }

                    jobsToRemove.Add(jobs.Keys[i]);
                }
                else
                {
                    data.frameCounter++;
                    renderJobs[jobs.Keys[i]] = data;
                }
            }

            jobs.Dispose();

            for (int i = 0; i < jobsToRemove.Length; i++)
            {
                renderJobs.Remove(jobsToRemove[i]);
            }
        }

        private void ProcessColliderJobs(NativeList<int3> jobsToRemove)
        {
            NativeKeyValueArrays<int3, ChunkJobData> jobs = colliderJobs.GetKeyValueArrays(Allocator.Temp);

            for (int i = 0; i < jobs.Keys.Length; i++)
            {
                ChunkJobData data = jobs.Values[i];
                if (data.urgent || data.job.IsCompleted || data.frameCounter >= MAX_FRAMES)
                {
                    data.job.Complete();
                    Chunk chunk = chunks[data.position];

                    if (!chunkColliders.TryGetValue(data.position, out MeshCollider collider))
                    {
                        collider = GetChunkCollider();
                        chunkColliders.Add(data.position, collider);
#if DEBUG
                        collider.gameObject.name = "Collider [" + data.position.x + "," + data.position.y + "," + data.position.z + "]";
#endif
                    }

                    Mesh originalMesh = collider.sharedMesh;
                    collider.sharedMesh = chunk.CompleteColliderMeshUpdate(originalMesh);

                    jobsToRemove.Add(jobs.Keys[i]);
                }
                else
                {
                    data.frameCounter++;
                    colliderJobs[jobs.Keys[i]] = data;
                }
            }

            jobs.Dispose();

            for (int i = 0; i < jobsToRemove.Length; i++)
            {
                colliderJobs.Remove(jobsToRemove[i]);
            }
        }

        private void ProcessChunkRemoval()
        {
            //TODO: Pool chunks in memory pool.
            for (int i = chunksToRemove.Length - 1; i >= 0; i--)
            {
                if (chunks.TryGetValue(chunksToRemove[i], out Chunk chunk))
                {
                    if (!chunk.CanRemove)
                    {
                        continue;
                    }

                    if (chunk.changed)
                    {
                        Serialization.SaveChunk(chunk, true);
                    }
                    chunk.Dispose();

                    if (chunkColliders.Count > 0)
                    {
                        if (chunkColliders.TryGetValue(chunksToRemove[i], out MeshCollider collider))
                        {
                            PoolChunkCollider(collider);
                            chunkColliders.Remove(chunksToRemove[i]);
                        }
                    }

                    if (chunkRenderers.Count > 0)
                    {
                        if (chunkRenderers.TryGetValue(chunksToRemove[i], out MeshRenderer renderer))
                        {
                            PoolChunkRenderer(renderer);
                            chunkRenderers.Remove(chunksToRemove[i]);
                        }
                    }

                    chunks.Remove(chunksToRemove[i]);
                }
            }
        }

        private bool AddChunkToRenderList(Chunk chunk, float priority = 0)
        {
            if (renderJobs.ContainsKey(chunk.position) || chunk.RequestedRemoval)
            {
                return false;
            }

            JobHandle job = chunk.ScheduleRenderJob();
            ChunkJobData data = new ChunkJobData(chunk.position, job, priority, false);
            renderJobs.Add(chunk.position, data);

            return true;
        }

        private Chunk CreateChunk(int3 position)
        {
            Chunk chunk = new Chunk(position)
            {
                blocks = new ChunkBlocks(Chunk.CHUNK_SIZE)
            };

            return chunk;
        }

        private void DestroyChunk(Chunk chunk)
        {
            Assert.IsNotNull(chunk);

            if (chunk.RequestedRemoval || chunksToRemove.Contains(chunk.position))
            {
                return;
            }

            chunk.RequestRemoval();
            chunksToRemove.Add(chunk.position);
        }

        private MeshCollider GetChunkCollider()
        {
            MeshCollider collider = pooledColliders.Count > 0 ? pooledColliders.Pop() : Instantiate(chunkColliderPrefab, transform);
            collider.gameObject.SetActive(true);
            return collider;
        }

        private void PoolChunkCollider(MeshCollider collider)
        {
            collider.gameObject.SetActive(false);
            pooledColliders.Push(collider);
        }

        private MeshRenderer GetChunkRenderer()
        {
            MeshRenderer renderer = pooledRenderers.Count > 0 ? pooledRenderers.Pop() : Instantiate(rendererPrefab, transform);
            renderer.material = mat;
            renderer.gameObject.SetActive(true);
            return renderer;
        }

        private void PoolChunkRenderer(MeshRenderer renderer)
        {
            renderer.gameObject.SetActive(false);
            pooledRenderers.Push(renderer);
        }
    }
}
