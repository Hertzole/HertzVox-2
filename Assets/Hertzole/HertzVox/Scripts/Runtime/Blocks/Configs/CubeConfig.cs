using UnityEngine;

namespace Hertzole.HertzVox
{
    [CreateAssetMenu(fileName = "New Cube Config", menuName = "HertzVox/Blocks/Cube")]
    public class CubeConfig : BaseConfig
    {
        [Header("Textures")]
        [SerializeField]
        private Texture2D topTexture = null;
        [SerializeField]
        private Texture2D bottomTexture = null;
        [SerializeField]
        private Texture2D northTexture = null;
        [SerializeField]
        private Texture2D southTexture = null;
        [SerializeField]
        private Texture2D westTexture = null;
        [SerializeField]
        private Texture2D eastTexture = null;

        [SerializeField]
        [HideInInspector]
        private int topTextureId = 0;
        [SerializeField]
        [HideInInspector]
        private int bottomTextureId = 0;
        [SerializeField]
        [HideInInspector]
        private int northTextureId = 0;
        [SerializeField]
        [HideInInspector]
        private int southTextureId = 0;
        [SerializeField]
        [HideInInspector]
        private int westTextureId = 0;
        [SerializeField]
        [HideInInspector]
        private int eastTextureId = 0;

        public Texture2D TopTexture { get { return topTexture; } }
        public Texture2D BottomTexture { get { return bottomTexture; } }
        public Texture2D NorthTexture { get { return northTexture; } }
        public Texture2D SouthTexture { get { return southTexture; } }
        public Texture2D WestTexture { get { return westTexture; } }
        public Texture2D EastTexture { get { return eastTexture; } }

        public int TopTextureId { get { return topTextureId; } set { topTextureId = value; } }
        public int BottomTextureId { get { return bottomTextureId; } set { bottomTextureId = value; } }
        public int NorthTextureId { get { return northTextureId; } set { northTextureId = value; } }
        public int SouthTextureId { get { return southTextureId; } set { southTextureId = value; } }
        public int WestTextureId { get { return westTextureId; } set { westTextureId = value; } }
        public int EastTextureId { get { return eastTextureId; } set { eastTextureId = value; } }

#if UNITY_EDITOR

        private void OnValidate()
        {
            if (topTexture != null && !topTexture.isReadable)
            {
                Debug.LogError("Top texture needs to be readable.");
                topTexture = null;
            }

            if (bottomTexture != null && !bottomTexture.isReadable)
            {
                Debug.LogError("Bottom texture needs to be readable.");
                bottomTexture = null;
            }

            if (northTexture != null && !northTexture.isReadable)
            {
                Debug.LogError("Front texture needs to be readable.");
                northTexture = null;
            }

            if (southTexture != null && !southTexture.isReadable)
            {
                Debug.LogError("South texture needs to be readable.");
                southTexture = null;
            }

            if (westTexture != null && !westTexture.isReadable)
            {
                Debug.LogError("West texture needs to be readable.");
                westTexture = null;
            }

            if (eastTexture != null && !eastTexture.isReadable)
            {
                Debug.LogError("East texture needs to be readable.");
                eastTexture = null;
            }
        }

#endif
    }
}