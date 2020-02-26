using System;
using System.Collections.Generic;
using Unity.Collections;
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

        public NativeArray<ushort> GetBlocks(Allocator allocator)
        {
            return new NativeArray<ushort>(blocks, allocator);
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

        public void CopyFrom(NativeArray<ushort> array)
        {
            blocks.CopyFrom(array);
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
            NativeList<int2> compressedBlocks = new NativeList<int2>(Allocator.Temp);

            int currentBlock = blocks[0];
            int blockCount = 1;
            for (int i = 1; i < blocks.Length; i++)
            {
                if (blocks[i] == currentBlock)
                {
                    blockCount++;
                    // As long as isn't the last block, just keep going. Else add the block.
                    if (i != blocks.Length - 1)
                    {
                        continue;
                    }
                }

                compressedBlocks.Add(new int2(currentBlock, blockCount));
                currentBlock = blocks[i];
                blockCount = 1;
            }

            //new CompressBlocksJob()
            //{
            //    blocks = blocks,
            //    compressedBlocks = compressedBlocks
            //}.Run();
            return compressedBlocks;
        }

        public void DecompressAndApply(NativeList<int2> list, Dictionary<ushort, string> palette)
        {
            int index = 0;

            for (int i = 0; i < list.Length; i++)
            {
                if (!BlockProvider.TryGetBlock(palette[(ushort)list[i].x], out Block block))
                {
#if DEBUG
                    UnityEngine.Debug.LogWarning("Couldn't find block with ID '" + palette[(ushort)list[i].x] + "' when decompressing. Replacing it with air.");
#endif
                    block = BlockProvider.GetBlock(BlockProvider.AIR_TYPE_ID);
                }

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