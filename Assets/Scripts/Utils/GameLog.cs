using System.Diagnostics;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Utils
{
    /// <summary>
    /// Gameplay logging wrapper. Calls are compiled out of player builds, so interpolated
    /// strings passed to these methods are never built (no GC cost) outside the Editor.
    /// Use this instead of <c>Debug.Log</c> inside gameplay loops.
    /// </summary>
    public static class GameLog
    {
        /// <summary>Logs an informational message (Editor only).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message) => UnityEngine.Debug.Log(message);

        /// <summary>Logs a warning (Editor and development builds only).</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warn(string message) => UnityEngine.Debug.LogWarning(message);
    }

}
