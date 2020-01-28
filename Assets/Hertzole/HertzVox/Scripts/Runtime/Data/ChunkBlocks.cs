using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public struct ChunkBlocks : IDisposable
    {
        private NativeArray<ushort> blocks;

        public ChunkBlocks(int size)
        {
            blocks = new NativeArray<ushort>(size * size * size, Allocator.Persistent);
        }

        public NativeArray<ushort> GetBlocks()
        {
            return blocks;
        }

        public Block Get(int x, int y, int z)
        {
            return BlockProvider.GetBlock(blocks[Helpers.GetIndex1DFrom3D(x, y, z, Chunk.CHUNK_SIZE)]);
        }

        public Block Get(int3 position)
        {
            return Get(position.x, position.y, position.z);
        }

        public Block Get(int index)
        {
            return BlockProvider.GetBlock(blocks[index]);
        }

        public void Set(int x, int y, int z, Block block)
        {
            blocks[Helpers.GetIndex1DFrom3D(x, y, z, Chunk.CHUNK_SIZE)] = block.id;
        }

        public void Set(int3 position, Block block)
        {
            Set(position.x, position.y, position.z, block);
        }

        public void Set(int index, Block block)
        {
            blocks[index] = block.id;
        }

        public void Dispose()
        {
            if (blocks.IsCreated)
            {
                blocks.Dispose();
            }
        }

        public NativeList<int2> Compress()
        {
            NativeList<int2> compressedBlocks = new NativeList<int2>(Allocator.TempJob);

            new CompressBlocksJob()
            {
                blocks = blocks,
                compressedBlocks = compressedBlocks
            }.Run();

            return compressedBlocks;
        }

        public void DecompressAndApply(NativeList<int2> list)
        {
            int index = 0;

            for (int i = 0; i < list.Length; i++)
            {
                Block block = BlockProvider.GetBlock((ushort)list[i].x);

                for (int j = 0; j < list[i].y; j++)
                {
                    blocks[index] = block.id;
                    index++;
                }
            }

            list.Dispose();
        }
    }
}