using System.Collections.Generic;
using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// Team Assessment synergy: the first time in a shift that both a Doctor and a Nurse card have been
    /// played, the player picks a Ward patient and one of their hidden symptoms is diagnosed for free.
    /// Its power is the lower of the two cards' tiers ("only as good as the weakest member of the team").
    /// </summary>
    public class StaffSynergyManager : Singleton<StaffSynergyManager>
    {
        private int bestDoctorTier;
        private int bestNurseTier;
        private bool usedThisShift;
        private int pendingTier;          // > 0 while waiting for a choice prompt to close
        private Vector3 pendingPosition;

        /// <summary><c>true</c> once this shift's Team Assessment has started (it can't be used up with no one to assess).</summary>
        public bool UsedThisShift => usedThisShift;

        /// <summary>Tier of a Team Assessment waiting to start (0 = none).</summary>
        public int PendingTier => pendingTier;

        /// <summary>Creates the manager when a board scene loads, so no scene setup is needed.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null && GameManager.Instance != null)
                new GameObject(nameof(StaffSynergyManager)).AddComponent<StaffSynergyManager>();
        }

        /// <summary>The synergy's diagnostic tier: the weaker of the two cards, clamped to 1-3.</summary>
        public static int SynergyTier(int doctorTier, int nurseTier) => Mathf.Clamp(Mathf.Min(doctorTier, nurseTier), 1, 3);

        protected override void Awake()
        {
            base.Awake();
            DraggableCard.OnCardPlayed += HandleCardPlayed;
            if (GameManager.Instance != null) GameManager.Instance.OnShiftChanged += HandleShiftChanged;
        }

        protected override void OnDestroy()
        {
            DraggableCard.OnCardPlayed -= HandleCardPlayed;
            if (GameManager.Instance != null) GameManager.Instance.OnShiftChanged -= HandleShiftChanged;
            base.OnDestroy();
        }

        private void HandleShiftChanged(int shift)
        {
            bestDoctorTier = bestNurseTier = pendingTier = 0;
            usedThisShift = false;
        }

        /// <summary>Records a played card (normally fed by <see cref="DraggableCard.OnCardPlayed"/>; public for tests).</summary>
        public void RegisterPlayedCard(ActionCardSO card, Vector3 position) => HandleCardPlayed(card, position);

        private void HandleCardPlayed(ActionCardSO card, Vector3 position)
        {
            if (usedThisShift || card == null || card.tags == null) return;

            if (card.tags.Contains(CardTag.Doctor)) bestDoctorTier = Mathf.Max(bestDoctorTier, card.powerTier);
            if (card.tags.Contains(CardTag.Nurse)) bestNurseTier = Mathf.Max(bestNurseTier, card.powerTier);
            if (bestDoctorTier == 0 || bestNurseTier == 0) return;

            usedThisShift = true;
            pendingTier = SynergyTier(bestDoctorTier, bestNurseTier);
            pendingPosition = position;
        }

        private void Update() => StartPendingAssessment();

        /// <summary>Starts a waiting Team Assessment unless a "choose one" prompt is still open (run every frame; public for tests).</summary>
        public void StartPendingAssessment()
        {
            // Wait until any "choose one" prompt (e.g. a nurse card's choice) has been answered.
            if (pendingTier == 0 || ChoicePrompt.IsOpen) return;
            int tier = pendingTier;
            pendingTier = 0;
            BeginAssessment(tier, pendingPosition);
        }

        private void BeginAssessment(int tier, Vector3 position)
        {
            var maxTier = (SymptomTier)tier;
            var eligible = new List<PatientCardView>();
            var board = GameManager.Instance != null ? GameManager.Instance.boardManager : null;
            if (board != null)
                foreach (var p in board.ActivePatients)
                    if (p != null && p.CanTeamAssess(maxTier)) eligible.Add(p);

            if (eligible.Count == 0)
            {
                // Not used up: the next Doctor or Nurse card this shift tries again once someone is in a Ward bed.
                usedThisShift = false;
                GameFeedback.ShowAt("Team ready - no one to assess", position, GameFeedback.CareColour);
                return;
            }

            PatientPicker.Begin(eligible,
                $"TEAM ASSESSMENT (Tier {tier}): click a highlighted patient to diagnose a hidden symptom.",
                patient =>
                {
                    GameFeedback.ShowAt("Team Assessment!", patient.transform.position, GameFeedback.RewardColour);
                    patient.TeamAssess(maxTier);
                });
        }
    }
}
