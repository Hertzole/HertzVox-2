using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public class Chunk : IEquatable<Chunk>
    {
        public ChunkBlocks blocks;
        public int3 position;

        public bool dirty = false;
        private bool updatingMesh;
        public bool urgentUpdate;
        private bool onlyThis;
        public bool changed;
        public bool render;

        private bool disposed = false;

        public bool NeedsTerrain { get; set; }
        public bool GeneratingTerrain { get; private set; }
        public bool UpdatingRender { get; private set; }
        public bool UpdatingCollider { get; private set; }

        public bool RequestedRemoval { get; private set; }
        public bool CanRemove { get { return !GeneratingTerrain && !UpdatingRender && !UpdatingCollider; } }

        public NativeArray<ushort> temporaryBlocks;

        private NativeList<float3> vertices;
        private NativeList<int> indicies;
        private NativeList<float4> uvs;
        private NativeList<float4> colors;
        private NativeList<float3> normals;

        private NativeList<float3> colliderVertices;
        private NativeList<int> colliderIndicies;

        private Mesh mesh;

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

        public void StartGenerating(NativeArray<ushort> blocks)
        {
            temporaryBlocks = blocks;
            GeneratingTerrain = true;
        }

        public JobHandle ScheduleRenderJob()
        {
            UpdatingRender = true;

            vertices = new NativeList<float3>(Allocator.TempJob);
            indicies = new NativeList<int>(Allocator.TempJob);
            uvs = new NativeList<float4>(Allocator.TempJob);
            colors = new NativeList<float4>(Allocator.TempJob);
            normals = new NativeList<float3>(Allocator.TempJob);

            return new BuildChunkJob()
            {
                size = CHUNK_SIZE,
                position = position,
                blocks = blocks.GetBlocks(Allocator.TempJob),
                blockMap = BlockProvider.GetBlockMap(),
                textures = TextureProvider.GetTextureMap(),
                vertices = vertices,
                indicies = indicies,
                uvs = uvs,
                colors = colors,
                normals = normals,
                northBlocks = BlockProvider.GetEmptyBlocks(),
                southBlocks = BlockProvider.GetEmptyBlocks(),
                eastBlocks = BlockProvider.GetEmptyBlocks(),
                westBlocks = BlockProvider.GetEmptyBlocks()
            }.Schedule();
        }

        public JobHandle ScheduleColliderJob()
        {
            UpdatingCollider = true;

            colliderVertices = new NativeList<float3>(Allocator.TempJob);
            colliderIndicies = new NativeList<int>(Allocator.TempJob);

            return new BuildChunkColliderJob()
            {
                blocks = blocks.GetBlocks(Allocator.TempJob),
                chunkSize = CHUNK_SIZE,
                position = position,
                vertices = colliderVertices,
                indicies = colliderIndicies,
                blockMap = BlockProvider.GetBlockMap()
            }.Schedule();
        }

        public void CompleteGenerating()
        {
            if (disposed)
            {
                Debug.LogWarning("Chunk has already been disposed.");
            }

            blocks.CopyFrom(temporaryBlocks);
            temporaryBlocks.Dispose();

            GeneratingTerrain = false;
        }

        public void CompleteMeshUpdate()
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                mesh.Clear();
            }

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

            UpdatingRender = false;
        }

        public Mesh CompleteColliderMeshUpdate(Mesh mesh)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                mesh.Clear();
            }

            if (disposed)
            {
                Debug.LogWarning("THIS CHUNK HAS BEEN DISPOSED FIRST???");
            }

            mesh.SetVertices<float3>(colliderVertices);
            mesh.SetIndices<int>(colliderIndicies, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            colliderVertices.Dispose();
            colliderIndicies.Dispose();

            UpdatingCollider = false;

            return mesh;
        }

        public void Dispose(bool force = false)
        {
            //TODO: Make sure chunk can be disposed first.

            if (!CanRemove && !force)
            {
                return;
            }

            disposed = true;

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

            if (normals.IsCreated)
            {
                normals.Dispose();
            }

            if (colliderVertices.IsCreated)
            {
                colliderVertices.Dispose();
            }

            if (colliderIndicies.IsCreated)
            {
                colliderIndicies.Dispose();
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

        public void RequestRemoval()
        {
            RequestedRemoval = true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Chunk chunk))
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

        public bool Equals(Chunk other)
        {
            return other.position.Equals(position);
        }
    }
}