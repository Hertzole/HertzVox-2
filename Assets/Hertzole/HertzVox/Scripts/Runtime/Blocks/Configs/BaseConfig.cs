using UnityEngine;

namespace Hertzole.HertzVox
{
    public abstract class BaseConfig : ScriptableObject
    {
        [SerializeField]
        private string blockName = "New Block";
        [SerializeField]
        private string blockID = "new_block";
        [SerializeField]
        private bool solid = true;

        public string BlockName { get { return blockName; } }
        public string BlockID { get { return blockID; } }

        public bool Solid { get { return solid; } }
    }
}
