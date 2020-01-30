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
        public int size;
        [ReadOnly]
        public NativeArray<ushort> blocks;
        [ReadOnly]
        public NativeHashMap<ushort, Block> blockMap;
        [ReadOnly]
        public NativeHashMap<int, int2> textures;
        [ReadOnly]
        public NativeArray<ushort> northBlocks;
        [ReadOnly]
        public NativeArray<ushort> southBlocks;
        [ReadOnly]
        public NativeArray<ushort> eastBlocks;
        [ReadOnly]
        public NativeArray<ushort> westBlocks;

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

        public void Execute()
        {
            NativeArray<byte> faces = new NativeArray<byte>(size * size * size, Allocator.Temp);

            int sizeEstimate = 0;
            int index = 0;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        if (blocks[index] == 0) // Is air
                        {
                            faces[index] = 0;
                            index++;
                            continue;
                        }

                        Block currentBlock = blockMap[blocks[index]];

                        // South
                        if (z == 0 && IsTransparent(blockMap[southBlocks[GetIndex1DFrom3D(x, y, size - 1, size)]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.South;
                            sizeEstimate += 4;
                        }
                        else if (z > 0 && IsTransparent(blockMap[blocks[index - 1]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.South;
                            sizeEstimate += 4;
                        }

                        // North
                        if (z == size - 1 && IsTransparent(blockMap[northBlocks[GetIndex1DFrom3D(x, y, 0, size)]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.North;
                            sizeEstimate += 4;
                        }
                        else if (z < size - 1 && IsTransparent(blockMap[blocks[index + 1]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.North;
                            sizeEstimate += 4;
                        }

                        // West
                        if (x == 0 && IsTransparent(blockMap[westBlocks[GetIndex1DFrom3D(size - 1, y, z, size)]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.West;
                            sizeEstimate += 4;
                        }
                        else if (x > 0 && IsTransparent(blockMap[blocks[index - size * size]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.West;
                            sizeEstimate += 4;
                        }

                        // East
                        if (x == size - 1 && IsTransparent(blockMap[eastBlocks[GetIndex1DFrom3D(0, y, z, size)]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.East;
                            sizeEstimate += 4;
                        }
                        else if (x < size - 1 && IsTransparent(blockMap[blocks[index + size * size]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.East;
                            sizeEstimate += 4;
                        }

                        // Down
                        if (y == 0)
                        {
                            faces[index] |= (byte)Direction.Down;
                            sizeEstimate += 4;
                        }
                        else if (y > 0 && IsTransparent(blockMap[blocks[index - size]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.Down;
                            sizeEstimate += 4;
                        }

                        // Up
                        if (y == size - 1)
                        {
                            faces[index] |= (byte)Direction.Up;
                            sizeEstimate += 4;
                        }
                        else if (y < size - 1 && IsTransparent(blockMap[blocks[index + size]], currentBlock))
                        {
                            faces[index] |= (byte)Direction.Up;
                            sizeEstimate += 4;
                        }

                        index++;
                    }
                }
            }

            // Generate mesh.
            index = 0;

            int vertexIndex = 0;
            int trianglesIndex = 0;

            for (int i = 0; i < sizeEstimate; i++)
            {
                vertices.Add(new float3());
                uvs.Add(new float4());
            }

            int triangleSize = (int)(sizeEstimate * 1.5f);
            for (int i = 0; i < triangleSize; i++)
            {
                indicies.Add(0);
            }

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        if (faces[index] == 0) // No face.
                        {
                            index++;
                            continue;
                        }

                        Block block = blockMap[blocks[index]];

                        if ((faces[index] & (byte)Direction.North) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y + 1, z + position.z + 1);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1);

                            int2 northTexture = textures[block.northTexture];

                            uvs[vertexIndex] = new float4(0, 0, northTexture.x, northTexture.y);
                            uvs[vertexIndex + 1] = new float4(1, 0, northTexture.x, northTexture.y);
                            uvs[vertexIndex + 2] = new float4(0, 1, northTexture.x, northTexture.y);
                            uvs[vertexIndex + 3] = new float4(1, 1, northTexture.x, northTexture.y);

                            indicies[trianglesIndex] = vertexIndex + 1;
                            indicies[trianglesIndex + 1] = vertexIndex + 2;
                            indicies[trianglesIndex + 2] = vertexIndex;

                            indicies[trianglesIndex + 3] = vertexIndex + 1;
                            indicies[trianglesIndex + 4] = vertexIndex + 3;
                            indicies[trianglesIndex + 5] = vertexIndex + 2;

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

                        if ((faces[index] & (byte)Direction.East) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x + 1, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 2] = new float3(x + position.x + 1, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1);

                            int2 eastTexture = textures[block.eastTexture];

                            uvs[vertexIndex] = new float4(0, 0, eastTexture.x, eastTexture.y);
                            uvs[vertexIndex + 1] = new float4(1, 0, eastTexture.x, eastTexture.y);
                            uvs[vertexIndex + 2] = new float4(0, 1, eastTexture.x, eastTexture.y);
                            uvs[vertexIndex + 3] = new float4(1, 1, eastTexture.x, eastTexture.y);

                            indicies[trianglesIndex] = vertexIndex;
                            indicies[trianglesIndex + 1] = vertexIndex + 2;
                            indicies[trianglesIndex + 2] = vertexIndex + 1;

                            indicies[trianglesIndex + 3] = vertexIndex + 2;
                            indicies[trianglesIndex + 4] = vertexIndex + 3;
                            indicies[trianglesIndex + 5] = vertexIndex + 1;

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

                        if ((faces[index] & (byte)Direction.South) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y, z + position.z);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z);

                            int2 southTexture = textures[block.southTexture];

                            uvs[vertexIndex] = new float4(0, 0, southTexture.x, southTexture.y);
                            uvs[vertexIndex + 1] = new float4(1, 0, southTexture.x, southTexture.y);
                            uvs[vertexIndex + 2] = new float4(0, 1, southTexture.x, southTexture.y);
                            uvs[vertexIndex + 3] = new float4(1, 1, southTexture.x, southTexture.y);

                            indicies[trianglesIndex] = vertexIndex;
                            indicies[trianglesIndex + 1] = vertexIndex + 2;
                            indicies[trianglesIndex + 2] = vertexIndex + 1;

                            indicies[trianglesIndex + 3] = vertexIndex + 2;
                            indicies[trianglesIndex + 4] = vertexIndex + 3;
                            indicies[trianglesIndex + 5] = vertexIndex + 1;

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

                        if ((faces[index] & (byte)Direction.West) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 3] = new float3(x + position.x, y + position.y + 1, z + position.z + 1);

                            int2 westTexture = textures[block.westTexture];

                            uvs[vertexIndex] = new float4(0, 0, westTexture.x, westTexture.y);
                            uvs[vertexIndex + 1] = new float4(0, 1, westTexture.x, westTexture.y);
                            uvs[vertexIndex + 2] = new float4(1, 0, westTexture.x, westTexture.y);
                            uvs[vertexIndex + 3] = new float4(1, 1, westTexture.x, westTexture.y);

                            indicies[trianglesIndex] = vertexIndex;
                            indicies[trianglesIndex + 1] = vertexIndex + 2;
                            indicies[trianglesIndex + 2] = vertexIndex + 1;

                            indicies[trianglesIndex + 3] = vertexIndex + 2;
                            indicies[trianglesIndex + 4] = vertexIndex + 3;
                            indicies[trianglesIndex + 5] = vertexIndex + 1;

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

                        if ((faces[index] & (byte)Direction.Up) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y + 1, z + position.z + 1);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1);

                            int2 topTexture = textures[block.topTexture];

                            uvs[vertexIndex] = new float4(0, 0, topTexture.x, topTexture.y);
                            uvs[vertexIndex + 1] = new float4(1, 0, topTexture.x, topTexture.y);
                            uvs[vertexIndex + 2] = new float4(0, 1, topTexture.x, topTexture.y);
                            uvs[vertexIndex + 3] = new float4(1, 1, topTexture.x, topTexture.y);

                            indicies[trianglesIndex] = vertexIndex;
                            indicies[trianglesIndex + 1] = vertexIndex + 2;
                            indicies[trianglesIndex + 2] = vertexIndex + 1;

                            indicies[trianglesIndex + 3] = vertexIndex + 2;
                            indicies[trianglesIndex + 4] = vertexIndex + 3;
                            indicies[trianglesIndex + 5] = vertexIndex + 1;

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

                        if ((faces[index] & (byte)Direction.Down) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 2] = new float3(x + position.x + 1, y + position.y, z + position.z);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y, z + position.z + 1);

                            int2 bottomTexture = textures[block.bottomTexture];

                            uvs[vertexIndex] = new float4(0, 0, bottomTexture.x, bottomTexture.y);
                            uvs[vertexIndex + 1] = new float4(1, 0, bottomTexture.x, bottomTexture.y);
                            uvs[vertexIndex + 2] = new float4(0, 1, bottomTexture.x, bottomTexture.y);
                            uvs[vertexIndex + 3] = new float4(1, 1, bottomTexture.x, bottomTexture.y);

                            indicies[trianglesIndex] = vertexIndex;
                            indicies[trianglesIndex + 1] = vertexIndex + 2;
                            indicies[trianglesIndex + 2] = vertexIndex + 1;

                            indicies[trianglesIndex + 3] = vertexIndex + 2;
                            indicies[trianglesIndex + 4] = vertexIndex + 3;
                            indicies[trianglesIndex + 5] = vertexIndex + 1;

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

            faces.Dispose();
        }

        //private bool IsTransparent(int3 position)
        //{
        //    return !IsWithinBounds(position, size) ? false : blocks.blocks[GetIndex1DFrom3D(position.x, position.y, position.z, size)].id == 0;
        //}

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

        private int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }
    }
}
