using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public partial class VoxelWorld
    {
        private const int MAX_FRAMES = 3;

        private void ProcessChunks()
        {
            foreach (Chunk chunk in chunks.Values)
            {
                if (chunk.dirty)
                {
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
                        renderQueue.Enqueue(new ChunkNode(data.position), data.priority);
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
                        colliderQueue.Enqueue(new ChunkNode(data.position), data.priority);
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
                    MeshCollider collider = chunkColliders.TryGetValue(chunk.position, out MeshCollider col) ? col : GetCollider();
                    Mesh originalMesh = collider.sharedMesh;
                    collider.sharedMesh = chunk.CompleteColliderMeshUpdate(originalMesh);
                    chunkColliders[data.position] = collider;

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
