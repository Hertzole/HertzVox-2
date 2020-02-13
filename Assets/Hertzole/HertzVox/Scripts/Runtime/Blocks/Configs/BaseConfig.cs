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
        private bool canCollide = true;
        [SerializeField]
        private bool transparent = false;
        [SerializeField]
        private bool connectToSame = true;

        public string BlockName { get { return blockName; } }
        public string BlockID { get { return blockID; } }

        public bool CanCollide { get { return canCollide; } }
        public bool Transparent { get { return transparent; } }
        public bool ConnectToSame { get { return connectToSame; } }
    }
}
