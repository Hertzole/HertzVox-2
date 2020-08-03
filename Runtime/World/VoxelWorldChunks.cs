using Priority_Queue;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hertzole.HertzVox
{
    public partial class VoxelWorld
    {
        // Check to see if the world is currently generating chunks.
        private bool generatingChunks = false;

        // How many frames the generate chunks job has taken.
        private int generatingChunksFrame;

        private NativeArray<VoxelLoaderData> voxelLoaderData;
        private NativeArray<int3> loadedChunks;

        private NativeList<int3> tempChunksToRemove;
        private NativeList<int3> chunksToRemove;
        private NativeList<int3> renderChunks;
        private NativeHashMap<int3, ChunkData> tempRenderChunks;

        private JobHandle generateChunksJob;

        private Stack<MeshCollider> pooledColliders = new Stack<MeshCollider>();
        private Stack<MeshRenderer> pooledRenderers = new Stack<MeshRenderer>();

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
        private Dictionary<int3, MeshCollider> chunkColliders = new Dictionary<int3, MeshCollider>();
        private Dictionary<int3, MeshRenderer> chunkRenderers = new Dictionary<int3, MeshRenderer>();

        public int ChunkCount { get { return chunks.Count; } }

        public ICollection<Chunk> Chunks { get { return chunks.Values; } }

        private void UpdateChunks()
        {
            if (!useRendererPrefab)
            {
                for (int i = 0; i < renderChunks.Length; i++)
                {
                    chunks[renderChunks[i]].Draw(mat);
                }
            }

            if (!generatingChunks && Time.unscaledTime >= nextChunkGenerate)
            {
                nextChunkGenerate = Time.unscaledTime + chunkGenerateDelay;
                StartGeneratingChunks();
            }

            ProcessChunks();

            NativeList<int3> jobsToRemove = new NativeList<int3>(Allocator.Temp);

            ProcessGeneratorJobs(jobsToRemove);
            jobsToRemove.Clear();
            ProcessRenderJobs(jobsToRemove);
            jobsToRemove.Clear();
            ProcessColliderJobs(jobsToRemove);
            jobsToRemove.Dispose();
            ProcessChunkRemoval();
        }

        private void LateUpdateChunks()
        {
            if (generatingChunks)
            {
                generatingChunksFrame++;
                if (generateChunksJob.IsCompleted || generatingChunksFrame >= maxJobFrames)
                {
                    FinishGeneratingChunks();
                }
            }

            int numChunks = 0;
            while (generateQueue.Count > 0)
            {
                if (generator == null)
                {
                    break;
                }

                if (numChunks > maxGenerateJobs)
                {
                    break;
                }

                ChunkNode node = generateQueue.Dequeue();

                if (chunks.TryGetValue(node.position, out Chunk chunk))
                {
                    if (generateJobs.ContainsKey(chunk.position) || !chunk.NeedsTerrain || chunk.RequestedRemoval || chunk.GeneratingTerrain)
                    {
                        continue;
                    }

                    chunk.StartGenerating(new NativeArray<int>(Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE, Allocator.TempJob));

                    JobHandle job = generator.GenerateChunk(chunk.temporaryBlocks, chunk.position);
                    ChunkJobData data = new ChunkJobData(node.position, job, node.Priority, false);
                    generateJobs.Add(chunk.position, data);

                    numChunks++;
                }
            }

            numChunks = 0;

            while (renderQueue.Count > 0)
            {
                if (numChunks > maxRenderJobs)
                {
                    break;
                }

                ChunkNode node = renderQueue.Dequeue();
                if (chunks.TryGetValue(node.position, out Chunk chunk))
                {
                    if (AddChunkToRenderList(chunk, node.Priority))
                    {
                        numChunks++;
                    }
                }
            }

            numChunks = 0;

            while (colliderQueue.Count > 0)
            {
                if (numChunks > maxColliderJobs)
                {
                    break;
                }

                ChunkNode node = colliderQueue.Dequeue();
                if (chunks.TryGetValue(node.position, out Chunk chunk))
                {
                    if (colliderJobs.ContainsKey(node.position) || chunk.RequestedRemoval)
                    {
                        continue;
                    }

                    JobHandle job = chunk.ScheduleColliderJob();
                    colliderJobs.Add(node.position, new ChunkJobData(node.position, job, node.Priority, false));
                    numChunks++;
                }
            }
        }

        private void StartGeneratingChunks()
        {
            if (generatingChunks)
            {
                return;
            }

            generatingChunks = true;
            generatingChunksFrame = 0;

            voxelLoaderData = new NativeArray<VoxelLoaderData>(loaders.Count, Allocator.TempJob);
            loadedChunks = new NativeArray<int3>(chunks.Count, Allocator.TempJob);
            tempChunksToRemove = new NativeList<int3>(Allocator.TempJob);
            tempRenderChunks = new NativeHashMap<int3, ChunkData>(0, Allocator.TempJob);

            for (int i = 0; i < loaders.Count; i++)
            {
                voxelLoaderData[i] = loaders[i].ToData();
            }

            int index = 0;
            foreach (KeyValuePair<int3, Chunk> chunk in chunks)
            {
                loadedChunks[index] = chunk.Key;
                index++;
            }

            LoadChunksJob job = new LoadChunksJob()
            {
                chunkSize = Chunk.CHUNK_SIZE,
                maxY = maxY,
                worldSizeX = new int2(minX, maxX),
                worldSizeZ = new int2(minZ, maxZ),
                infiniteX = infiniteX,
                infiniteZ = infiniteZ,
                loaders = voxelLoaderData,
                loadedChunks = loadedChunks,
                chunksToRemove = tempChunksToRemove,
                renderChunks = tempRenderChunks
            };

            generateChunksJob = job.Schedule();
        }

        private void FinishGeneratingChunks()
        {
            if (!generatingChunks)
            {
                return;
            }

            generateChunksJob.Complete();

            voxelLoaderData.Dispose();
            loadedChunks.Dispose();

            NativeArray<int3> renChunksKeys = tempRenderChunks.GetKeyArray(Allocator.Temp);
            NativeArray<ChunkData> renChunksValues = tempRenderChunks.GetValueArray(Allocator.Temp);

            renderChunks.Clear();
            renderChunks.AddRange(renChunksKeys);

            for (int i = 0; i < renChunksKeys.Length; i++)
            {
                int3 chunkPos = renChunksKeys[i];
                bool shouldRender = renChunksValues[i].render;
                float priority = renChunksValues[i].priority;
                if (!chunks.TryGetValue(chunkPos, out Chunk chunk))
                {
                    chunk = CreateChunk(chunkPos);
                    chunks.Add(chunkPos, chunk);
                    chunk.NeedsTerrain = !Serialization.LoadChunk(chunk, true);
                    if (chunk.NeedsTerrain)
                    {
                        AddToQueue(generateQueue, chunkPos, priority);
                    }
                    else if (shouldRender)
                    {
                        TryToQueueChunkRender(chunk, priority);
                    }
                }
                else if (!shouldRender && chunkRenderers.TryGetValue(chunkPos, out MeshRenderer renderer))
                {
                    // This is used for removing edge chunks that shouldn't be rendered. 
                    // If we don't do this their meshes will stick around but not be properly updated.
                    PoolChunkRenderer(renderer);
                    chunkRenderers.Remove(chunkPos);
                }
                else if (shouldRender && chunk.HasRender && !chunkRenderers.ContainsKey(chunkPos))
                {
                    // This is in case there are chunks that should render but are missing renderers.
                    // Missing renderers can be caused by the method above.
                    CreateChunkRenderer(chunkPos, chunk.mesh);
                }
                else if (chunk.HasTerrain && !chunk.UpdatingRenderer && !chunk.HasRender && shouldRender)
                {
                    // The chunk is new and needs a mesh.
                    TryToQueueChunkRender(chunk, priority);
                }

                chunk.render = shouldRender;
            }

            renChunksKeys.Dispose();
            renChunksValues.Dispose();

            tempRenderChunks.Dispose();

            chunksToRemove.Clear();

            for (int i = 0; i < tempChunksToRemove.Length; i++)
            {
                DestroyChunk(chunks[tempChunksToRemove[i]]);
            }

            tempChunksToRemove.Dispose();
            generatingChunks = false;
        }

        public void UpdateChunkNeighbors(int3 chunkPosition, bool updateNorth, bool updateSouth, bool updateEast, bool updateWest, bool updateTop, bool updateBottom)
        {
            // Only update neighbor chunks if they need to be updated.
            if (updateNorth)
            {
                int3 northPos = new int3(chunkPosition.x, chunkPosition.y, chunkPosition.z + Chunk.CHUNK_SIZE);
                // Use TryGetValue instead because then we can get the chunk and also check if it exists, 
                // instead of getting the chunk again when calling update.
                if (chunks.TryGetValue(northPos, out Chunk northChunk) && northChunk.HasTerrain)
                {
                    northChunk.UpdateChunk();
                }
            }
            if (updateSouth)
            {
                int3 southPos = new int3(chunkPosition.x, chunkPosition.y, chunkPosition.z - Chunk.CHUNK_SIZE);
                if (chunks.TryGetValue(southPos, out Chunk southChunk) && southChunk.HasTerrain)
                {
                    southChunk.UpdateChunk();
                }
            }
            if (updateEast)
            {
                int3 eastPos = new int3(chunkPosition.x + Chunk.CHUNK_SIZE, chunkPosition.y, chunkPosition.z);
                if (chunks.TryGetValue(eastPos, out Chunk eastChunk) && eastChunk.HasTerrain)
                {
                    eastChunk.UpdateChunk();
                }
            }
            if (updateWest)
            {
                int3 westPos = new int3(chunkPosition.x - Chunk.CHUNK_SIZE, chunkPosition.y, chunkPosition.z);
                if (chunks.TryGetValue(westPos, out Chunk westChunk) && westChunk.HasTerrain)
                {
                    westChunk.UpdateChunk();
                }
            }
            if (updateTop)
            {
                int3 topPos = new int3(chunkPosition.x, chunkPosition.y + Chunk.CHUNK_SIZE, chunkPosition.z);
                if (chunks.TryGetValue(topPos, out Chunk topChunk) && topChunk.HasTerrain)
                {
                    topChunk.UpdateChunk();
                }
            }
            if (updateBottom)
            {
                int3 bottomPos = new int3(chunkPosition.x, chunkPosition.y - Chunk.CHUNK_SIZE, chunkPosition.z);
                if (chunks.TryGetValue(bottomPos, out Chunk bottomChunk) && bottomChunk.HasTerrain)
                {
                    bottomChunk.UpdateChunk();
                }
            }
        }

        private void FixValues(ref int start, ref int end)
        {
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }
        }

        public Chunk GetChunk(int3 position)
        {
            chunks.TryGetValue(position, out Chunk chunk);
            return chunk;
        }

        public bool TryGetChunk(int3 position, out Chunk chunk)
        {
            return chunks.TryGetValue(position, out chunk);
        }

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
                if (chunk.dirty && chunk.render)
                {
                    if (!TryToQueueChunkRender(chunk))
                    {
                        continue;
                    }
                    chunk.dirty = false;
                }
            }
        }

        private bool TryToQueueChunkRender(Chunk chunk, float priority = 0)
        {
            if (!AreNeighborsReady(chunk.position))
            {
                return false;
            }

            if (chunk.urgentUpdate)
            {
                AddChunkToRenderList(chunk, priority);
            }
            else
            {
                AddToQueue(renderQueue, chunk.position, priority);
            }

            return true;
        }

        private bool AreNeighborsReady(int3 position)
        {
            // North
            if (!IsNeighborReady(new int3(position.x, position.y, position.z + Chunk.CHUNK_SIZE)))
            {
                return false;
            }

            // South
            if (!IsNeighborReady(new int3(position.x, position.y, position.z - Chunk.CHUNK_SIZE)))
            {
                return false;
            }

            // East
            if (!IsNeighborReady(new int3(position.x + Chunk.CHUNK_SIZE, position.y, position.z)))
            {
                return false;
            }

            // West
            if (!IsNeighborReady(new int3(position.x - Chunk.CHUNK_SIZE, position.y, position.z)))
            {
                return false;
            }

            // Top
            if (position.y != Chunk.CHUNK_SIZE * maxY && !IsNeighborReady(new int3(position.x, position.y + Chunk.CHUNK_SIZE, position.z)))
            {
                return false;
            }

            // Bottom
            if (position.y != 0 && !IsNeighborReady(new int3(position.x, position.y - Chunk.CHUNK_SIZE, position.z)))
            {
                return false;
            }

            return true;
        }

        private bool IsNeighborReady(int3 position)
        {
            if (chunks.TryGetValue(position, out Chunk chunk))
            {
                if (!chunk.HasTerrain)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private void ProcessGeneratorJobs(NativeList<int3> jobsToRemove)
        {
            NativeKeyValueArrays<int3, ChunkJobData> jobs = generateJobs.GetKeyValueArrays(Allocator.Temp);

            for (int i = 0; i < jobs.Keys.Length; i++)
            {
                ChunkJobData data = jobs.Values[i];

                if (data.urgent || data.job.IsCompleted || data.frameCounter >= maxJobFrames)
                {
                    data.job.Complete();
                    Chunk chunk = chunks[data.position];
                    chunk.CompleteGenerating();

                    if (!chunk.RequestedRemoval && chunk.render)
                    {
                        TryToQueueChunkRender(chunk, data.priority);
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
                if (data.urgent || data.job.IsCompleted || data.frameCounter >= maxJobFrames)
                {
                    data.job.Complete();
                    Chunk chunk = chunks[data.position];

                    if (useRendererPrefab)
                    {
                        CreateChunkRenderer(data.position, chunk.CompleteMeshUpdate(chunk.mesh));
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
                if (data.urgent || data.job.IsCompleted || data.frameCounter >= maxJobFrames)
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

                    collider.sharedMesh = chunk.CompleteColliderMeshUpdate(collider.sharedMesh);

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
            return new Chunk(this, position, new ChunkBlocks(Chunk.CHUNK_SIZE));
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

        private MeshRenderer CreateChunkRenderer(int3 position, Mesh mesh)
        {
            if (!chunkRenderers.TryGetValue(position, out MeshRenderer renderer))
            {
                renderer = GetChunkRenderer();
                chunkRenderers.Add(position, renderer);
#if DEBUG
                renderer.gameObject.name = "Renderer [" + position.x + "," + position.y + "," + position.z + "]";
#endif
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            filter.mesh = mesh;
            return renderer;
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
