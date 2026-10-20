using System.Collections.Generic;
using UnityEngine;

namespace ChiefComplaint.Cards.Data
{
    /// <summary>
    /// Maps each <see cref="ActionCardType"/> to its icon sprite and tint colour.
    /// Shared by the world-space and UI action card views so both show the same type badge.
    /// </summary>
    [CreateAssetMenu(fileName = "CardTypeIcons", menuName = "Chief Complaint/Card Type Icons")]
    public class CardTypeIconsSO : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public ActionCardType type;
            public Sprite icon;
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>Returns the icon for <paramref name="type"/>, or <c>null</c> if none is assigned.</summary>
        public Sprite GetIcon(ActionCardType type)
        {
            foreach (var entry in entries)
            {
                if (entry.type == type) return entry.icon;
            }
            return null;
        }

        /// <summary>Readable name for a card type, e.g. <c>RedTape</c> becomes "Red Tape".</summary>
        public static string GetDisplayName(ActionCardType type)
        {
            return System.Text.RegularExpressions.Regex.Replace(type.ToString(), "(?<=[a-z])(?=[A-Z])", " ");
        }

        /// <summary>The neon tint used for each card type's icon (matches the card frame colours).</summary>
        public static Color GetTypeColor(ActionCardType type)
        {
            return type switch
            {
                ActionCardType.Staff          => new Color(0.00f, 0.68f, 0.94f), // Neon Blue
                ActionCardType.Diagnostic     => new Color(0.51f, 0.75f, 0.51f), // Neon Green
                ActionCardType.Treatment      => new Color(0.97f, 0.58f, 0.12f), // Amber
                ActionCardType.Utility        => new Color(0.57f, 0.15f, 0.56f), // Purple
                ActionCardType.Administration => new Color(0.40f, 0.40f, 0.40f), // Grey
                ActionCardType.Supply         => new Color(0.99f, 0.93f, 0.13f), // Yellow
                ActionCardType.RedTape        => new Color(0.93f, 0.11f, 0.14f), // Red
                _ => Color.white
            };
        }
    }
}
