using System;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public struct VoxelLoaderData : IEquatable<VoxelLoaderData>
    {
        public bool singleChunk;
        public int chunkDistanceX;
        public int chunkDistanceZ;
        public float3 position;

        public override bool Equals(object obj)
        {
            return obj != null && obj is VoxelLoaderData data && Equals(data);
        }

        public bool Equals(VoxelLoaderData other)
        {
            return singleChunk == other.singleChunk && chunkDistanceX == other.chunkDistanceX && chunkDistanceZ == other.chunkDistanceZ && position.Equals(other.position);
        }

        public override int GetHashCode()
        {
            int hashCode = 1469017180;
            hashCode = hashCode * -1521134295 + singleChunk.GetHashCode();
            hashCode = hashCode * -1521134295 + chunkDistanceX.GetHashCode();
            hashCode = hashCode * -1521134295 + chunkDistanceZ.GetHashCode();
            hashCode = hashCode * -1521134295 + position.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(VoxelLoaderData left, VoxelLoaderData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VoxelLoaderData left, VoxelLoaderData right)
        {
            return !(left == right);
        }
    }
}
