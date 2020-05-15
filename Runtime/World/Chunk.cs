using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public class Chunk : IEquatable<Chunk>
    {
        private ChunkBlocks blocks;
        public int3 position;

        public bool dirty = false;
        public bool urgentUpdate;
        public bool changed;
        public bool render;
        private bool onlyThis;
        private bool updateNeighbors;

        private bool disposed = false;

        public bool NeedsTerrain { get; set; }
        public bool GeneratingTerrain { get; private set; }
        public bool UpdatingRenderer { get; private set; }
        public bool UpdatingCollider { get; private set; }
        public bool HasTerrain { get; private set; }
        public bool HasRender { get; private set; }

        public bool RequestedRemoval { get; private set; }
        public bool CanRemove { get { return !GeneratingTerrain && !UpdatingRenderer && !UpdatingCollider; } }

        public NativeArray<int> temporaryBlocks;

        private NativeList<float3> vertices;
        private NativeList<int> indicies;
        private NativeList<float4> uvs;
        private NativeList<float4> colors;
        private NativeList<float3> normals;

        private NativeList<float3> colliderVertices;
        private NativeList<int> colliderIndicies;
        private NativeList<float3> colliderNormals;

        public Mesh mesh;

        private VoxelWorld world;

        public const int CHUNK_SIZE = 16;

        public Chunk(VoxelWorld world, int3 position)
        {
            this.world = world;
            this.position = position;
        }

        public Chunk(VoxelWorld world, int3 position, ChunkBlocks blocks) : this(world, position)
        {
            this.blocks = blocks;
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
            updateNeighbors = false;
        }

        public void UpdateChunkAndNeighbors(bool urgent = false)
        {
            UpdateChunk(urgent);
            updateNeighbors = true;
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

        public void StartGenerating(NativeArray<int> blocks)
        {
            temporaryBlocks = blocks;
            GeneratingTerrain = true;
        }

        public JobHandle ScheduleRenderJob()
        {
            UpdatingRenderer = true;
            HasRender = true;

            vertices = new NativeList<float3>(Allocator.TempJob);
            indicies = new NativeList<int>(Allocator.TempJob);
            uvs = new NativeList<float4>(Allocator.TempJob);
            colors = new NativeList<float4>(Allocator.TempJob);
            normals = new NativeList<float3>(Allocator.TempJob);

            NativeArray<int> northBlocks = world.TryGetChunk(new int3(position.x, position.y, position.z + CHUNK_SIZE), out Chunk northChunk) ?
                northChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> southBlocks = world.TryGetChunk(new int3(position.x, position.y, position.z - CHUNK_SIZE), out Chunk southChunk) ?
                southChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> eastBlocks = world.TryGetChunk(new int3(position.x + CHUNK_SIZE, position.y, position.z), out Chunk eastChunk) ?
                eastChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> westBlocks = world.TryGetChunk(new int3(position.x - CHUNK_SIZE, position.y, position.z), out Chunk westChunk) ?
                westChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> upBlocks = world.TryGetChunk(new int3(position.x, position.y + CHUNK_SIZE, position.z), out Chunk topChunk) ?
                topChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> downBlocks = world.TryGetChunk(new int3(position.x, position.y - CHUNK_SIZE, position.z), out Chunk downChunk) ?
                downChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            if (!onlyThis && updateNeighbors)
            {
                northChunk?.OnlyUpdateThis(true);
                southChunk?.OnlyUpdateThis(true);
                eastChunk?.OnlyUpdateThis(true);
                westChunk?.OnlyUpdateThis(true);
                topChunk?.OnlyUpdateThis(true);
                downChunk?.OnlyUpdateThis(true);
            }

            return new BuildChunkJob()
            {
                chunkSize = CHUNK_SIZE,
                position = position,
                blocks = blocks.GetBlocks(Allocator.TempJob),
                blockMap = BlockProvider.GetBlockMap(),
                textures = TextureProvider.GetTextureMap(),
                vertices = vertices,
                indicies = indicies,
                uvs = uvs,
                colors = colors,
                normals = normals,
                northBlocks = northBlocks,
                southBlocks = southBlocks,
                eastBlocks = eastBlocks,
                westBlocks = westBlocks,
                upBlocks = upBlocks,
                downBlocks = downBlocks
            }.Schedule();
        }

        public JobHandle ScheduleColliderJob()
        {
            UpdatingCollider = true;

            colliderVertices = new NativeList<float3>(Allocator.TempJob);
            colliderIndicies = new NativeList<int>(Allocator.TempJob);
            colliderNormals = new NativeList<float3>(Allocator.TempJob);

            NativeArray<int> northBlocks = world.TryGetChunk(new int3(position.x, position.y, position.z + CHUNK_SIZE), out Chunk northChunk) ?
                northChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> southBlocks = world.TryGetChunk(new int3(position.x, position.y, position.z - CHUNK_SIZE), out Chunk southChunk) ?
                southChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> eastBlocks = world.TryGetChunk(new int3(position.x + CHUNK_SIZE, position.y, position.z), out Chunk eastChunk) ?
                eastChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> westBlocks = world.TryGetChunk(new int3(position.x - CHUNK_SIZE, position.y, position.z), out Chunk westChunk) ?
                westChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> upBlocks = world.TryGetChunk(new int3(position.x, position.y + CHUNK_SIZE, position.z), out Chunk topChunk) ?
                topChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            NativeArray<int> downBlocks = world.TryGetChunk(new int3(position.x, position.y - CHUNK_SIZE, position.z), out Chunk downChunk) ?
                downChunk.blocks.GetBlocks(Allocator.TempJob) : BlockProvider.GetEmptyBlocks(Allocator.TempJob);

            return new BuildChunkColliderJob()
            {
                size = CHUNK_SIZE,
                position = position,
                blocks = blocks.GetBlocks(Allocator.TempJob),
                blockMap = BlockProvider.GetBlockMap(),
                vertices = colliderVertices,
                indicies = colliderIndicies,
                normals = colliderNormals,
                northBlocks = northBlocks,
                southBlocks = southBlocks,
                eastBlocks = eastBlocks,
                westBlocks = westBlocks,
                upBlocks = upBlocks,
                downBlocks = downBlocks
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
            HasTerrain = true;
        }

        public Mesh CompleteMeshUpdate(Mesh mesh)
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
            //mesh.SetNormals<float3>(normals);

            mesh.RecalculateNormals();

            vertices.Dispose();
            indicies.Dispose();
            uvs.Dispose();
            colors.Dispose();
            normals.Dispose();

            UpdatingRenderer = false;
            HasRender = true;

            this.mesh = mesh;

            return mesh;
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

            mesh.SetVertices<float3>(colliderVertices);
            mesh.SetIndices<int>(colliderIndicies, MeshTopology.Triangles, 0);
            mesh.SetNormals<float3>(colliderNormals);

            colliderVertices.Dispose();
            colliderIndicies.Dispose();
            colliderNormals.Dispose();

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
            if (temporaryBlocks.IsCreated)
            {
                temporaryBlocks.Dispose();
            }

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

            if (colliderNormals.IsCreated)
            {
                colliderNormals.Dispose();
            }
        }

        public void SetBlock(int x, int y, int z, Block block, bool urgent = true)
        {
            SetBlockRaw(x, y, z, block);
            UpdateChunkAndNeighbors(urgent);
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
            UpdateChunkAndNeighbors();
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

        public Block GetBlock(int x, int y, int z)
        {
            return blocks.Get(x, y, z);
        }

        public NativeArray<int> GetAllBlocks(Allocator allocator)
        {
            return blocks.GetBlocks(allocator);
        }

        public void RequestRemoval()
        {
            RequestedRemoval = true;
        }

        public void DecompressAndApply(NativeList<int2> list, Dictionary<int, string> palette)
        {
            blocks.DecompressAndApply(list, palette);
            HasTerrain = true;
        }

        public NativeList<int2> CompressBlocks()
        {
            return blocks.Compress();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Chunk chunk))
            {
                return false;
            }

            return Equals(chunk);
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
