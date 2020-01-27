using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hertzole.HertzVox
{
    public static class BlockProvider
    {
        private static Dictionary<ushort, Block> blockIds;
        private static Dictionary<ushort, string> blockNames;
        private static Dictionary<string, ushort> blockIdentifiers;

        private static bool isInitialized;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            blockIds = new Dictionary<ushort, Block>();
            blockNames = new Dictionary<ushort, string>();
            blockIdentifiers = new Dictionary<string, ushort>();
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

            blockIds = new Dictionary<ushort, Block>();
            blockNames = new Dictionary<ushort, string>();
            ushort index = 1;

            blockIds.Add(0, new Block(0));
            blockNames.Add(0, "Air");
            blockIdentifiers.Add("air", 0);

            for (int i = 0; i < blockCollection.Blocks.Length; i++)
            {
                Assert.IsFalse(blockIds.ContainsKey(blockCollection.Blocks[i].BlockID), "Found multiple blocks with the ID " + blockCollection.Blocks[i].BlockID);

                blockNames.Add(blockCollection.Blocks[i].BlockID, blockCollection.Blocks[i].BlockName);
                blockIdentifiers.Add(blockCollection.Blocks[i].BlockIdentifier, blockCollection.Blocks[i].BlockID);

                if (blockCollection.Blocks[i] is CubeConfig cube)
                {
                    Block block = new Block(index, cube);
                    blockIds.Add(blockCollection.Blocks[i].BlockID, block);
                }
                else
                {
                    Block block = new Block(index);
                    blockIds.Add(blockCollection.Blocks[i].BlockID, block);
                }

                index++;
            }

            isInitialized = true;
        }

        public static bool TryGetBlock(ushort id, out Block block)
        {
            if (!isInitialized)
            {
                Debug.LogError("You must Initialize block provider first!");
                block = new Block(0);
                return false;
            }

            Assert.IsNotNull(blockIds);

            return blockIds.TryGetValue(id, out block);
        }

        public static bool TryGetBlock(string identifier, out Block block)
        {
            return TryGetBlock(blockIdentifiers[identifier], out block);
        }

        public static Block GetBlock(ushort id)
        {
            if (!isInitialized)
            {
                Debug.LogError("You must Initialize block provider first!");
                return new Block(0);
            }

            return blockIds[id];
        }

        public static Block GetBlock(string identifier)
        {
            return GetBlock(blockIdentifiers[identifier]);
        }

        public static string GetBlockName(ushort id)
        {
            return blockNames[id];
        }

        public static string GetBlockName(string identifier)
        {
            return GetBlockName(blockIdentifiers[identifier]);
        }
    }
}
