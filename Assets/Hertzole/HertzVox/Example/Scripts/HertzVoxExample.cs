using UnityEngine;

namespace Hertzole.HertzVox.Example
{
    public class HertzVoxExample : MonoBehaviour
    {
        private string[] availableBlocks = new string[] { "air", "stone", "dirt", "grass", "log", "planks", "leaves" };
        private int selectedBlock = 0;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                VoxelRaycastHit hit = HertzVox.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), VoxelWorld.Main, 100);
                if (hit.block.id != 0)
                {
                    VoxelWorld.Main.SetBlock(selectedBlock == 0 ? hit.blockPosition : hit.adjacentPosition, BlockProvider.GetBlock(availableBlocks[selectedBlock]));
                }
            }
        }

        private void OnGUI()
        {
            selectedBlock = GUILayout.Toolbar(selectedBlock, availableBlocks);
        }
    }
}