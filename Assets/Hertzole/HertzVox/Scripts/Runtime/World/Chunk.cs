using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public class Chunk : IDisposable
    {
        public ChunkBlocks blocks;
        public int3 position;

        private bool dirty;
        private bool updatingMesh;
        private bool urgentUpdate;
        public bool changed;

        private int frameCount;

        private JobHandle job;

        private NativeList<float3> vertices;
        private NativeList<int> indicies;
        private NativeList<float4> uvs;
        private NativeList<float4> colors;

        private Mesh mesh;

        public const int CHUNK_SIZE = 16;

        public Chunk(int3 position)
        {
            this.position = position;
        }

        public void Draw(Material chunkMaterial)
        {
            if (mesh != null)
            {
                Graphics.DrawMesh(mesh, Matrix4x4.identity, chunkMaterial, 0);
            }
        }

        public void Update()
        {
            if (dirty)
            {
                UpdateMesh();
            }

            if (updatingMesh)
            {
                frameCount++;
            }
        }

        public void LateUpdate()
        {
            if (!updatingMesh)
            {
                return;
            }

            if (job.IsCompleted || frameCount >= 4)
            {
                if (!urgentUpdate)
                {
                    job.Complete();
                    OnMeshUpdated();
                }
            }
        }

        public void UpdateChunk(bool urgent = false)
        {
            urgentUpdate = urgent;
            dirty = true;
        }

        public void UpdateChunkIfNeeded()
        {
            if (mesh == null)
            {
                UpdateChunk(false);
            }
        }

        private void UpdateMesh()
        {
            if (updatingMesh)
            {
                //TODO: Handle updating mesh while already updating.
                Debug.LogWarning("Already updating chunk at " + position + ". Handle this!");
                return;
            }

            frameCount = 0;
            updatingMesh = true;
            dirty = false;

            vertices = new NativeList<float3>(Allocator.TempJob);
            indicies = new NativeList<int>(Allocator.TempJob);
            uvs = new NativeList<float4>(Allocator.TempJob);
            colors = new NativeList<float4>(Allocator.TempJob);

            BuildChunkJob job = new BuildChunkJob()
            {
                size = CHUNK_SIZE,
                position = position,
                blocks = blocks.blocks,
                textures = TextureProvider.GetTextureMap(),
                vertices = vertices,
                indicies = indicies,
                uvs = uvs,
                colors = colors
            };

            if (urgentUpdate)
            {
                job.Run();
                OnMeshUpdated();
            }
            else
            {
                this.job = job.Schedule();
            }
        }

        private void OnMeshUpdated()
        {
            updatingMesh = false;

            if (mesh == null)
            {
                mesh = new Mesh() { name = "Chunk" };
            }

            mesh.Clear();

            mesh.SetVertices<float3>(vertices);
            mesh.SetIndices<int>(indicies, MeshTopology.Triangles, 0);
            mesh.SetUVs<float4>(0, uvs);
            mesh.SetColors<float4>(colors);
            mesh.RecalculateNormals();

            vertices.Dispose();
            indicies.Dispose();
            uvs.Dispose();
            colors.Dispose();
        }

        public void Dispose()
        {
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }

            mesh = null;

            blocks.Dispose();
            if (vertices.IsCreated)
            {
                vertices.Dispose();
            }

            if (indicies.IsCreated)
            {
                indicies.Dispose();
            }

            if (uvs.IsCreated)
            {
                uvs.Dispose();
            }

            if (colors.IsCreated)
            {
                colors.Dispose();
            }
        }

        public void SetBlock(int x, int y, int z, Block block, bool urgent = true)
        {
            SetBlockRaw(x, y, z, block);
            UpdateChunk(urgent);
        }

        public void SetBlock(int3 position, Block block, bool urgent = true)
        {
            SetBlock(position.x, position.y, position.z, block, urgent);
        }

        public void SetBlockRaw(int x, int y, int z, Block block)
        {
            changed = true;
            blocks.Set(x, y, z, block);
        }

        public void SetBlockRaw(int3 position, Block block)
        {
            SetBlockRaw(position.x, position.y, position.z, block);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is Chunk chunk && position.x == chunk.position.x && position.y == chunk.position.y)
            {
                return chunk.position.z == position.z;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return position.GetHashCode() * 17;
        }
    }
}