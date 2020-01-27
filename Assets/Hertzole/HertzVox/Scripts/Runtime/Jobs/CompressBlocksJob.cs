using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    [BurstCompile]
    public struct CompressBlocksJob : IJob
    {
        [ReadOnly]
        public NativeArray<Block> blocks;
        [ReadOnly]
        public int chunkSize;
        [WriteOnly]
        public NativeList<int2> compressedBlocks;

        public void Execute()
        {
            int currentBlock = blocks[0].id;
            int blockCount = 1;
            for (int i = 1; i < blocks.Length; i++)
            {
                if (blocks[i].id == currentBlock)
                {
                    blockCount++;
                }
                else
                {
                    compressedBlocks.Add(new int2(currentBlock, blockCount));
                    currentBlock = blocks[i].id;
                    blockCount = 1;
                }
            }

            // Compressed Z and X much closer and becomes a much smaller file for relative flat terrain. Just can't decompress it yet. >_>
            //for (int y = 0; y < chunkSize; y++)
            //{
            //    for (int x = 0; x < chunkSize; x++)
            //    {
            //        for (int z = 0; z < chunkSize; z++)
            //        {
            //            int index = GetIndex1DFrom3D(x, y, z, chunkSize);
            //            Debug.Log(index);
            //            if (blocks[index].id == currentBlock)
            //            {
            //                blockCount++;
            //            }
            //            else
            //            {
            //                compressedBlocks.Add(new int2(currentBlock, blockCount));
            //                currentBlock = blocks[index].id;
            //                blockCount = 1;
            //            }
            //        }
            //    }
            //}
        }

        private int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }
    }
}
