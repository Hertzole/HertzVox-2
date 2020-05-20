using System;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public struct SetRangedData : IEquatable<SetRangedData>
    {
        public int3 from;
        public int3 to;
        public Block block;

        public override bool Equals(object obj)
        {
            return obj != null && obj is SetRangedData data && Equals(data);
        }

        public bool Equals(SetRangedData other)
        {
            return other.from.Equals(from) && other.to.Equals(to) && other.block.Equals(block);
        }

        public override int GetHashCode()
        {
            int hashCode = -1873493643;
            hashCode = hashCode * -1521134295 + from.GetHashCode();
            hashCode = hashCode * -1521134295 + to.GetHashCode();
            hashCode = hashCode * -1521134295 + block.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(SetRangedData left, SetRangedData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SetRangedData left, SetRangedData right)
        {
            return !(left == right);
        }
    }
}
