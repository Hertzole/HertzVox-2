using Priority_Queue;
using System;
using Unity.Mathematics;

namespace Hertzole.HertzVox
{
    public class ChunkNode : FastPriorityQueueNode, IEquatable<ChunkNode>
    {
        public int3 position;

        public ChunkNode(int3 position)
        {
            this.position = position;
        }

        public bool Equals(ChunkNode other)
        {
            return other.position.Equals(position);
        }
    }
}
