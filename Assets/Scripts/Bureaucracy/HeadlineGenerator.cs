using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>Records each shift's events and supplies the next morning briefing's headline.</summary>
    public class HeadlineGenerator : Singleton<HeadlineGenerator>
    {
        private readonly System.Random rng = new System.Random();
        private ShiftReport current;
        private ShiftReport last;
        private int curedAtShiftStart;
        private int accreditedAtShiftStart;

        protected override void Awake()
        {
            base.Awake();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnShiftStarted += HandleShiftStarted;
                gm.shiftEndSteps.Register(45, RecordShift);
                if (gm.boardManager != null) gm.boardManager.OnPatientDied += HandleDied;
            }
        }

        private void Start()
        {
            if (MorningBriefing.Instance != null) MorningBriefing.Instance.headlineProvider = shift => HeadlineWriter.Write(last, rng);
        }

        protected override void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnShiftStarted -= HandleShiftStarted;
                gm.shiftEndSteps.Unregister(RecordShift);
                if (gm.boardManager != null) gm.boardManager.OnPatientDied -= HandleDied;
            }
            base.OnDestroy();
        }

        private void HandleDied(PatientCardView p) => current.deaths++;

        private void HandleShiftStarted(int shift)
        {
            current = new ShiftReport { shift = shift };
            if (PatientDeckManager.Instance != null) curedAtShiftStart = PatientDeckManager.Instance.patientsCured;
            if (AccreditationManager.Instance != null) accreditedAtShiftStart = AccreditationManager.Instance.tracker.AccreditedCount;
        }

        private void RecordShift(System.Action next)
        {
            if (PatientDeckManager.Instance != null) current.cured = PatientDeckManager.Instance.patientsCured - curedAtShiftStart;
            if (CitationManager.Instance != null) current.citations = CitationManager.Instance.ledger.ShiftCount;
            if (IntakeManager.Instance != null) { current.referred = IntakeManager.Instance.ReferredThisShift; current.fraudCaught = IntakeManager.Instance.FraudCaughtThisShift; }
            if (InspectionManager.Instance != null) current.inspection = InspectionManager.Instance.ConsumeLastOutcome();

            var acc = AccreditationManager.Instance;
            if (acc != null && acc.tracker.AccreditedCount > accreditedAtShiftStart)
            {
                foreach (var d in AccreditationTracker.Departments)
                    if (acc.tracker.IsAccredited(d)) current.newlyAccredited = DirectiveManager.DepartmentName(d);
            }

            last = current;
            next();
        }
    }
}
