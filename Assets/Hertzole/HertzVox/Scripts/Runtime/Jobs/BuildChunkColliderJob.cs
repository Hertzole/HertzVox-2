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
        public NativeArray<ushort> blocks;
        [ReadOnly]
        public int chunkSize;

        //[WriteOnly]
        public NativeList<float3> vertices;
        [WriteOnly]
        public NativeList<int> indicies;

        public NativeArray<bool> mask;

        public void Execute()
        {
            // Sweep over each axis (X, Y and Z)
            for (int d = 0; d < 3; ++d)
            {
                int i, j, k, l, w, h;
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;
                NativeArray<int> x = new NativeArray<int>(3, Allocator.Temp);
                NativeArray<int> q = new NativeArray<int>(3, Allocator.Temp);

                q[d] = 1;

                // Check each slice of the chunk one at a time
                for (x[d] = -1; x[d] < chunkSize;)
                {
                    // Compute the mask
                    int n = 0;
                    for (x[v] = 0; x[v] < chunkSize; ++x[v])
                    {
                        for (x[u] = 0; x[u] < chunkSize; ++x[u])
                        {
                            // q determines the direction (X, Y or Z) that we are searching
                            // m.IsBlockAt(x,y,z) takes global map positions and returns true if a block exists there

                            bool blockCurrent = 0 <= x[d] ? IsBlockAt(x[0], x[1], x[2]) : true;
                            bool blockCompare = x[d] < chunkSize - 1 ? IsBlockAt(x[0] + q[0], x[1] + q[1], x[2] + q[2]) : true;

                            // The mask is set to true if there is a visible face between two blocks,
                            //   i.e. both aren't empty and both aren't blocks
                            mask[n++] = blockCurrent != blockCompare;
                        }
                    }

                    ++x[d];

                    n = 0;

                    // Generate a mesh from the mask using lexicographic ordering,      
                    //   by looping over each block in this slice of the chunk
                    for (j = 0; j < chunkSize; ++j)
                    {
                        for (i = 0; i < chunkSize;)
                        {
                            if (mask[n])
                            {
                                // Compute the width of this quad and store it in w                        
                                //   This is done by searching along the current axis until mask[n + w] is false
                                for (w = 1; i + w < chunkSize && mask[n + w]; w++)
                                { }

                                // Compute the height of this quad and store it in h                        
                                //   This is done by checking if every block next to this row (range 0 to w) is also part of the mask.
                                //   For example, if w is 5 we currently have a quad of dimensions 1 x 5. To reduce triangle count,
                                //   greedy meshing will attempt to expand this quad out to CHUNK_SIZE x 5, but will stop if it reaches a hole in the mask

                                bool done = false;
                                for (h = 1; j + h < chunkSize; h++)
                                {
                                    // Check each block next to this quad
                                    for (k = 0; k < w; ++k)
                                    {
                                        // If there's a hole in the mask, exit
                                        if (!mask[n + k + h * chunkSize])
                                        {
                                            done = true;
                                            break;
                                        }
                                    }

                                    if (done)
                                    {
                                        break;
                                    }
                                }

                                x[u] = i;
                                x[v] = j;

                                // du and dv determine the size and orientation of this face
                                NativeArray<int> du = new NativeArray<int>(3, Allocator.Temp);
                                du[u] = w;

                                NativeArray<int> dv = new NativeArray<int>(3, Allocator.Temp);
                                dv[v] = h;

                                // Create a quad for this face. Colour, normal or textures are not stored in this block vertex format.
                                AppendQuad(new int3(x[0], x[1], x[2]),                 // Top-left vertice position
                                                       new int3(x[0] + du[0], x[1] + du[1], x[2] + du[2]),         // Top right vertice position
                                                       new int3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]),         // Bottom left vertice position
                                                       new int3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2])  // Bottom right vertice position
                                                       );

                                // Clear this part of the mask, so we don't add duplicate faces
                                for (l = 0; l < h; ++l)
                                {
                                    for (k = 0; k < w; ++k)
                                    {
                                        mask[n + k + l * chunkSize] = false;
                                    }
                                }

                                // Increment counters and continue
                                i += w;
                                n += w;

                                du.Dispose();
                                dv.Dispose();
                            }
                            else
                            {
                                i++;
                                n++;
                            }
                        }
                    }
                }

                x.Dispose();
                q.Dispose();
            }
        }

        private void AppendQuad(int3 tl, int3 tr, int3 bl, int3 br)
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

        private int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }

        private bool IsBlockAt(int x, int y, int z)
        {
            return blocks[GetIndex1DFrom3D(x, y, z, chunkSize)] == 0;
        }
    }
}