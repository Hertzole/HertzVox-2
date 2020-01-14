using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public static class Helpers
    {
        public static int3 ContainingChunkPosition(int3 position)
        {
            int3 p;
            p.x = Mathf.FloorToInt(position.x / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            p.y = Mathf.FloorToInt(position.y / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            p.z = Mathf.FloorToInt(position.z / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;

            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }
    }
}
