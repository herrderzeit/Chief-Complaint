using UnityEngine;

namespace ChiefComplaint.Utils
{
    /// <summary>
    /// Caches the main camera so gameplay code does not repeatedly resolve <c>Camera.main</c>.
    /// Re-resolves automatically if the cached camera is destroyed (e.g. on scene reload).
    /// </summary>
    public static class CameraUtil
    {
        private static Camera cached;

        /// <summary>The cached main camera, or <c>null</c> if the scene has none.</summary>
        public static Camera Main
        {
            get
            {
                if (cached == null) cached = Camera.main;
                return cached;
            }
        }
    }
}
