using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ChiefComplaint.Cards.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// Validates the stored card assets under Assets/Data. Patient symptom counts are read from the
    /// raw asset text because <see cref="PatientCardSO"/>.OnValidate truncates the list in memory.
    /// </summary>
    [TestFixture]
    [Category("Data")]
    public class CardDataValidationTests
    {
        private const int MaxSymptomsPerPatient = 3;

        private static string[] FindAssetPaths<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { "Assets/Data" })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .ToArray();

        [Test]
        public void PatientAssets_AreFound()
        {
            Assert.IsNotEmpty(FindAssetPaths<PatientCardSO>(), "No PatientCardSO assets found under Assets/Data.");
        }

        [Test]
        public void PatientAssets_StoreNoMoreThanThreeSymptomCategories()
        {
            var offenders = FindAssetPaths<PatientCardSO>()
                .Select(path => (path, count: CountStoredCategories(path)))
                .Where(x => x.count > MaxSymptomsPerPatient)
                .Select(x => $"{x.path} ({x.count})")
                .ToArray();

            Assert.IsEmpty(offenders, "Patients over the " + MaxSymptomsPerPatient + "-symptom limit:\n" + string.Join("\n", offenders));
        }

        [Test]
        public void PatientAssets_HaveValidTriageAndStability()
        {
            foreach (var path in FindAssetPaths<PatientCardSO>())
            {
                var patient = AssetDatabase.LoadAssetAtPath<PatientCardSO>(path);
                Assert.That(patient.triageLevel, Is.InRange(1, 5), $"{path}: triage level out of range.");
                Assert.Greater(patient.startingStability, 0, $"{path}: starting stability must be positive.");
                Assert.IsNotEmpty(patient.patientName, $"{path}: missing patient name.");
            }
        }

        // ------------------------------------------------------------------ patient pools

        private const string PatientsDir = "Assets/Data/Patients/";

        private static PatientCardSO[] LoadPatients(string folder) =>
            AssetDatabase.FindAssets("t:PatientCardSO", new[] { PatientsDir + folder })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Select(p => AssetDatabase.LoadAssetAtPath<PatientCardSO>(p))
                         .ToArray();

        // folder, phase, stability min..max, decay, triage min..max
        [TestCase("WalkIn", EscalationType.WalkIn, 10, 12, 1, 4, 5)]
        [TestCase("Ambulance", EscalationType.Ambulance, 6, 9, 2, 2, 3)]
        [TestCase("TraumaFlight", EscalationType.TraumaFlight, 6, 8, 3, 1, 2)]
        public void PatientPool_MatchesItsBand(string folder, EscalationType phase, int minStab, int maxStab, int decay, int minTriage, int maxTriage)
        {
            var patients = LoadPatients(folder);
            Assert.IsNotEmpty(patients, $"No patients found under {PatientsDir}{folder}.");

            foreach (var p in patients)
            {
                Assert.AreEqual(phase, p.escalationPhase, $"{folder}/{p.name}: escalationPhase must match its folder.");
                Assert.That(p.startingStability, Is.InRange(minStab, maxStab), $"{folder}/{p.name}: starting stability outside {minStab}–{maxStab}.");
                Assert.AreEqual(decay, p.stabilityDecayPerTurn, $"{folder}/{p.name}: decay should be {decay}.");
                Assert.That(p.triageLevel, Is.InRange(minTriage, maxTriage), $"{folder}/{p.name}: triage outside {minTriage}–{maxTriage}.");
            }
        }

        [Test]
        public void Handover_OnlyArthurMartinezAndArthurMillerAmongAmbulances()
        {
            var withHandover = LoadPatients("Ambulance").Where(p => p.arrivesWithHandover).Select(p => p.patientName).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(new[] { "Arthur Martinez", "Arthur Miller" }, withHandover,
                "Exactly Arthur Martinez and Arthur Miller should arrive by Ambulance with a handover.");
        }

        [Test]
        public void Handover_EveryTraumaFlight_NoWalkIn()
        {
            var flights = LoadPatients("TraumaFlight");
            Assert.IsNotEmpty(flights);
            foreach (var p in flights)
                Assert.IsTrue(p.arrivesWithHandover, $"{p.name}: every Trauma Flight arrives with a paramedic handover.");

            foreach (var p in LoadPatients("WalkIn"))
                Assert.IsFalse(p.arrivesWithHandover, $"{p.name}: Walk-Ins never arrive with a handover.");
        }

        /// <summary>Death cost by triage for Ambulances and Trauma Flights: triage 1 = 5, 2 = 4, 3 = 3.</summary>
        private static int ExpectedDeathCost(int triage) => triage switch { 1 => 5, 2 => 4, 3 => 3, _ => -1 };

        private static PatientTrigger[] DeathReputationTriggers(PatientCardSO p) =>
            (p.triggers ?? new List<PatientTrigger>())
                .Where(t => t.condition == PatientTriggerCondition.OnDeath && t.effect == PatientTriggerEffect.LoseReputation)
                .ToArray();

        [TestCase("Ambulance")]
        [TestCase("TraumaFlight")]
        public void DeathCostLadder_OneTriggerValuedByTriage(string folder)
        {
            var patients = LoadPatients(folder);
            Assert.IsNotEmpty(patients);

            foreach (var p in patients)
            {
                var deaths = DeathReputationTriggers(p);
                Assert.AreEqual(1, deaths.Length, $"{folder}/{p.name}: needs exactly one OnDeath LoseReputation trigger.");
                Assert.Greater(deaths[0].effectValue, 0, $"{folder}/{p.name}: the death cost must be positive.");
                Assert.AreEqual(ExpectedDeathCost(p.triageLevel), deaths[0].effectValue,
                    $"{folder}/{p.name} (triage {p.triageLevel}): wrong death cost.");
                Assert.IsTrue(p.HasOwnDeathPenalty, $"{folder}/{p.name}: the trigger should replace the flat penalty.");
            }
        }

        [Test]
        public void DeathCostLadder_WalkInsUseTheFlatPenalty()
        {
            foreach (var p in LoadPatients("WalkIn"))
            {
                Assert.IsEmpty(DeathReputationTriggers(p), $"{p.name}: Walk-Ins have no OnDeath LoseReputation trigger.");
                Assert.IsFalse(p.HasOwnDeathPenalty, $"{p.name}: Walk-Ins pay the flat death penalty.");
            }
        }

        [Test]
        public void DeathCostLadder_EveryDeathTriggerInTheProjectIsPositive()
        {
            foreach (var path in FindAssetPaths<PatientCardSO>())
            {
                var p = AssetDatabase.LoadAssetAtPath<PatientCardSO>(path);
                foreach (var t in DeathReputationTriggers(p))
                    Assert.Greater(t.effectValue, 0, $"{path}: an OnDeath LoseReputation trigger of {t.effectValue} would make a death free.");
            }
        }

        // ------------------------------------------------------------------ action cards

        /// <summary>Upgrade price rule: round-half-away-from-zero(0.6 × the target's draft price).</summary>
        private static int ExpectedUpgradeCost(ActionCardSO target) =>
            (int)System.Math.Round(0.6m * target.CalculateDraftValue(), System.MidpointRounding.AwayFromZero);

        [Test]
        public void Upgrades_CostSixtyPercentOfTargetAndSourceTierInTokens()
        {
            var sources = FindAssetPaths<ActionCardSO>()
                .Select(p => AssetDatabase.LoadAssetAtPath<ActionCardSO>(p))
                .Where(c => c.directDraftUpgrade != null)
                .ToArray();
            Assert.IsNotEmpty(sources, "No cards with a directDraftUpgrade found.");

            foreach (var card in sources)
            {
                Assert.AreEqual(ExpectedUpgradeCost(card.directDraftUpgrade), card.draftUpgradeCost,
                    $"{card.name}: draftUpgradeCost should be round(0.6 × {card.directDraftUpgrade.name} price {card.directDraftUpgrade.CalculateDraftValue()}).");
                Assert.AreEqual(card.powerTier, card.upgradeTokenCost, $"{card.name}: upgradeTokenCost should equal its power tier.");
            }
        }

        private const string ActionsDir = "Assets/Data/Actions/";

        private static ActionCardSO LoadCard(string relativePath)
        {
            var card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(ActionsDir + relativePath + ".asset");
            Assert.IsNotNull(card, $"Missing card asset {relativePath}.");
            return card;
        }

        private static int HealOf(ChiefComplaint.Cards.Effects.CardEffectSO effect) =>
            effect is ChiefComplaint.Cards.Effects.StabilisationHealEffectSO heal ? heal.healAmount : -1;

        // card, heal amount, heal and pause are Ward-wide
        [TestCase("Upgrade/Starter/Card_T2_RegisteredNurse", 2, false)]
        [TestCase("Upgrade/Starter/Card_T3_ChiefNurse", 1, true)]
        [TestCase("Draft/Card_AgencyNurse", 1, false)]
        public void NurseCards_HealAndPause_NoChoice(string path, int heal, bool wardWide)
        {
            var card = LoadCard(path);
            Assert.AreEqual(2, card.effects.Count, $"{card.cardName}: exactly two effects (a heal and a pause).");
            Assert.IsFalse(card.effects.Any(e => e is ChiefComplaint.Cards.Effects.ChoiceEffectSO), $"{card.cardName}: no ChoiceEffect.");

            var inner = card.effects.Select(e =>
            {
                if (!wardWide) return e;
                Assert.IsInstanceOf<ChiefComplaint.Cards.Effects.WardWideEffectSO>(e, $"{card.cardName}: every effect should be Ward-wide.");
                return ((ChiefComplaint.Cards.Effects.WardWideEffectSO)e).patientEffect;
            }).ToArray();

            Assert.AreEqual(1, inner.Count(e => e is ChiefComplaint.Cards.Effects.PauseStabilityEffectSO), $"{card.cardName}: one pause.");
            var heals = inner.OfType<ChiefComplaint.Cards.Effects.StabilisationHealEffectSO>().ToArray();
            Assert.AreEqual(1, heals.Length, $"{card.cardName}: one heal.");
            Assert.AreEqual(heal, HealOf(heals[0]), $"{card.cardName}: heal amount.");
        }

        [Test]
        public void AgencyNurse_DecidedStats()
        {
            var card = LoadCard("Draft/Card_AgencyNurse");
            Assert.AreEqual(0, card.apCost);
            Assert.AreEqual(1, card.powerTier);
            Assert.IsTrue(card.exhaustsOnUse, "Agency Nurse exhausts on use.");
            Assert.AreEqual(7, card.CalculateDraftValue(), "Agency Nurse price.");
        }

        [Test]
        public void DoubleShift_DecidedStats()
        {
            var card = LoadCard("Draft/Card_DoubleShift");
            Assert.AreEqual(0, card.apCost);
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(ActionCardType.Staff, card.cardType);
            Assert.AreEqual(0, card.reputationCost, "Double Shift no longer costs Reputation.");
            Assert.AreEqual(1, card.cardsToDraw, "Double Shift draws 1 card.");
            Assert.IsTrue(card.exhaustsOnUse, "Double Shift exhausts on use.");
            Assert.AreEqual(1, card.effects.Count);
            Assert.AreEqual(3, ((ChiefComplaint.Cards.Effects.GainAPEffectSO)card.effects[0]).apGenerated, "Double Shift gains 3 AP.");
            Assert.AreEqual("Gain 3 AP. Draw 1 card. Exhausts.", card.rulesText);
            Assert.AreEqual(9, card.CalculateDraftValue(), "Double Shift price.");
        }

        [Test]
        public void SharpsBin_DecidedStats()
        {
            var card = LoadCard("Draft/Card_SharpsBin");
            Assert.AreEqual(ActionCardType.Supply, card.cardType);
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(1, card.apCost);
            Assert.IsTrue(card.exhaustsOnUse, "Sharps Bin exhausts on use.");
            Assert.IsTrue(card.effects.Any(e => e is ChiefComplaint.Cards.Effects.TrashCardEffectSO), "Sharps Bin trashes a card.");
        }

        [Test]
        public void WardRound_DecidedStats()
        {
            var card = LoadCard("Draft/Card_WardRound");
            Assert.AreEqual(1, card.apCost);
            Assert.AreEqual(2, card.powerTier);
        }

        [Test]
        public void ComplianceAudit_DecidedStats()
        {
            var card = LoadCard("Draft/Card_ComplianceAudit");
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(1, card.apCost);
            Assert.IsTrue(card.exhaustsOnUse, "Compliance Audit exhausts on use.");
            var cleanse = card.effects.OfType<ChiefComplaint.Cards.Effects.CleanseRedTapeEffectSO>().Single();
            Assert.AreEqual(1, cleanse.repBonusPerRedTapeCleansed, "Compliance Audit cleanse bonus.");
        }

        [TestCase("Upgrade/Starter/Card_T2_SpecialisedCare", 1, 7)]
        [TestCase("Upgrade/Starter/Card_T3_SurgicalIntervention", 2, 11)]
        public void TreatmentUpgrades_DecidedApAndPrice(string path, int apCost, int price)
        {
            var card = LoadCard(path);
            Assert.AreEqual(apCost, card.apCost, $"{card.cardName}: AP cost.");
            Assert.AreEqual(price, card.CalculateDraftValue(), $"{card.cardName}: price.");
        }

        [Test]
        public void SpecialisedCare_StillCostsSevenToUpgrade()
        {
            Assert.AreEqual(7, LoadCard("Upgrade/Starter/Card_T2_SpecialisedCare").draftUpgradeCost);
        }

        // ------------------------------------------------------------------ supply room (MainBoard scene)

        private const string ScenePath = "Assets/Scenes/MainBoard.unity";

        private static List<ChiefComplaint.Drafting.SupplyPileConfig> SceneSupplyPiles()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var draft = scene.GetRootGameObjects()
                                 .SelectMany(g => g.GetComponentsInChildren<ChiefComplaint.Drafting.DraftManager>(true))
                                 .First();
                return new List<ChiefComplaint.Drafting.SupplyPileConfig>(draft.supplyPiles);
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static readonly (string card, EscalationType unlockAt, int maxStock)[] ExpectedPiles =
        {
            ("Sharps Bin", EscalationType.WalkIn, 2),
            ("Agency Nurse", EscalationType.WalkIn, 4),
            ("Shift Handover", EscalationType.WalkIn, 3),
            ("Ward Round", EscalationType.WalkIn, 2),
            ("Stat Labs", EscalationType.Ambulance, 3),
            ("Grateful Patient", EscalationType.Ambulance, 2),
            ("Compliance Audit", EscalationType.Ambulance, 2),
            ("Blood Bank", EscalationType.Ambulance, 3),
            ("Double Shift", EscalationType.Ambulance, 3),
            ("Crash Cart", EscalationType.TraumaFlight, 2),
            ("Trauma Team", EscalationType.TraumaFlight, 2),
            ("Damage Control Surgery", EscalationType.TraumaFlight, 2),
        };

        [Test]
        public void SupplyRoom_ScenePilesInOrderWithUnlockAndStock()
        {
            var piles = SceneSupplyPiles();
            Assert.AreEqual(ExpectedPiles.Length, piles.Count, "Supply Room pile count.");
            for (int i = 0; i < ExpectedPiles.Length; i++)
            {
                var (card, unlockAt, maxStock) = ExpectedPiles[i];
                Assert.IsNotNull(piles[i]?.card, $"Pile {i}: missing card.");
                Assert.AreEqual(card, piles[i].card.cardName, $"Pile {i}: card.");
                Assert.AreEqual(unlockAt, piles[i].unlockAt, $"Pile {i} ({card}): unlock stage.");
                Assert.AreEqual(maxStock, piles[i].maxStock, $"Pile {i} ({card}): max stock.");
            }
        }

        [Test]
        public void SymptomAssets_HaveNamesForEveryTier()
        {
            foreach (var path in FindAssetPaths<SymptomCardSO>())
            {
                var symptom = AssetDatabase.LoadAssetAtPath<SymptomCardSO>(path);
                Assert.IsNotEmpty(symptom.tier1Name, $"{path}: missing tier 1 name.");
                Assert.IsNotEmpty(symptom.tier2Name, $"{path}: missing tier 2 name.");
                Assert.IsNotEmpty(symptom.tier3Name, $"{path}: missing tier 3 name.");
            }
        }

        [Test]
        public void ActionAssets_HaveSaneCostsAndTiers()
        {
            foreach (var path in FindAssetPaths<ActionCardSO>())
            {
                var card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(path);
                Assert.That(card.apCost, Is.InRange(0, 5), $"{path}: AP cost out of range.");
                Assert.That(card.powerTier, Is.InRange(1, 3), $"{path}: power tier out of range.");
                Assert.That(card.successChance, Is.InRange(0f, 1f), $"{path}: success chance out of range.");
            }
        }

        [Test]
        public void PatientAssets_CategoryCountParsesNonZero()
        {
            // Guards the raw-YAML parser itself: if Unity changes how it serialises the enum list again,
            // every patient would silently count 0 and the symptom-limit test above could never fail.
            var outOfRange = FindAssetPaths<PatientCardSO>()
                .Select(path => (path, count: CountStoredCategories(path)))
                .Where(x => x.count < 1 || x.count > MaxSymptomsPerPatient)
                .Select(x => $"{x.path} ({x.count})")
                .ToArray();

            Assert.IsEmpty(outOfRange,
                "Patients whose stored incomingCategories did not parse to 1–" + MaxSymptomsPerPatient +
                " entries (a 0 usually means the asset format changed and CountStoredCategories needs updating):\n" +
                string.Join("\n", outOfRange));
        }

        // Unity packs an enum list as 8 hex digits per entry; since Unity 6.6 it appends a trailing 'i'.
        private static readonly Regex PackedHexList = new Regex("^((?:[0-9a-fA-F]{8})+)i?$");

        /// <summary>
        /// Counts the entries stored under <c>incomingCategories:</c> in the raw YAML. Unity writes an
        /// enum list as one packed hex string (8 hex characters per entry, optionally followed by
        /// <c>i</c>), but it can also write a block list or an empty <c>[]</c>, so all three forms are handled.
        /// </summary>
        private static int CountStoredCategories(string assetPath)
        {
            var lines = File.ReadAllLines(assetPath);
            int start = System.Array.FindIndex(lines, l => l.TrimStart().StartsWith("incomingCategories:"));
            if (start < 0) return 0;

            string inline = lines[start].Substring(lines[start].IndexOf(':') + 1).Trim();
            if (inline == "[]") return 0;
            Match packed = PackedHexList.Match(inline);
            if (packed.Success) return packed.Groups[1].Length / 8;

            int count = 0;
            for (int i = start + 1; i < lines.Length && lines[i].TrimStart().StartsWith("- "); i++)
                count++;
            return count;
        }
    }
}
