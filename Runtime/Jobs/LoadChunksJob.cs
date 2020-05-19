using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    [BurstCompile]
    public struct LoadChunksJob : IJob
    {
        [ReadOnly]
        public int chunkSize;
        [ReadOnly]
        public int maxY;
        [ReadOnly]
        public int2 worldSizeX;
        [ReadOnly]
        public int2 worldSizeZ;
        [ReadOnly]
        public bool infiniteX;
        [ReadOnly]
        public bool infiniteZ;
        [ReadOnly]
        public NativeArray<VoxelLoaderData> loaders;

        [ReadOnly]
        public NativeArray<int3> loadedChunks;
        [WriteOnly]
        public NativeList<int3> chunksToRemove;
        public NativeHashMap<int3, ChunkData> renderChunks;

        public void Execute()
        {
            for (int i = 0; i < loaders.Length; i++)
            {
                VoxelLoaderData loader = loaders[i];

                int3 targetPosition = WorldToChunk(loader.position);

                int xMin = (loader.singleChunk ? 0 : -loader.chunkDistanceX) + targetPosition.x - 1;
                int zMin = (loader.singleChunk ? 0 : -loader.chunkDistanceZ) + targetPosition.z - 1;
                int xMax = (loader.singleChunk ? 1 : loader.chunkDistanceX) + targetPosition.x + 1;
                int zMax = (loader.singleChunk ? 1 : loader.chunkDistanceZ) + targetPosition.z + 1;

                for (int x = xMin; x < xMax; x++)
                {
                    for (int z = zMin; z < zMax; z++)
                    {
                        for (int y = 0; y < maxY; y++)
                        {
                            if ((!infiniteX && (x < worldSizeX.x || x > worldSizeX.y)) || (!infiniteZ && (z < worldSizeZ.x || z > worldSizeZ.y)))
                            {
                                continue;
                            }

                            int3 chunkPosition = new int3(x * chunkSize, y * chunkSize, z * chunkSize);

                            if (renderChunks.ContainsKey(chunkPosition))
                            {
                                continue;
                            }

                            bool shouldRender = false;
                            if (x != xMin && z != zMin && x != xMax - 1 && z != zMax - 1)
                            {
                                shouldRender = true;
                            }

                            float priority = math.distancesq(chunkPosition, targetPosition);
                            renderChunks.Add(chunkPosition, new ChunkData() { priority = priority, render = shouldRender });
                        }
                    }
                }
            }

            for (int i = 0; i < loadedChunks.Length; i++)
            {
                if (!renderChunks.ContainsKey(loadedChunks[i]))
                {
                    chunksToRemove.Add(loadedChunks[i]);
                }
            }
        }

        private int3 WorldToChunk(float3 worldPosition)
        {
            return math.int3(math.float3(math.floor(worldPosition.x / chunkSize), math.floor(worldPosition.y / chunkSize), math.floor(worldPosition.z / chunkSize)));
        }
    }
}
