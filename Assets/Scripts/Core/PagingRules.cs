using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// "Page the on-call team" costs: raw AP to treat a diagnosed symptom in its department without a Treatment card.
    /// More than the matching Treatment card (Standard Care 1, Specialised Care 1, Surgical Intervention 2), because
    /// calling a team in is slower than having a prepared order, but never more than a shift's base AP (3).
    /// Mirrored by BalanceSim's PageCostTier1-3.
    /// </summary>
    public static class PagingRules
    {
        public static int ApCost(SymptomTier tier) => tier switch
        {
            SymptomTier.Tier1 => 2,
            SymptomTier.Tier2 => 3,
            _ => 3
        };
    }
}
