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

namespace ChiefComplaint.Core
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public struct GeneratedSymptom
    {
        public SymptomCardSO Card;
        public SymptomTier Tier;
    }

    /// <summary>
    /// Manages the three escalating patient pools (Walk-In → Ambulance → Trauma Flight)
    /// and the four specialty symptom decks. Handles patient drawing, symptom generation,
    /// and the progression triggers that introduce harder patients as the run advances.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class PatientDeckManager : Singleton<PatientDeckManager>
    {
        [Header("Patient Pools (Escalation)")]
        public List<PatientCardSO> walkInPool = new List<PatientCardSO>();
        public List<PatientCardSO> ambulancePool = new List<PatientCardSO>();
        public List<PatientCardSO> traumaFlightPool = new List<PatientCardSO>();

        [Header("The 4 Specialty Symptom Decks")]
        public List<SymptomCardSO> cardiopulmonaryDeck = new List<SymptomCardSO>();
        public List<SymptomCardSO> infectiousDeck = new List<SymptomCardSO>();
        public List<SymptomCardSO> internalDeck = new List<SymptomCardSO>();
        public List<SymptomCardSO> traumaDeck = new List<SymptomCardSO>();

        [Header("Active Encounter Deck")]
        public List<PatientCardSO> activePatientDeck = new List<PatientCardSO>();

        // Progression Tracking Events
        public event System.Action<int> OnPatientsCuredChanged;
        public event System.Action<string> OnEscalationPhaseChanged;

        [Header("Progression Tracking")]

        public int patientsCured = 0;
        public int walkInsCured = 0;
        public int ambulancesCured = 0;
        public int traumaFlightsCured = 0;
        private bool ambulanceTriggered = false;
        private bool traumaTriggered = false;

        [Header("Escalation Thresholds")]
        [Tooltip("Total patients cured before the Ambulance pool is added to the active deck.")]
        public int ambulanceCureThreshold = 3;
        [Tooltip("Total patients cured before the Trauma Flight pool is added to the active deck.")]
        public int traumaFlightCureThreshold = 8;

        [Tooltip("The Ambulance pool also unlocks when this shift starts, even without enough cures (0 = cures only).")]
        public int ambulanceShiftThreshold = 5;
        [Tooltip("The Trauma Flight pool also unlocks when this shift starts, even without enough cures (0 = cures only).")]
        public int traumaFlightShiftThreshold = 10;

        /// <summary>
        /// Called by GameManager at the start of each shift, so the run escalates over time even if the player
        /// stops curing patients (which would otherwise keep them on easy Walk-Ins indefinitely).
        /// </summary>
        public void OnShiftStarted(int shift)
        {
            currentShift = shift;
            CheckProgressionTriggers();
        }

        private int currentShift = 1;

        protected override void Awake()
        {
            base.Awake();

            // Built in Awake (not Start) so the deck is ready before any other manager's Start
            // asks for a patient. Begin with Walk-In patients only; harder pools are unlocked
            // via progression triggers.
            activePatientDeck.Clear();
            activePatientDeck.AddRange(walkInPool);
        }

        // -------------------------------------------------------------------------
        // Patient Drawing
        // -------------------------------------------------------------------------

        /// <summary>
        /// Draws a random patient from the active deck (without replacement) and returns it.
        /// </summary>
        /// <returns>
        /// A <see cref="PatientCardSO"/> removed from the active deck, or <c>null</c> if the deck is empty.
        /// </returns>
        public PatientCardSO DrawRandomPatient()
        {
            if (activePatientDeck.Count == 0) return null;

            int randomIndex = Random.Range(0, activePatientDeck.Count);
            PatientCardSO drawnPatient = activePatientDeck[randomIndex];

            // Remove so the same patient cannot be drawn again this run.
            activePatientDeck.RemoveAt(randomIndex);

            return drawnPatient;
        }

        // -------------------------------------------------------------------------
        // Symptom Generation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Generates a set of symptoms for <paramref name="patient"/> by drawing one card
        /// per category listed in <see cref="PatientCardSO.incomingCategories"/>.
        /// The first symptom's tier is derived from the patient's triage level; all
        /// subsequent symptoms default to Tier 1.
        /// </summary>
        /// <param name="patient">The patient card whose categories dictate which decks are drawn from.</param>
        /// <param name="drawnSymptoms">Output list of symptom cards assigned to the patient.</param>
        /// <param name="drawnTiers">Output list of tiers that correspond 1-to-1 with <paramref name="drawnSymptoms"/>.</param>
        public List<GeneratedSymptom> GenerateSymptomsForPatient(PatientCardSO patient)
        {
            List<GeneratedSymptom> generated = new List<GeneratedSymptom>();
            List<SymptomCardSO> alreadyDrawn = new List<SymptomCardSO>();

            for (int i = 0; i < patient.incomingCategories.Count; i++)
            {
                var category = patient.incomingCategories[i];
                SymptomCardSO drawn = DrawSymptomForCategory(category, alreadyDrawn, patient.clinicalTags);

                if (drawn != null)
                {
                    alreadyDrawn.Add(drawn);

                    // Only the primary symptom (index 0) scales with triage severity.
                    SymptomTier assignedTier = SymptomTier.Tier1;
                    if (i == 0)
                    {
                        assignedTier = DetermineTierFromTriage(patient.triageLevel);
                    }

                    generated.Add(new GeneratedSymptom { Card = drawn, Tier = assignedTier });
                }
            }

            return generated;
        }

        private List<SymptomCardSO> _cachedAvailablePool = new List<SymptomCardSO>();
        private List<SymptomCardSO> _cachedTagFilteredPool = new List<SymptomCardSO>();

        /// <summary>
        /// Picks a random symptom from the master deck matching <paramref name="category"/>,
        /// preferring symptoms whose clinicalTags overlap with the patient's tags.
        /// Falls back to any unexcluded symptom in the category if no tag match is found.
        /// </summary>
        /// <param name="category">The specialty category to draw from.</param>
        /// <param name="excludeList">Symptoms already assigned; prevents duplicates on a single patient.</param>
        /// <param name="patientTags">The patient's clinical tags used to constrain the symptom pool.</param>
        /// <returns>A symptom card, or <c>null</c> if the category deck is empty or exhausted.</returns>
        private SymptomCardSO DrawSymptomForCategory(SymptomCategory category, List<SymptomCardSO> excludeList, List<string> patientTags = null)
        {
            List<SymptomCardSO> masterDeck = category switch
            {
                SymptomCategory.Trauma          => traumaDeck,
                SymptomCategory.Cardiopulmonary => cardiopulmonaryDeck,
                SymptomCategory.Infectious      => infectiousDeck,
                SymptomCategory.Internal        => internalDeck,
                _                               => internalDeck
            };

            if (masterDeck == null || masterDeck.Count == 0) return null;

            // Build the candidate pool, excluding already-drawn symptoms.
            _cachedAvailablePool.Clear();
            foreach (var sym in masterDeck)
            {
                if (!excludeList.Contains(sym)) _cachedAvailablePool.Add(sym);
            }

            if (_cachedAvailablePool.Count == 0) return null;

            // If the patient has clinical tags, prefer symptoms that share at least one tag.
            if (patientTags != null && patientTags.Count > 0)
            {
                _cachedTagFilteredPool.Clear();
                foreach (var sym in _cachedAvailablePool)
                {
                    if (sym.clinicalTags == null || sym.clinicalTags.Count == 0) continue;
                    foreach (var tag in sym.clinicalTags)
                    {
                        if (patientTags.Contains(tag))
                        {
                            _cachedTagFilteredPool.Add(sym);
                            break;
                        }
                    }
                }

                // Only use the filtered pool if it has results; otherwise fall back gracefully.
                if (_cachedTagFilteredPool.Count > 0)
                {
                    return _cachedTagFilteredPool[Random.Range(0, _cachedTagFilteredPool.Count)];
                }

                GameLog.Warn($"[PatientDeckManager] No tag-matching symptoms found in {category} deck. Falling back to unfiltered draw.");
            }

            return _cachedAvailablePool[Random.Range(0, _cachedAvailablePool.Count)];
        }

        /// <summary>
        /// Maps a triage level (1–5) to a <see cref="SymptomTier"/>.
        /// Triage 1 = Critical (Tier 3), Triage 2–3 = Moderate (Tier 2), Triage 4–5 = Minor (Tier 1).
        /// </summary>
        /// <param name="triage">ESI triage level (1 = most severe, 5 = least severe).</param>
        /// <returns>The corresponding <see cref="SymptomTier"/> for the primary symptom.</returns>
        private SymptomTier DetermineTierFromTriage(int triage)
        {
            if (triage == 1) return SymptomTier.Tier3;
            if (triage == 2 || triage == 3) return SymptomTier.Tier2;
            return SymptomTier.Tier1;
        }

        // -------------------------------------------------------------------------
        // Progression
        // -------------------------------------------------------------------------

        /// <summary>
        /// Called when a patient is successfully discharged. Increments the cure counter
        /// and checks whether escalation triggers should fire.
        /// </summary>
        public void PatientSuccessfullyCured(PatientCardSO patient)
        {
            patientsCured++;

            // Use the EscalationType field rather than searching each static pool list.
            switch (patient.escalationPhase)
            {
                case EscalationType.WalkIn:       walkInsCured++;       break;
                case EscalationType.Ambulance:    ambulancesCured++;    break;
                case EscalationType.TraumaFlight: traumaFlightsCured++; break;
            }

            OnPatientsCuredChanged?.Invoke(patientsCured);
            CheckProgressionTriggers();
        }

        /// <summary>
        /// Evaluates escalation thresholds and injects harder patient pools into the
        /// active deck once milestones are reached (see <see cref="ambulanceCureThreshold"/>
        /// and <see cref="traumaFlightCureThreshold"/>).
        /// </summary>
        private void CheckProgressionTriggers()
        {
            bool escalated = false;

            bool ambulanceByShift = ambulanceShiftThreshold > 0 && currentShift >= ambulanceShiftThreshold;
            bool traumaByShift = traumaFlightShiftThreshold > 0 && currentShift >= traumaFlightShiftThreshold;

            if ((patientsCured >= ambulanceCureThreshold || ambulanceByShift) && !ambulanceTriggered)
            {
                GameLog.Info("WARNING: Ambulances are arriving! Adding Tier 2 patients.");
                activePatientDeck.AddRange(ambulancePool);
                ambulanceTriggered = true;
                escalated = true;
            }

            if ((patientsCured >= traumaFlightCureThreshold || traumaByShift) && !traumaTriggered)
            {
                GameLog.Info("CRITICAL: Trauma Flights incoming! Adding Tier 3 patients.");
                activePatientDeck.AddRange(traumaFlightPool);
                traumaTriggered = true;
                escalated = true;
            }

            if (escalated)
            {
                OnEscalationPhaseChanged?.Invoke(GetEscalationPhase());
            }
        }

        /// <summary>The highest escalation stage reached so far (unlocks supply-room piles, among other things).</summary>
        public EscalationType CurrentEscalation =>
            traumaTriggered ? EscalationType.TraumaFlight :
            ambulanceTriggered ? EscalationType.Ambulance : EscalationType.WalkIn;

        /// <summary>
        /// Returns the name of the highest escalation phase reached so far.
        /// </summary>
        public string GetEscalationPhase()
        {
            if (traumaTriggered) return "Trauma Flights";
            if (ambulanceTriggered) return "Ambulances";
            return "Walk-Ins";
        }
    }

}
