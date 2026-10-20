using ChiefComplaint.Cards.Interaction;
using NUnit.Framework;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// The all-or-nothing Reputation cost gate (<see cref="DraggableCard.CanAffordReputation"/>):
    /// a card with a cost can only be played while Reputation is strictly above it.
    /// </summary>
    [TestFixture]
    [Category("Economy")]
    public class ReputationCostTests
    {
        [TestCase(0, 0, true, Description = "No cost is always affordable, even at 0 Reputation")]
        [TestCase(0, 10, true, Description = "No cost is always affordable")]
        [TestCase(-1, 0, true, Description = "A negative cost is treated as no cost")]
        [TestCase(2, 0, false, Description = "Cost 2 at 0 Reputation is refused")]
        [TestCase(2, 1, false, Description = "Cost 2 at 1 Reputation is refused")]
        [TestCase(2, 2, false, Description = "Paying would leave 0 Reputation and end the run")]
        [TestCase(2, 3, true, Description = "Cost 2 at 3 Reputation leaves 1")]
        [TestCase(2, 20, true)]
        public void CanAffordReputation_RequiresReputationAboveCost(int cost, int reputation, bool expected)
        {
            Assert.AreEqual(expected, DraggableCard.CanAffordReputation(cost, reputation),
                $"cost {cost}, Reputation {reputation}");
        }
    }
}
