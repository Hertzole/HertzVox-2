using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Hertzole.HertzVox
{
    [DefaultExecutionOrder(-50)]
    public class VoxelWorld : MonoBehaviour
    {
        [SerializeField]
        private BlockCollection blockCollection = null;
        [SerializeField]
        private Material chunkMaterial = null;
        [SerializeField]
        private MeshCollider chunkColliderPrefab = null;
        [SerializeField]
        private float chunkGenerateDelay = 0.25f;
        [SerializeField]
        private bool clearTempOnDestroy = true;

        [Header("World Size")]
        [SerializeField]
        private int minX = -10;
        [SerializeField]
        private int maxX = 10;
        [SerializeField]
        private int minZ = -10;
        [SerializeField]
        private int maxZ = 10;
        [SerializeField]
        private int maxY = 8;

        private float nextChunkGenerate;

        private int3 lastLoaderPosition;

        private Material mat;

        private VoxelLoader loader;

        private IVoxGeneration generator;

        private NativeList<int3> renderChunks;
        private NativeList<int3> chunksToRemove;
        private List<MeshCollider> activeColliders = new List<MeshCollider>();

        private Stack<MeshCollider> pooledColliders = new Stack<MeshCollider>();

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
        private Dictionary<int3, MeshCollider> chunkColliders = new Dictionary<int3, MeshCollider>();
        //private NativeHashMap<int3, Chunk> chunks;

        public static VoxelWorld Main { get; private set; }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Main = null;
        }
#endif

        private void OnEnable()
        {
            if (Main != null && Main != this)
            {
                Debug.LogError("Multiple Voxel Worlds! There can only be one.", gameObject);
                return;
            }

            if (Main == null)
            {
                Main = this;
            }

            Chunk.OnMeshCompleted += OnChunkMeshUpdated;
        }

        private void OnDisable()
        {
            Chunk.OnMeshCompleted -= OnChunkMeshUpdated;
        }

        private void Awake()
        {
            BlockProvider.Initialize(blockCollection);
            TextureProvider.Initialize(blockCollection);
            Serialization.Initialize(Application.persistentDataPath + "/HertzVox/");

            Texture2D atlas = TextureProvider.GetAtlas();

            mat = new Material(chunkMaterial)
            {
                mainTexture = atlas
            };

            int xSize = atlas.width / blockCollection.TextureSize;
            int ySize = atlas.height / blockCollection.TextureSize;

            mat.SetInt("_AtlasX", xSize);
            mat.SetInt("_AtlasY", ySize);
            mat.SetVector("_AtlasRec", new Vector4(1.0f / xSize, 1.0f / ySize));

            renderChunks = new NativeList<int3>(Allocator.Persistent);
            chunksToRemove = new NativeList<int3>(Allocator.Persistent);

            generator = GetComponent<IVoxGeneration>();

            if (generator == null)
            {
                Debug.LogWarning("There's no voxel generator (IVoxGenerator) attached to Voxel World " + gameObject.name + ". No chunks will show up.");
            }

            //chunks = new NativeHashMap<int3, Chunk>(0, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            foreach (Chunk chunk in chunks.Values)
            {
                chunk.Dispose(true);
            }

            TextureProvider.Dispose();
            BlockProvider.Dispose();

            Destroy(mat);

            renderChunks.Dispose();
            chunksToRemove.Dispose();

            if (clearTempOnDestroy)
            {
                Serialization.ClearTemp();
            }
        }

        private void Update()
        {
            for (int i = 0; i < renderChunks.Length; i++)
            {
                chunks[renderChunks[i]].Draw(mat);
                chunks[renderChunks[i]].Update();
            }

            if (Time.unscaledTime >= nextChunkGenerate)
            {
                nextChunkGenerate = Time.unscaledTime + chunkGenerateDelay;
                GenerateChunksAroundTargets();
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < renderChunks.Length; i++)
            {
                chunks[renderChunks[i]].LateUpdate();
            }
        }

        public Block GetBlock(int3 position)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
                int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
                int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

                return chunk.blocks.Get(xx, yy, zz);
            }

            return BlockProvider.GetBlock("air");
        }

        public void SetBlock(int3 position, Block block, bool urgent = true)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
                int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
                int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

                chunk.SetBlock(xx, yy, zz, block);
                chunk.UpdateChunk(urgent);
            }
        }

        public void SetBlocks(int3 start, int3 end, Block block)
        {
            SetBlocks(start.x, start.y, start.z, end.x, end.y, end.z, block);
        }

        public void SetBlocks(int fromX, int fromY, int fromZ, int toX, int toY, int toZ, Block block)
        {
            FixValues(ref fromX, ref toX);
            FixValues(ref fromY, ref toY);
            FixValues(ref fromZ, ref toZ);

            int3 chunkFrom = Helpers.ContainingChunkPosition(fromX, fromY, fromZ);
            int3 chunkTo = Helpers.ContainingChunkPosition(toX, toY, toZ);

            int minY = Helpers.Mod(fromY, Chunk.CHUNK_SIZE);

            for (int cy = chunkFrom.y; cy <= chunkTo.y; cy += Chunk.CHUNK_SIZE, minY = 0)
            {
                int maxY = math.min(toY - cy, Chunk.CHUNK_SIZE - 1);
                int minZ = Helpers.Mod(fromZ, Chunk.CHUNK_SIZE);

                for (int cz = chunkFrom.z; cz <= chunkTo.z; cz += Chunk.CHUNK_SIZE, minZ = 0)
                {
                    int maxZ = math.min(toZ - cz, Chunk.CHUNK_SIZE - 1);
                    int minX = Helpers.Mod(fromX, Chunk.CHUNK_SIZE);

                    for (int cx = chunkFrom.x; cx <= chunkTo.x; cx += Chunk.CHUNK_SIZE, minX = 0)
                    {
                        bool ghostChunk = false;
                        int3 chunkPosition = new int3(cx, cy, cz);
                        Chunk chunk = GetChunk(chunkPosition);
                        if (chunk == null)
                        {
                            ghostChunk = true;
                            chunk = CreateChunk(chunkPosition);
                        }

                        int maxX = math.min(toX - cx, Chunk.CHUNK_SIZE - 1);

                        int3 from = new int3(minX, minY, minZ);
                        int3 to = new int3(maxX, maxY, maxZ);

                        chunk.SetRangeRaw(from, to, block);
                        if (!ghostChunk)
                        {
                            chunk.UpdateChunk();
                        }
                        else
                        {
                            Serialization.SaveChunk(chunk, true);
                            chunk.Dispose();
                        }
                    }
                }
            }
        }

        private void FixValues(ref int start, ref int end)
        {
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }
        }

        public Chunk GetChunk(int3 position)
        {
            //Assert.IsTrue(Helpers.ContainingChunkPosition(ref position).Equals(position));

            chunks.TryGetValue(position, out Chunk chunk);
            return chunk;
        }

        private void GenerateChunksAroundTargets()
        {
            if (loader == null)
            {
                return;
            }

            int3 targetPosition = Helpers.WorldToChunk(loader.transform.position, Chunk.CHUNK_SIZE);
            if (lastLoaderPosition.Equals(targetPosition))
            {
                return;
            }

            lastLoaderPosition = targetPosition;

            renderChunks.Clear();

            int xMin = -loader.ChunkDistanceX + targetPosition.x - 1;
            int zMin = -loader.ChunkDistanceZ + targetPosition.z - 1;
            int xMax = loader.ChunkDistanceX + targetPosition.x + 2;
            int zMax = loader.ChunkDistanceZ + targetPosition.z + 2;

            Profiler.BeginSample("Create chunk region");
            for (int x = xMin; x < xMax; x++)
            {
                for (int z = zMin; z < zMax; z++)
                {
                    for (int y = 0; y < maxY; y++)
                    {
                        if (x < minX || x > maxX || z < minZ || z > maxZ)
                        {
                            continue;
                        }

                        int3 chunkPosition = new int3(x * Chunk.CHUNK_SIZE, y * Chunk.CHUNK_SIZE, z * Chunk.CHUNK_SIZE);

                        if (!chunks.TryGetValue(chunkPosition, out Chunk chunk))
                        {
                            chunk = CreateChunk(chunkPosition);
                            chunks.Add(chunkPosition, chunk);
                            Profiler.BeginSample("Load chunk");
                            Serialization.LoadChunk(chunk, true);
                            Profiler.EndSample();
                        }

                        renderChunks.Add(chunkPosition);
                        chunk.render = false;

                        Profiler.BeginSample("Update chunks");
                        if (x != xMin && z != zMin && x != xMax - 1 && z != zMax - 1)
                        {
                            chunk.render = true;
                            chunk.UpdateChunkIfNeeded();
                        }
                        Profiler.EndSample();
                    }
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("Remove chunks stage 1");
            chunksToRemove.Clear();
            foreach (int3 chunk in chunks.Keys)
            {
                if (!renderChunks.Contains(chunk))
                {
                    chunksToRemove.Add(chunk);
                }
            }
            Profiler.EndSample();

            Profiler.BeginSample("Remove chunks stage 2");
            //TODO: Pool chunks in memory pool.
            for (int i = 0; i < chunksToRemove.Length; i++)
            {
                if (chunks.TryGetValue(chunksToRemove[i], out Chunk chunk))
                {
                    if (chunk.changed)
                    {
                        Serialization.SaveChunk(chunk, true);
                    }
                    chunk.Dispose();
                    chunks.Remove(chunksToRemove[i]);

                }

                if (chunkColliders.TryGetValue(chunksToRemove[i], out MeshCollider collider))
                {
                    PoolCollider(collider);
                    chunkColliders.Remove(chunksToRemove[i]);
                }
            }
            Profiler.EndSample();
        }

        private void OnChunkMeshUpdated(int3 position, Mesh mesh)
        {
            if (chunkColliders.TryGetValue(position, out MeshCollider collider))
            {
                collider.sharedMesh = mesh;
            }
            else
            {
                collider = GetCollider();
                collider.sharedMesh = mesh;
                chunkColliders.Add(position, collider);
            }
        }

        private Chunk CreateChunk(int3 position)
        {
            Chunk chunk = new Chunk(position);
            ChunkBlocks chunkBlocks = new ChunkBlocks(Chunk.CHUNK_SIZE);
            chunk.blocks = chunkBlocks;

            if (generator != null)
            {
                generator.GenerateChunk(chunk, position);
            }

            return chunk;
        }

        private MeshCollider GetCollider()
        {
            MeshCollider collider = pooledColliders.Count > 0 ? pooledColliders.Pop() : Instantiate(chunkColliderPrefab, transform);
            collider.gameObject.SetActive(true);
            return collider;
        }

        private void PoolCollider(MeshCollider collider)
        {
            collider.gameObject.SetActive(false);
            activeColliders.Remove(collider);
            pooledColliders.Push(collider);
        }

        public void RegisterLoader(VoxelLoader loader)
        {
            this.loader = loader;
        }

        public void UnregisterLoader(VoxelLoader loader)
        {
            this.loader = null;
        }

        private void OnDrawGizmos()
        {
            foreach (KeyValuePair<int3, Chunk> chunk in chunks)
            {
                Gizmos.DrawWireCube(new Vector3(chunk.Key.x + (Chunk.CHUNK_SIZE / 2), chunk.Key.y + (Chunk.CHUNK_SIZE / 2), chunk.Key.z + (Chunk.CHUNK_SIZE / 2)), Vector3.one * Chunk.CHUNK_SIZE);
            }
        }
    }
}