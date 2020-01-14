using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public class VoxelWorld : MonoBehaviour
    {
        [SerializeField]
        private BlockCollection blockCollection = null;
        [SerializeField]
        private Material chunkMaterial = null;

        private Material mat;

        private Dictionary<int3, Chunk> chunks = new Dictionary<int3, Chunk>();

        public static VoxelWorld Main { get; private set; }

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Main = null;
        }
#endif

        private void Awake()
        {
            if (Main != null)
            {
                Debug.LogError("Multiple Voxel Worlds! There can only be one.", gameObject);
                return;
            }

            Main = this;

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
        }

        private void Start()
        {
            Chunk chunk = new Chunk
            {
                position = int3.zero
            };

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
                            if (BlockProvider.TryGetBlock("stone", out Block stone))
                            {
                                blocks.blocks[index] = stone;
                            }
                        }
                        else if (y >= 4 && y < 6)
                        {
                            if (BlockProvider.TryGetBlock("dirt", out Block dirt))
                            {
                                blocks.blocks[index] = dirt;
                            }
                        }
                        else if (y == 6)
                        {
                            if (BlockProvider.TryGetBlock("grass", out Block grass))
                            {
                                blocks.blocks[index] = grass;
                            }
                        }

                        index++;
                    }
                }
            }

            chunk.blocks = blocks;

            chunk.UpdateChunk();

            chunks.Add(int3.zero, chunk);
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
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Draw(mat);
                chunks[i].Update();
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log(GetBlock(new int3(0, 6, 0)).id);
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].LateUpdate();
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

            return chunk.blocks.Get(position);
        }

        public void SetBlock(int3 position, Block block)
        {
            int3 chunkPos = Helpers.ContainingChunkPosition(position);

            Chunk chunk = GetChunk(chunkPos);
            if (chunk != null)
            {
                chunk.SetBlock(position, block);
                chunk.UpdateChunk();
            }
        }

        public Chunk GetChunk(int3 position)
        {
            //Assert.IsTrue(Helpers.ContainingChunkPosition(ref position).Equals(position));

            chunks.TryGetValue(position, out Chunk chunk);
            return chunk;
        }
    }
}