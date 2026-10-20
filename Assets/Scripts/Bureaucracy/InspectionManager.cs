using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>
    /// Hospital Board inspections every few shifts. Each checks three criteria (accreditation progress
    /// plus two drawn at random), revealed a couple of shifts ahead. Passing while all four departments
    /// are accredited wins the run.
    /// </summary>
    public class InspectionManager : Singleton<InspectionManager>
    {
        [Tooltip("An inspection happens at the end of every Nth shift.")]
        [Min(1)] public int interval = 5;
        [Tooltip("How many shifts ahead the checklist is revealed in the morning briefing.")]
        [Min(0)] public int revealLeadShifts = 2;
        [Tooltip("Funding bonus for passing every criterion.")]
        public int passBonusFunding = 8;
        [Tooltip("Reputation lost for failing an inspection.")]
        public int failReputationPenalty = 8;

        public int InspectionNumber { get; private set; } = 1;
        public int NextInspectionShift { get; private set; }
        public IReadOnlyList<InspectionCriterion> Criteria => criteria;

        private List<InspectionCriterion> criteria = new List<InspectionCriterion>();
        private readonly System.Random rng = new System.Random();
        private int curedAtLastInspection;
        private int deathsSinceLast;

        protected override void Awake()
        {
            base.Awake();
            NextInspectionShift = interval;
            criteria = InspectionRules.Draw(InspectionNumber, rng);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftEndSteps.Register(30, RunInspection);
                if (gm.boardManager != null) gm.boardManager.OnPatientDied += HandlePatientDied;
            }
        }

        private void Start()
        {
            if (MorningBriefing.Instance != null) MorningBriefing.Instance.noticeProviders.Add(BriefingNotice);
        }

        protected override void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftEndSteps.Unregister(RunInspection);
                if (gm.boardManager != null) gm.boardManager.OnPatientDied -= HandlePatientDied;
            }
            base.OnDestroy();
        }

        private void HandlePatientDied(PatientCardView patient) => deathsSinceLast++;

        /// <summary>Shifts until the next inspection (0 = it happens at the end of this shift).</summary>
        public int ShiftsUntilInspection =>
            NextInspectionShift - (GameManager.Instance != null ? GameManager.Instance.CurrentShift : 1);

        private string BriefingNotice()
        {
            int until = ShiftsUntilInspection;
            var sb = new StringBuilder();
            if (until == 0) sb.AppendLine("<b><color=#ff6b6b>INSPECTION TODAY</color></b> at the end of this shift. Checklist:");
            else if (until <= revealLeadShifts) sb.AppendLine($"<b><color=#ffd166>Inspection in {until} shift{(until == 1 ? "" : "s")}</color></b> (end of Shift {NextInspectionShift}). Checklist:");
            else return $"<color=#9fd7ff>Next Board inspection: end of Shift {NextInspectionShift}.</color>";

            foreach (var c in criteria) sb.AppendLine($"  - {c.Describe()}");
            return sb.ToString().TrimEnd();
        }

        /// <summary>The hospital's current state, as the inspector sees it.</summary>
        public InspectionSnapshot TakeSnapshot()
        {
            var s = new InspectionSnapshot();
            var acc = AccreditationManager.Instance;
            if (acc != null) { s.accreditedDepartments = acc.tracker.AccreditedCount; s.allAccredited = acc.tracker.AllAccredited; }

            var pdm = PatientDeckManager.Instance;
            if (pdm != null) s.curedSinceLast = pdm.patientsCured - curedAtLastInspection;
            s.deathsSinceLast = deathsSinceLast;

            if (CitationManager.Instance != null) s.citationsSinceLast = CitationManager.Instance.ledger.PeriodCount;

            var deck = DeckManager.Instance;
            if (deck != null)
            {
                foreach (var c in deck.drawPile) if (c.cardType == ActionCardType.RedTape) s.redTapeInDeck++;
                foreach (var c in deck.discardPile) if (c.cardType == ActionCardType.RedTape) s.redTapeInDeck++;
                foreach (var c in deck.handData) if (c.cardType == ActionCardType.RedTape) s.redTapeInDeck++;
            }

            var board = GameManager.Instance != null ? GameManager.Instance.boardManager : null;
            if (board != null)
                foreach (var p in board.ActivePatients)
                    if (p != null && !p.isRemoved && p.currentStability <= PatientCardView.LowStabilityThreshold) s.codeBlues++;
            return s;
        }

        private void RunInspection(System.Action next)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentShift < NextInspectionShift) { next(); return; }

            var snapshot = TakeSnapshot();
            var outcome = InspectionRules.Evaluate(criteria, snapshot);
            bool win = InspectionRules.IsWin(outcome, snapshot);

            var body = new StringBuilder();
            body.AppendLine($"<b>Board Inspection #{InspectionNumber}</b>  -  Departments accredited: {snapshot.accreditedDepartments}/4");
            body.AppendLine();
            foreach (var c in criteria)
            {
                bool ok = InspectionRules.Passes(c, snapshot);
                body.AppendLine($"{(ok ? "<color=#5be37d>PASS</color>" : "<color=#ff6b6b>FAIL</color>")}  {c.Describe()}");
            }
            body.AppendLine();
            body.AppendLine(outcome switch
            {
                InspectionOutcome.Pass => $"<b><color=#5be37d>PASSED.</color></b> The Board awards a +{passBonusFunding} Funding grant.",
                InspectionOutcome.Conditional => "<b><color=#ffd166>CONDITIONAL PASS.</color></b> Fix the issues. Corrective paperwork (a Red Tape card) has been issued.",
                _ => $"<b><color=#ff6b6b>FAILED.</color></b> The hospital's reputation suffers (-{failReputationPenalty} Reputation)."
            });
            if (win) body.AppendLine("\n<b><color=#5be37d>All four departments are accredited. The hospital is fully ACCREDITED!</color></b>");

            string title = outcome == InspectionOutcome.Fail ? "INSPECTION FAILED" : "INSPECTION REPORT";
            BureaucracyDialog.Show(title, body.ToString(), new BureaucracyDialog.Option(win ? "Celebrate" : "Continue", () =>
            {
                ApplyOutcome(outcome);
                StartNextPeriod();
                if (win) { gm.TriggerVictory(); return; }
                next();
            }));
        }

        private void ApplyOutcome(InspectionOutcome outcome)
        {
            var gm = GameManager.Instance;
            switch (outcome)
            {
                case InspectionOutcome.Pass: gm.AddFunding(passBonusFunding); break;
                case InspectionOutcome.Conditional: if (DeckManager.Instance != null) DeckManager.Instance.SpawnRandomRedTape(); break;
                default: gm.SpendReputation(failReputationPenalty); break;
            }
            GameLog.Info($"INSPECTION #{InspectionNumber}: {outcome}");
        }

        private void StartNextPeriod()
        {
            InspectionNumber++;
            NextInspectionShift += interval;
            criteria = InspectionRules.Draw(InspectionNumber, rng);
            deathsSinceLast = 0;
            if (PatientDeckManager.Instance != null) curedAtLastInspection = PatientDeckManager.Instance.patientsCured;
            if (CitationManager.Instance != null) CitationManager.Instance.ledger.ResetPeriod();
        }
    }
}
