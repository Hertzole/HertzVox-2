using System.Collections.Generic;
using UnityEngine;

namespace Hertzole.HertzVox
{
    [CreateAssetMenu(fileName = "New Block Collection", menuName = "HertzVox/Block Collection")]
    public class BlockCollection : ScriptableObject
    {
        [SerializeField]
        private BaseConfig[] blocks = null;
        [Space]
        [SerializeField]
        private int textureSize = 16;
        [SerializeField]
        private Texture2D blankTexture = null;
        [SerializeField]
        [HideInInspector]
        private List<Texture2D> uniqueTextures = new List<Texture2D>();
        [SerializeField]
        [HideInInspector]
        private List<int> uniqueTextureIds = new List<int>();

        public BaseConfig[] Blocks { get { return blocks; } }
        public int TextureSize { get { return textureSize; } }

        public List<Texture2D> UniqueTextures { get { return uniqueTextures; } }
        public List<int> UniqueTextureIds { get { return uniqueTextureIds; } }

#if UNITY_EDITOR
        public void ValidateTextures()
        {
            Debug.Log("BlockCollection :: Validate Textures");

            Dictionary<string, int> textures = new Dictionary<string, int>();

            uniqueTextures.Clear();
            uniqueTextureIds.Clear();

            for (int i = 0; i < blocks.Length; i++)
            {
                if (blocks[i] is CubeConfig cube)
                {
                    cube.TopTextureId = AddTexture(cube.TopTexture, textures);
                    cube.BottomTextureId = AddTexture(cube.BottomTexture, textures);
                    cube.NorthTextureId = AddTexture(cube.NorthTexture, textures);
                    cube.SouthTextureId = AddTexture(cube.SouthTexture, textures);
                    cube.WestTextureId = AddTexture(cube.WestTexture, textures);
                    cube.EastTextureId = AddTexture(cube.EastTexture, textures);
                }
            }
        }

        private int AddTexture(Texture2D texture, Dictionary<string, int> textures)
        {
            if (texture == null)
            {
                texture = blankTexture;

                if (texture == null)
                {
                    return -1;
                }
            }

            int index = textures.Count;

            string path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);

            if (!textures.ContainsKey(guid))
            {
                textures.Add(guid, index);
                uniqueTextures.Add(texture);
                uniqueTextureIds.Add(index);
            }
            else
            {
                index = textures[guid];
            }

            return index;
        }

        private void OnValidate()
        {
            ValidateTextures();
        }
#endif
    }
}