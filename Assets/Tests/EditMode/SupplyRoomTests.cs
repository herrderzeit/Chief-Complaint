using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.StaffRoom;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// Rules of the staff room's supply room (<see cref="SupplyRoom"/>): staged unlocks, restocking, the cart and committing purchases.
    /// Pile layout used by every test: 0 = Walk-In (max 3), 1 = Walk-In (max 2), 2 = Ambulance (max 3), 3 = Trauma (max 2).
    /// </summary>
    [TestFixture]
    [Category("StaffRoom")]
    public class SupplyRoomTests
    {
        private const int WalkInA = 0, WalkInB = 1, Ambulance = 2, Trauma = 3;

        private readonly List<ActionCardSO> _cards = new List<ActionCardSO>();
        private SupplyRoom _room;

        private ActionCardSO MakeCard(string name)
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            card.name = name;
            _cards.Add(card);
            return card;
        }

        private SupplyPileConfig Pile(string name, EscalationType unlockAt, int maxStock) =>
            new SupplyPileConfig { card = MakeCard(name), unlockAt = unlockAt, maxStock = maxStock };

        [SetUp]
        public void SetUp()
        {
            _room = new SupplyRoom(new[]
            {
                Pile("WalkInA", EscalationType.WalkIn, 3),
                Pile("WalkInB", EscalationType.WalkIn, 2),
                Pile("Ambulance", EscalationType.Ambulance, 3),
                Pile("Trauma", EscalationType.TraumaFlight, 2),
            });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var card in _cards) if (card != null) Object.DestroyImmediate(card);
            _cards.Clear();
        }

        // ---------------------------------------------------------------- unlocking

        [Test]
        public void NewRoom_AllPilesLockedAndEmpty()
        {
            Assert.AreEqual(4, _room.Piles.Count);
            foreach (var pile in _room.Piles)
            {
                Assert.IsFalse(pile.Unlocked, $"{pile.Card.name} should be locked before the first staff room visit");
                Assert.AreEqual(0, pile.Stock, $"{pile.Card.name} should hold no stock before the first staff room visit");
            }
        }

        [Test]
        public void FirstStaffRoom_WalkInPilesUnlockFull_LaterStagesStayLocked()
        {
            _room.Restock(EscalationType.WalkIn);

            Assert.IsTrue(_room.Piles[WalkInA].Unlocked);
            Assert.AreEqual(3, _room.Piles[WalkInA].Stock, "Walk-In pile A should start full");
            Assert.IsTrue(_room.Piles[WalkInB].Unlocked);
            Assert.AreEqual(2, _room.Piles[WalkInB].Stock, "Walk-In pile B should start full");

            Assert.IsFalse(_room.Piles[Ambulance].Unlocked, "Ambulance pile must stay locked at Walk-In");
            Assert.AreEqual(0, _room.Piles[Ambulance].Stock);
            Assert.IsFalse(_room.Piles[Trauma].Unlocked, "Trauma pile must stay locked at Walk-In");
            Assert.AreEqual(0, _room.Piles[Trauma].Stock);
        }

        [Test]
        public void ReachingAmbulance_UnlocksAmbulancePilesFull_TraumaStaysLocked()
        {
            _room.Restock(EscalationType.WalkIn);
            _room.Restock(EscalationType.Ambulance);

            Assert.IsTrue(_room.Piles[Ambulance].Unlocked);
            Assert.AreEqual(3, _room.Piles[Ambulance].Stock, "A newly unlocked pile should start full, not at +1");
            Assert.IsFalse(_room.Piles[Trauma].Unlocked, "Trauma pile must stay locked at Ambulance");
        }

        [Test]
        public void ReachingTrauma_DirectlyUnlocksEveryStage()
        {
            _room.Restock(EscalationType.TraumaFlight);

            foreach (var pile in _room.Piles)
            {
                Assert.IsTrue(pile.Unlocked, $"{pile.Card.name} should unlock at Trauma");
                Assert.AreEqual(pile.Config.maxStock, pile.Stock, $"{pile.Card.name} should start full");
            }
        }

        // ---------------------------------------------------------------- restocking

        // reserved before the next visit, expected stock after it (pile A, max 3)
        [TestCase(0, 3, Description = "Full pile stays at max")]
        [TestCase(1, 3, Description = "One short restocks to max")]
        [TestCase(2, 2, Description = "Two short restocks by only 1")]
        [TestCase(3, 1, Description = "Empty pile restocks by 1")]
        public void Restock_RestocksOpenPilesByOne_CappedAtMax(int bought, int expectedStock)
        {
            _room.Restock(EscalationType.WalkIn);
            for (int i = 0; i < bought; i++) Assert.IsTrue(_room.TryReserve(WalkInA));
            var purchases = new List<ActionCardSO>();
            _room.CommitCart(purchases);

            _room.Restock(EscalationType.WalkIn);

            Assert.AreEqual(expectedStock, _room.Piles[WalkInA].Stock);
        }

        [Test]
        public void Restock_RepeatedRestocksNeverExceedMax()
        {
            _room.Restock(EscalationType.WalkIn);
            for (int i = 0; i < 3; i++) _room.TryReserve(WalkInA);
            _room.CommitCart(new List<ActionCardSO>());

            for (int visit = 1; visit <= 5; visit++)
            {
                _room.Restock(EscalationType.WalkIn);
                Assert.AreEqual(Mathf.Min(3, visit), _room.Piles[WalkInA].Stock, $"Stock after restock #{visit}");
            }
        }

        [Test]
        public void Restock_LockedPilesDoNotRestock()
        {
            _room.Restock(EscalationType.WalkIn);
            _room.Restock(EscalationType.WalkIn);

            Assert.AreEqual(0, _room.Piles[Ambulance].Stock);
            Assert.AreEqual(0, _room.Piles[Trauma].Stock);
        }

        // ---------------------------------------------------------------- cart

        [Test]
        public void TryReserve_OpenPile_DecrementsStockAndIncrementsCart()
        {
            _room.Restock(EscalationType.WalkIn);

            Assert.IsTrue(_room.TryReserve(WalkInA));

            Assert.AreEqual(2, _room.Piles[WalkInA].Stock);
            Assert.AreEqual(1, _room.Piles[WalkInA].InCart);
            Assert.AreEqual(2, _room.Piles[WalkInB].Stock, "Other piles must not change");
            Assert.AreEqual(0, _room.Piles[WalkInB].InCart);
        }

        [TestCase(Ambulance)]
        [TestCase(Trauma)]
        public void TryReserve_LockedPile_Fails(int index)
        {
            _room.Restock(EscalationType.WalkIn);

            Assert.IsFalse(_room.TryReserve(index));
            Assert.AreEqual(0, _room.Piles[index].InCart);
            Assert.AreEqual(0, _room.Piles[index].Stock);
        }

        [Test]
        public void TryReserve_EmptyPile_Fails()
        {
            _room.Restock(EscalationType.WalkIn);
            Assert.IsTrue(_room.TryReserve(WalkInB));
            Assert.IsTrue(_room.TryReserve(WalkInB));

            Assert.IsFalse(_room.TryReserve(WalkInB), "A pile with no stock left must refuse");
            Assert.AreEqual(0, _room.Piles[WalkInB].Stock);
            Assert.AreEqual(2, _room.Piles[WalkInB].InCart);
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void TryReserveAndUnreserve_OutOfRangeIndex_Fail(int index)
        {
            _room.Restock(EscalationType.TraumaFlight);

            Assert.IsFalse(_room.TryReserve(index));
            Assert.IsFalse(_room.TryUnreserve(index));
        }

        [Test]
        public void TryUnreserve_ReturnsCopyToPile()
        {
            _room.Restock(EscalationType.WalkIn);
            _room.TryReserve(WalkInA);
            _room.TryReserve(WalkInA);

            Assert.IsTrue(_room.TryUnreserve(WalkInA));

            Assert.AreEqual(2, _room.Piles[WalkInA].Stock);
            Assert.AreEqual(1, _room.Piles[WalkInA].InCart);
        }

        [Test]
        public void TryUnreserve_EmptyCart_Fails()
        {
            _room.Restock(EscalationType.WalkIn);

            Assert.IsFalse(_room.TryUnreserve(WalkInA));
            Assert.AreEqual(3, _room.Piles[WalkInA].Stock, "A failed unreserve must not add stock above max");
            Assert.AreEqual(0, _room.Piles[WalkInA].InCart);
        }

        [Test]
        public void CommitCart_ReturnsOneEntryPerCopy_AndEmptiesCart()
        {
            _room.Restock(EscalationType.Ambulance);
            _room.TryReserve(WalkInA);
            _room.TryReserve(WalkInA);
            _room.TryReserve(Ambulance);

            var bought = new List<ActionCardSO>();
            _room.CommitCart(bought);

            Assert.AreEqual(3, bought.Count);
            Assert.AreEqual(2, bought.FindAll(c => c == _room.Piles[WalkInA].Card).Count);
            Assert.AreEqual(1, bought.FindAll(c => c == _room.Piles[Ambulance].Card).Count);
            foreach (var pile in _room.Piles)
                Assert.AreEqual(0, pile.InCart, $"{pile.Card.name} cart should be empty after commit");
            Assert.AreEqual(1, _room.Piles[WalkInA].Stock, "Committed copies leave the supply room");
            Assert.AreEqual(2, _room.Piles[Ambulance].Stock);
        }

        [Test]
        public void CommitCart_AfterCommit_CannotUnreserve()
        {
            _room.Restock(EscalationType.WalkIn);
            _room.TryReserve(WalkInA);
            _room.CommitCart(new List<ActionCardSO>());

            Assert.IsFalse(_room.TryUnreserve(WalkInA));
            Assert.AreEqual(2, _room.Piles[WalkInA].Stock);
        }

        [Test]
        public void Restock_ClearsLeftoverCart()
        {
            _room.Restock(EscalationType.WalkIn);
            _room.TryReserve(WalkInA);
            _room.TryReserve(WalkInB);

            _room.Restock(EscalationType.WalkIn);

            foreach (var pile in _room.Piles)
                Assert.AreEqual(0, pile.InCart, $"{pile.Card.name} cart should be cleared by Restock");
            var bought = new List<ActionCardSO>();
            _room.CommitCart(bought);
            Assert.IsEmpty(bought, "A cleared cart commits nothing");
        }

        [Test]
        public void Pile_Price_MatchesCalculateMarketPrice()
        {
            var pile = _room.Piles[WalkInA];
            pile.Card.cardType = ActionCardType.Staff;
            pile.Card.powerTier = 2;
            pile.Card.apCost = 1;

            Assert.AreEqual(pile.Card.CalculateMarketPrice(), pile.Price);
        }

        // ---------------------------------------------------------------- construction

        [Test]
        public void Constructor_NullConfigs_GivesEmptyRoom()
        {
            var room = new SupplyRoom(null);
            Assert.AreEqual(0, room.Piles.Count);
            Assert.DoesNotThrow(() => room.Restock(EscalationType.TraumaFlight));
        }

        [Test]
        public void Constructor_SkipsNullConfigsAndConfigsWithoutCard()
        {
            var kept = Pile("Kept", EscalationType.WalkIn, 2);
            var room = new SupplyRoom(new[]
            {
                null,
                new SupplyPileConfig { card = null, unlockAt = EscalationType.WalkIn, maxStock = 3 },
                kept,
            });

            Assert.AreEqual(1, room.Piles.Count);
            Assert.AreSame(kept, room.Piles[0].Config);
        }
    }
}
