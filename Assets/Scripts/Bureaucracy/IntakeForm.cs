using System.Collections.Generic;
using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Bureaucracy
{
    public enum FormDiscrepancy
    {
        None,
        /// <summary>The stated age doesn't match the chart.</summary>
        AgeMismatch,
        /// <summary>The stated complaint doesn't match the chart (a possible drug-seeker).</summary>
        ComplaintMismatch,
        /// <summary>The insurance shown expired before today.</summary>
        ExpiredInsurance,
        /// <summary>The form was never signed.</summary>
        MissingSignature
    }

    /// <summary>
    /// The paperwork a patient brings to Triage. Generated at runtime (no asset changes); the player
    /// compares it with the patient's chart and stamps it ADMIT or REFER.
    /// </summary>
    public class IntakeForm
    {
        public string name;
        public int statedAge;
        public string statedComplaint;
        /// <summary><c>false</c> means the patient has no insurance at all (not fraud, just uninsured).</summary>
        public bool insured;
        /// <summary>Insurance is valid up to and including this shift.</summary>
        public int insuranceValidUntilShift;
        public bool signed;
        public string signature;

        /// <summary>What, if anything, is wrong with the form. Hidden from the player.</summary>
        public FormDiscrepancy discrepancy;

        /// <summary>Set once the form has been stamped, so it is only shown once.</summary>
        public bool processed;
        /// <summary>Set when a form with a discrepancy was admitted; caught by the End Shift audit.</summary>
        public bool admittedFalsified;

        public bool IsFalsified => discrepancy != FormDiscrepancy.None;

        /// <summary>Insurance exists and is still valid on <paramref name="currentShift"/>.</summary>
        public bool HasValidInsurance(int currentShift) => insured && insuranceValidUntilShift >= currentShift;
    }

    /// <summary>Builds intake forms. Pure and seedable so it can be unit tested.</summary>
    public static class IntakeFormGenerator
    {
        /// <summary>Chance that a form is falsified on <paramref name="shift"/>: 20%, rising 1% per shift to 35%.</summary>
        public static float DiscrepancyChance(int shift) => System.Math.Min(0.35f, 0.20f + 0.01f * (shift - 1));

        public static IntakeForm Generate(PatientCardSO patient, int currentShift, IList<string> otherComplaints, System.Random rng)
        {
            var form = new IntakeForm
            {
                name = patient.patientName,
                statedAge = patient.age,
                statedComplaint = patient.chiefComplaint,
                // Emergency arrivals are always covered; about 1 in 5 Walk-Ins has no insurance.
                insured = patient.escalationPhase != EscalationType.WalkIn || rng.NextDouble() >= 0.2,
                insuranceValidUntilShift = currentShift + 2 + rng.Next(0, 8),
                signed = true,
                signature = Initials(patient.patientName)
            };

            if (rng.NextDouble() >= DiscrepancyChance(currentShift)) return form;

            var options = new List<FormDiscrepancy> { FormDiscrepancy.AgeMismatch, FormDiscrepancy.MissingSignature };
            if (otherComplaints != null && otherComplaints.Count > 0) options.Add(FormDiscrepancy.ComplaintMismatch);
            if (form.insured) options.Add(FormDiscrepancy.ExpiredInsurance);

            form.discrepancy = options[rng.Next(options.Count)];
            switch (form.discrepancy)
            {
                case FormDiscrepancy.AgeMismatch:
                    int shift = 15 + rng.Next(0, 16);
                    form.statedAge = patient.age + (patient.age > 45 ? -shift : shift);
                    break;
                case FormDiscrepancy.ComplaintMismatch:
                    string other = otherComplaints[rng.Next(otherComplaints.Count)];
                    form.statedComplaint = other != patient.chiefComplaint ? other : "Severe Back Pain (needs strong painkillers)";
                    break;
                case FormDiscrepancy.ExpiredInsurance:
                    form.insuranceValidUntilShift = currentShift - 1 - rng.Next(0, 3);
                    break;
                case FormDiscrepancy.MissingSignature:
                    form.signed = false;
                    form.signature = "";
                    break;
            }
            return form;
        }

        private static string Initials(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return "X";
            var parts = fullName.Split(' ');
            return parts.Length > 1 ? $"{parts[0][0]}. {parts[parts.Length - 1]}" : fullName;
        }
    }
}
