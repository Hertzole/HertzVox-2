using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public static class TextureProvider
    {
        private static Texture2D atlasTexture;
        private static Rect[] rects;

        private static NativeHashMap<int, int2> textures = new NativeHashMap<int, int2>();

        private static bool isInitialized;

#if UNITY_2019_3_OR_NEWER
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Dispose();
        }
#endif

        public static void Initialize(BlockCollection blocks)
        {
            if (isInitialized)
            {
                Debug.LogWarning("Texture provider is already initialized.");
                return;
            }

            GenerateAtlas(blocks);

            isInitialized = true;
        }

        public static void Dispose()
        {
            if (atlasTexture != null)
            {
                Object.Destroy(atlasTexture);
            }

            atlasTexture = null;
            isInitialized = false;

            if (textures.IsCreated)
            {
                textures.Dispose();
            }
        }

        private static void GenerateAtlas(BlockCollection blocks)
        {
            Texture2D[] uniqueTextures = blocks.UniqueTextures.ToArray();

            textures = new NativeHashMap<int, int2>(0, Allocator.Persistent);

            atlasTexture = new Texture2D(8192, 8192);
            rects = atlasTexture.PackTextures(uniqueTextures, 0, 8192, false);
            atlasTexture.filterMode = FilterMode.Point;

            for (int i = 0; i < uniqueTextures.Length; i++)
            {
                Rect uvs = rects[i];

                if (!textures.TryGetValue(blocks.UniqueTextureIds[i], out _))
                {
                    int2 coords = new int2((int)(atlasTexture.width * uvs.x / blocks.TextureSize), (int)(atlasTexture.height * uvs.y / blocks.TextureSize));
                    textures.Add(blocks.UniqueTextureIds[i], coords);
                }
            }
        }

        public static Texture2D GetAtlas()
        {
            if (!isInitialized)
            {
                Debug.LogError("You need to Initialize texture proivder before getting the atlas.");
                return null;
            }

            return atlasTexture;
        }

        //public static int2 GetTexture(int id)
        //{
        //    if (!isInitialized)
        //    {
        //        Debug.LogError("You must Initialize texture provider first!");
        //        return int2.zero;
        //    }

        //    return textures[id];
        //}

        public static NativeHashMap<int, int2> GetTextureMap()
        {
            return textures;
        }
    }
}
