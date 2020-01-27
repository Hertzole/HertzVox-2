using UnityEngine;

namespace Hertzole.HertzVox
{
    public abstract class BaseConfig : ScriptableObject
    {
        [SerializeField]
        private string blockName = "New Block";
        [SerializeField]
        private ushort blockID = 1;
        [SerializeField]
        private string blockIdentifier = "new_block";
        [SerializeField]
        private bool solid = true;
        [SerializeField]
        private bool transparent = false;

        public string BlockName { get { return blockName; } }
        public ushort BlockID { get { return blockID; } }
        public string BlockIdentifier { get { return blockIdentifier; } }

        public bool Solid { get { return solid; } }
        public bool Transparent { get { return transparent; } }
    }
}
