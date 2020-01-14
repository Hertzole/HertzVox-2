using UnityEngine;

namespace Hertzole.HertzVox.Example
{
    public class HertzVoxExample : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                VoxelRaycastHit hit = HertzVox.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), VoxelWorld.Main, 100);
                if (hit.block.id != 0)
                {
                    VoxelWorld.Main.SetBlock(hit.blockPosition, BlockProvider.GetBlock("air"));
                }
            }
        }
    }
}