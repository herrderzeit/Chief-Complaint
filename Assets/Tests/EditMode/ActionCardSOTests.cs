using ChiefComplaint.Cards.Data;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests
{
    /// <summary>Pure-maths tests for <see cref="ActionCardSO.CalculateDraftValue"/>.</summary>
    [TestFixture]
    [Category("Economy")]
    public class ActionCardSOTests
    {
        private ActionCardSO _card;

        [SetUp]
        public void SetUp() => _card = ScriptableObject.CreateInstance<ActionCardSO>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_card);

        // type, powerTier, apCost, cardsToDraw, expected
        [TestCase(ActionCardType.Staff,          1, 1, 0, 5,  Description = "Base 4 + tier 1")]
        [TestCase(ActionCardType.Staff,          1, 0, 0, 7,  Description = "Free card adds 2")]
        [TestCase(ActionCardType.Treatment,      2, 2, 0, 6,  Description = "Base 3 + 4 - 1 for 2+ AP")]
        [TestCase(ActionCardType.Utility,        3, 1, 1, 14, Description = "Base 3 + 9 + 2 for one card drawn")]
        [TestCase(ActionCardType.Diagnostic,     2, 1, 2, 11, Description = "Base 3 + 4 + 4 for two cards drawn")]
        [TestCase(ActionCardType.Administration, 1, 1, 0, 4,  Description = "Base 3 + tier 1")]
        [TestCase(ActionCardType.Supply,         1, 1, 0, 3,  Description = "Base 2 + tier 1")]
        [TestCase(ActionCardType.Supply,         1, 2, 0, 2,  Description = "Clamped to the minimum of 2")]
        public void CalculateDraftValue_MatchesFormula(ActionCardType type, int tier, int ap, int draw, int expected)
        {
            _card.cardType = type;
            _card.powerTier = tier;
            _card.apCost = ap;
            _card.cardsToDraw = draw;

            Assert.AreEqual(expected, _card.CalculateDraftValue());
        }

        // type, tier, AP, draw, exhausts, Reputation cost, success chance, expected
        [TestCase(ActionCardType.Staff,      1, 0, 0, true,  0, 1f,    5,  Description = "Agency Nurse: 4 + 1 + 2 - 2 for exhausting")]
        [TestCase(ActionCardType.Staff,      1, 0, 1, true,  0, 1f,    7,  Description = "Double Shift: 4 + 1 + 2 + 2 - 2")]
        [TestCase(ActionCardType.Staff,      3, 2, 0, false, 1, 1f,    10, Description = "4 + 9 - 1 - 2 per Reputation")]
        [TestCase(ActionCardType.Staff,      3, 2, 0, false, 2, 1f,    8,  Description = "Two Reputation take 4 off")]
        [TestCase(ActionCardType.Staff,      3, 2, 0, false, -1, 1f,   12, Description = "A negative Reputation cost is no cost")]
        [TestCase(ActionCardType.Diagnostic, 2, 0, 0, false, 0, 0.5f,  7,  Description = "Stat Labs: 3 + 4 + 2 - 2 for a failure chance")]
        [TestCase(ActionCardType.Diagnostic, 2, 0, 0, false, 0, 0.99f, 7,  Description = "Any failure chance takes 2 off")]
        [TestCase(ActionCardType.Diagnostic, 2, 0, 0, false, 0, 1f,    9,  Description = "A sure thing pays full price")]
        [TestCase(ActionCardType.Supply,     1, 1, 0, true,  0, 1f,    2,  Description = "Sharps Bin: 2 + 1 - 2 = 1, clamped to 2")]
        [TestCase(ActionCardType.Utility,    1, 2, 0, true,  3, 0.5f,  2,  Description = "Every discount at once still floors at 2")]
        [TestCase(ActionCardType.RedTape,    1, 0, 0, true,  3, 0.5f,  0,  Description = "Red Tape is always free")]
        public void CalculateDraftValue_DiscountsExhaustReputationCostAndFailureChance(
            ActionCardType type, int tier, int ap, int draw, bool exhausts, int reputationCost, float success, int expected)
        {
            _card.cardType = type;
            _card.powerTier = tier;
            _card.apCost = ap;
            _card.cardsToDraw = draw;
            _card.exhaustsOnUse = exhausts;
            _card.reputationCost = reputationCost;
            _card.successChance = success;

            Assert.AreEqual(expected, _card.CalculateDraftValue());
        }

        [TestCase(1, 0, 0)]
        [TestCase(3, 0, 2)]
        [TestCase(2, 5, 1)]
        public void CalculateDraftValue_RedTapeIsAlwaysFree(int tier, int ap, int draw)
        {
            _card.cardType = ActionCardType.RedTape;
            _card.powerTier = tier;
            _card.apCost = ap;
            _card.cardsToDraw = draw;

            Assert.AreEqual(0, _card.CalculateDraftValue());
        }

        [Test]
        public void CalculateDraftValue_NeverBelowTwoForPurchasableCards()
        {
            foreach (ActionCardType type in System.Enum.GetValues(typeof(ActionCardType)))
            {
                if (type == ActionCardType.RedTape) continue;
                _card.cardType = type;
                _card.powerTier = 1;
                _card.apCost = 5;
                _card.cardsToDraw = 0;

                Assert.GreaterOrEqual(_card.CalculateDraftValue(), 2, $"{type} fell below the minimum cost.");
            }
        }
    }
}
