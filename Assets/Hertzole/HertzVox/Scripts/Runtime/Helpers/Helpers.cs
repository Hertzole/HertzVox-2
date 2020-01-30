using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public static class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ContainingChunkPosition(int3 position)
        {
            int3 p;
            p.x = Mathf.FloorToInt(position.x / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            p.y = Mathf.FloorToInt(position.y / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            p.z = Mathf.FloorToInt(position.z / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;

            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ContainingChunkPosition(int x, int y, int z)
        {
            int3 p;
            p.x = Mathf.FloorToInt(x / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            p.y = Mathf.FloorToInt(y / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            p.z = Mathf.FloorToInt(z / (float)Chunk.CHUNK_SIZE) * Chunk.CHUNK_SIZE;
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex1DFrom2D(int x, int z, int sizeX)
        {
            return x + z * sizeX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetIndex1DFrom3D(int x, int y, int z, int size)
        {
            return x * size * size + y * size + z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return (r < 0) ? (r + modulus) : r;
        }

        public static int3 WorldToChunk(Vector3 worldPosition, int chunkSize)
        {
            return new int3(Mathf.FloorToInt(worldPosition.x / chunkSize), Mathf.FloorToInt(worldPosition.y / chunkSize), Mathf.FloorToInt(worldPosition.z / chunkSize));
        }
    }
}
