using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers stability decay, healing, the heal event and diagnosis/treatment validation of <see cref="PatientCardView"/>.
    /// Lethal decay is deliberately not tested here: it destroys the card, which is not allowed in EditMode.
    /// </summary>
    public class PatientCardViewTests
    {
        private GameObject go;
        private PatientCardView view;
        private PatientCardSO patient;
        private readonly List<Object> created = new List<Object>();

        [SetUp]
        public void Setup()
        {
            go = new GameObject("PatientUnderTest");
            view = go.AddComponent<PatientCardView>();

            patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.startingStability = 10;
            patient.stabilityDecayPerTurn = 2;
            created.Add(patient);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(go);
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private SymptomCardSO NewSymptom(SymptomCategory category)
        {
            var s = ScriptableObject.CreateInstance<SymptomCardSO>();
            s.specialtyCategory = category;
            created.Add(s);
            return s;
        }

        private void Initialise(params SymptomTier[] tiers)
        {
            var symptoms = new List<SymptomCardSO>();
            var tierList = new List<SymptomTier>();
            foreach (var t in tiers)
            {
                symptoms.Add(NewSymptom(SymptomCategory.Internal));
                tierList.Add(t);
            }
            view.InitialiseCard(patient, symptoms, tierList, null);
        }

        [Test]
        public void InitialiseCard_SetsStabilityFromData()
        {
            Initialise(SymptomTier.Tier1);

            Assert.AreEqual(10, view.maxStability);
            Assert.AreEqual(10, view.currentStability);
            Assert.AreEqual(2, view.decayRate);
            Assert.IsFalse(view.isRemoved);
        }

        [Test]
        public void InitialiseCard_SortsSymptomsLowestTierFirst()
        {
            Initialise(SymptomTier.Tier3, SymptomTier.Tier1, SymptomTier.Tier2);

            CollectionAssert.AreEqual(
                new[] { SymptomTier.Tier1, SymptomTier.Tier2, SymptomTier.Tier3 },
                view.assignedTiers);
        }

        [Test]
        public void InitialiseCard_WithHandover_RevealsThePrimarySymptomFirst()
        {
            patient.arrivesWithHandover = true;
            Initialise(SymptomTier.Tier1, SymptomTier.Tier1, SymptomTier.Tier3);

            Assert.AreEqual(1, view.diagnosedCount);
            Assert.AreEqual(SymptomTier.Tier3, view.assignedTiers[0], "The primary symptom is handed over already diagnosed.");
            CollectionAssert.AreEqual(
                new[] { SymptomTier.Tier1, SymptomTier.Tier1 },
                view.assignedTiers.GetRange(1, 2),
                "The remaining hidden symptoms stay sorted by tier.");
        }

        [Test]
        public void InitialiseCard_WithoutHandover_StartsUndiagnosedAndSorted()
        {
            Initialise(SymptomTier.Tier3, SymptomTier.Tier1, SymptomTier.Tier1);

            Assert.AreEqual(0, view.diagnosedCount);
            CollectionAssert.AreEqual(
                new[] { SymptomTier.Tier1, SymptomTier.Tier1, SymptomTier.Tier3 },
                view.assignedTiers);
        }

        [Test]
        public void AdvanceShiftDecay_ReducesStabilityByDecayRate()
        {
            Initialise(SymptomTier.Tier1);

            view.AdvanceShiftDecay();

            Assert.AreEqual(8, view.currentStability);
        }

        [Test]
        public void AdvanceShiftDecay_InWard_HalvesDecayRoundedUp()
        {
            Initialise(SymptomTier.Tier1);
            var ward = new GameObject("Ward").transform;
            view.assignedWardSlot = ward;

            view.AdvanceShiftDecay(); // decay 2 -> ward decay 1

            Assert.AreEqual(9, view.currentStability);
            Object.DestroyImmediate(ward.gameObject);
        }

        [Test]
        public void AdvanceShiftDecay_PausedClock_SkipsDecayOnceThenResumes()
        {
            Initialise(SymptomTier.Tier1);
            view.PauseStabilityClock();

            view.AdvanceShiftDecay();
            Assert.AreEqual(10, view.currentStability, "A paused clock must skip decay.");
            Assert.IsFalse(view.stabilityClockPaused, "The pause is consumed after one shift.");

            view.AdvanceShiftDecay();
            Assert.AreEqual(8, view.currentStability);
        }

        [Test]
        public void InitialiseCard_ClearsPauseLeftOverFromPooledPatient()
        {
            Initialise(SymptomTier.Tier1);
            view.PauseStabilityClock();

            // The pooled card is reused for a new patient before the pause was consumed.
            Initialise(SymptomTier.Tier1);

            Assert.IsFalse(view.stabilityClockPaused, "A new patient must not inherit the previous patient's pause.");
        }

        [Test]
        public void ApplyStabilisation_ClampsToMaxStability()
        {
            Initialise(SymptomTier.Tier1);
            view.AdvanceShiftDecay(); // 8

            view.ApplyStabilisation(100);

            Assert.AreEqual(view.maxStability, view.currentStability);
        }

        [Test]
        public void ApplyStabilisation_RaisesOnStabilityChangedOnce()
        {
            Initialise(SymptomTier.Tier1);
            int raised = 0;
            PatientCardView sender = null;
            view.OnStabilityChanged += p => { raised++; sender = p; };

            view.ApplyStabilisation(1);

            Assert.AreEqual(1, raised);
            Assert.AreSame(view, sender);
        }

        [Test]
        public void IsFullyDiagnosed_FalseUntilEverySymptomRevealed()
        {
            Initialise(SymptomTier.Tier1, SymptomTier.Tier2);
            Assert.IsFalse(view.isFullyDiagnosed);

            view.diagnosedCount = 2;
            Assert.IsTrue(view.isFullyDiagnosed);
        }

        [Test]
        public void TryDiagnoseNext_WhenAlreadyFullyDiagnosed_ReturnsFalse()
        {
            Initialise(SymptomTier.Tier1);
            view.diagnosedCount = 1;

            Assert.IsFalse(view.TryDiagnoseNext(SymptomTier.Tier3));
            Assert.AreEqual(1, view.diagnosedCount);
        }

        [Test]
        public void TryDiagnoseNext_OutsideWard_ReturnsFalseWithoutRevealing()
        {
            Initialise(SymptomTier.Tier1);

            Assert.IsFalse(view.TryDiagnoseNext(SymptomTier.Tier3));
            Assert.AreEqual(0, view.diagnosedCount);
        }

        [Test]
        public void TryCureSymptom_WithoutParent_ReturnsFalse()
        {
            Initialise(SymptomTier.Tier1);

            Assert.IsFalse(view.TryCureSymptom(SymptomTier.Tier3));
        }
    }
}
