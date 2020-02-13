using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public static class BlockProvider
    {
        private static NativeHashMap<ushort, Block> blocks;
        private static Dictionary<string, ushort> blockIds;
        private static Dictionary<string, string> blockNames;
        private static Dictionary<ushort, string> palette;

        private static NativeArray<ushort> emptyBlocks;

        private static bool isInitialized;

        public const int AIR_TYPE_ID = 0;
        public const string AIR_TYPE = "air";

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Dispose();

            blockNames = null;
            palette = null;
            isInitialized = false;
        }
#endif

        public static void Initialize(BlockCollection blockCollection)
        {
            if (isInitialized)
            {
                Debug.LogWarning("Block provider is already initialized.");
                return;
            }

            blocks = new NativeHashMap<ushort, Block>(0, Allocator.Persistent);
            blockIds = new Dictionary<string, ushort>();
            blockNames = new Dictionary<string, string>();
            palette = new Dictionary<ushort, string>();

            emptyBlocks = new NativeArray<ushort>(Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE, Allocator.Persistent);

            blocks.Add(AIR_TYPE_ID, new Block(AIR_TYPE_ID) { canCollide = false });
            blockNames.Add(AIR_TYPE, "Air");
            blockIds.Add(AIR_TYPE, AIR_TYPE_ID);
            palette.Add(AIR_TYPE_ID, AIR_TYPE);

            for (ushort i = 0; i < blockCollection.Blocks.Length; i++)
            {
                ushort id = (ushort)(i + 1);
#if DEBUG
                if (blockNames.ContainsKey(blockCollection.Blocks[i].BlockID))
                {
                    Debug.LogError("Found multiple blocks with the ID '" + blockCollection.Blocks[i].BlockID + "'.");
                }
#endif

                blockIds.Add(blockCollection.Blocks[i].BlockID, id);
                blockNames.Add(blockCollection.Blocks[i].BlockID, blockCollection.Blocks[i].BlockName);
                palette.Add(id, blockCollection.Blocks[i].BlockID);

                if (blockCollection.Blocks[i] is CubeConfig cube)
                {
                    Block block = new Block(id, cube);
                    blocks.Add(id, block);
                }
                else
                {
                    Block block = new Block(id);
                    blocks.Add(id, block);
                }
            }

            isInitialized = true;
        }

        public static void Dispose()
        {
            if (blocks.IsCreated)
            {
                blocks.Dispose();
            }

            if (emptyBlocks.IsCreated)
            {
                emptyBlocks.Dispose();
            }
        }

        public static bool TryGetBlock(ushort id, out Block block)
        {
            if (!isInitialized)
            {
                Debug.LogError("You must Initialize block provider first!");
                block = blocks[AIR_TYPE_ID];
                return false;
            }

            return blocks.TryGetValue(id, out block);
        }

        public static bool TryGetBlock(string identifier, out Block block)
        {
            if (!blockIds.ContainsKey(identifier))
            {
                block = blocks[AIR_TYPE_ID];
                return false;
            }

            return TryGetBlock(blockIds[identifier], out block);
        }

        public static Block GetBlock(ushort id)
        {
            if (!isInitialized)
            {
                Debug.LogError("You must Initialize block provider first!");
                return blocks[AIR_TYPE_ID];
            }

            return blocks[id];
        }

        public static Block GetBlock(string identifier)
        {
#if DEBUG
            if (!blockIds.ContainsKey(identifier))
            {
                Debug.LogError("No block with the ID '" + identifier + "'.");
                return blocks[AIR_TYPE_ID];
            }
#endif

            return GetBlock(blockIds[identifier]);
        }

        public static string GetBlockName(ushort id)
        {
            return GetBlockName(palette[id]);
        }

        public static string GetBlockName(string identifier)
        {
            return blockNames[identifier];
        }

        public static NativeHashMap<ushort, Block> GetBlockMap()
        {
            return blocks;
        }

        public static NativeArray<ushort> GetEmptyBlocks()
        {
            return emptyBlocks;
        }

        public static Dictionary<ushort, string> GetBlockPalette()
        {
            return palette;
        }
    }
}
