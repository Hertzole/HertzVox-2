using System;
using Unity.Jobs;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public struct ChunkJobData : IEquatable<ChunkJobData>
    {
        public int3 position;
        public JobHandle job;
        public float priority;
        public int frameCounter;
        public bool urgent;

        public ChunkJobData(int3 position, JobHandle job, float priority, bool urgent)
        {
            this.position = position;
            this.job = job;
            this.priority = priority;
            this.urgent = urgent;

            frameCounter = 0;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is ChunkJobData data && data.position.Equals(position);
        }

        public bool Equals(ChunkJobData other)
        {
            return other.position.Equals(position);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 * position.GetHashCode();

                return hash;
            }
        }

        public static bool operator ==(ChunkJobData left, ChunkJobData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChunkJobData left, ChunkJobData right)
        {
            return !(left == right);
        }
    }
}
