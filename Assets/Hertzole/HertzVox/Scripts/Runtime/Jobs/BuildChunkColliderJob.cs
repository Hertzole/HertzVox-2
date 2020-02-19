using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    /*
     *  Algorithm from https://eddieabbondanz.io/post/voxel/greedy-mesh/
     * */

    [BurstCompile]
    public struct BuildChunkColliderJob : IJob
    {
        [ReadOnly]
        public int3 position;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<ushort> blocks;
        [ReadOnly]
        public int chunkSize;
        [ReadOnly]
        public NativeHashMap<ushort, Block> blockMap;

        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> indicies;

        private int3 startPos;
        private int3 currPos;
        private int3 quadSize;
        private int3 m, n;
        private int3 offsetPos;

        private int verticesLength;

        private ushort startBlock;

        public void Execute()
        {
            DoNewCode();
        }

        private void DoNewCode()
        {
            verticesLength = 0;
            int direction;
            int workAxis1;
            int workAxis2;

            for (int face = 0; face < 6; face++)
            {
                bool isBackFace = face > 2;
                direction = face % 3;
                workAxis1 = (direction + 1) % 3;
                workAxis2 = (direction + 2) % 3;

                startPos = new int3();
                currPos = new int3();

                for (startPos[direction] = 0; startPos[direction] < chunkSize; startPos[direction]++)
                {
                    NativeArray<bool> merged = new NativeArray<bool>(chunkSize * chunkSize, Allocator.Temp);

                    // Build the slices of the mesh.
                    for (startPos[workAxis1] = 0; startPos[workAxis1] < chunkSize; startPos[workAxis1]++)
                    {
                        for (startPos[workAxis2] = 0; startPos[workAxis2] < chunkSize; startPos[workAxis2]++)
                        {
                            startBlock = GetBlock(startPos);

                            // If this block has already been mereged, is air, or not visible: skip it.
                            if (merged[GetIndex1DFrom2D(startPos[workAxis1], startPos[workAxis2], chunkSize)] || !blockMap[startBlock].canCollide || !IsBlockFaceVisible(startPos, direction, isBackFace))
                            {
                                continue;
                            }

                            quadSize = new int3();
                            for (currPos = startPos, currPos[workAxis2]++; currPos[workAxis2] < chunkSize &&
                                CompareStep(startPos, currPos, direction, isBackFace) && !merged[GetIndex1DFrom2D(currPos[workAxis1], currPos[workAxis2], chunkSize)];
                                currPos[workAxis2]++)
                            { }
                            quadSize[workAxis2] = currPos[workAxis2] - startPos[workAxis2];

                            for (currPos = startPos, currPos[workAxis1]++; currPos[workAxis1] < chunkSize && CompareStep(startPos, currPos, direction, isBackFace) &&
                                !merged[GetIndex1DFrom2D(currPos[workAxis1], currPos[workAxis2], chunkSize)]; currPos[workAxis1]++)
                            {
                                for (currPos[workAxis2] = startPos[workAxis2]; currPos[workAxis2] < chunkSize && CompareStep(startPos, currPos, direction, isBackFace) &&
                                    !merged[GetIndex1DFrom2D(currPos[workAxis1], currPos[workAxis2], chunkSize)]; currPos[workAxis2]++)
                                { }

                                // If we didn't reach the end then its not a good add.
                                if (currPos[workAxis2] - startPos[workAxis2] < quadSize[workAxis2])
                                {
                                    break;
                                }
                                else
                                {
                                    currPos[workAxis2] = startPos[workAxis2];
                                }
                            }
                            quadSize[workAxis1] = currPos[workAxis1] - startPos[workAxis1];

                            m = new int3();
                            m[workAxis1] = quadSize[workAxis1];

                            n = new int3();
                            n[workAxis2] = quadSize[workAxis2];

                            offsetPos = startPos;
                            offsetPos[direction] += isBackFace ? 0 : 1;

                            AppendQuad(offsetPos, offsetPos + m, offsetPos + m + n, offsetPos + n, isBackFace);

                            for (int f = 0; f < quadSize[workAxis1]; f++)
                            {
                                for (int g = 0; g < quadSize[workAxis2]; g++)
                                {
                                    merged[GetIndex1DFrom2D(startPos[workAxis1] + f, startPos[workAxis2] + g, chunkSize)] = true;
                                }
                            }
                        }
                    }

                    merged.Dispose();
                }
            }
        }

        private bool IsBlockFaceVisible(int3 blockPosition, int axis, bool backFace)
        {
            blockPosition[axis] += backFace ? -1 : 1;
            return !CanBlockCollide(blockPosition.x, blockPosition.y, blockPosition.z);
        }

        private bool CompareStep(int3 a, int3 b, int direction, bool isBackFace)
        {
            ushort blockA = GetBlock(a);
            ushort blockB = GetBlock(b);

            return blockA == blockB && blockMap[blockB].canCollide && IsBlockFaceVisible(b, direction, isBackFace);
        }

        private void AppendQuad(int3 tl, int3 tr, int3 bl, int3 br, bool isBackFace)
        {
            vertices.Add(position + tl);
            vertices.Add(position + tr);
            vertices.Add(position + bl);
            vertices.Add(position + br);

            verticesLength += 4;

            if (!isBackFace)
            {
                indicies.Add(verticesLength - 4);
                indicies.Add(verticesLength - 3);
                indicies.Add(verticesLength - 2);

                indicies.Add(verticesLength - 4);
                indicies.Add(verticesLength - 2);
                indicies.Add(verticesLength - 1);
            }
            else
            {
                indicies.Add(verticesLength - 2);
                indicies.Add(verticesLength - 3);
                indicies.Add(verticesLength - 4);

                indicies.Add(verticesLength - 1);
                indicies.Add(verticesLength - 2);
                indicies.Add(verticesLength - 4);
            }
        }

        private void AppendQuadOld(int3 tl, int3 tr, int3 bl, int3 br)
        {
            int index = vertices.Length;

            vertices.Add(position + tl);
            vertices.Add(position + tr);
            vertices.Add(position + bl);
            vertices.Add(position + br);

            indicies.Add(index);
            indicies.Add(index + 1);
            indicies.Add(index + 2);
            indicies.Add(index + 2);
            indicies.Add(index + 3);
            indicies.Add(index);
        }

        private int GetIndex1DFrom2D(int x, int z, int size)
        {
            return x + z * size;
        }

        private int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }

        private ushort GetBlock(int3 position)
        {
            return blocks[GetIndex1DFrom3D(position.x, position.y, position.z, chunkSize)];
        }

        private bool CanBlockCollide(int x, int y, int z)
        {
            int index = GetIndex1DFrom3D(x, y, z, chunkSize);
            if (index < 0 || index >= blocks.Length)
            {
                return false;
            }

            return blockMap[blocks[index]].canCollide;
        }
    }
}