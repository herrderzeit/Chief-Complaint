using System.Collections.Generic;
using UnityEngine;

namespace ChiefComplaint.Bureaucracy
{
    public enum DilemmaEffectType
    {
        Funding,
        Reputation,
        /// <summary>AP gained (or lost, if negative) this shift.</summary>
        APNow,
        /// <summary>AP gained (or lost) at the start of next shift.</summary>
        APNextShift,
        /// <summary>Adds this many random Red Tape cards to the discard pile.</summary>
        AddRedTape,
        /// <summary>Staff cards cost this much extra AP next shift.</summary>
        StaffCostNextShift
    }

    [System.Serializable]
    public struct DilemmaEffect
    {
        public DilemmaEffectType type;
        public int amount;

        public string Describe() => type switch
        {
            DilemmaEffectType.Funding => $"{Signed(amount)} Funding",
            DilemmaEffectType.Reputation => $"{Signed(amount)} Reputation",
            DilemmaEffectType.APNow => $"{Signed(amount)} AP this shift",
            DilemmaEffectType.APNextShift => $"{Signed(amount)} AP next shift",
            DilemmaEffectType.AddRedTape => $"+{amount} Red Tape",
            _ => $"Staff cards +{amount} AP next shift"
        };

        private static string Signed(int n) => n >= 0 ? $"+{n}" : n.ToString();
    }

    /// <summary>A moral-choice event shown after the morning briefing, Papers, Please style.</summary>
    [CreateAssetMenu(fileName = "New Dilemma", menuName = "Chief Complaint/Bureaucracy/Dilemma")]
    public class DilemmaSO : ScriptableObject
    {
        public string title = "Dilemma";
        [TextArea(2, 5)] public string description;

        public string optionALabel = "Accept";
        public List<DilemmaEffect> optionAEffects = new List<DilemmaEffect>();

        public string optionBLabel = "Decline";
        public List<DilemmaEffect> optionBEffects = new List<DilemmaEffect>();
    }
}
