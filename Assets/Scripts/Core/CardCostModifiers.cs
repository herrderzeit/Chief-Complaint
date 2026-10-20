using System;
using System.Collections.Generic;
using UnityEngine;
using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// Temporary AP cost changes from directives, unpaid upkeep and events. Each system registers a
    /// provider that returns the extra cost for a card given the current game state.
    /// </summary>
    public static class CardCostModifiers
    {
        private static readonly List<Func<ActionCardSO, int>> providers = new List<Func<ActionCardSO, int>>();

        /// <summary>Raised when a provider is added or removed, or a system says its modifiers changed.</summary>
        public static event Action OnChanged;

        public static void Add(Func<ActionCardSO, int> provider) { providers.Add(provider); OnChanged?.Invoke(); }
        public static void Remove(Func<ActionCardSO, int> provider) { providers.Remove(provider); OnChanged?.Invoke(); }

        /// <summary>Call when the state a provider reads from has changed (e.g. a new directive).</summary>
        public static void NotifyChanged() => OnChanged?.Invoke();

        /// <summary>The AP the card costs right now, never below zero.</summary>
        public static int GetCost(ActionCardSO card)
        {
            if (card == null) return 0;
            int cost = card.apCost;
            foreach (var p in providers) cost += p(card);
            return Mathf.Max(0, cost);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            providers.Clear();
            OnChanged = null;
        }
    }
}
