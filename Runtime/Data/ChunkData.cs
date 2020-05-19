using System;

namespace Hertzole.HertzVox
{
    public struct ChunkData : IEquatable<ChunkData>
    {
        public float priority;
        public bool render;

        public bool Equals(ChunkData other)
        {
            return other.render == render && other.priority == priority;
        }

        public override int GetHashCode()
        {
            int hashCode = -167718018;
            hashCode = hashCode * -1521134295 + priority.GetHashCode();
            hashCode = hashCode * -1521134295 + render.GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is ChunkData data && Equals(data);
        }

        public static bool operator ==(ChunkData left, ChunkData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkData left, ChunkData right)
        {
            return !(left == right);
        }
    }
}
