using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

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

        public void GenerateChunk(Chunk chunk, int3 position)
        {
            ushort[] blocks = new ushort[Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE];

            int index = 0;

            Profiler.BeginSample("Create Chunk :: Set blocks");
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        if (y < 4 && position.y == 0)
                        {
                            blocks[index] = stone.id;
                        }
                        else if (y >= 4 && y < 6 && position.y == 0)
                        {
                            blocks[index] = dirt.id;
                        }
                        else if (y == 6 && position.y == 0)
                        {
                            blocks[index] = grass.id;
                        }
                        else
                        {
                            blocks[index] = air.id;
                        }
                        index++;
                    }
                }
            }

            chunk.blocks.CopyFrom(blocks);
            Profiler.EndSample();
        }
    }
}