using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public class Chunk
    {
        public ChunkBlocks blocks;
        public int3 position;

        private bool dirty;
        private bool updatingMesh;
        private bool urgentUpdate;
        private bool onlyThis;
        private bool isEmpty;
        public bool changed;
        public bool render;

        private int frameCount;

        private JobHandle job;

        private NativeList<float3> vertices;
        private NativeList<int> indicies;
        private NativeList<float4> uvs;
        private NativeList<float4> colors;
        private NativeList<float3> normals;

        private Mesh mesh;

        public event System.Action<int3, Mesh> OnMeshCompleted;

        public const int CHUNK_SIZE = 16;

        public Chunk(int3 position)
        {
            this.position = position;
        }

        public void Draw(Material chunkMaterial)
        {
            if (mesh != null && render)
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
            onlyThis = false;
        }

        internal void OnlyUpdateThis(bool urgent = false)
        {
            UpdateChunk(urgent);
            onlyThis = true;
        }

        public void UpdateChunkIfNeeded()
        {
            if (mesh == null)
            {
                OnlyUpdateThis(false);
            }
        }

        private void UpdateMesh()
        {
            if (!render)
            {
                return;
            }

            if (updatingMesh)
            {
                //TODO: Handle updating mesh while already updating.
                Debug.LogWarning("Already updating chunk at " + position + ". Handle this!");
                return;
            }

            bool blocksEmpty = blocks.IsEmpty();

            if (blocksEmpty && blocksEmpty == isEmpty)
            {
                return;
            }

            isEmpty = blocksEmpty;
            frameCount = 0;
            updatingMesh = true;
            dirty = false;

            vertices = new NativeList<float3>(Allocator.TempJob);
            indicies = new NativeList<int>(Allocator.TempJob);
            uvs = new NativeList<float4>(Allocator.TempJob);
            colors = new NativeList<float4>(Allocator.TempJob);
            normals = new NativeList<float3>(Allocator.TempJob);

            Chunk northChunk = VoxelWorld.Main.GetChunk(position + new int3(0, 0, CHUNK_SIZE));
            Chunk southChunk = VoxelWorld.Main.GetChunk(position - new int3(0, 0, CHUNK_SIZE));
            Chunk eastChunk = VoxelWorld.Main.GetChunk(position + new int3(CHUNK_SIZE, 0, 0));
            Chunk westChunk = VoxelWorld.Main.GetChunk(position - new int3(CHUNK_SIZE, 0, 0));

            BuildChunkJob job = new BuildChunkJob()
            {
                size = CHUNK_SIZE,
                position = position,
                blocks = blocks.GetBlocks(),
                blockMap = BlockProvider.GetBlockMap(),
                textures = TextureProvider.GetTextureMap(),
                vertices = vertices,
                indicies = indicies,
                uvs = uvs,
                colors = colors,
                normals = normals,
                northBlocks = northChunk == null ? BlockProvider.GetEmptyBlocks() : northChunk.blocks.GetBlocks(),
                southBlocks = southChunk == null ? BlockProvider.GetEmptyBlocks() : southChunk.blocks.GetBlocks(),
                eastBlocks = eastChunk == null ? BlockProvider.GetEmptyBlocks() : eastChunk.blocks.GetBlocks(),
                westBlocks = westChunk == null ? BlockProvider.GetEmptyBlocks() : westChunk.blocks.GetBlocks(),
            };

            if (!onlyThis)
            {
                northChunk?.OnlyUpdateThis(true);
                southChunk?.OnlyUpdateThis(true);
                eastChunk?.OnlyUpdateThis(true);
                westChunk?.OnlyUpdateThis(true);
            }

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

            if (vertices.Length >= 65535 && mesh.indexFormat != UnityEngine.Rendering.IndexFormat.UInt32)
            {
                Debug.LogWarning(this + " had too many vertices and the mesh has been converted to 32-bit format.");
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            //if (position.Equals(int3.zero))
            //{


            //    Debug.Log(vertices.Length);
            //}

            mesh.Clear();

            mesh.SetVertices<float3>(vertices);
            mesh.SetIndices<int>(indicies, MeshTopology.Triangles, 0);
            mesh.SetUVs<float4>(0, uvs);
            mesh.SetColors<float4>(colors);
            mesh.SetNormals<float3>(normals);

            vertices.Dispose();
            indicies.Dispose();
            uvs.Dispose();
            colors.Dispose();
            normals.Dispose();

            //TODO: Handle colliders much better.

            NativeArray<bool> mask = new NativeArray<bool>(CHUNK_SIZE * CHUNK_SIZE, Allocator.TempJob);
            NativeList<float3> colliderVertices = new NativeList<float3>(Allocator.TempJob);
            NativeList<int> colliderIndicies = new NativeList<int>(Allocator.TempJob);

            new BuildChunkColliderJob()
            {
                mask = mask,
                blocks = blocks.GetBlocks(),
                chunkSize = CHUNK_SIZE,
                position = position,
                vertices = colliderVertices,
                indicies = colliderIndicies
            }.Run();

            Mesh colliderMesh = new Mesh();
            colliderMesh.SetVertices<float3>(colliderVertices);
            colliderMesh.SetIndices<int>(colliderIndicies, MeshTopology.Triangles, 0);
            colliderMesh.RecalculateNormals();

            mask.Dispose();
            colliderVertices.Dispose();
            colliderIndicies.Dispose();

            OnMeshCompleted?.Invoke(position, colliderMesh);
        }

        public void Dispose(bool force = false)
        {
            //TODO: Make sure chunk can be disposed first.

            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }

            mesh = null;

            if (!job.IsCompleted)
            {
                job.Complete();
            }

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

            if (normals.IsCreated)
            {
                normals.Dispose();
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

        public void SetRange(int3 from, int3 to, Block block)
        {
            SetRangeRaw(from, to, block);
            UpdateChunk();
        }

        public void SetRangeRaw(int3 from, int3 to, Block block)
        {
            for (int x = from.x; x <= to.x; x++)
            {
                for (int y = from.y; y <= to.y; y++)
                {
                    for (int z = from.z; z <= to.z; z++)
                    {
                        SetBlockRaw(x, y, z, block);
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            Chunk chunk = obj as Chunk;
            if (chunk == null)
            {
                return false;
            }

            if (position.x != chunk.position.x || position.y != chunk.position.y)
            {
                return false;
            }

            return chunk.position.z == position.z;
        }

        public override int GetHashCode()
        {
            return position.GetHashCode() * 17;
        }

        public override string ToString()
        {
            return $"Chunk ({position.x},{position.y},{position.z})";
        }
    }
}