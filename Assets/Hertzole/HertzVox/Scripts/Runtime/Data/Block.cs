using System;

namespace Hertzole.HertzVox
{
    public struct Block
    {
        public int id;

        [NonSerialized]
        internal int topTexture;
        [NonSerialized]
        internal int bottomTexture;
        [NonSerialized]
        internal int northTexture;
        [NonSerialized]
        internal int southTexture;
        [NonSerialized]
        internal int eastTexture;
        [NonSerialized]
        internal int westTexture;

        public Block(int id)
        {
            this.id = id;

            topTexture = 0;
            bottomTexture = 0;
            northTexture = 0;
            southTexture = 0;
            eastTexture = 0;
            westTexture = 0;
        }

        public Block(int id, int topTexture, int bottomTexture, int northTexture, int southTexture, int eastTexture, int westTexture) : this(id)
        {
            this.topTexture = topTexture;
            this.bottomTexture = bottomTexture;
            this.northTexture = northTexture;
            this.southTexture = southTexture;
            this.eastTexture = eastTexture;
            this.westTexture = westTexture;
        }

        public Block(int id, CubeConfig cubeConfig)
        {
            this.id = id;

            topTexture = cubeConfig.TopTextureId;
            bottomTexture = cubeConfig.BottomTextureId;
            northTexture = cubeConfig.NorthTextureId;
            southTexture = cubeConfig.SouthTextureId;
            westTexture = cubeConfig.WestTextureId;
            eastTexture = cubeConfig.EastTextureId;
        }

        public override bool Equals(object obj)
        {
            return obj is Block block ? block.id == id : false;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public static bool operator ==(Block left, Block right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Block left, Block right)
        {
            return !(left == right);
        }
    }
}
