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
        public ChunkBlocks blocks;
        [ReadOnly]
        public NativeHashMap<int, int2> textures;

        [WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> indicies;
        [WriteOnly]
        public NativeList<float4> uvs;

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
                        if (blocks.blocks[index].id == 0) // Is air
                        {
                            faces[index] = 0;
                            index++;
                            continue;
                        }

                        Block currentBlock = blocks.blocks[index];

                        // South
                        if (z == 0)
                        {
                            faces[index] |= (byte)Direction.South;
                            sizeEstimate += 4;
                        }
                        else if (z > 0 && IsTransparent(blocks.blocks[index - 1], currentBlock))
                        {
                            faces[index] |= (byte)Direction.South;
                            sizeEstimate += 4;
                        }

                        // North
                        if (z == size - 1)
                        {
                            faces[index] |= (byte)Direction.North;
                            sizeEstimate += 4;
                        }
                        else if (z < size - 1 && IsTransparent(blocks.blocks[index + 1], currentBlock))
                        {
                            faces[index] |= (byte)Direction.North;
                            sizeEstimate += 4;
                        }

                        // West
                        if (x == 0)
                        {
                            faces[index] |= (byte)Direction.West;
                            sizeEstimate += 4;
                        }
                        else if (x > 0 && IsTransparent(blocks.blocks[index - size * size], currentBlock))
                        {
                            faces[index] |= (byte)Direction.West;
                            sizeEstimate += 4;
                        }

                        // East
                        if (x == size - 1)
                        {
                            faces[index] |= (byte)Direction.East;
                            sizeEstimate += 4;
                        }
                        else if (x < size - 1 && IsTransparent(blocks.blocks[index + size * size], currentBlock))
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
                        else if (y > 0 && IsTransparent(blocks.blocks[index - size], currentBlock))
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
                        else if (y < size - 1 && IsTransparent(blocks.blocks[index + size], currentBlock))
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

                        if ((faces[index] & (byte)Direction.North) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y + 1, z + position.z + 1);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1);

                            int2 northTexture = textures[blocks.blocks[index].northTexture];

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

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        if ((faces[index] & (byte)Direction.East) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x + 1, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 2] = new float3(x + position.x + 1, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1);

                            int2 eastTexture = textures[blocks.blocks[index].eastTexture];

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

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        if ((faces[index] & (byte)Direction.South) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y, z + position.z);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z);

                            int2 southTexture = textures[blocks.blocks[index].southTexture];

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

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        if ((faces[index] & (byte)Direction.West) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 3] = new float3(x + position.x, y + position.y + 1, z + position.z + 1);

                            int2 westTexture = textures[blocks.blocks[index].westTexture];

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

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        if ((faces[index] & (byte)Direction.Up) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x + 1, y + position.y + 1, z + position.z);
                            vertices[vertexIndex + 2] = new float3(x + position.x, y + position.y + 1, z + position.z + 1);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y + 1, z + position.z + 1);

                            int2 topTexture = textures[blocks.blocks[index].topTexture];

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

                            vertexIndex += 4;
                            trianglesIndex += 6;
                        }

                        if ((faces[index] & (byte)Direction.Down) != 0)
                        {
                            vertices[vertexIndex] = new float3(x + position.x, y + position.y, z + position.z);
                            vertices[vertexIndex + 1] = new float3(x + position.x, y + position.y, z + position.z + 1);
                            vertices[vertexIndex + 2] = new float3(x + position.x + 1, y + position.y, z + position.z);
                            vertices[vertexIndex + 3] = new float3(x + position.x + 1, y + position.y, z + position.z + 1);

                            int2 bottomTexture = textures[blocks.blocks[index].bottomTexture];

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
            else if (block.transparent)
            {
                return true;
            }

            return false;
        }

        private int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }

        private bool IsWithinBounds(int3 position, int size)
        {
            return size > position.x && size > position.y && size > position.z && position.x >= 0 && position.y >= 0 && position.z >= 0;
        }
    }
}
