using UnityEngine;
using Conditional = System.Diagnostics.ConditionalAttribute;

namespace Hertzole.HertzVox
{
    internal static class VoxLogger
    {
        [Conditional("HERTZVOX_DEBUG")]
        internal static void Log(object message)
        {
            Debug.Log("[HertzVox] :: " + message);
        }

        [Conditional("HERTZVOX_DEBUG")]
        internal static void LogWarning(object message)
        {
            Debug.LogWarning("[HertzVox] :: " + message);
        }

        [Conditional("HERTZVOX_DEBUG")]
        internal static void LogError(object message)
        {
            Debug.LogError("[HertzVox] :: " + message);
        }
    }
}
