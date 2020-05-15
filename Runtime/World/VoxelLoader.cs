using UnityEngine;

namespace Hertzole.HertzVox
{
    public class VoxelLoader : MonoBehaviour
    {
        [SerializeField]
        private bool singleChunk = false;
        [SerializeField]
        private int chunkDistanceX = 10;
        [SerializeField]
        private int chunkDistanceZ = 10;

        public bool SingleChunk { get { return singleChunk; } set { singleChunk = value; } }

        public int ChunkDistanceX { get { return chunkDistanceX; } set { chunkDistanceX = value; } }
        public int ChunkDistanceZ { get { return chunkDistanceZ; } set { chunkDistanceZ = value; } }

        private void OnEnable()
        {
            if (VoxelWorld.Main != null)
            {
                VoxelWorld.Main.RegisterLoader(this);
            }
            else
            {
                Debug.LogWarning("There's no VoxelWorld in the scene for " + gameObject.name + "' to register to.", gameObject);
            }
        }

        private void OnDisable()
        {
            if (VoxelWorld.Main != null)
            {
                VoxelWorld.Main.UnregisterLoader(this);
            }
        }
    }
}
