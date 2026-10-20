using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using NUnit.Framework;

namespace ChiefComplaint.Tests
{
    /// <summary>
    /// Layout of symptom sub-cards beneath a patient (<see cref="PatientCardView.SubCardSlots"/>):
    /// the primary (last occurrence of the highest tier) sits in the centre (0); the others fill -1, +1, then further out.
    /// </summary>
    [TestFixture]
    [Category("Patients")]
    public class SubCardSlotsTests
    {
        private const SymptomTier T1 = SymptomTier.Tier1, T2 = SymptomTier.Tier2, T3 = SymptomTier.Tier3;

        private static IEnumerable<TestCaseData> Cases()
        {
            yield return new TestCaseData(new[] { T1, T1, T2 }, new[] { -1, 1, 0 }).SetName("SubCardSlots_SortedThree_PrimaryLastInCentre");
            yield return new TestCaseData(new[] { T3, T1, T2 }, new[] { 0, -1, 1 }).SetName("SubCardSlots_HandoverOrder_PrimaryFirstInCentre");
            yield return new TestCaseData(new[] { T1, T2 }, new[] { -1, 0 }).SetName("SubCardSlots_TwoSorted_SecondInCentre");
            yield return new TestCaseData(new[] { T2, T1 }, new[] { 0, -1 }).SetName("SubCardSlots_TwoHandover_FirstInCentre");
            yield return new TestCaseData(new[] { T1 }, new[] { 0 }).SetName("SubCardSlots_Single_InCentre");
            yield return new TestCaseData(new SymptomTier[0], new int[0]).SetName("SubCardSlots_Empty_NoSlots");
            yield return new TestCaseData(new[] { T2, T2 }, new[] { -1, 0 }).SetName("SubCardSlots_TiedTiers_LastOccurrenceIsPrimary");
            yield return new TestCaseData(new[] { T1, T1, T1, T1, T3 }, new[] { -1, 1, -2, 2, 0 }).SetName("SubCardSlots_FiveSymptoms_SpreadOutwards");
        }

        [TestCaseSource(nameof(Cases))]
        public void SubCardSlots_PlacesPrimaryInCentre(SymptomTier[] tiers, int[] expected)
        {
            CollectionAssert.AreEqual(expected, PatientCardView.SubCardSlots(tiers));
        }
    }
}
