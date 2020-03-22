using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    [Serializable]
    public struct VoxelJsonData
    {
        public VoxelJsonPaletteData[] palette;
        public VoxelJsonChunkData[] chunks;

        public VoxelJsonData(Dictionary<ushort, string> palette)
        {
            this.palette = new VoxelJsonPaletteData[palette.Count];

            int index = 0;

            foreach (KeyValuePair<ushort, string> item in palette)
            {
                this.palette[index] = new VoxelJsonPaletteData(item.Key, item.Value);
                index++;
            }

            chunks = Array.Empty<VoxelJsonChunkData>();
        }
    }

    [Serializable]
    public struct VoxelJsonChunkData
    {
        public Vector3Int position;
        public Vector2Int[] blocks;

        public VoxelJsonChunkData(Chunk chunk)
        {
            position = new Vector3Int(chunk.position.x, chunk.position.y, chunk.position.z);
            NativeList<int2> compressedBlocks = chunk.CompressBlocks();

            blocks = new Vector2Int[compressedBlocks.Length];
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i] = new Vector2Int(compressedBlocks[i].x, compressedBlocks[i].y);
            }

            compressedBlocks.Dispose();
        }
    }

    [Serializable]
    public struct VoxelJsonPaletteData : IEquatable<VoxelJsonPaletteData>
    {
        public ushort index;
        public string id;

        public VoxelJsonPaletteData(ushort index, string id)
        {
            this.index = index;
            this.id = id;
        }

        public bool Equals(VoxelJsonPaletteData other)
        {
            return other.index == index && other.id == id;
        }

        public override int GetHashCode()
        {
            int hashCode = -1056285117;
            hashCode = hashCode * -1521134295 + index.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(id);
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is VoxelJsonPaletteData data && Equals(data);
        }

        public static bool operator ==(VoxelJsonPaletteData left, VoxelJsonPaletteData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VoxelJsonPaletteData left, VoxelJsonPaletteData right)
        {
            return !(left == right);
        }
    }
}