using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    [BurstCompile]
    public struct BuildChunkColliderJob : IJob
    {
        [ReadOnly]
        public int3 position;
        [ReadOnly]
        public int size;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<ushort> blocks;
        [ReadOnly]
        public NativeHashMap<ushort, Block> blockMap;
        [ReadOnly]
        public NativeArray<ushort> northBlocks;
        [ReadOnly]
        public NativeArray<ushort> southBlocks;
        [ReadOnly]
        public NativeArray<ushort> eastBlocks;
        [ReadOnly]
        public NativeArray<ushort> westBlocks;
        [ReadOnly]
        public NativeArray<ushort> upBlocks;
        [ReadOnly]
        public NativeArray<ushort> downBlocks;

        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> indicies;
        [WriteOnly]
        public NativeList<float3> normals;

        public void Execute()
        {
            DoOldCode();
        }

        private void DoOldCode()
        {
            int index = 0;
            int vertexIndex = 0;
            int trianglesIndex = 0;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        Block block = blockMap[blocks[index]];

                        if (!block.canCollide)
                        {
                            index++;
                            continue;
                        }

                        // North
                        if ((z < size - 1 && IsTransparent(blockMap[blocks[index + 1]], block)) || (z == size - 1 && IsTransparent(blockMap[northBlocks[GetIndex1DFrom3D(x, y, 0, size)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1));

                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 2);

                            float3 normal = new float3(0, 0, 1);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // East
                        if ((x < size - 1 && IsTransparent(blockMap[blocks[index + size * size]], block)) || (x == size - 1 && IsTransparent(blockMap[eastBlocks[GetIndex1DFrom3D(0, y, z, size)]], block)))
                        {
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            float3 normal = new float3(1, 0, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // South
                        if ((z > 0 && IsTransparent(blockMap[blocks[index - 1]], block)) || (z == 0 && IsTransparent(blockMap[southBlocks[GetIndex1DFrom3D(x, y, size - 1, size)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            float3 normal = new float3(0, 0, -1);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // West
                        if ((x > 0 && IsTransparent(blockMap[blocks[index - size * size]], block)) || (x == 0 && IsTransparent(blockMap[westBlocks[GetIndex1DFrom3D(size - 1, y, z, size)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z + 1));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            float3 normal = new float3(-1, 0, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // Up
                        if ((y < size - 1 && IsTransparent(blockMap[blocks[index + size]], block)) || (y == size - 1 && IsTransparent(blockMap[upBlocks[GetIndex1DFrom3D(x, 0, z, size)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            float3 normal = new float3(0, 1, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // Down
                        if ((y > 0 && IsTransparent(blockMap[blocks[index - size]], block)) || (y == 0 && IsTransparent(blockMap[upBlocks[GetIndex1DFrom3D(x, size - 1, z, size)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z + 1));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            float3 normal = new float3(0, -1, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        index++;
                    }
                }
            }
        }

        private int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }

        private bool IsTransparent(Block block, Block currentBlock)
        {
            if (block.id == 0)
            {
                return true;
            }
            else if (block.transparent && !block.connectToSame)
            {
                return true && block.canCollide;
            }
            else if (block.transparent && block.connectToSame)
            {
                if (block.id == currentBlock.id)
                {
                    return false && block.canCollide;
                }
                else
                {
                    return true && block.canCollide;
                }
            }

            return false && block.canCollide;
        }
    }
}