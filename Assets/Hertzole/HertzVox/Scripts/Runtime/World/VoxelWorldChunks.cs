using Priority_Queue;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public partial class VoxelWorld
    {
        private const int MAX_FRAMES = 3;

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
                    chunk.CompleteMeshUpdate();

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
                        collider = GetCollider();
                        chunkColliders.Add(data.position, collider);
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

                    if (chunkColliders.TryGetValue(chunksToRemove[i], out MeshCollider collider))
                    {
                        PoolCollider(collider);
                        chunkColliders.Remove(chunksToRemove[i]);
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
    }
}
