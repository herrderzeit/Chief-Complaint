using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChiefComplaint.Sim.Bots;
using ChiefComplaint.Sim.Data;
using ChiefComplaint.Sim.Stats;
using Xunit;

namespace ChiefComplaint.Sim.Tests
{
    /// <summary>Loads the real Unity project this repo contains and checks the content reached the simulator intact.</summary>
    public class ProjectDataTests
    {
        private static readonly GameData Data = new ProjectLoader(ProjectLoader.FindProjectRoot(Directory.GetCurrentDirectory())).Load();

        [Fact]
        public void Loads_every_pool_and_deck_from_the_scene()
        {
            Assert.Equal(10, Data.StartingDeck.Count);
            Assert.NotEmpty(Data.MarketPool);
            Assert.NotEmpty(Data.RedTapePool);
            Assert.NotEmpty(Data.WalkInPool);
            Assert.NotEmpty(Data.AmbulancePool);
            Assert.NotEmpty(Data.TraumaFlightPool);
            Assert.Equal(4, Data.SymptomDecks.Count);
            Assert.All(Data.SymptomDecks.Values, d => Assert.NotEmpty(d));
        }

        [Fact]
        public void Scene_config_matches_the_decided_tuning()
        {
            // Pins the final balance pass read from MainBoard.unity's GameManager; change deliberately.
            Assert.Equal(7, Data.Config.StartingReputation);
            Assert.Equal(3, Data.Config.DiversionReputationPenalty);
            Assert.Equal(3, Data.Config.WalkInFundingReward);
            Assert.Equal(6, Data.Config.AmbulanceFundingReward);
            Assert.Equal(8, Data.Config.TraumaFlightFundingReward);
            Assert.Equal(3, Data.Config.ExtraPatientEveryNShifts);
            Assert.Equal(15, Data.Config.MaxReputation);
            Assert.Equal(2, Data.Config.CureReputationCap);
            Assert.Equal(10, Data.Config.StartingFunding);
        }

        [Fact]
        public void MaxReputation_default_matches_the_decided_tuning()
        {
            // The fallback used when the scene has no maxReputation must agree with GameManager's default.
            Assert.Equal(15, new GameConfig().MaxReputation);
        }

        [Fact]
        public void StartingFunding_default_matches_the_decided_tuning()
        {
            // The fallback used when the scene has no currentFunding must agree with GameManager's default.
            Assert.Equal(10, new GameConfig().StartingFunding);
        }

        [Fact]
        public void Escalation_and_diversion_fallbacks_agree_with_the_scene()
        {
            // GameConfig's fallbacks must match the game, so a scene without these fields still simulates the real rules.
            var defaults = new GameConfig();
            Assert.Equal(defaults.DiversionReputationPenalty, Data.Config.DiversionReputationPenalty);
            Assert.Equal(defaults.AmbulanceShiftThreshold, Data.Config.AmbulanceShiftThreshold);
            Assert.Equal(defaults.TraumaFlightShiftThreshold, Data.Config.TraumaFlightShiftThreshold);
        }

        // ------------------------------------------------------------------ patient pools

        private static List<PatientDef> Pool(EscalationType phase) => phase switch
        {
            EscalationType.Ambulance => Data.AmbulancePool,
            EscalationType.TraumaFlight => Data.TraumaFlightPool,
            _ => Data.WalkInPool,
        };

        [Theory]
        [InlineData(EscalationType.WalkIn, 8, 12, 1, 4, 5)]
        [InlineData(EscalationType.Ambulance, 6, 9, 2, 2, 3)]
        [InlineData(EscalationType.TraumaFlight, 7, 9, 3, 1, 2)]
        public void Patient_pool_matches_its_band(EscalationType phase, int minStab, int maxStab, int decay, int minTriage, int maxTriage)
        {
            List<PatientDef> pool = Pool(phase);
            Assert.NotEmpty(pool);
            foreach (PatientDef p in pool)
            {
                Assert.True(p.Phase == phase, $"{p.Name}: escalationPhase {p.Phase} in the {phase} pool");
                Assert.True(p.StartingStability >= minStab && p.StartingStability <= maxStab, $"{p.Name}: stability {p.StartingStability}");
                Assert.True(p.DecayPerTurn == decay, $"{p.Name}: decay {p.DecayPerTurn}");
                Assert.True(p.TriageLevel >= minTriage && p.TriageLevel <= maxTriage, $"{p.Name}: triage {p.TriageLevel}");
            }
        }

        [Theory]
        // Walk-In stability by triage: triage 4 (more urgent) arrives at exactly 8, triage 5 at 10-12.
        [InlineData(4, 8, 8)]
        [InlineData(5, 10, 12)]
        public void Walk_in_stability_matches_its_triage(int triage, int minStab, int maxStab)
        {
            var patients = Data.WalkInPool.Where(p => p.TriageLevel == triage).ToList();
            Assert.NotEmpty(patients);
            foreach (PatientDef p in patients)
                Assert.True(p.StartingStability >= minStab && p.StartingStability <= maxStab,
                    $"{p.Name} (triage {triage}): stability {p.StartingStability}, expected {minStab}-{maxStab}");
        }

        [Fact]
        public void Trauma_Flight_stability_matches_its_triage()
        {
            // Final balance pass: triage 1 arrives at 7 (John Jones alone at 9), triage 2 at 8.
            foreach (PatientDef p in Data.TraumaFlightPool)
            {
                int expected = p.TriageLevel == 2 ? 8 : p.Name == "John Jones" ? 9 : 7;
                Assert.True(p.StartingStability == expected,
                    $"{p.Name} (triage {p.TriageLevel}): stability {p.StartingStability}, expected {expected}");
            }
            Assert.Contains(Data.TraumaFlightPool, p => p.Name == "John Jones" && p.TriageLevel == 1);
        }

        [Theory]
        [InlineData("IV Saline", 1)]
        [InlineData("Fluid Resuscitation", 2)]
        public void Fluid_line_upgrades_accept_tokens_of_any_colour(string name, int tokenCost)
        {
            CardDef card = Card(name);
            Assert.True(card.UpgradeTokenAnyColour, $"{name} should take upgrade tokens of any colour.");
            Assert.Equal(tokenCost, card.UpgradeTokenCost);
        }

        [Fact]
        public void Triage_4_walk_ins_are_the_decided_five()
        {
            Assert.Equal(new[] { "John Garcia", "Joseph Wilson", "Marcus Gonzalez", "Richard Garcia", "Susan Martin" },
                Data.WalkInPool.Where(p => p.TriageLevel == 4).Select(p => p.Name).OrderBy(n => n).ToArray());
        }

        [Fact]
        public void Arthur_Martinez_is_a_triage_2_ambulance_with_handover()
        {
            PatientDef p = Data.AmbulancePool.Single(x => x.Name == "Arthur Martinez");
            Assert.Equal(2, p.TriageLevel);
            Assert.Equal(2, p.ReputationReward);
            Assert.True(p.ArrivesWithHandover);
            var death = p.Triggers.Single(t => t.Condition == TriggerCondition.OnDeath && t.Effect == TriggerEffect.LoseReputation);
            Assert.Equal(4, death.Value);
        }

        [Fact]
        public void Michael_Johnson_categories_are_internal_and_two_cardiopulmonary()
        {
            PatientDef p = Data.TraumaFlightPool.Single(x => x.Name == "Michael Johnson");
            Assert.Equal(new[] { SymptomCategory.Internal, SymptomCategory.Cardiopulmonary, SymptomCategory.Cardiopulmonary }, p.Categories);
        }

        /// <summary>
        /// Every patient's symptoms come from tag-matching cards: for each category the patient needs, its scene deck
        /// holds at least as many distinct symptoms sharing a clinical tag with the patient as the patient draws from
        /// that category (a repeated category needs a second, different card), so the draw never falls back.
        /// </summary>
        [Fact]
        public void Every_patient_has_enough_tag_matching_symptoms_and_never_falls_back()
        {
            var problems = new List<string>();
            foreach (PatientDef p in Data.WalkInPool.Concat(Data.AmbulancePool).Concat(Data.TraumaFlightPool))
            {
                if (p.Tags.Count == 0) { problems.Add($"{p.Name}: no clinical tags"); continue; }
                foreach (var group in p.Categories.GroupBy(c => c))
                {
                    int matching = Data.SymptomDecks[group.Key].Distinct().Count(s => s.Tags.Any(p.Tags.Contains));
                    if (matching < group.Count())
                        problems.Add($"{p.Name}: needs {group.Count()} {group.Key} symptom(s), {matching} tag-matching in the deck");
                }
            }
            Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        }

        [Theory]
        [InlineData(EscalationType.Ambulance, 3, 7)]
        [InlineData(EscalationType.Ambulance, 2, 8)]
        [InlineData(EscalationType.TraumaFlight, 1, 9)]
        [InlineData(EscalationType.TraumaFlight, 2, 6)]
        public void Pool_triage_counts_match_the_decided_roster(EscalationType phase, int triage, int count)
        {
            Assert.Equal(count, Pool(phase).Count(p => p.TriageLevel == triage));
        }

        [Fact]
        public void Handover_only_on_Arthur_Martinez_and_every_Trauma_Flight()
        {
            Assert.Equal(new[] { "Arthur Martinez" },
                Data.AmbulancePool.Where(p => p.ArrivesWithHandover).Select(p => p.Name).OrderBy(n => n).ToArray());
            Assert.False(Data.AmbulancePool.Single(p => p.Name == "Arthur Miller").ArrivesWithHandover, "Arthur Miller");
            Assert.All(Data.TraumaFlightPool, p => Assert.True(p.ArrivesWithHandover, p.Name));
            Assert.All(Data.WalkInPool, p => Assert.False(p.ArrivesWithHandover, p.Name));
        }

        [Fact]
        public void Death_cost_ladder_by_triage()
        {
            foreach (PatientDef p in Data.AmbulancePool.Concat(Data.TraumaFlightPool))
            {
                var deaths = p.Triggers.Where(t => t.Condition == TriggerCondition.OnDeath && t.Effect == TriggerEffect.LoseReputation).ToList();
                Assert.True(deaths.Count == 1, $"{p.Name}: {deaths.Count} OnDeath LoseReputation triggers");
                int expected = p.TriageLevel switch { 1 => 5, 2 => 4, 3 => 3, _ => -1 };
                Assert.True(deaths[0].Value == expected, $"{p.Name} (triage {p.TriageLevel}): death cost {deaths[0].Value}, expected {expected}");
                Assert.True(p.HasOwnDeathPenalty, p.Name);
            }
            Assert.All(Data.WalkInPool, p => Assert.False(p.HasOwnDeathPenalty, p.Name));
            Assert.All(Data.WalkInPool, p => Assert.DoesNotContain(p.Triggers,
                t => t.Condition == TriggerCondition.OnDeath && t.Effect == TriggerEffect.LoseReputation));
        }

        // ------------------------------------------------------------------ cards

        [Fact]
        public void Upgrades_cost_sixty_percent_of_the_target_and_the_source_tier_in_tokens()
        {
            var sources = Data.AllCards.Values.Where(c => c.Upgrade != null).ToList();
            Assert.NotEmpty(sources);
            foreach (CardDef c in sources)
            {
                int expected = (int)Math.Round(0.6m * c.Upgrade.MarketPrice, MidpointRounding.AwayFromZero);
                Assert.True(c.UpgradeCost == expected,
                    $"{c.Name}: upgrade cost {c.UpgradeCost}, expected {expected} (target {c.Upgrade.Name} price {c.Upgrade.MarketPrice})");
                Assert.True(c.UpgradeTokenCost == c.PowerTier, $"{c.Name}: token cost {c.UpgradeTokenCost}, tier {c.PowerTier}");
            }
        }

        private static CardDef Card(string name) => Data.AllCards.Values.First(c => c.Name == name);

        [Theory]
        [InlineData("Registered Nurse", 2, false)]
        [InlineData("Chief Nurse", 1, true)]
        [InlineData("Agency Nurse", 1, false)]
        public void Nurse_cards_heal_and_pause_without_a_choice(string name, int heal, bool wardWide)
        {
            CardDef card = Card(name);
            Assert.Equal(2, card.Effects.Count);
            Assert.DoesNotContain(card.Effects, e => e is ChoiceEffect);
            var inner = card.Effects.Select(e => wardWide ? Assert.IsType<WardWideEffect>(e).PatientEffect : e).ToList();
            Assert.Single(inner.OfType<PauseEffect>());
            Assert.Equal(heal, inner.OfType<HealEffect>().Single().Amount);
        }

        [Theory]
        // name, AP, tier (-1 = not pinned), reputation cost, exhausts, price (-1 = not pinned)
        [InlineData("Agency Nurse", 1, 1, 0, false, 5)]
        [InlineData("Double Shift", 0, 1, 0, true, 9)]
        [InlineData("Sharps Bin", 0, 1, 0, true, 5)]
        [InlineData("Ward Round", 0, 1, 0, false, 6)]
        [InlineData("Compliance Audit", 1, 1, 0, true, 2)]
        [InlineData("Shift Handover", 0, 1, 0, false, 10)]
        [InlineData("Damage Control Surgery", 1, 3, 0, true, 10)]
        [InlineData("Specialised Care", 1, -1, 0, false, 7)]
        [InlineData("Surgical Intervention", 2, -1, 0, false, 11)]
        public void Decided_card_stats(string name, int ap, int tier, int repCost, bool exhausts, int price)
        {
            CardDef card = Card(name);
            Assert.Equal(ap, card.ApCost);
            if (tier > 0) Assert.Equal(tier, card.PowerTier);
            Assert.Equal(repCost, card.ReputationCost);
            Assert.Equal(exhausts, card.ExhaustsOnUse);
            if (price >= 0) Assert.Equal(price, card.MarketPrice);
        }

        [Theory]
        // Supply Room and upgrade-only prices after the exhaust / Reputation cost / failure-chance discounts.
        [InlineData("Agency Nurse", 5)]
        [InlineData("Double Shift", 9)]
        [InlineData("Sharps Bin", 5)]
        [InlineData("Compliance Audit", 2)]
        [InlineData("Blood Bank", 4)]
        [InlineData("Grateful Patient", 6)]
        [InlineData("Trauma Team", 10)]
        [InlineData("Damage Control Surgery", 10)]
        [InlineData("Stat Labs", 7)]
        [InlineData("Shift Handover", 10)]
        [InlineData("Ward Round", 6)]
        [InlineData("Crash Cart", 11)]
        [InlineData("Philanthropist", 11)]
        public void Decided_prices(string name, int price)
        {
            Assert.Equal(price, Card(name).MarketPrice);
        }

        [Fact]
        public void Grateful_Patient_upgrades_to_Philanthropist_for_7()
        {
            CardDef grateful = Card("Grateful Patient");
            Assert.Equal("Philanthropist", grateful.Upgrade?.Name);
            Assert.Equal(7, grateful.UpgradeCost);
        }

        [Fact]
        public void Cards_without_an_upgrade_have_no_upgrade_cost()
        {
            foreach (CardDef c in Data.AllCards.Values.Where(c => c.Upgrade == null))
                Assert.True(c.UpgradeCost == 0, $"{c.Name}: upgrade cost {c.UpgradeCost} without an upgrade");
        }

        [Fact]
        public void Damage_Control_Surgery_pauses_then_cures_up_to_tier_3()
        {
            CardDef card = Card("Damage Control Surgery");
            Assert.Equal(2, card.Effects.Count);
            Assert.IsType<PauseEffect>(card.Effects[0]);
            Assert.Equal(3, Assert.IsType<CureEffect>(card.Effects[1]).MaxTier);
            Assert.True(card.ExhaustsOnUse);
        }

        [Theory]
        [InlineData("HIPAA Violation", 3)]
        [InlineData("Malpractice Threat", 2)]
        [InlineData("Mandatory Training", 1)]
        public void Reputation_red_tape_counts_its_loss_in_the_gate(string name, int loss)
        {
            CardDef card = Card(name);
            Assert.Equal(CardType.RedTape, card.Type);
            Assert.Equal(loss, card.TotalReputationCost);
        }

        [Fact]
        public void Double_Shift_Compliance_Audit_and_Sharps_Bin_effects()
        {
            Assert.Equal(3, Card("Double Shift").Effects.OfType<GainApEffect>().Single().Amount);
            Assert.Equal(2, Card("Double Shift").CardsToDraw);
            Assert.Equal(0, Card("Double Shift").ReputationCost);
            Assert.True(Card("Double Shift").ExhaustsOnUse);
            Assert.Equal(CardType.Staff, Card("Double Shift").Type);
            Assert.Equal(1, Card("Compliance Audit").Effects.OfType<CleanseRedTapeEffect>().Single().RepPerCard);
            Assert.Equal(CardType.Supply, Card("Sharps Bin").Type);
            Assert.True(Assert.Single(Card("Sharps Bin").Effects.OfType<TrashEffect>()).RedTapeOnly,
                "Sharps Bin may only dispose of Red Tape.");
            Assert.Equal(1, Card("Sharps Bin").CardsToDraw);
            Assert.True(Card("Sharps Bin").ExhaustsAtShiftEnd);
        }

        [Fact]
        public void Supply_room_piles_in_order_with_unlock_and_stock()
        {
            var expected = new (string card, EscalationType unlockAt, int maxStock)[]
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
            Assert.Equal(expected, Data.SupplyPiles.Select(p => (p.Card.Name, p.UnlockAt, p.MaxStock)).ToArray());
        }

        [Fact]
        public void Every_card_effect_is_understood()
        {
            Assert.DoesNotContain(Data.AllCards.Values.SelectMany(c => c.Effects), e => e is UnknownEffect);
            Assert.DoesNotContain(Data.Warnings, w => w.Contains("not simulated"));
        }

        [Fact]
        public void Starter_cards_load_with_their_effects_and_upgrades()
        {
            CardDef student = Data.StartingDeck.First(c => c.Name == "Medical Student");
            Assert.Equal(0, student.ApCost);
            Assert.Contains(CardTag.Doctor, student.Tags);
            Assert.Equal(1, student.Effects.OfType<GainApEffect>().Single().Amount);
            Assert.NotNull(student.Upgrade);

            CardDef assessment = Data.StartingDeck.First(c => c.Name == "Initial Assessment");
            Assert.Equal(1, assessment.Effects.OfType<DiagnoseEffect>().Single().MaxTier);
        }

        [Fact]
        public void Patients_have_categories_and_vitals()
        {
            foreach (PatientDef p in Data.WalkInPool.Concat(Data.AmbulancePool).Concat(Data.TraumaFlightPool))
            {
                Assert.InRange(p.Categories.Count, 1, 3);
                Assert.True(p.StartingStability > 0, p.Name);
                Assert.True(p.DecayPerTurn > 0, p.Name);
            }
        }

        [Fact]
        public void Runs_are_reproducible_from_the_seed()
        {
            RunRecord a = Runner.RunOne(Data, Data.Config, Program.Bot("smart"), 42);
            RunRecord b = Runner.RunOne(Data, Data.Config, Program.Bot("smart"), 42);
            Assert.Equal(a.Won, b.Won);
            Assert.Equal(a.Steps, b.Steps);
            Assert.Equal(a.Shifts.Select(s => s.Reputation), b.Shifts.Select(s => s.Reputation));
        }

        [Theory]
        [InlineData("random")]
        [InlineData("nobuy")]
        [InlineData("randombuy")]
        [InlineData("smart")]
        [InlineData("stall")]
        public void Every_bot_finishes_its_runs(string bot)
        {
            RunRecord[] runs = Runner.RunBatch(Data, Data.Config, Program.Bot(bot), 50, 1);
            Assert.All(runs, r => Assert.True(r.Won || r.LastShift <= Data.Config.ShiftsToWin));
            Assert.All(runs, r => Assert.True(r.Steps < Runner.MaxStepsPerShift * Data.Config.ShiftsToWin * 3));
        }
    }
}
