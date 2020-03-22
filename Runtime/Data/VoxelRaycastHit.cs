using Unity.Mathematics;
using UnityEngine;

namespace Hertzole.HertzVox
{
    public struct VoxelRaycastHit
    {
        public int3 blockPosition;
        public int3 adjacentPosition;
        public float3 direction;
        public Vector3 scenePosition;
        public Block block;
    }
}