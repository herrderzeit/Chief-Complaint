using System.Collections.Generic;
using System.Reflection;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.StaffRoom;
using ChiefComplaint.Utils;
using NUnit.Framework;
using UnityEngine;

namespace ChiefComplaint.Tests.EditMode
{
    /// <summary>
    /// Covers symptom upgrade tokens: the <see cref="UpgradeTokenWallet"/> rules, paying for an upgrade through
    /// <see cref="GameManager"/>, the Upgrades tab cost text, and earning a token when a symptom is treated.
    /// </summary>
    [Category("Economy")]
    public class UpgradeTokenTests
    {
        private const SymptomCategory Trauma = SymptomCategory.Trauma;
        private const SymptomCategory Cardio = SymptomCategory.Cardiopulmonary;
        private const SymptomCategory Infectious = SymptomCategory.Infectious;
        private const SymptomCategory Internal = SymptomCategory.Internal;

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void Teardown()
        {
            // Restore the singleton before destroying our GameManager, so its OnDestroy cannot clear a real instance.
            RestoreInstance();
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        private static int[] Counts(UpgradeTokenWallet w) => new[] { w.Get(Trauma), w.Get(Cardio), w.Get(Infectious), w.Get(Internal) };

        private static UpgradeTokenWallet Wallet(int trauma, int cardio, int infectious, int internalMed)
        {
            var w = new UpgradeTokenWallet();
            w.Add(Trauma, trauma);
            w.Add(Cardio, cardio);
            w.Add(Infectious, infectious);
            w.Add(Internal, internalMed);
            return w;
        }

        private ActionCardSO NewCard(int fundingCost, int tokenCost, bool anyColour = false, SymptomCategory colour = Trauma, bool withUpgrade = true)
        {
            var card = ScriptableObject.CreateInstance<ActionCardSO>();
            created.Add(card);
            card.upgradeCost = fundingCost;
            card.upgradeTokenCost = tokenCost;
            card.upgradeTokenAnyColour = anyColour;
            card.upgradeTokenColour = colour;
            if (withUpgrade)
            {
                var upgraded = ScriptableObject.CreateInstance<ActionCardSO>();
                created.Add(upgraded);
                card.upgradesTo = upgraded;
            }
            return card;
        }

        // ---------------------------------------------------------------------
        // UpgradeTokenWallet
        // ---------------------------------------------------------------------

        [Test]
        public void NewWallet_IsEmpty()
        {
            var w = new UpgradeTokenWallet();
            CollectionAssert.AreEqual(new[] { 0, 0, 0, 0 }, Counts(w));
            Assert.AreEqual(0, w.Total);
        }

        [Test]
        public void Add_DefaultsToOneAndKeepsColoursSeparate()
        {
            var w = new UpgradeTokenWallet();
            w.Add(Cardio);
            w.Add(Internal, 3);

            CollectionAssert.AreEqual(new[] { 0, 1, 0, 3 }, Counts(w));
            Assert.AreEqual(4, w.Total);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-5)]
        public void Add_IgnoresZeroAndNegativeAmounts(int amount)
        {
            var w = Wallet(2, 0, 0, 0);
            w.Add(Trauma, amount);
            Assert.AreEqual(2, w.Get(Trauma), "Adding a non-positive amount must never remove tokens.");
        }

        [TestCase(0, false, true)]
        [TestCase(-1, false, true)]
        [TestCase(0, true, true)]
        [TestCase(1, false, true)]   // holds 2 Trauma
        [TestCase(2, false, true)]
        [TestCase(3, false, false)]
        [TestCase(5, true, true)]    // holds 5 in total
        [TestCase(6, true, false)]
        public void CanPay_Boundaries(int cost, bool anyColour, bool expected)
        {
            var w = Wallet(2, 1, 0, 2);
            Assert.AreEqual(expected, w.CanPay(cost, anyColour, Trauma));
        }

        [Test]
        public void CanPay_ZeroCostFromEmptyWallet_IsAlwaysTrue()
        {
            Assert.IsTrue(new UpgradeTokenWallet().CanPay(0, false, Infectious));
        }

        [Test]
        public void CanPay_ExactColourIgnoresOtherColours()
        {
            var w = Wallet(0, 9, 9, 9);
            Assert.IsFalse(w.CanPay(1, false, Trauma), "Other colours must not pay a single-colour cost.");
            Assert.IsTrue(w.CanPay(1, true, Trauma), "Any colour may pay an any-colour cost.");
        }

        [Test]
        public void TryPay_ExactColour_DeductsOnlyThatColour()
        {
            var w = Wallet(3, 2, 1, 4);
            Assert.IsTrue(w.TryPay(2, false, Internal));
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 2 }, Counts(w));
        }

        [Test]
        public void TryPay_ShortOfColour_ReturnsFalseAndDeductsNothing()
        {
            var w = Wallet(1, 5, 0, 0);
            Assert.IsFalse(w.TryPay(2, false, Trauma));
            CollectionAssert.AreEqual(new[] { 1, 5, 0, 0 }, Counts(w));
        }

        [Test]
        public void TryPay_AnyColourShortInTotal_ReturnsFalseAndDeductsNothing()
        {
            var w = Wallet(1, 0, 0, 0);
            Assert.IsFalse(w.TryPay(2, true, Trauma));
            CollectionAssert.AreEqual(new[] { 1, 0, 0, 0 }, Counts(w));
        }

        [Test]
        public void TryPay_ZeroCost_ChangesNothing()
        {
            var w = Wallet(1, 1, 1, 1);
            Assert.IsTrue(w.TryPay(0, true, Trauma));
            Assert.IsTrue(w.TryPay(0, false, Trauma));
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, Counts(w));
        }

        [Test]
        public void TryPay_AnyColour_DrainsTheLargestPile()
        {
            var w = Wallet(1, 4, 0, 2);
            Assert.IsTrue(w.TryPay(2, true, Trauma));
            CollectionAssert.AreEqual(new[] { 1, 2, 0, 2 }, Counts(w), "Both tokens should come from Cardiology, the largest pile.");
        }

        [Test]
        public void TryPay_AnyColour_RechecksTheLargestPileForEachToken()
        {
            var w = Wallet(0, 3, 0, 2);
            Assert.IsTrue(w.TryPay(2, true, Trauma));
            // Cardio 3 → 2, then a 2/2 tie with Internal goes to the lower enum value (Cardio).
            CollectionAssert.AreEqual(new[] { 0, 1, 0, 2 }, Counts(w));
        }

        [Test]
        public void TryPay_AnyColour_TieGoesToLowestEnumValue()
        {
            var w = Wallet(0, 2, 2, 2);
            Assert.IsTrue(w.TryPay(1, true, Internal), "The colour parameter is ignored for any-colour costs.");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 2 }, Counts(w));
        }

        [Test]
        public void TryPay_AnyColour_CanDrainEveryPile()
        {
            var w = Wallet(1, 1, 1, 1);
            Assert.IsTrue(w.TryPay(4, true, Trauma));
            Assert.AreEqual(0, w.Total);
        }

        [TestCase(SymptomCategory.Trauma, "Trauma")]
        [TestCase(SymptomCategory.Cardiopulmonary, "Cardiology")]
        [TestCase(SymptomCategory.Infectious, "Isolation")]
        [TestCase(SymptomCategory.Internal, "Internal Med")]
        public void ColourName_MatchesDepartment(SymptomCategory colour, string expected)
        {
            Assert.AreEqual(expected, UpgradeTokenWallet.ColourName(colour));
        }

        // ---------------------------------------------------------------------
        // GameManager upgrade payment (costs are relative to the configured starting Funding)
        // ---------------------------------------------------------------------

        private GameManager NewGameManager()
        {
            var go = new GameObject("UpgradeTokenTestsGameManager");
            created.Add(go);
            return go.AddComponent<GameManager>();
        }

        [Test]
        public void AddUpgradeTokens_AddsAndRaisesEventWithNewCount()
        {
            var gm = NewGameManager();
            SymptomCategory? raisedColour = null;
            int raisedCount = -1;
            gm.OnUpgradeTokensChanged += (c, n) => { raisedColour = c; raisedCount = n; };

            gm.AddUpgradeTokens(Infectious);
            gm.AddUpgradeTokens(Infectious, 2);

            Assert.AreEqual(3, gm.UpgradeTokens.Get(Infectious));
            Assert.AreEqual(Infectious, raisedColour);
            Assert.AreEqual(3, raisedCount);
        }

        [Test]
        public void CanAffordUpgrade_FalseWithoutADirectUpgradeOrCard()
        {
            var gm = NewGameManager();
            Assert.IsFalse(gm.CanAffordUpgrade(null));
            Assert.IsFalse(gm.CanAffordUpgrade(NewCard(0, 0, withUpgrade: false)));
        }

        // Funding cost relative to the starting balance: exactly the balance is affordable, one over is not.
        [TestCase(0, true)]
        [TestCase(1, false)]
        public void CanAffordUpgrade_FundingBoundary(int offsetFromBalance, bool expected)
        {
            var gm = NewGameManager();
            int cost = gm.CurrentFunding + offsetFromBalance;
            Assert.AreEqual(expected, gm.CanAffordUpgrade(NewCard(cost, 0)), $"Upgrade costing {cost} with {gm.CurrentFunding} Funding");
        }

        [Test]
        public void CanAffordUpgrade_NeedsTokensOfTheRightColour()
        {
            var gm = NewGameManager();
            var card = NewCard(3, 1, colour: Trauma);
            gm.AddUpgradeTokens(Cardio, 5);
            Assert.IsFalse(gm.CanAffordUpgrade(card));

            gm.AddUpgradeTokens(Trauma);
            Assert.IsTrue(gm.CanAffordUpgrade(card));
        }

        [Test]
        public void TryPayForUpgrade_ShortOnFunding_PaysNothing()
        {
            var gm = NewGameManager();
            gm.AddUpgradeTokens(Internal, 2);
            int start = gm.CurrentFunding;
            var card = NewCard(start + 1, 2, colour: Internal);
            bool fundingRaised = false, tokensRaised = false;
            gm.OnFundingChanged += _ => fundingRaised = true;
            gm.OnUpgradeTokensChanged += (_, __) => tokensRaised = true;

            Assert.IsFalse(gm.TryPayForUpgrade(card));

            Assert.AreEqual(start, gm.CurrentFunding);
            Assert.AreEqual(2, gm.UpgradeTokens.Get(Internal), "Tokens must not be taken when the Funding is short.");
            Assert.IsFalse(fundingRaised);
            Assert.IsFalse(tokensRaised);
        }

        [Test]
        public void TryPayForUpgrade_ShortOnTokens_PaysNothing()
        {
            var gm = NewGameManager();
            gm.AddUpgradeTokens(Internal, 1);
            int start = gm.CurrentFunding;
            var card = NewCard(5, 2, colour: Internal);
            Assume.That(start, Is.GreaterThanOrEqualTo(5), "Fixture: the Funding must be affordable so only the tokens are short.");
            bool fundingRaised = false, tokensRaised = false;
            gm.OnFundingChanged += _ => fundingRaised = true;
            gm.OnUpgradeTokensChanged += (_, __) => tokensRaised = true;

            Assert.IsFalse(gm.TryPayForUpgrade(card));

            Assert.AreEqual(start, gm.CurrentFunding, "Funding must not be taken when the tokens are short.");
            Assert.AreEqual(1, gm.UpgradeTokens.Get(Internal));
            Assert.IsFalse(fundingRaised);
            Assert.IsFalse(tokensRaised);
        }

        [Test]
        public void TryPayForUpgrade_WithBoth_PaysBothAndRaisesEvents()
        {
            var gm = NewGameManager();
            gm.AddUpgradeTokens(Trauma, 3);
            gm.AddUpgradeTokens(Cardio, 1);
            int start = gm.CurrentFunding;
            var card = NewCard(5, 2, colour: Trauma);
            int? fundingRaised = null;
            var tokenEvents = new Dictionary<SymptomCategory, int>();
            gm.OnFundingChanged += v => fundingRaised = v;
            gm.OnUpgradeTokensChanged += (c, n) => tokenEvents[c] = n;

            Assert.IsTrue(gm.TryPayForUpgrade(card));

            Assert.AreEqual(start - 5, gm.CurrentFunding);
            Assert.AreEqual(1, gm.UpgradeTokens.Get(Trauma));
            Assert.AreEqual(1, gm.UpgradeTokens.Get(Cardio));
            Assert.AreEqual(start - 5, fundingRaised);
            Assert.IsTrue(tokenEvents.ContainsKey(Trauma), "OnUpgradeTokensChanged should report the colour that was paid.");
            Assert.AreEqual(1, tokenEvents[Trauma]);
        }

        [Test]
        public void TryPayForUpgrade_AnyColour_PaysFromTheLargestPile()
        {
            var gm = NewGameManager();
            gm.AddUpgradeTokens(Trauma, 1);
            gm.AddUpgradeTokens(Infectious, 3);
            int start = gm.CurrentFunding;
            var card = NewCard(3, 2, anyColour: true);

            Assert.IsTrue(gm.TryPayForUpgrade(card));

            Assert.AreEqual(start - 3, gm.CurrentFunding);
            Assert.AreEqual(1, gm.UpgradeTokens.Get(Trauma));
            Assert.AreEqual(1, gm.UpgradeTokens.Get(Infectious));
        }

        [Test]
        public void TryPayForUpgrade_NoTokenCost_PaysFundingOnly()
        {
            var gm = NewGameManager();
            int start = gm.CurrentFunding;
            var card = NewCard(3, 0);

            Assert.IsTrue(gm.TryPayForUpgrade(card));

            Assert.AreEqual(start - 3, gm.CurrentFunding);
            Assert.AreEqual(0, gm.UpgradeTokens.Total);
        }

        [Test]
        public void TryPayForUpgrade_NeverMutatesTheCardAsset()
        {
            var gm = NewGameManager();
            gm.AddUpgradeTokens(Trauma, 2);
            var card = NewCard(3, 1, colour: Trauma);

            gm.TryPayForUpgrade(card);

            Assert.AreEqual(3, card.upgradeCost);
            Assert.AreEqual(1, card.upgradeTokenCost);
            Assert.AreEqual(Trauma, card.upgradeTokenColour);
        }

        // ---------------------------------------------------------------------
        // DeckUpgradeManager.UpgradeCostText
        // ---------------------------------------------------------------------

        [TestCase(3, 0, false, SymptomCategory.Trauma, "3 Funding")]
        [TestCase(3, 1, false, SymptomCategory.Trauma, "3 Funding + 1 Trauma token")]
        [TestCase(5, 2, false, SymptomCategory.Internal, "5 Funding + 2 Internal Med tokens")]
        [TestCase(3, 1, false, SymptomCategory.Cardiopulmonary, "3 Funding + 1 Cardiology token")]
        [TestCase(5, 2, false, SymptomCategory.Infectious, "5 Funding + 2 Isolation tokens")]
        [TestCase(3, 1, true, SymptomCategory.Trauma, "3 Funding + 1 token (any)")]
        [TestCase(5, 2, true, SymptomCategory.Internal, "5 Funding + 2 tokens (any)")]
        [TestCase(5, 0, true, SymptomCategory.Trauma, "5 Funding")]
        public void UpgradeCostText_Cases(int funding, int tokens, bool anyColour, SymptomCategory colour, string expected)
        {
            Assert.AreEqual(expected, DeckUpgradeManager.UpgradeCostText(NewCard(funding, tokens, anyColour, colour)));
        }

        // ---------------------------------------------------------------------
        // Earning a token by treating a symptom (PatientCardView.TryCureSymptom)
        // ---------------------------------------------------------------------

        /// <summary>
        /// Builds a GameManager registered as <see cref="Singleton{T}.Instance"/> (Awake does not run in EditMode),
        /// a department slot of <paramref name="department"/>, and a fully diagnosed patient sitting in it
        /// with one Tier 1 symptom of <paramref name="symptomCategory"/>.
        /// </summary>
        private (GameManager gm, DepartmentSlot slot, PatientCardView view) BuildTreatment(SymptomCategory department, SymptomCategory symptomCategory)
        {
            var gm = NewGameManager();
            SetGameManagerInstance(gm);

            var slotGo = new GameObject("DepartmentUnderTest");
            created.Add(slotGo);
            var slot = slotGo.AddComponent<DepartmentSlot>();
            slot.category = department;
            slot.isAvailable = true;

            var patient = ScriptableObject.CreateInstance<PatientCardSO>();
            patient.startingStability = 10;
            created.Add(patient);
            var symptom = ScriptableObject.CreateInstance<SymptomCardSO>();
            symptom.specialtyCategory = symptomCategory;
            created.Add(symptom);

            var patientGo = new GameObject("PatientUnderTest");
            created.Add(patientGo);
            var view = patientGo.AddComponent<PatientCardView>();
            view.InitialiseCard(patient, new List<SymptomCardSO> { symptom }, new List<SymptomTier> { SymptomTier.Tier1 }, null);
            view.diagnosedCount = 1;
            patientGo.transform.SetParent(slotGo.transform);
            return (gm, slot, view);
        }

        private static PropertyInfo InstanceProperty => typeof(Singleton<GameManager>).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);

        private GameManager previousInstance;
        private bool instanceOverridden;

        private void SetGameManagerInstance(GameManager gm)
        {
            if (!instanceOverridden)
            {
                previousInstance = GameManager.Instance;
                instanceOverridden = true;
            }
            InstanceProperty.GetSetMethod(true).Invoke(null, new object[] { gm });
        }

        private void RestoreInstance()
        {
            if (!instanceOverridden) return;
            InstanceProperty.GetSetMethod(true).Invoke(null, new object[] { previousInstance });
            previousInstance = null;
            instanceOverridden = false;
        }

        [TestCase(SymptomCategory.Trauma)]
        [TestCase(SymptomCategory.Cardiopulmonary)]
        [TestCase(SymptomCategory.Infectious)]
        [TestCase(SymptomCategory.Internal)]
        public void TryCureSymptom_Success_AwardsOneTokenOfTheDepartmentsColour(SymptomCategory department)
        {
            var (gm, slot, view) = BuildTreatment(department, department);

            Assert.IsTrue(view.TryCureSymptom(SymptomTier.Tier1));

            for (int i = 0; i < 4; i++)
                Assert.AreEqual((SymptomCategory)i == department ? 1 : 0, gm.UpgradeTokens.Get((SymptomCategory)i),
                    $"Treating in {department} should award exactly one {department} token.");
            Assert.IsFalse(slot.isAvailable);
        }

        [Test]
        public void TryCureSymptom_RngFailure_AwardsNoToken()
        {
            var (gm, _, view) = BuildTreatment(Trauma, Trauma);

            Assert.IsTrue(view.TryCureSymptom(SymptomTier.Tier1, rngFailed: true), "An RNG failure still consumes the card.");

            Assert.AreEqual(0, gm.UpgradeTokens.Total);
            Assert.AreEqual(1, view.assignedSymptoms.Count);
        }

        [Test]
        public void TryCureSymptom_WrongDepartment_AwardsNoToken()
        {
            var (gm, _, view) = BuildTreatment(Cardio, Trauma);

            Assert.IsFalse(view.TryCureSymptom(SymptomTier.Tier3));

            Assert.AreEqual(0, gm.UpgradeTokens.Total);
        }

        [Test]
        public void TryCureSymptom_DepartmentAlreadyUsed_AwardsNoToken()
        {
            var (gm, slot, view) = BuildTreatment(Trauma, Trauma);
            slot.MarkAsUsed();

            Assert.IsFalse(view.TryCureSymptom(SymptomTier.Tier1));

            Assert.AreEqual(0, gm.UpgradeTokens.Total);
        }

        [Test]
        public void TryCureSymptom_TierTooHigh_AwardsNoToken()
        {
            var (gm, _, view) = BuildTreatment(Trauma, Trauma);
            view.assignedTiers[0] = SymptomTier.Tier3;

            Assert.IsFalse(view.TryCureSymptom(SymptomTier.Tier1));

            Assert.AreEqual(0, gm.UpgradeTokens.Total);
        }

        [Test]
        public void TryCureSymptom_NotDiagnosed_AwardsNoToken()
        {
            var (gm, _, view) = BuildTreatment(Trauma, Trauma);
            view.diagnosedCount = 0;

            Assert.IsFalse(view.TryCureSymptom(SymptomTier.Tier1));

            Assert.AreEqual(0, gm.UpgradeTokens.Total);
        }
    }
}
