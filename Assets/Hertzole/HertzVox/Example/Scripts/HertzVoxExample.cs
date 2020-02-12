using UnityEngine;

namespace Hertzole.HertzVox.Example
{
    public class HertzVoxExample : MonoBehaviour
    {
        private string[] availableBlocks = new string[] { "air", "stone", "dirt", "grass", "log", "planks", "leaves", "colored", "glass" };
        private int selectedBlock = 0;

        private bool vsync = true;

        private void Start()
        {
            QualitySettings.vSyncCount = 1;
            vsync = true;
        }

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

            if (Input.GetKeyDown(KeyCode.F1))
            {
                vsync = !vsync;
                QualitySettings.vSyncCount = vsync ? 1 : 0;
            }
        }

        private void OnGUI()
        {
            selectedBlock = GUILayout.Toolbar(selectedBlock, availableBlocks);
        }
    }
}