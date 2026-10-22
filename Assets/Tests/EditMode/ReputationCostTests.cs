using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Effects;
using ChiefComplaint.Cards.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// The all-or-nothing Reputation cost gate (<see cref="DraggableCard.CanAffordReputation"/>):
    /// a card with a cost can only be played while Reputation is strictly above it. The cost the gate checks is
    /// <see cref="ActionCardSO.TotalReputationCost"/>: the card's own cost plus Reputation its effects take.
    /// </summary>
    [TestFixture]
    [Category("Economy")]
    public class ReputationCostTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Teardown()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        [TestCase(0, 0, true, Description = "No cost is always affordable, even at 0 Reputation")]
        [TestCase(0, 10, true, Description = "No cost is always affordable")]
        [TestCase(-1, 0, true, Description = "A negative cost is treated as no cost")]
        [TestCase(2, 0, false, Description = "Cost 2 at 0 Reputation is refused")]
        [TestCase(2, 1, false, Description = "Cost 2 at 1 Reputation is refused")]
        [TestCase(2, 2, false, Description = "Paying would leave 0 Reputation and end the run")]
        [TestCase(2, 3, true, Description = "Cost 2 at 3 Reputation leaves 1")]
        [TestCase(2, 20, true)]
        [TestCase(3, 3, false, Description = "A Red Tape losing 3 at 3 Reputation would end the run")]
        [TestCase(3, 4, true, Description = "A Red Tape losing 3 at 4 Reputation leaves 1")]
        public void CanAffordReputation_RequiresReputationAboveCost(int cost, int reputation, bool expected)
        {
            Assert.AreEqual(expected, DraggableCard.CanAffordReputation(cost, reputation),
                $"cost {cost}, Reputation {reputation}");
        }

        private ActionCardSO Card(int reputationCost, params int[] reputationEffects)
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            card.reputationCost = reputationCost;
            card.effects = new List<CardEffectSO>();
            created.Add(card);
            foreach (int amount in reputationEffects)
            {
                var effect = ScriptableObject.CreateInstance<GainReputationEffectSO>();
                effect.reputationGenerated = amount;
                created.Add(effect);
                card.effects.Add(effect);
            }
            return card;
        }

        [TestCase(-3, 3)]
        [TestCase(-1, 1)]
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        public void GainReputationEffect_ReputationLossIsOnlyTheNegativePart(int generated, int expected)
        {
            var effect = ScriptableObject.CreateInstance<GainReputationEffectSO>();
            created.Add(effect);
            effect.reputationGenerated = generated;
            Assert.AreEqual(expected, effect.ReputationLoss);
        }

        [Test]
        public void CardEffect_DefaultReputationLossIsZero()
        {
            var effect = ScriptableObject.CreateInstance<GainAPEffectSO>();
            created.Add(effect);
            Assert.AreEqual(0, effect.ReputationLoss);
        }

        [Test]
        public void TotalReputationCost_RedTapeLosingThree_IsThree()
        {
            var card = Card(0, -3);
            card.cardType = ActionCardType.RedTape;

            Assert.AreEqual(3, card.TotalReputationCost);
            Assert.IsFalse(DraggableCard.CanAffordReputation(card.TotalReputationCost, 3), "Playing it at 3 Reputation would end the run.");
            Assert.IsTrue(DraggableCard.CanAffordReputation(card.TotalReputationCost, 4));
        }

        [TestCase(1, new[] { -2 }, 3, Description = "Own cost plus an effect loss")]
        [TestCase(2, new int[0], 2, Description = "Own cost only")]
        [TestCase(0, new[] { 2 }, 0, Description = "A Reputation gain costs nothing")]
        [TestCase(0, new[] { -1, -2 }, 3, Description = "Losses from several effects add up")]
        [TestCase(-1, new[] { -1 }, 1, Description = "A negative own cost counts as 0")]
        public void TotalReputationCost_AddsEffectLossesToTheCardCost(int reputationCost, int[] effects, int expected)
        {
            Assert.AreEqual(expected, Card(reputationCost, effects).TotalReputationCost);
        }

        [Test]
        public void TotalReputationCost_IgnoresMissingEffects()
        {
            var card = Card(1, -2);
            card.effects.Add(null);
            Assert.AreEqual(3, card.TotalReputationCost);
            card.effects = null;
            Assert.AreEqual(1, card.TotalReputationCost);
        }
    }
}
