using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Cards.Data
{
    /// <summary>Defines the four specialty medical categories that symptoms and departments map to.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public enum SymptomCategory
    {
        Trauma,
        Cardiopulmonary,
        Infectious,
        Internal
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public enum EscalationType
    {
        WalkIn,
        Ambulance,
        TraumaFlight
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public enum PatientTriggerCondition
    {
        None,
        OnSpawn,
        OnDeath,
        OnDiagnosed,
        OnCured,
        OnTurnEnd
    }

    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public enum PatientTriggerEffect
    {
        None,
        GainAP,
        LoseAP,
        GainReputation,
        LoseReputation,
        DrawCard,
        DiscardCard,
        ChangeStability,
        AddCardToDiscard,
        AddCardToHand
    }

    [System.Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public struct PatientTrigger
    {
        [Tooltip("When does this trigger happen?")]
        public PatientTriggerCondition condition;
        [Tooltip("What happens to the player/game state?")]
        public PatientTriggerEffect effect;
        [Tooltip("The amount of AP, Reputation, Stability, or Cards involved.")]
        public int effectValue;
        [Tooltip("Optional: A specific card to generate (e.g. adding a Red Tape curse to the deck).")]
        public ActionCardSO cardReference;
    }

    /// <summary>
    /// ScriptableObject data container for a single patient card. Defines the patient's
    /// biography, triage classification, symptom category requirements, stability vitals,
    /// and the Reputation reward on successful discharge.
    /// </summary>
    [CreateAssetMenu(fileName = "New Patient", menuName = "Chief Complaint/Patient Card")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class PatientCardSO : ScriptableObject
    {
        [Header("Biography & Presentation")]
        public string patientName = "Jane Doe";
        /// <summary>The visible presenting issue shown on the card face (e.g. "Chest Pain").</summary>
        public string chiefComplaint = "Chest Pain";
        public int age = 42;

        [Header("Clinical Tags")]
        [Tooltip("Keywords to logically link symptoms (e.g. 'Respiratory', 'Fracture').")]
        public List<string> clinicalTags = new List<string>();

        [Header("Medical Classification")]
        public EscalationType escalationPhase = EscalationType.WalkIn;

        [Tooltip("1 = Resuscitation (Critical) to 5 = Non-Urgent")]
        [Range(1, 5)]
        public int triageLevel = 3;

        [Header("Specialty Decks Needed")]
        [Tooltip("The categories of face-down symptom cards to draw from.")]
        public List<SymptomCategory> incomingCategories = new List<SymptomCategory>();

        [Header("Vitals")]
        public int startingStability = 10;
        public int stabilityDecayPerTurn = 1;

        [Header("Arrival")]
        [Tooltip("Paramedic handover: the primary (highest-tier) symptom is already diagnosed when the patient arrives.")]
        public bool arrivesWithHandover = false;

        [Header("Rewards")]
        public int reputationReward = 2;

        [Header("Triggers & Events")]
        [Tooltip("Special effects that trigger when the patient enters the river, dies, or is treated.")]
        public List<PatientTrigger> triggers = new List<PatientTrigger>();

        [Header("Visuals")]
        public Sprite portraitSprite;

        /// <summary>
        /// <c>true</c> when an OnDeath trigger already takes Reputation. That trigger replaces the flat death penalty
        /// (GameManager.deathReputationPenalty) rather than stacking with it, so one death never costs 9–10 Reputation.
        /// </summary>
        public bool HasOwnDeathPenalty
        {
            get
            {
                if (triggers == null) return false;
                foreach (PatientTrigger trigger in triggers)
                    if (trigger.condition == PatientTriggerCondition.OnDeath && trigger.effect == PatientTriggerEffect.LoseReputation)
                        return true;
                return false;
            }
        }

        private void OnValidate()
        {
            if (incomingCategories != null && incomingCategories.Count > 3)
            {
                Debug.LogWarning($"[PatientCardSO] '{name}' has {incomingCategories.Count} symptoms! The UI and AP economy only support a maximum of 3. Truncating list.");
                while (incomingCategories.Count > 3)
                {
                    incomingCategories.RemoveAt(incomingCategories.Count - 1);
                }
            }
        }
    }
}
