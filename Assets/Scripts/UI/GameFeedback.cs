using UnityEngine;
using ChiefComplaint.Utils;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Single entry point for short player-facing warning messages. Centralises the warning colour,
    /// the world-to-screen conversion and the null checks that were previously repeated at every call site.
    /// </summary>
    public static class GameFeedback
    {
        /// <summary>Colour used for all "you can't do that" style warnings.</summary>
        public static readonly Color WarningColour = new Color(1f, 0.4f, 0.4f);

        /// <summary>
        /// Shows a warning popup above a world-space position.
        /// Safe to call when there is no <see cref="NotificationManager"/> or no main camera.
        /// </summary>
        /// <param name="message">The text to display.</param>
        /// <param name="worldPosition">World position the popup should appear at.</param>
        public static void WarnAt(string message, Vector3 worldPosition)
        {
            if (NotificationManager.Instance == null) return;
            Camera cam = CameraUtil.Main;
            if (cam == null) return;
            NotificationManager.Instance.ShowNotificationAtPosition(message, cam.WorldToScreenPoint(worldPosition), WarningColour);
        }
    }
}
