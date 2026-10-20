using System.Collections.Generic;
using ChiefComplaint.Bureaucracy;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>Pure-logic tests for the bureaucracy layer (citations, shift sequences, cost modifiers).</summary>
    [Category("Bureaucracy")]
    public class BureaucracyTests
    {
        // ---------------------------------------------------------------- Citations

        [Test]
        public void CitationLedger_EscalatesWarningWarningFineThenRedTape()
        {
            var ledger = new CitationLedger { warningsPerShift = 2 };

            Assert.AreEqual(CitationPenalty.Warning, ledger.Issue());
            Assert.AreEqual(CitationPenalty.Warning, ledger.Issue());
            Assert.AreEqual(CitationPenalty.Fine, ledger.Issue());
            Assert.AreEqual(CitationPenalty.RedTape, ledger.Issue());
            Assert.AreEqual(CitationPenalty.RedTape, ledger.Issue());
        }

        [Test]
        public void CitationLedger_ShiftResetRestoresWarningsButKeepsPeriodCount()
        {
            var ledger = new CitationLedger { warningsPerShift = 2 };
            ledger.Issue(); ledger.Issue(); ledger.Issue();

            ledger.ResetShift();

            Assert.AreEqual(CitationPenalty.Warning, ledger.Issue());
            Assert.AreEqual(4, ledger.PeriodCount, "Period count spans shifts until the next inspection.");
            Assert.AreEqual(1, ledger.ShiftCount);
        }

        // ---------------------------------------------------------------- Shift sequence

        [Test]
        public void ShiftSequence_RunsStepsInOrderThenCompletes()
        {
            var seq = new ShiftSequence();
            var log = new List<string>();
            seq.Register(20, next => { log.Add("b"); next(); });
            seq.Register(10, next => { log.Add("a"); next(); });

            seq.Run(() => log.Add("done"));

            CollectionAssert.AreEqual(new[] { "a", "b", "done" }, log);
        }

        [Test]
        public void ShiftSequence_WaitsForAStepToContinue()
        {
            var seq = new ShiftSequence();
            System.Action pending = null;
            bool done = false;
            seq.Register(0, next => pending = next);

            seq.Run(() => done = true);
            Assert.IsFalse(done, "The sequence must wait while a dialog step is open.");

            pending();
            Assert.IsTrue(done);
        }

        [Test]
        public void ShiftSequence_StopsWhenToldTo()
        {
            var seq = new ShiftSequence();
            bool secondRan = false, done = false;
            bool stop = false;
            seq.Register(0, next => { stop = true; next(); });
            seq.Register(1, next => { secondRan = true; next(); });

            seq.Run(() => done = true, () => stop);

            Assert.IsFalse(secondRan);
            Assert.IsFalse(done);
        }

        // ---------------------------------------------------------------- Directives

        [Test]
        public void DirectivePicker_NeverRepeatsThePreviousDirective()
        {
            var a = ScriptableObject.CreateInstance<DirectiveSO>();
            var b = ScriptableObject.CreateInstance<DirectiveSO>();
            var pool = new List<DirectiveSO> { a, b };
            try
            {
                for (int i = 0; i < 2; i++)
                    Assert.AreSame(b, DirectivePicker.Pick(pool, a, n => i % n));
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }

        [Test]
        public void DirectivePicker_EmptyPoolGivesNoDirective()
        {
            Assert.IsNull(DirectivePicker.Pick(new List<DirectiveSO>(), null));
        }

        // ---------------------------------------------------------------- Intake paperwork

        private static PatientCardSO MakePatient(EscalationType type = EscalationType.WalkIn)
        {
            var p = ScriptableObject.CreateInstance<PatientCardSO>();
            p.patientName = "Jane Doe"; p.age = 40; p.chiefComplaint = "Chest Pain"; p.escalationPhase = type;
            return p;
        }

        [Test]
        public void IntakeForm_DiscrepancyChanceRisesAndCaps()
        {
            Assert.AreEqual(0.20f, IntakeFormGenerator.DiscrepancyChance(1), 0.001f);
            Assert.AreEqual(0.25f, IntakeFormGenerator.DiscrepancyChance(6), 0.001f);
            Assert.AreEqual(0.35f, IntakeFormGenerator.DiscrepancyChance(40), 0.001f);
        }

        [Test]
        public void IntakeForm_EveryDiscrepancyIsVisibleOnTheForm()
        {
            var patient = MakePatient();
            var others = new List<string> { "Toothache (lost my prescription)" };
            var rng = new System.Random(1234);
            int falsified = 0;
            try
            {
                for (int i = 0; i < 500; i++)
                {
                    var form = IntakeFormGenerator.Generate(patient, 5, others, rng);
                    switch (form.discrepancy)
                    {
                        case FormDiscrepancy.None:
                            Assert.AreEqual(patient.age, form.statedAge);
                            Assert.AreEqual(patient.chiefComplaint, form.statedComplaint);
                            Assert.IsTrue(form.signed);
                            Assert.IsTrue(!form.insured || form.insuranceValidUntilShift >= 5);
                            break;
                        case FormDiscrepancy.AgeMismatch: Assert.AreNotEqual(patient.age, form.statedAge); falsified++; break;
                        case FormDiscrepancy.ComplaintMismatch: Assert.AreNotEqual(patient.chiefComplaint, form.statedComplaint); falsified++; break;
                        case FormDiscrepancy.ExpiredInsurance: Assert.IsFalse(form.HasValidInsurance(5)); falsified++; break;
                        case FormDiscrepancy.MissingSignature: Assert.IsFalse(form.signed); falsified++; break;
                    }
                }
                // Expected about 24% at shift 5.
                Assert.That(falsified, Is.InRange(80, 160), "Roughly a quarter of forms should be falsified at shift 5.");
            }
            finally { Object.DestroyImmediate(patient); }
        }

        [Test]
        public void IntakeForm_EmergencyArrivalsAreAlwaysInsured()
        {
            var patient = MakePatient(EscalationType.Ambulance);
            var rng = new System.Random(7);
            try
            {
                for (int i = 0; i < 200; i++)
                    Assert.IsTrue(IntakeFormGenerator.Generate(patient, 3, null, rng).insured);
            }
            finally { Object.DestroyImmediate(patient); }
        }

        // ---------------------------------------------------------------- Cost modifiers

        [Test]
        public void CardCostModifiers_AddsProvidersAndNeverGoesNegative()
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            card.apCost = 1;
            System.Func<ActionCardSO, int> plusOne = c => 1;
            System.Func<ActionCardSO, int> minusFive = c => -5;
            try
            {
                CardCostModifiers.Add(plusOne);
                Assert.AreEqual(2, CardCostModifiers.GetCost(card));

                CardCostModifiers.Add(minusFive);
                Assert.AreEqual(0, CardCostModifiers.GetCost(card));
            }
            finally
            {
                CardCostModifiers.Remove(plusOne);
                CardCostModifiers.Remove(minusFive);
                Object.DestroyImmediate(card);
            }
        }
    }
}
