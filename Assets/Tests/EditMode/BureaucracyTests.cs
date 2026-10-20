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

        // ---------------------------------------------------------------- Accreditation

        [Test]
        public void Accreditation_DepartmentAccreditsAtThresholdOnce()
        {
            var t = new AccreditationTracker { threshold = 3 };
            Assert.IsFalse(t.Record(SymptomCategory.Trauma));
            Assert.IsFalse(t.Record(SymptomCategory.Trauma));
            Assert.IsTrue(t.Record(SymptomCategory.Trauma), "The third treatment accredits the department.");
            Assert.IsFalse(t.Record(SymptomCategory.Trauma), "Further treatments don't re-accredit.");
            Assert.AreEqual(1, t.AccreditedCount);
            Assert.IsFalse(t.AllAccredited);
        }

        [Test]
        public void Accreditation_AllFourDepartmentsMeansFullyAccredited()
        {
            var t = new AccreditationTracker { threshold = 1 };
            foreach (var d in AccreditationTracker.Departments) t.Record(d);
            Assert.IsTrue(t.AllAccredited);
        }

        // ---------------------------------------------------------------- Inspections

        private static List<InspectionCriterion> Checklist(params CriterionType[] types)
        {
            var list = new List<InspectionCriterion>();
            foreach (var t in types) list.Add(new InspectionCriterion { type = t, target = 2 });
            return list;
        }

        [Test]
        public void Inspection_DrawAlwaysChecksAccreditationAndScalesItsTarget()
        {
            var rng = new System.Random(3);
            for (int n = 1; n <= 6; n++)
            {
                var c = InspectionRules.Draw(n, rng);
                Assert.AreEqual(3, c.Count);
                Assert.AreEqual(CriterionType.DepartmentsAccredited, c[0].type);
                Assert.AreEqual(System.Math.Min(4, n), c[0].target);
                Assert.AreNotEqual(c[1].type, c[2].type, "The two random criteria must differ.");
            }
        }

        [Test]
        public void Inspection_OutcomeByCriteriaPassed()
        {
            var criteria = Checklist(CriterionType.DepartmentsAccredited, CriterionType.NoDeaths, CriterionType.NoCodeBlue);
            var good = new InspectionSnapshot { accreditedDepartments = 2 };
            var oneMiss = new InspectionSnapshot { accreditedDepartments = 2, deathsSinceLast = 1 };
            var twoMiss = new InspectionSnapshot { accreditedDepartments = 0, deathsSinceLast = 1 };

            Assert.AreEqual(InspectionOutcome.Pass, InspectionRules.Evaluate(criteria, good));
            Assert.AreEqual(InspectionOutcome.Conditional, InspectionRules.Evaluate(criteria, oneMiss));
            Assert.AreEqual(InspectionOutcome.Fail, InspectionRules.Evaluate(criteria, twoMiss));
        }

        [TestCase(InspectionOutcome.Pass, true, true)]
        [TestCase(InspectionOutcome.Conditional, true, true)]
        [TestCase(InspectionOutcome.Fail, true, false)]
        [TestCase(InspectionOutcome.Pass, false, false)]
        public void Inspection_WinNeedsAPassAndFullAccreditation(InspectionOutcome outcome, bool allAccredited, bool expectWin)
        {
            Assert.AreEqual(expectWin, InspectionRules.IsWin(outcome, new InspectionSnapshot { allAccredited = allAccredited }));
        }

        // ---------------------------------------------------------------- Upkeep

        [TestCase(0, 0, 1, 0)]
        [TestCase(2, 3, 1, 3)]   // staff 2 + supplies 1
        [TestCase(5, 4, 3, 7)]   // staff capped at 3 + supplies 2 + maintenance 2
        public void Upkeep_BillTotals(int patients, int treatments, int shift, int expectedTotal)
        {
            int total = 0;
            foreach (var b in UpkeepRules.BillsFor(patients, treatments, shift)) total += b.amount;
            Assert.AreEqual(expectedTotal, total);
        }

        [Test]
        public void Upkeep_PaysInPriorityOrderUntilMoneyRunsOut()
        {
            var bills = UpkeepRules.BillsFor(3, 4, 3); // staff 3, supplies 2, maintenance 2

            var unpaid = UpkeepRules.PayInOrder(bills, 4, out int spent);

            Assert.AreEqual(3, spent, "Staff (3) is paid; supplies (2) no longer fits in the remaining 1.");
            Assert.AreEqual(2, unpaid.Count);
            Assert.AreEqual(BillType.Supplies, unpaid[0].type);
            Assert.AreEqual(BillType.Maintenance, unpaid[1].type);
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
