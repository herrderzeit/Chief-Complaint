using System.Collections.Generic;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Effects;
using ChiefComplaint.Cards.Views;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Drag preview: the play-zone text from <see cref="ActionCardSO.PlayZonePreview"/>, which patients a card's
    /// effects can target (<see cref="CardEffectSO.IsValidTarget"/>, <see cref="PatientCardView.CanCureWith"/>)
    /// and the targetable / hover outline on <see cref="PatientCardView"/>.
    /// </summary>
    [Category("DragPreview")]
    public class DragPreviewTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Teardown()
        {
            // Destroy in reverse so child GameObjects go before (or with) their parents without errors.
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        // ---------------------------------------------------------------------
        // Builders
        // ---------------------------------------------------------------------

        private T SO<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            created.Add(so);
            return so;
        }

        private GameObject GO(string name, string tag = null)
        {
            var go = new GameObject(name);
            if (tag != null) go.tag = tag;
            created.Add(go);
            return go;
        }

        private ActionCardSO Card(params CardEffectSO[] effects)
        {
            var card = SO<ActionCardSO>();
            card.effects = new List<CardEffectSO>(effects);
            return card;
        }

        private GainAPEffectSO GainAP(int n) { var e = SO<GainAPEffectSO>(); e.apGenerated = n; return e; }
        private StabilisationHealEffectSO Heal(int n) { var e = SO<StabilisationHealEffectSO>(); e.healAmount = n; return e; }
        private DrawCardsEffectSO Draw(int n) { var e = SO<DrawCardsEffectSO>(); e.cardsToDraw = n; return e; }
        private DiagnoseEffectSO Diagnose(SymptomTier t) { var e = SO<DiagnoseEffectSO>(); e.diagnosesSymptomTier = t; return e; }
        private CureSymptomEffectSO Cure(SymptomTier t) { var e = SO<CureSymptomEffectSO>(); e.curesSymptomTier = t; return e; }

        private ChoiceEffectSO Choice(CardEffectSO a, CardEffectSO b)
        {
            var e = SO<ChoiceEffectSO>();
            e.optionA = a;
            e.optionB = b;
            return e;
        }

        private WardWideEffectSO WardWide(CardEffectSO inner)
        {
            var e = SO<WardWideEffectSO>();
            e.patientEffect = inner;
            return e;
        }

        /// <summary>Builds a patient under <paramref name="parent"/> with one symptom per tier (all of <paramref name="category"/>).</summary>
        private PatientCardView Patient(GameObject parent, SymptomCategory category, params SymptomTier[] tiers)
        {
            var go = GO("Patient");
            if (parent != null) go.transform.SetParent(parent.transform);
            var view = go.AddComponent<PatientCardView>();

            var data = SO<PatientCardSO>();
            data.startingStability = 10;
            var symptoms = new List<SymptomCardSO>();
            for (int i = 0; i < tiers.Length; i++)
            {
                var s = SO<SymptomCardSO>();
                s.name = "Symptom" + i;
                s.specialtyCategory = category;
                symptoms.Add(s);
            }
            view.InitialiseCard(data, symptoms, new List<SymptomTier>(tiers), null);
            return view;
        }

        private PatientCardView WardPatient(params SymptomTier[] tiers) =>
            Patient(GO("Ward", "WardSlot"), SymptomCategory.Trauma, tiers);

        private (PatientCardView patient, DepartmentSlot dept) DeptPatient(SymptomCategory deptCategory, SymptomCategory symptomCategory, params SymptomTier[] tiers)
        {
            var deptGo = GO("Department");
            var dept = deptGo.AddComponent<DepartmentSlot>();
            dept.category = deptCategory;
            dept.isAvailable = true;
            var patient = Patient(deptGo, symptomCategory, tiers);
            return (patient, dept);
        }

        // ---------------------------------------------------------------------
        // 1. PlayZonePreview
        // ---------------------------------------------------------------------

        [Test]
        public void PlayZonePreview_MedicalStudent_ShowsAP()
        {
            Assert.AreEqual("+1 AP", Card(GainAP(1)).PlayZonePreview());
        }

        [Test]
        public void PlayZonePreview_DoubleShift_ShowsAPThenReputationCost()
        {
            var card = Card(GainAP(3));
            card.reputationCost = 2;
            Assert.AreEqual("+3 AP\n-2 Rep", card.PlayZonePreview());
        }

        [Test]
        public void PlayZonePreview_NursingStudentChoice_ShowsOnlyTheUntargetedOption()
        {
            Assert.AreEqual("+1 Card", Card(Choice(Heal(2), Draw(1))).PlayZonePreview(),
                "On the play zone only the draw option applies; the heal needs a patient.");
        }

        [Test]
        public void PlayZonePreview_ShiftHandover_ShowsCardsToDrawPlural()
        {
            var card = Card();
            card.cardsToDraw = 2;
            Assert.AreEqual("+2 Cards", card.PlayZonePreview());
        }

        [Test]
        public void PlayZonePreview_BloodBank_ShowsWardWideHealAndExhausts()
        {
            var card = Card(WardWide(Heal(3)));
            card.exhaustsOnUse = true;
            Assert.AreEqual("Ward: +3 Stability each\nExhausts", card.PlayZonePreview());
        }

        [Test]
        public void PlayZonePreview_StatLabs_PatientOnlyEffectWithChance_IsEmpty()
        {
            var card = Card(Diagnose(SymptomTier.Tier2));
            card.successChance = 0.5f;
            Assert.AreEqual(string.Empty, card.PlayZonePreview(),
                "The % chance line must not appear when the card gives nothing on the play zone.");
        }

        [Test]
        public void PlayZonePreview_InitialAssessment_DiagnoseOnly_IsEmpty()
        {
            Assert.AreEqual(string.Empty, Card(Diagnose(SymptomTier.Tier1)).PlayZonePreview());
        }

        [Test]
        public void PlayZonePreview_ExhaustsAloneWithNoOtherLines_IsEmpty()
        {
            var card = Card(Diagnose(SymptomTier.Tier1));
            card.exhaustsOnUse = true;
            Assert.AreEqual(string.Empty, card.PlayZonePreview(), "\"Exhausts\" is only added when the card shows something else.");
        }

        [Test]
        public void PlayZonePreview_ChanceLineAddedWhenThereAreLines()
        {
            var card = Card(GainAP(2));
            card.successChance = 0.75f;
            Assert.AreEqual("+2 AP\n75% chance", card.PlayZonePreview());
        }

        [Test]
        public void PlayZonePreview_NullEffectEntry_IsSkipped()
        {
            var card = Card(null, GainAP(1));
            Assert.AreEqual("+1 AP", card.PlayZonePreview());
        }

        [TestCase(1, "+1 Card")]
        [TestCase(3, "+3 Cards")]
        public void DrawCards_Describe_Pluralises(int n, string expected)
        {
            Assert.AreEqual(expected, Draw(n).Describe());
        }

        [TestCase(2, "+2 Rep")]
        [TestCase(0, "+0 Rep")]
        [TestCase(-2, "-2 Rep")]
        public void GainReputation_Describe_ShowsSign(int n, string expected)
        {
            var e = SO<GainReputationEffectSO>();
            e.reputationGenerated = n;
            Assert.AreEqual(expected, e.Describe());
        }

        [Test]
        public void Choice_Describe_JoinsBothOptions()
        {
            Assert.AreEqual("+2 Stability or +1 Card", Choice(Heal(2), Draw(1)).Describe());
        }

        // ---------------------------------------------------------------------
        // 2. IsValidTarget
        // ---------------------------------------------------------------------

        [Test]
        public void DiagnoseT1_ValidOnWardPatientWithTier1NextHidden()
        {
            var patient = WardPatient(SymptomTier.Tier1, SymptomTier.Tier2);
            Assert.IsTrue(Diagnose(SymptomTier.Tier1).IsValidTarget(patient));
        }

        [Test]
        public void DiagnoseT1_InvalidInTriage()
        {
            var patient = Patient(GO("TriageSlot"), SymptomCategory.Trauma, SymptomTier.Tier1);
            Assert.IsFalse(Diagnose(SymptomTier.Tier1).IsValidTarget(patient), "A parent not tagged WardSlot is not a Ward bed.");
        }

        [Test]
        public void DiagnoseT1_InvalidWithNoParent()
        {
            var patient = Patient(null, SymptomCategory.Trauma, SymptomTier.Tier1);
            Assert.IsFalse(Diagnose(SymptomTier.Tier1).IsValidTarget(patient));
        }

        [Test]
        public void DiagnoseT1_InvalidWhenFullyDiagnosed()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            patient.diagnosedCount = patient.assignedSymptoms.Count;
            Assert.IsTrue(patient.isFullyDiagnosed);
            Assert.IsFalse(Diagnose(SymptomTier.Tier1).IsValidTarget(patient));
        }

        [Test]
        public void DiagnoseT1_InvalidWhenNextHiddenTierIsHigher()
        {
            var patient = WardPatient(SymptomTier.Tier2, SymptomTier.Tier3);
            Assert.IsFalse(Diagnose(SymptomTier.Tier1).IsValidTarget(patient));
            Assert.IsTrue(Diagnose(SymptomTier.Tier2).IsValidTarget(patient), "Control: a Tier 2 diagnosis can reveal it.");
        }

        [Test]
        public void Diagnose_NullPatient_IsInvalid()
        {
            Assert.IsFalse(Diagnose(SymptomTier.Tier3).IsValidTarget(null));
        }

        [Test]
        public void Heal_InvalidAtFullStability_ValidWhenHurt()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            var heal = Heal(2);
            Assert.AreEqual(patient.maxStability, patient.currentStability);
            Assert.IsFalse(heal.IsValidTarget(patient), "Healing at full Stability would waste the card.");

            patient.currentStability = patient.maxStability - 1;
            Assert.IsTrue(heal.IsValidTarget(patient));
        }

        [Test]
        public void ChoiceHealOrDraw_ValidOnHurtPatient_InvalidAtFullStability()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            var choice = Choice(Heal(2), Draw(1));
            Assert.IsFalse(choice.IsValidTarget(patient), "Draw never targets a patient, and the heal would be wasted.");

            patient.currentStability = 4;
            Assert.IsTrue(choice.IsValidTarget(patient));
        }

        [Test]
        public void GainAP_NeverValidOnPatient()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            patient.currentStability = 1;
            Assert.IsFalse(GainAP(1).IsValidTarget(patient));
            Assert.IsFalse(GainAP(1).IsValidTarget(null));
        }

        [Test]
        public void CureEffect_IsValidTarget_FollowsCanCureWith()
        {
            var (patient, dept) = DeptPatient(SymptomCategory.Trauma, SymptomCategory.Trauma, SymptomTier.Tier2);
            patient.diagnosedCount = 1;
            Assert.IsTrue(Cure(SymptomTier.Tier2).IsValidTarget(patient));
            Assert.IsFalse(Cure(SymptomTier.Tier1).IsValidTarget(patient), "Tier 1 treatment can't cure a Tier 2 symptom.");
            dept.MarkAsUsed();
            Assert.IsFalse(Cure(SymptomTier.Tier3).IsValidTarget(patient));
        }

        // ---------------------------------------------------------------------
        // 3. CanCureWith
        // ---------------------------------------------------------------------

        [Test]
        public void CanCureWith_ValidWhenFullyDiagnosedInMatchingAvailableDepartment()
        {
            var (patient, _) = DeptPatient(SymptomCategory.Cardiopulmonary, SymptomCategory.Cardiopulmonary, SymptomTier.Tier1, SymptomTier.Tier2);
            patient.diagnosedCount = patient.assignedSymptoms.Count;
            Assert.IsTrue(patient.CanCureWith(SymptomTier.Tier2));
            Assert.IsTrue(patient.CanCureWith(SymptomTier.Tier1), "Any one symptom at or below the tier is enough.");
        }

        [Test]
        public void CanCureWith_InvalidWhenTierTooLow()
        {
            var (patient, _) = DeptPatient(SymptomCategory.Trauma, SymptomCategory.Trauma, SymptomTier.Tier3);
            patient.diagnosedCount = 1;
            Assert.IsFalse(patient.CanCureWith(SymptomTier.Tier2));
        }

        [Test]
        public void CanCureWith_InvalidWhenDepartmentUsed()
        {
            var (patient, dept) = DeptPatient(SymptomCategory.Trauma, SymptomCategory.Trauma, SymptomTier.Tier1);
            patient.diagnosedCount = 1;
            dept.MarkAsUsed();
            Assert.IsFalse(dept.isAvailable);
            Assert.IsFalse(patient.CanCureWith(SymptomTier.Tier3));
        }

        [Test]
        public void CanCureWith_InvalidWhenNotFullyDiagnosed()
        {
            var (patient, _) = DeptPatient(SymptomCategory.Trauma, SymptomCategory.Trauma, SymptomTier.Tier1, SymptomTier.Tier2);
            patient.diagnosedCount = 1;
            Assert.IsFalse(patient.CanCureWith(SymptomTier.Tier3));
        }

        [Test]
        public void CanCureWith_InvalidWhenCategoryDoesNotMatch()
        {
            var (patient, _) = DeptPatient(SymptomCategory.Infectious, SymptomCategory.Trauma, SymptomTier.Tier1);
            patient.diagnosedCount = 1;
            Assert.IsFalse(patient.CanCureWith(SymptomTier.Tier3));
        }

        [Test]
        public void CanCureWith_InvalidInSurgery()
        {
            var (patient, _) = DeptPatient(SymptomCategory.Trauma, SymptomCategory.Trauma, SymptomTier.Tier1);
            patient.diagnosedCount = 1;
            patient.isInSurgery = true;
            Assert.IsFalse(patient.CanCureWith(SymptomTier.Tier3));
        }

        [Test]
        public void CanCureWith_InvalidInWardWithoutDepartment()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            patient.diagnosedCount = 1;
            Assert.IsFalse(patient.CanCureWith(SymptomTier.Tier3));
        }

        // ---------------------------------------------------------------------
        // 4. Outline: targetable glow vs hover highlight
        // ---------------------------------------------------------------------

        [Test]
        public void Outline_TargetableAndHighlightShareTheOutline()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(patient.transform);
            var sr = outlineGo.AddComponent<SpriteRenderer>();
            var hoverColour = new Color(1f, 0.9f, 0.2f, 1f);
            sr.color = hoverColour;
            outlineGo.SetActive(false);
            patient.highlightOutline = outlineGo;
            patient.targetableOutlineColour = new Color(0.4f, 1f, 0.7f, 0.35f);

            patient.SetTargetable(true);
            Assert.IsTrue(outlineGo.activeSelf, "Targetable shows the outline.");
            Assert.AreEqual(patient.targetableOutlineColour, sr.color, "Targetable uses the soft targetable colour.");

            patient.SetHighlight(true);
            Assert.IsTrue(outlineGo.activeSelf);
            Assert.AreEqual(hoverColour, sr.color, "Hover wins and restores the outline's original colour.");

            patient.SetHighlight(false);
            Assert.IsTrue(outlineGo.activeSelf, "Still targetable after the hover ends.");
            Assert.AreEqual(patient.targetableOutlineColour, sr.color);

            patient.SetTargetable(false);
            Assert.IsFalse(outlineGo.activeSelf, "Neither hovered nor targetable hides the outline.");
        }

        [Test]
        public void Outline_HighlightAloneShowsOriginalColour()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(patient.transform);
            var sr = outlineGo.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            outlineGo.SetActive(false);
            patient.highlightOutline = outlineGo;

            patient.SetHighlight(true);
            Assert.IsTrue(outlineGo.activeSelf);
            Assert.AreEqual(Color.white, sr.color);
            patient.SetHighlight(false);
            Assert.IsFalse(outlineGo.activeSelf);
        }

        [Test]
        public void Outline_NoOutlineAssigned_DoesNotThrow()
        {
            var patient = WardPatient(SymptomTier.Tier1);
            patient.highlightOutline = null;
            Assert.DoesNotThrow(() => { patient.SetTargetable(true); patient.SetHighlight(true); patient.SetTargetable(false); });
        }
    }
}
