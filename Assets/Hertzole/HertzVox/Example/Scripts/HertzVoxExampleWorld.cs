using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox.Example
{
    public class HertzVoxExampleWorld : MonoBehaviour, IVoxGeneration
    {
        private Block stone;
        private Block dirt;
        private Block grass;
        private Block air;

        private void Awake()
        {
            stone = BlockProvider.GetBlock("stone");
            dirt = BlockProvider.GetBlock("dirt");
            grass = BlockProvider.GetBlock("grass");
            air = BlockProvider.GetBlock("air");
        }

        public JobHandle GenerateChunk(NativeArray<ushort> blocks, int3 position)
        {
            return new GenerateJob()
            {
                position = position,
                chunkSize = Chunk.CHUNK_SIZE,
                blocks = blocks,
                stoneId = stone.id,
                dirtId = dirt.id,
                grassId = grass.id,
                airId = air.id
            }.Schedule();
        }
    }

    [BurstCompile]
    public struct GenerateJob : IJob
    {
        [ReadOnly]
        public int3 position;
        [ReadOnly]
        public int chunkSize;

        [ReadOnly]
        public ushort stoneId;
        [ReadOnly]
        public ushort dirtId;
        [ReadOnly]
        public ushort grassId;
        [ReadOnly]
        public ushort airId;

        [WriteOnly]
        public NativeArray<ushort> blocks;

        public void Execute()
        {
            int index = 0;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if (y < 4 && position.y == 0)
                        {
                            blocks[index] = stoneId;
                        }
                        else if (y >= 4 && y < 6 && position.y == 0)
                        {
                            blocks[index] = dirtId;
                        }
                        else if (y == 6 && position.y == 0)
                        {
                            blocks[index] = grassId;
                        }
                        else
                        {
                            blocks[index] = airId;
                        }
                        index++;
                    }
                }
            }
        }
    }
}