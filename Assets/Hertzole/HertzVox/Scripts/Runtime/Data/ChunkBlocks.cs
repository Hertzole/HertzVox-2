using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public struct ChunkBlocks : IDisposable
    {
        public NativeArray<Block> blocks;

        public Block Get(int x, int y, int z)
        {
            return blocks[Helpers.GetIndex1DFrom3D(x, y, z, Chunk.CHUNK_SIZE)];
        }

        public Block Get(int3 position)
        {
            return Get(position.x, position.y, position.z);
        }

        public void Set(int x, int y, int z, Block block)
        {
            blocks[Helpers.GetIndex1DFrom3D(x, y, z, Chunk.CHUNK_SIZE)] = block;
        }

        public void Set(int3 position, Block block)
        {
            Set(position.x, position.y, position.z, block);
        }

        public void Dispose()
        {
            if (blocks.IsCreated)
            {
                blocks.Dispose();
            }
        }
    }
}