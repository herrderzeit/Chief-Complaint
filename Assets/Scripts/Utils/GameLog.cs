using System.Diagnostics;

namespace ChiefComplaint.Utils
{
    /// <summary>
    /// Gameplay logging wrapper. Calls are compiled out of release player builds, so interpolated
    /// strings passed to these methods are never built (no GC cost) outside the Editor and
    /// development builds. Use this instead of <c>Debug.Log</c> inside gameplay loops.
    /// </summary>
    public static class GameLog
    {
        /// <summary>Logs an informational message (Editor and development builds only).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        public static void Info(string message) => UnityEngine.Debug.Log(message);

        /// <summary>Logs a warning (Editor and development builds only).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
        public static void Warn(string message) => UnityEngine.Debug.LogWarning(message);
    }
}
