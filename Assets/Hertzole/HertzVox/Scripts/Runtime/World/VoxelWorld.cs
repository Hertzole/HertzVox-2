using System.Collections.Generic;
using System.Text;
using Unity.Mathematics;
using UnityEngine;

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
        private float chunkGenerateDelay = 0.25f;
        [SerializeField]
        private bool clearTempOnDestroy = true;

        private float nextChunkGenerate;

        private Block stone;
        private Block dirt;
        private Block grass;
        private Block air;
        private Block logs;
        private Block planks;
        private Block leaves;

        private int3 lastLoaderPosition;

        private Material mat;

        private VoxelLoader loader;

        private List<Chunk> renderChunks = new List<Chunk>();
        private List<int3> chunksToRemove = new List<int3>();

        public int2 AtlasSize { get; private set; }

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
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
            AtlasSize = new int2(xSize, ySize);

            stone = BlockProvider.GetBlock("stone");
            dirt = BlockProvider.GetBlock("dirt");
            grass = BlockProvider.GetBlock("grass");
            air = BlockProvider.GetBlock("air");
            logs = BlockProvider.GetBlock("log");
            planks = BlockProvider.GetBlock("planks");
            leaves = BlockProvider.GetBlock("leaves");

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

            if (clearTempOnDestroy)
            {
                Serialization.ClearTemp();
            }
        }

        private void Update()
        {
            for (int i = 0; i < renderChunks.Count; i++)
            {
                renderChunks[i].Draw(mat);
                renderChunks[i].Update();
            }

            if (Time.unscaledTime >= nextChunkGenerate)
            {
                nextChunkGenerate = Time.unscaledTime + chunkGenerateDelay;
                GenerateChunksAroundTargets();
            }

            if (Input.GetKeyDown(KeyCode.F1) && chunks.TryGetValue(int3.zero, out Chunk chunk))
            {
                Unity.Collections.NativeList<int2> compressedBlocks = chunk.blocks.Compress();

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < compressedBlocks.Length; i++)
                {
                    sb.Append($"[{compressedBlocks[i].x},{compressedBlocks[i].y}],");
                }

                compressedBlocks.Dispose();

                Debug.Log(sb.ToString());
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < renderChunks.Count; i++)
            {
                renderChunks[i].LateUpdate();
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

        public void SetBlock(int3 position, Block block)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            if (chunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
                int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
                int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

                chunk.SetBlock(xx, yy, zz, block);
                chunk.UpdateChunk();
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

            for (int x = xMin; x < xMax; x++)
            {
                for (int z = zMin; z < zMax; z++)
                {
                    int3 chunkPosition = new int3(x * Chunk.CHUNK_SIZE, 0, z * Chunk.CHUNK_SIZE);

                    if (!chunks.TryGetValue(chunkPosition, out Chunk chunk))
                    {
                        chunk = CreateChunk(chunkPosition);
                        chunks.Add(chunkPosition, chunk);
                        Serialization.LoadChunk(chunk, true);
                    }

                    renderChunks.Add(chunk);
                    chunk.render = false;

                    if (x != xMin && z != zMin && x != xMax - 1 && z != zMax - 1)
                    {
                        chunk.render = true;
                        chunk.UpdateChunkIfNeeded();
                    }
                }
            }

            chunksToRemove.Clear();
            foreach (KeyValuePair<int3, Chunk> chunk in chunks)
            {
                if (!renderChunks.Contains(chunk.Value))
                {
                    chunksToRemove.Add(chunk.Key);
                }
            }

            //TODO: Pool chunks in memory pool.
            for (int i = 0; i < chunksToRemove.Count; i++)
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
            }
        }

        private Chunk CreateChunk(int3 position)
        {
            Chunk chunk = new Chunk(position);
            Debug.Log("Create chunk: " + position + " | " + position / Chunk.CHUNK_SIZE);
            ChunkBlocks blocks = new ChunkBlocks(Chunk.CHUNK_SIZE);

            int index = 0;

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        if (y < 4)
                        {
                            blocks.Set(index, stone);
                        }
                        else if (y >= 4 && y < 6)
                        {
                            blocks.Set(index, dirt);
                        }
                        else if (y == 6)
                        {
                            blocks.Set(index, grass);
                        }
                        else
                        {
                            blocks.Set(index, air);
                        }

                        index++;
                    }
                }
            }

            chunk.blocks = blocks;

            return chunk;
        }

        public void RegisterLoader(VoxelLoader loader)
        {
            this.loader = loader;
        }

        public void UnregisterLoader(VoxelLoader loader)
        {
            this.loader = null;
        }

        private void OnGUI()
        {
            GUILayout.Box("Chunks: " + chunks.Count);
            GUILayout.Box("Render Chunks: " + renderChunks.Count);
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