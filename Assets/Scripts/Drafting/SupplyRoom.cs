using System;
using System.Collections.Generic;
using UnityEngine;
using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Drafting
{
    /// <summary>Inspector setup for one supply-room pile: which card, when it unlocks and how many copies it holds.</summary>
    [Serializable]
    public class SupplyPileConfig
    {
        public ActionCardSO card;
        [Tooltip("Escalation stage that unlocks this pile. Walk-In piles are open from the first draft.")]
        public EscalationType unlockAt = EscalationType.WalkIn;
        [Min(1)] public int maxStock = 3;
    }

    /// <summary>
    /// Rules of the draft "supply room": every draftable card has its own pile with limited stock.
    /// Piles unlock (full) as the run escalates, and every unlocked pile restocks +1 at each later draft,
    /// up to its maximum. Buying reserves a copy into the cart; undoing returns it. Plain C# so it can be
    /// tested without a scene; <see cref="DraftManager"/> handles Funding and the UI.
    /// </summary>
    public class SupplyRoom
    {
        /// <summary>Runtime state of one pile.</summary>
        public class Pile
        {
            public SupplyPileConfig Config { get; }
            public ActionCardSO Card => Config.card;
            public bool Unlocked { get; internal set; }
            public int Stock { get; internal set; }
            public int InCart { get; internal set; }
            public int Price => Card != null ? Card.CalculateDraftValue() : 0;

            internal Pile(SupplyPileConfig config) { Config = config; }
        }

        private readonly List<Pile> piles = new List<Pile>();
        public IReadOnlyList<Pile> Piles => piles;

        public SupplyRoom(IEnumerable<SupplyPileConfig> configs)
        {
            if (configs == null) return;
            foreach (var config in configs)
                if (config != null && config.card != null) piles.Add(new Pile(config));
        }

        /// <summary>
        /// Starts a draft: unlocks piles whose stage has been reached (they start full) and restocks
        /// every pile that was already open by 1, up to its maximum. Clears the previous cart.
        /// </summary>
        public void OpenDraft(EscalationType reached)
        {
            foreach (var pile in piles)
            {
                pile.InCart = 0;
                if (!pile.Unlocked)
                {
                    if (reached < pile.Config.unlockAt) continue;
                    pile.Unlocked = true;
                    pile.Stock = pile.Config.maxStock;
                }
                else
                {
                    pile.Stock = Mathf.Min(pile.Config.maxStock, pile.Stock + 1);
                }
            }
        }

        /// <summary>Moves one copy from the pile into the cart. Returns false if the pile is locked or empty.</summary>
        public bool TryReserve(int index)
        {
            if (!IsValid(index)) return false;
            var pile = piles[index];
            if (!pile.Unlocked || pile.Stock <= 0) return false;
            pile.Stock--;
            pile.InCart++;
            return true;
        }

        /// <summary>Returns one copy from the cart to its pile. Returns false if none of that card is in the cart.</summary>
        public bool TryUnreserve(int index)
        {
            if (!IsValid(index)) return false;
            var pile = piles[index];
            if (pile.InCart <= 0) return false;
            pile.InCart--;
            pile.Stock++;
            return true;
        }

        /// <summary>Empties the cart into <paramref name="bought"/> (one entry per copy); the copies leave the supply room.</summary>
        public void CommitCart(List<ActionCardSO> bought)
        {
            foreach (var pile in piles)
            {
                for (int i = 0; i < pile.InCart; i++) bought.Add(pile.Card);
                pile.InCart = 0;
            }
        }

        private bool IsValid(int index) => index >= 0 && index < piles.Count;
    }
}
