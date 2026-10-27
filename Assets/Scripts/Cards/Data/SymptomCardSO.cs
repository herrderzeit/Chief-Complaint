using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Data
{
    /// <summary>
    /// Severity tiers for symptoms. Determines which treatment cards can cure a symptom
    /// and which specialty department is required.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public enum SymptomTier
    {
        None  = 0,
        Tier1 = 1,
        Tier2 = 2,
        Tier3 = 3
    }

    /// <summary>
    /// ScriptableObject data container for a single symptom card. Stores the name and
    /// description for all three severity tiers so a single asset can represent the full
    /// progression of one symptom (e.g. Minor Chest Pain → Acute MI → STEMI).
    /// </summary>
    [CreateAssetMenu(fileName = "New Symptom", menuName = "Chief Complaint/Symptom Card")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class SymptomCardSO : ScriptableObject
    {
        [Header("Deck Assignment")]
        /// <summary>Which specialty deck this symptom belongs to (e.g. Trauma, Internal).</summary>
        public SymptomCategory specialtyCategory;

        [Header("Clinical Tags")]
        [Tooltip("Keywords to logically match with patients (e.g. 'Respiratory', 'Fracture').")]
        public List<string> clinicalTags = new List<string>();

        [Header("Tier 1 (Minor - Cured by Standard Care)")]
        public string tier1Name;
        [TextArea(2, 3)] public string tier1Description;

        [Header("Tier 2 (Moderate - Cured by Specialized Care)")]
        public string tier2Name;
        [TextArea(2, 3)] public string tier2Description;

        [Header("Tier 3 (Critical - Cured by Surgical Intervention)")]
        public string tier3Name;
        [TextArea(2, 3)] public string tier3Description;

        /// <summary>
        /// Returns the symptom's display name appropriate for the given <paramref name="tier"/>.
        /// Falls back to Tier 1 for unrecognised tiers.
        /// </summary>
        /// <param name="tier">The severity tier to look up.</param>
        /// <returns>The localised symptom name for that tier.</returns>
        public string GetNameByTier(SymptomTier tier)
        {
            switch (tier)
            {
                case SymptomTier.Tier1: return tier1Name;
                case SymptomTier.Tier2: return tier2Name;
                case SymptomTier.Tier3: return tier3Name;
                default: return tier1Name;
            }
        }

        /// <summary>
        /// Returns the symptom's flavour/rules description appropriate for the given <paramref name="tier"/>.
        /// Falls back to Tier 1 for unrecognised tiers.
        /// </summary>
        /// <param name="tier">The severity tier to look up.</param>
        /// <returns>The symptom description text for that tier.</returns>
        public string GetDescriptionByTier(SymptomTier tier)
        {
            return tier switch
            {
                SymptomTier.Tier1 => tier1Description,
                SymptomTier.Tier2 => tier2Description,
                SymptomTier.Tier3 => tier3Description,
                _ => tier1Description
            };
        }
    }
}
