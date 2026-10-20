using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>
    /// Intake paperwork: when a patient is first moved from Triage into a Ward bed, shows their form
    /// next to their chart and asks the player to ADMIT or REFER. Falsified forms that slip through
    /// are cited in the End Shift audit.
    /// </summary>
    public class IntakeManager : Singleton<IntakeManager>
    {
        [Tooltip("Reputation gained for referring a patient whose paperwork was falsified.")]
        public int fraudCaughtReputation = 1;
        [Tooltip("Reputation lost for referring a patient whose paperwork was genuine.")]
        public int genuineReferredPenalty = 2;

        /// <summary>Plausible fake complaints used for complaint-mismatch forms.</summary>
        public List<string> suspiciousComplaints = new List<string>
        {
            "Chronic Back Pain (only opioids help)",
            "Toothache (lost my prescription)",
            "Migraine (allergic to everything but codeine)",
            "Sprained Wrist (needs a sick note)",
            "Insomnia (out of sleeping pills)"
        };

        /// <summary>Patients referred this shift and this run (for headlines and inspections).</summary>
        public int ReferredThisShift { get; private set; }
        public int FraudCaughtThisShift { get; private set; }

        private readonly List<string> falsifiedAdmissions = new List<string>();
        private readonly System.Random rng = new System.Random();

        protected override void Awake()
        {
            base.Awake();
            AdmissionGate.interceptor = Intercept;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftEndSteps.Register(20, EndOfShiftAudit);
                gm.OnShiftStarted += _ => { ReferredThisShift = 0; FraudCaughtThisShift = 0; };
            }
        }

        protected override void OnDestroy()
        {
            if (AdmissionGate.interceptor == (System.Func<PatientCardView, System.Action, bool>)Intercept) AdmissionGate.interceptor = null;
            if (GameManager.Instance != null) GameManager.Instance.shiftEndSteps.Unregister(EndOfShiftAudit);
            base.OnDestroy();
        }

        private bool Intercept(PatientCardView patient, System.Action admit)
        {
            if (patient == null || patient.patientData == null) return false;
            if (DirectiveManager.Instance != null && DirectiveManager.Instance.IsActive(DirectiveType.PaperlessDay)) return false;

            int shift = GameManager.Instance != null ? GameManager.Instance.CurrentShift : 1;
            if (patient.intakeForm == null)
                patient.intakeForm = IntakeFormGenerator.Generate(patient.patientData, shift, suspiciousComplaints, rng);
            if (patient.intakeForm.processed) return false;

            if (GameManager.Instance != null && !GameManager.Instance.BeginEvent()) return false;
            ShowForm(patient, shift, admit);
            return true;
        }

        private void ShowForm(PatientCardView patient, int shift, System.Action admit)
        {
            var form = patient.intakeForm;
            var chart = patient.patientData;

            var body = new StringBuilder();
            body.AppendLine($"<b>Today: Shift {shift}</b>");
            body.AppendLine();
            body.AppendLine("<b><color=#9fd7ff>PATIENT CHART</color></b><pos=52%><b><color=#ffb347>INTAKE FORM</color></b>");
            body.AppendLine($"Name: {chart.patientName}<pos=52%>Name: {form.name}");
            body.AppendLine($"Age: {chart.age}<pos=52%>Age: {form.statedAge}");
            body.AppendLine($"Complaint: {chart.chiefComplaint}<pos=52%>Complaint: {form.statedComplaint}");
            body.AppendLine($"Arrival: {ArrivalName(chart.escalationPhase)}<pos=52%>Insurance: {InsuranceLine(form)}");
            body.AppendLine($"<pos=52%>Signature: {(form.signed ? $"<i>{form.signature}</i>" : "(blank)")}");
            body.AppendLine();
            body.AppendLine("<size=24><i>Check the form against the chart. Admit genuine patients; refer falsified paperwork.</i></size>");

            var directives = DirectiveManager.Instance;
            if (directives != null && directives.Current != null)
                body.AppendLine($"<size=24><color=#ffb347>Today's directive: {directives.Current.title}</color></size>");

            BureaucracyDialog.Show("INTAKE FORM", body.ToString(),
                new BureaucracyDialog.Option("ADMIT", () => { Finish(); Admit(patient, shift, admit); }, BureaucracyDialog.StampGreen),
                new BureaucracyDialog.Option("REFER", () => { Finish(); Refer(patient); }, BureaucracyDialog.StampRed),
                new BureaucracyDialog.Option("Not yet", Finish, new Color(0.55f, 0.55f, 0.6f)));
        }

        private static string ArrivalName(EscalationType type) => type switch
        {
            EscalationType.WalkIn => "Walk-In",
            EscalationType.TraumaFlight => "Trauma Flight",
            _ => "Ambulance"
        };

        private static string InsuranceLine(IntakeForm form)
        {
            if (!form.insured) return "<color=#ff6b6b>NONE</color>";
            return $"Valid until Shift {form.insuranceValidUntilShift}";
        }

        private static void Finish()
        {
            if (GameManager.Instance != null) GameManager.Instance.EndEvent();
        }

        private void Admit(PatientCardView patient, int shift, System.Action admit)
        {
            var form = patient.intakeForm;
            form.processed = true;

            if (form.IsFalsified)
            {
                form.admittedFalsified = true;
                falsifiedAdmissions.Add(patient.patientData.patientName);
            }

            if (DirectiveManager.Instance != null && DirectiveManager.Instance.IsActive(DirectiveType.InsuranceRequired)
                && patient.patientData.escalationPhase == EscalationType.WalkIn && !form.HasValidInsurance(shift))
            {
                Cite($"Admitted {patient.patientData.patientName} without valid insurance");
            }

            admit?.Invoke();
        }

        private void Refer(PatientCardView patient)
        {
            var form = patient.intakeForm;
            form.processed = true;
            ReferredThisShift++;
            var gm = GameManager.Instance;

            if (form.IsFalsified)
            {
                FraudCaughtThisShift++;
                if (gm != null && fraudCaughtReputation > 0) gm.AddReputation(fraudCaughtReputation);
                GameFeedback.WarnAt("Fraud caught!", patient.transform.position);
            }
            else if (gm != null && genuineReferredPenalty > 0)
            {
                gm.SpendReputation(genuineReferredPenalty);
                GameFeedback.WarnAt("A genuine patient was turned away!", patient.transform.position);
            }

            if (DirectiveManager.Instance != null && DirectiveManager.Instance.IsActive(DirectiveType.ReferralFreeze))
                Cite($"Referred {patient.patientData.patientName} during a referral freeze");

            patient.Refer();
        }

        private void EndOfShiftAudit(System.Action next)
        {
            foreach (var name in falsifiedAdmissions)
                Cite($"Audit: {name} was admitted on falsified paperwork");
            falsifiedAdmissions.Clear();
            next();
        }

        private static void Cite(string reason)
        {
            if (CitationManager.Instance != null) CitationManager.Instance.Issue(reason);
            else GameLog.Warn(reason);
        }
    }
}
