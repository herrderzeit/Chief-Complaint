using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// "Page the on-call team" costs: raw AP to treat a diagnosed symptom in its department without a Treatment card.
    /// Always 1 AP more than the matching Treatment card (Standard Care 1, Specialised Care 1, Surgical Intervention 2),
    /// because calling a team in is slower than having a prepared order. Mirrored by BalanceSim's PageCostTier1-3.
    /// </summary>
    public static class PagingRules
    {
        public static int ApCost(SymptomTier tier) => tier switch
        {
            SymptomTier.Tier1 => 2,
            SymptomTier.Tier2 => 2,
            _ => 3
        };
    }
}
