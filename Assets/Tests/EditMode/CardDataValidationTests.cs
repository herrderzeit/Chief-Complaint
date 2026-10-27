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
        [TestCase("WalkIn", EscalationType.WalkIn, 8, 12, 1, 4, 5)]
        [TestCase("Ambulance", EscalationType.Ambulance, 6, 9, 2, 2, 3)]
        [TestCase("TraumaFlight", EscalationType.TraumaFlight, 7, 9, 3, 1, 2)]
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

        // Walk-In stability by triage: triage 4 arrives at exactly 8, triage 5 at 10-12.
        [TestCase(4, 8, 8)]
        [TestCase(5, 10, 12)]
        public void WalkIns_StabilityMatchesTriage(int triage, int minStab, int maxStab)
        {
            var patients = LoadPatients("WalkIn").Where(p => p.triageLevel == triage).ToArray();
            Assert.IsNotEmpty(patients, $"No triage-{triage} Walk-Ins found.");
            foreach (var p in patients)
                Assert.That(p.startingStability, Is.InRange(minStab, maxStab), $"{p.name} (triage {triage}): starting stability.");
        }

        [Test]
        public void TraumaFlights_StabilityMatchesTriage()
        {
            // Final balance pass: triage 1 arrives at 7 (John Jones alone at 9), triage 2 at 8.
            var flights = LoadPatients("TraumaFlight");
            Assert.IsNotEmpty(flights);
            foreach (var p in flights)
            {
                int expected = p.triageLevel == 2 ? 8 : p.patientName == "John Jones" ? 9 : 7;
                Assert.AreEqual(expected, p.startingStability, $"{p.name} (triage {p.triageLevel}): starting stability.");
            }
            Assert.IsTrue(flights.Any(p => p.patientName == "John Jones" && p.triageLevel == 1), "John Jones should be a triage-1 Trauma Flight.");
        }

        [Test]
        public void WalkIns_TriageFourAreTheDecidedFive()
        {
            var names = LoadPatients("WalkIn").Where(p => p.triageLevel == 4).Select(p => p.patientName).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(new[] { "John Garcia", "Joseph Wilson", "Marcus Gonzalez", "Richard Garcia", "Susan Martin" }, names);
        }

        // folder, triage, count
        [TestCase("Ambulance", 3, 7)]
        [TestCase("Ambulance", 2, 8)]
        [TestCase("TraumaFlight", 1, 9)]
        [TestCase("TraumaFlight", 2, 6)]
        public void PatientPool_TriageCountsMatchTheDecidedRoster(string folder, int triage, int count)
        {
            Assert.AreEqual(count, LoadPatients(folder).Count(p => p.triageLevel == triage), $"{folder}: triage-{triage} patients.");
        }

        [Test]
        public void ArthurMartinez_IsATriageTwoAmbulanceWithHandover()
        {
            var p = LoadPatients("Ambulance").Single(x => x.patientName == "Arthur Martinez");
            Assert.AreEqual(2, p.triageLevel);
            Assert.AreEqual(58, p.age);
            Assert.AreEqual(2, p.reputationReward);
            Assert.IsTrue(p.arrivesWithHandover, "Arthur Martinez keeps his paramedic handover.");
            var deaths = DeathReputationTriggers(p);
            Assert.AreEqual(1, deaths.Length);
            Assert.AreEqual(4, deaths[0].effectValue, "A triage-2 death costs 4.");
        }

        [Test]
        public void MichaelJohnson_CategoriesAreInternalThenTwoCardiopulmonary()
        {
            var p = LoadPatients("TraumaFlight").Single(x => x.patientName == "Michael Johnson");
            CollectionAssert.AreEqual(
                new[] { SymptomCategory.Internal, SymptomCategory.Cardiopulmonary, SymptomCategory.Cardiopulmonary },
                p.incomingCategories);
        }

        [Test]
        public void Handover_OnlyArthurMartinezAmongAmbulances()
        {
            var withHandover = LoadPatients("Ambulance").Where(p => p.arrivesWithHandover).Select(p => p.patientName).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(new[] { "Arthur Martinez" }, withHandover,
                "Exactly one Ambulance, Arthur Martinez, should arrive with a paramedic handover.");
        }

        [Test]
        public void ArthurMiller_NoLongerArrivesWithHandover()
        {
            var p = LoadPatients("Ambulance").Single(x => x.patientName == "Arthur Miller");
            Assert.IsFalse(p.arrivesWithHandover, "Arthur Miller lost his paramedic handover in the latest tuning.");
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

        /// <summary>Upgrade price rule: round-half-away-from-zero(0.6 × the target's market price).</summary>
        private static int ExpectedUpgradeCost(ActionCardSO target) =>
            (int)System.Math.Round(0.6m * target.CalculateMarketPrice(), System.MidpointRounding.AwayFromZero);

        [Test]
        public void Upgrades_CostSixtyPercentOfTargetAndSourceTierInTokens()
        {
            var sources = FindAssetPaths<ActionCardSO>()
                .Select(p => AssetDatabase.LoadAssetAtPath<ActionCardSO>(p))
                .Where(c => c.upgradesTo != null)
                .ToArray();
            Assert.IsNotEmpty(sources, "No cards with a upgradesTo found.");

            foreach (var card in sources)
            {
                Assert.AreEqual(ExpectedUpgradeCost(card.upgradesTo), card.upgradeCost,
                    $"{card.name}: upgradeCost should be round(0.6 × {card.upgradesTo.name} price {card.upgradesTo.CalculateMarketPrice()}).");
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
        [TestCase("Market/Card_AgencyNurse", 1, false)]
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
            var card = LoadCard("Market/Card_AgencyNurse");
            Assert.AreEqual(1, card.apCost, "Agency Nurse costs 1 AP.");
            Assert.AreEqual(1, card.powerTier);
            Assert.IsFalse(card.exhaustsOnUse, "Agency Nurse no longer exhausts.");
            StringAssert.DoesNotContain("Exhaust", card.rulesText, "Agency Nurse rules text should not mention exhausting.");
            Assert.AreEqual(5, card.CalculateMarketPrice(), "Agency Nurse price.");
        }

        [Test]
        public void DoubleShift_DecidedStats()
        {
            var card = LoadCard("Market/Card_DoubleShift");
            Assert.AreEqual(0, card.apCost);
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(ActionCardType.Staff, card.cardType);
            Assert.AreEqual(0, card.reputationCost, "Double Shift no longer costs Reputation.");
            Assert.AreEqual(2, card.cardsToDraw, "Double Shift draws 2 cards.");
            Assert.IsTrue(card.exhaustsOnUse, "Double Shift exhausts on use.");
            Assert.AreEqual(1, card.effects.Count);
            Assert.AreEqual(3, ((ChiefComplaint.Cards.Effects.GainAPEffectSO)card.effects[0]).apGenerated, "Double Shift gains 3 AP.");
            Assert.AreEqual("Gain 3 AP. Draw 2 cards. Exhausts.", card.rulesText);
            Assert.AreEqual(9, card.CalculateMarketPrice(), "Double Shift price.");
        }

        [Test]
        public void SharpsBin_DecidedStats()
        {
            var card = LoadCard("Market/Card_SharpsBin");
            Assert.AreEqual(ActionCardType.Supply, card.cardType);
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(0, card.apCost, "Sharps Bin is free to play.");
            Assert.AreEqual(1, card.cardsToDraw, "Sharps Bin draws 1 card.");
            Assert.IsTrue(card.exhaustsOnUse, "Sharps Bin exhausts on use.");
            Assert.IsTrue(card.exhaustsAtShiftEnd, "Sharps Bin leaves at the end of the shift if unused.");
            var trash = card.effects.OfType<ChiefComplaint.Cards.Effects.TrashCardEffectSO>().ToArray();
            Assert.AreEqual(1, trash.Length, "Sharps Bin has one Trash effect.");
            Assert.IsTrue(trash[0].redTapeOnly, "Sharps Bin may only dispose of Red Tape.");
            StringAssert.Contains("Red Tape", card.rulesText, "Sharps Bin rules text should mention Red Tape.");
            StringAssert.Contains("Draw 1", card.rulesText, "Sharps Bin rules text should mention drawing 1 card.");
            Assert.AreEqual(5, card.CalculateMarketPrice(), "Sharps Bin price.");
        }

        [Test]
        public void WardRound_DecidedStats()
        {
            var card = LoadCard("Market/Card_WardRound");
            Assert.AreEqual(0, card.apCost, "Ward Round is free to play.");
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(6, card.CalculateMarketPrice(), "Ward Round price.");
        }

        // card, upgrade token cost
        [TestCase("Starter/Card_IVSaline", 1)]
        [TestCase("Upgrade/Starter/Card_T2_FluidResuscitation", 2)]
        public void FluidLine_UpgradeTokensOfAnyColour(string path, int tokenCost)
        {
            var card = LoadCard(path);
            Assert.IsTrue(card.upgradeTokenAnyColour, $"{card.cardName}: should take upgrade tokens of any colour.");
            Assert.AreEqual(tokenCost, card.upgradeTokenCost, $"{card.cardName}: upgrade token cost.");
        }

        [Test]
        public void ComplianceAudit_DecidedStats()
        {
            var card = LoadCard("Market/Card_ComplianceAudit");
            Assert.AreEqual(1, card.powerTier);
            Assert.AreEqual(1, card.apCost);
            Assert.IsTrue(card.exhaustsOnUse, "Compliance Audit exhausts on use.");
            var cleanse = card.effects.OfType<ChiefComplaint.Cards.Effects.CleanseRedTapeEffectSO>().Single();
            Assert.AreEqual(1, cleanse.repBonusPerRedTapeCleansed, "Compliance Audit cleanse bonus.");
        }

        [Test]
        public void ShiftHandover_DecidedStats()
        {
            var card = LoadCard("Market/Card_ShiftHandover");
            Assert.AreEqual(0, card.apCost, "Shift Handover is free to play.");
            Assert.AreEqual(2, card.cardsToDraw);
        }

        [Test]
        public void DamageControlSurgery_PausesThenCuresUpToTierThree()
        {
            var card = LoadCard("Market/Card_DamageControlSurgery");
            Assert.AreEqual(2, card.effects.Count, "Damage Control Surgery: a pause and a cure.");
            Assert.IsInstanceOf<ChiefComplaint.Cards.Effects.PauseStabilityEffectSO>(card.effects[0], "The pause comes first.");
            var cure = card.effects[1] as ChiefComplaint.Cards.Effects.CureSymptomEffectSO;
            Assert.IsNotNull(cure, "The second effect cures a symptom.");
            Assert.AreEqual(SymptomTier.Tier3, cure.curesSymptomTier);
            Assert.IsTrue(card.exhaustsOnUse, "Damage Control Surgery exhausts.");
        }

        // Supply Room and upgrade-only prices after the exhaust / Reputation cost / failure-chance discounts.
        [TestCase("Market/Card_AgencyNurse", 5)]
        [TestCase("Market/Card_DoubleShift", 9)]
        [TestCase("Market/Card_SharpsBin", 5)]
        [TestCase("Market/Card_ComplianceAudit", 2)]
        [TestCase("Market/Card_BloodBank", 4)]
        [TestCase("Market/Card_GratefulPatient", 6)]
        [TestCase("Market/Card_TraumaTeam", 10)]
        [TestCase("Market/Card_DamageControlSurgery", 10)]
        [TestCase("Market/Card_StatLabs", 7)]
        [TestCase("Market/Card_ShiftHandover", 10)]
        [TestCase("Market/Card_WardRound", 6)]
        [TestCase("Market/Card_CrashCart", 11)]
        [TestCase("Upgrade/Market/Card_Philanthropist", 11)]
        public void DecidedPrices(string path, int price)
        {
            var card = LoadCard(path);
            Assert.AreEqual(price, card.CalculateMarketPrice(), $"{card.cardName}: price.");
        }

        [Test]
        public void GratefulPatient_UpgradesToPhilanthropistForSeven()
        {
            var card = LoadCard("Market/Card_GratefulPatient");
            Assert.IsNotNull(card.upgradesTo);
            Assert.AreEqual("Philanthropist", card.upgradesTo.cardName);
            Assert.AreEqual(7, card.upgradeCost);
        }

        [Test]
        public void Cards_WithoutAnUpgrade_HaveNoUpgradeCost()
        {
            var offenders = FindAssetPaths<ActionCardSO>()
                .Select(p => (path: p, card: AssetDatabase.LoadAssetAtPath<ActionCardSO>(p)))
                .Where(x => x.card.upgradesTo == null && x.card.upgradeCost != 0)
                .Select(x => $"{x.path} ({x.card.upgradeCost})")
                .ToArray();
            Assert.IsEmpty(offenders, "Cards without a upgradesTo must have upgradeCost 0:\n" + string.Join("\n", offenders));
        }

        [TestCase("RedTape/Card_HIPAAViolation", 3)]
        [TestCase("RedTape/Card_MalpracticeThreat", 2)]
        [TestCase("RedTape/Card_MandatoryTraining", 1)]
        public void ReputationRedTape_RulesTextAndGateAgree(string path, int loss)
        {
            var card = LoadCard(path);
            Assert.AreEqual(ActionCardType.RedTape, card.cardType);
            Assert.IsTrue(card.isPlayable, $"{card.cardName}: played to clear.");
            Assert.IsTrue(card.exhaustsOnUse, $"{card.cardName}: exhausts.");
            Assert.AreEqual(loss, card.TotalReputationCost, $"{card.cardName}: the Reputation gate must count the loss.");
            Assert.AreEqual($"Play to clear: lose {loss} Reputation. Exhausts.", card.rulesText);
        }

        [TestCase("Upgrade/Starter/Card_T2_SpecialisedCare", 1, 7)]
        [TestCase("Upgrade/Starter/Card_T3_SurgicalIntervention", 2, 11)]
        public void TreatmentUpgrades_DecidedApAndPrice(string path, int apCost, int price)
        {
            var card = LoadCard(path);
            Assert.AreEqual(apCost, card.apCost, $"{card.cardName}: AP cost.");
            Assert.AreEqual(price, card.CalculateMarketPrice(), $"{card.cardName}: price.");
        }

        [Test]
        public void SpecialisedCare_StillCostsSevenToUpgrade()
        {
            Assert.AreEqual(7, LoadCard("Upgrade/Starter/Card_T2_SpecialisedCare").upgradeCost);
        }

        // ------------------------------------------------------------------ supply room (MainBoard scene)

        private const string ScenePath = "Assets/Scenes/MainBoard.unity";

        private static List<ChiefComplaint.StaffRoom.SupplyPileConfig> SceneSupplyPiles()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var staffRoom = scene.GetRootGameObjects()
                                 .SelectMany(g => g.GetComponentsInChildren<ChiefComplaint.StaffRoom.StaffRoomManager>(true))
                                 .First();
                return new List<ChiefComplaint.StaffRoom.SupplyPileConfig>(staffRoom.supplyPiles);
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

        /// <summary>
        /// Every patient's symptoms come from tag-matching cards: for each category a patient needs, that category's
        /// scene deck holds at least as many distinct symptoms sharing a clinical tag with the patient as the patient
        /// draws from it (a repeated category needs a second, different card), so the draw never falls back.
        /// </summary>
        [Test]
        public void ScenePatients_HaveEnoughTagMatchingSymptomsAndNeverFallBack()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var pdm = scene.GetRootGameObjects()
                               .SelectMany(g => g.GetComponentsInChildren<ChiefComplaint.Core.PatientDeckManager>(true))
                               .First();
                var decks = new Dictionary<SymptomCategory, List<SymptomCardSO>>
                {
                    { SymptomCategory.Trauma, pdm.traumaDeck },
                    { SymptomCategory.Cardiopulmonary, pdm.cardiopulmonaryDeck },
                    { SymptomCategory.Infectious, pdm.infectiousDeck },
                    { SymptomCategory.Internal, pdm.internalDeck },
                };
                var patients = pdm.walkInPool.Concat(pdm.ambulancePool).Concat(pdm.traumaFlightPool).ToArray();
                Assert.IsNotEmpty(patients, "No patients in the scene's PatientDeckManager pools.");

                var problems = new List<string>();
                foreach (var p in patients)
                {
                    if (p == null) { problems.Add("A pool holds a missing patient."); continue; }
                    if (p.clinicalTags == null || p.clinicalTags.Count == 0) { problems.Add($"{p.patientName}: no clinical tags."); continue; }
                    foreach (var group in p.incomingCategories.GroupBy(c => c))
                    {
                        int matching = decks[group.Key]
                            .Where(s => s != null && s.clinicalTags != null)
                            .Distinct()
                            .Count(s => s.clinicalTags.Any(t => p.clinicalTags.Contains(t)));
                        if (matching < group.Count())
                            problems.Add($"{p.patientName}: needs {group.Count()} {group.Key} symptom(s), only {matching} tag-matching in the deck.");
                    }
                }
                Assert.IsEmpty(problems, "Patients whose symptom draw would fall back to an untagged card:\n" + string.Join("\n", problems));
            }
            finally
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
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
