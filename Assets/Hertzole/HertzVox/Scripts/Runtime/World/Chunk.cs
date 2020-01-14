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

        private int frameCount;

        private JobHandle job;

        private NativeList<float3> vertices;
        private NativeList<int> indicies;
        private NativeList<float4> uvs;

        private Mesh mesh;

        private VoxelWorld world;

        public const int CHUNK_SIZE = 16;

        public Chunk(VoxelWorld world, int3 position)
        {
            this.world = world;
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

            if (job.IsCompleted || urgentUpdate || frameCount >= 4)
            {
                job.Complete();
                OnMeshUpdated();
            }
        }

        public void UpdateChunk(bool urgent = false)
        {
            urgentUpdate = urgent;
            dirty = true;
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

            job = new BuildChunkJob()
            {
                size = CHUNK_SIZE,
                position = position,
                blocks = blocks,
                textures = TextureProvider.GetTextureMap(),
                vertices = vertices,
                indicies = indicies,
                uvs = uvs
            }.Schedule();
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
            mesh.RecalculateNormals();

            vertices.Dispose();
            indicies.Dispose();
            uvs.Dispose();
        }

        public void Dispose()
        {
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
        }

        public void SetBlock(int x, int y, int z, Block block, bool urgent = true)
        {
            blocks.Set(x, y, z, block);
            UpdateChunk(urgent);
        }

        public void SetBlock(int3 position, Block block, bool urgent = true)
        {
            SetBlock(position.x, position.y, position.z, block, urgent);
        }

        public void SetBlockRaw(int x, int y, int z, Block block)
        {
            blocks.Set(x, y, z, block);
        }

        public void SetBlockRaw(int3 position, Block block)
        {
            SetBlockRaw(position.x, position.y, position.z, block);
        }
    }
}