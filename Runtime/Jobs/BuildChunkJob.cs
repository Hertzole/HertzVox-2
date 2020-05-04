using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    [BurstCompile]
    public struct BuildChunkJob : IJob
    {
        [ReadOnly]
        public int3 position;
        [ReadOnly]
        public int chunkSize;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> blocks;
        [ReadOnly]
        public NativeHashMap<int, Block> blockMap;
        [ReadOnly]
        public NativeHashMap<int, int2> textures;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> northBlocks;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> southBlocks;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> eastBlocks;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> westBlocks;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> upBlocks;
        [ReadOnly]
        [DeallocateOnJobCompletion]
        public NativeArray<int> downBlocks;

        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> indicies;
        [WriteOnly]
        public NativeList<float4> uvs;
        [WriteOnly]
        public NativeList<float4> colors;
        [WriteOnly]
        public NativeList<float3> normals;

        private int vertexIndex;

        public void Execute()
        {
            BuildChunk();
        }

        private void BuildChunk()
        {
            int index = 0;
            vertexIndex = 0;
            int trianglesIndex = 0;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        Block block = blockMap[blocks[index]];

                        if (block.id == 0)
                        {
                            index++;
                            continue;
                        }

                        // North
                        if ((z < chunkSize - 1 && IsTransparent(blockMap[blocks[index + 1]], block)) || (z == chunkSize - 1 && IsTransparent(blockMap[northBlocks[GetIndex1DFrom3D(x, y, 0, chunkSize)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1));

                            int2 northTexture = textures[block.northTexture];

                            uvs.Add(new float4(0, 0, northTexture.x, northTexture.y));
                            uvs.Add(new float4(1, 0, northTexture.x, northTexture.y));
                            uvs.Add(new float4(0, 1, northTexture.x, northTexture.y));
                            uvs.Add(new float4(1, 1, northTexture.x, northTexture.y));

                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 2);

                            colors.Add(block.northColor);
                            colors.Add(block.northColor);
                            colors.Add(block.northColor);
                            colors.Add(block.northColor);

                            float3 normal = new float3(0, 0, 1);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // East
                        if ((x < chunkSize - 1 && IsTransparent(blockMap[blocks[index + chunkSize * chunkSize]], block)) || (x == chunkSize - 1 && IsTransparent(blockMap[eastBlocks[GetIndex1DFrom3D(0, y, z, chunkSize)]], block)))
                        {
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1));

                            int2 eastTexture = textures[block.eastTexture];

                            uvs.Add(new float4(0, 0, eastTexture.x, eastTexture.y));
                            uvs.Add(new float4(1, 0, eastTexture.x, eastTexture.y));
                            uvs.Add(new float4(0, 1, eastTexture.x, eastTexture.y));
                            uvs.Add(new float4(1, 1, eastTexture.x, eastTexture.y));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            colors.Add(block.eastColor);
                            colors.Add(block.eastColor);
                            colors.Add(block.eastColor);
                            colors.Add(block.eastColor);

                            float3 normal = new float3(1, 0, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // South
                        if ((z > 0 && IsTransparent(blockMap[blocks[index - 1]], block)) || (z == 0 && IsTransparent(blockMap[southBlocks[GetIndex1DFrom3D(x, y, chunkSize - 1, chunkSize)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z));

                            int2 southTexture = textures[block.southTexture];

                            uvs.Add(new float4(0, 0, southTexture.x, southTexture.y));
                            uvs.Add(new float4(1, 0, southTexture.x, southTexture.y));
                            uvs.Add(new float4(0, 1, southTexture.x, southTexture.y));
                            uvs.Add(new float4(1, 1, southTexture.x, southTexture.y));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            colors.Add(block.southColor);
                            colors.Add(block.southColor);
                            colors.Add(block.southColor);
                            colors.Add(block.southColor);

                            float3 normal = new float3(0, 0, -1);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // West
                        if ((x > 0 && IsTransparent(blockMap[blocks[index - chunkSize * chunkSize]], block)) || (x == 0 && IsTransparent(blockMap[westBlocks[GetIndex1DFrom3D(chunkSize - 1, y, z, chunkSize)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z + 1));

                            int2 westTexture = textures[block.westTexture];

                            uvs.Add(new float4(0, 0, westTexture.x, westTexture.y));
                            uvs.Add(new float4(0, 1, westTexture.x, westTexture.y));
                            uvs.Add(new float4(1, 0, westTexture.x, westTexture.y));
                            uvs.Add(new float4(1, 1, westTexture.x, westTexture.y));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            colors.Add(block.westColor);
                            colors.Add(block.westColor);
                            colors.Add(block.westColor);
                            colors.Add(block.westColor);

                            float3 normal = new float3(-1, 0, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // Up
                        if ((y < chunkSize - 1 && IsTransparent(blockMap[blocks[index + chunkSize]], block)) || (y == chunkSize - 1 && IsTransparent(blockMap[upBlocks[GetIndex1DFrom3D(x, 0, z, chunkSize)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y + 1, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1));

                            int2 topTexture = textures[block.topTexture];

                            uvs.Add(new float4(0, 0, topTexture.x, topTexture.y));
                            uvs.Add(new float4(1, 0, topTexture.x, topTexture.y));
                            uvs.Add(new float4(0, 1, topTexture.x, topTexture.y));
                            uvs.Add(new float4(1, 1, topTexture.x, topTexture.y));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            colors.Add(block.topColor);
                            colors.Add(block.topColor);
                            colors.Add(block.topColor);
                            colors.Add(block.topColor);

                            float3 normal = new float3(0, 1, 0);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);
                            normals.Add(normal);

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        // Down
                        if ((y > 0 && IsTransparent(blockMap[blocks[index - chunkSize]], block)) || (y == 0 && IsTransparent(blockMap[downBlocks[GetIndex1DFrom3D(x, chunkSize - 1, z, chunkSize)]], block)))
                        {
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x, y + position.y, z + position.z + 1));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z));
                            vertices.Add(new float3(x + position.x + 1, y + position.y, z + position.z + 1));

                            int2 bottomTexture = textures[block.bottomTexture];

                            uvs.Add(new float4(0, 0, bottomTexture.x, bottomTexture.y));
                            uvs.Add(new float4(1, 0, bottomTexture.x, bottomTexture.y));
                            uvs.Add(new float4(0, 1, bottomTexture.x, bottomTexture.y));
                            uvs.Add(new float4(1, 1, bottomTexture.x, bottomTexture.y));

                            indicies.Add(vertexIndex);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 1);
                            indicies.Add(vertexIndex + 2);
                            indicies.Add(vertexIndex + 3);
                            indicies.Add(vertexIndex + 1);

                            colors.Add(block.bottomColor);
                            colors.Add(block.bottomColor);
                            colors.Add(block.bottomColor);
                            colors.Add(block.bottomColor);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                return true;
            }
            else if (block.transparent && block.connectToSame)
            {
                if (block.id == currentBlock.id)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }
    }
}
