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

        /// <summary>Colours for effect results: AP, cards, patient care and rewards.</summary>
        public static readonly Color APColour = new Color(0.35f, 0.85f, 1f);
        public static readonly Color CardColour = new Color(0.85f, 0.85f, 1f);
        public static readonly Color CareColour = new Color(0.45f, 1f, 0.55f);
        public static readonly Color RewardColour = new Color(1f, 0.85f, 0.35f);

        // Several results from one card (e.g. pause + heal) stack instead of overlapping.
        private const float StackWindow = 0.3f;
        private const float StackSpacing = 42f;
        private static float lastShowTime = -1f;
        private static int stackIndex;

        /// <summary>
        /// Shows a warning popup above a world-space position.
        /// Safe to call when there is no <see cref="NotificationManager"/> or no main camera.
        /// </summary>
        /// <param name="message">The text to display.</param>
        /// <param name="worldPosition">World position the popup should appear at.</param>
        public static void WarnAt(string message, Vector3 worldPosition) => ShowAt(message, worldPosition, WarningColour);

        /// <summary>
        /// Shows an effect result (e.g. "+1 AP", "Chest Pain diagnosed") above a world-space position.
        /// Popups shown within a fraction of a second of each other are stacked so they stay readable.
        /// </summary>
        public static void ShowAt(string message, Vector3 worldPosition, Color colour)
        {
            if (NotificationManager.Instance == null) return;
            Camera cam = CameraUtil.Main;
            if (cam == null) return;

            stackIndex = Time.unscaledTime - lastShowTime < StackWindow ? stackIndex + 1 : 0;
            lastShowTime = Time.unscaledTime;

            Vector3 screen = cam.WorldToScreenPoint(worldPosition) + Vector3.down * (StackSpacing * stackIndex);
            NotificationManager.Instance.ShowNotificationAtPosition(message, screen, colour);
        }

        /// <summary>"+1 Card" / "+2 Cards".</summary>
        public static string Cards(int n) => $"+{n} Card{(n == 1 ? "" : "s")}";
    }
}
