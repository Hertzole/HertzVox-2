using System.Collections.Generic;
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

        private float nextChunkGenerate;

        private Block stone;
        private Block dirt;
        private Block grass;

        private Vector3Int lastLoaderPosition;

        private Material mat;

        private VoxelLoader loader;

        //private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();
        private List<Chunk> renderChunks = new List<Chunk>();
        private List<Chunk> chunks = new List<Chunk>();
        private Dictionary<int3, Chunk> chunkPositions = new Dictionary<int3, Chunk>();

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

            stone = BlockProvider.GetBlock("stone");
            dirt = BlockProvider.GetBlock("dirt");
            grass = BlockProvider.GetBlock("grass");
        }

        private void OnDestroy()
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Dispose();
            }

            TextureProvider.Dispose();

            Destroy(mat);
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

            Chunk chunk = GetChunk(chunkPos);
            if (chunk == null)
            {
                return BlockProvider.GetBlock("air");
            }

            int xx = Helpers.Mod(position.x, Chunk.CHUNK_SIZE);
            int yy = Helpers.Mod(position.y, Chunk.CHUNK_SIZE);
            int zz = Helpers.Mod(position.z, Chunk.CHUNK_SIZE);

            return chunk.blocks.Get(xx, yy, zz);
        }

        public void SetBlock(int3 position, Block block)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            Chunk chunk = GetChunk(chunkPos);
            if (chunk != null)
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

            chunkPositions.TryGetValue(position, out Chunk chunk);
            return chunk;
        }

        private void GenerateChunksAroundTargets()
        {
            if (loader == null)
            {
                return;
            }

            Vector3Int targetPosition = Helpers.WorldToChunk(loader.transform.position, Chunk.CHUNK_SIZE);
            if (lastLoaderPosition == targetPosition)
            {
                return;
            }

            lastLoaderPosition = targetPosition;

            renderChunks.Clear();

            Chunk chunk = null;

            for (int x = -5 + targetPosition.x; x < 5 + targetPosition.x; x++)
            {
                for (int z = -5 + targetPosition.z; z < 5 + targetPosition.z; z++)
                {
                    int3 chunkPosition = new int3(x * Chunk.CHUNK_SIZE, 0, z * Chunk.CHUNK_SIZE);
                    if (chunkPositions.TryGetValue(chunkPosition, out chunk))
                    {
                        renderChunks.Add(chunk);
                    }
                    else
                    {
                        chunk = CreateChunk(chunkPosition);
                        chunks.Add(chunk);
                        renderChunks.Add(chunk);
                        chunkPositions.Add(chunkPosition, chunk);
                    }
                }
            }
        }

        private Chunk CreateChunk(int3 position)
        {
            Chunk chunk = new Chunk(this, position);
            ChunkBlocks blocks = new ChunkBlocks
            {
                blocks = new Unity.Collections.NativeArray<Block>(16 * 16 * 16, Unity.Collections.Allocator.Persistent)
            };

            int index = 0;

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        if (y < 4)
                        {
                            blocks.blocks[index] = stone;
                        }
                        else if (y >= 4 && y < 6)
                        {
                            blocks.blocks[index] = dirt;
                        }
                        else if (y == 6)
                        {
                            blocks.blocks[index] = grass;
                        }

                        index++;
                    }
                }
            }

            chunk.blocks = blocks;

            chunk.UpdateChunk();
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
    }
}