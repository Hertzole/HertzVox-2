using UnityEngine;

namespace Hertzole.HertzVox
{
    public class VoxelLoader : MonoBehaviour
    {
        private void OnEnable()
        {
            VoxelWorld.Main.RegisterLoader(this);
        }

        private void OnDisable()
        {
            VoxelWorld.Main.UnregisterLoader(this);
        }
    }
}
