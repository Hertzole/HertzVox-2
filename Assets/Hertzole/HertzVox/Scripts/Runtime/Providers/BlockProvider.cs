using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Hertzole.HertzVox
{
    public static class BlockProvider
    {
        private static Dictionary<string, Block> blockIds;
        private static Dictionary<string, string> blockNames;
        private static Dictionary<int, string> blockIdentifiers;

        private static bool isInitialized;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            blockIds = new Dictionary<string, Block>();
            blockNames = new Dictionary<string, string>();
            blockIdentifiers = new Dictionary<int, string>();
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

            blockIds = new Dictionary<string, Block>();
            blockNames = new Dictionary<string, string>();
            blockIdentifiers = new Dictionary<int, string>();
            int index = 1;

            blockIds.Add("air", new Block(0));
            blockNames.Add("air", "Air");
            blockIdentifiers.Add(0, "air");

            for (int i = 0; i < blockCollection.Blocks.Length; i++)
            {
                Assert.IsFalse(blockIds.ContainsKey(blockCollection.Blocks[i].BlockID), "Found multiple blocks with the ID " + blockCollection.Blocks[i].BlockID);

                blockNames.Add(blockCollection.Blocks[i].BlockID, blockCollection.Blocks[i].BlockName);
                blockIdentifiers.Add(index, blockCollection.Blocks[i].BlockID);

                if (blockCollection.Blocks[i] is CubeConfig cube)
                {
                    blockIds.Add(blockCollection.Blocks[i].BlockID, new Block(index, cube));
                }
                else
                {
                    blockIds.Add(blockCollection.Blocks[i].BlockID, new Block(index));
                }

                index++;
            }

            isInitialized = true;
        }

        public static bool TryGetBlock(string id, out Block block)
        {
            if (!isInitialized)
            {
                Debug.LogError("You must Initialize block provider first!");
                block = new Block(-1);
                return false;
            }

            Assert.IsNotNull(blockIds);

            return blockIds.TryGetValue(id, out block);
        }

        public static Block GetBlock(string id)
        {
            if (!isInitialized)
            {
                Debug.LogError("You must Initialize block provider first!");
                return new Block(-1);
            }

            return blockIds[id];
        }

        public static string GetBlockName(string id)
        {
            return blockNames[id];
        }

        public static string GetBlockName(int id)
        {
            return GetBlockName(blockIdentifiers[id]);
        }
    }
}
