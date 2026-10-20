using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Drafting
{
    /// <summary>
    /// Controls the inter-shift draft phase: the "supply room" market. Every draftable card has a pile with
    /// limited stock (<see cref="SupplyRoom"/>); piles unlock as the run escalates and restock between drafts.
    /// Prices come from <see cref="ActionCardSO.CalculateDraftValue"/>. Bought copies wait in a cart (Funding
    /// already spent, refundable) and join the discard pile when the next shift starts.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DraftManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject draftPanel;
        [Tooltip("One market offer; an instance is spawned into the market container for each card on sale.")]
        public DraftMarketCardView marketCardPrefab;
        public Transform marketContainer;
        public GameObject endShiftButton;

        [Header("Supply Room")]
        [Tooltip("One entry per pile: the card, the escalation stage that unlocks it, and its maximum stock.")]
        public List<SupplyPileConfig> supplyPiles = new List<SupplyPileConfig>();

        /// <summary>Raised whenever pile stock, the cart or unlocks change (for UI and audio).</summary>
        public event System.Action OnSupplyChanged;

        private SupplyRoom supplyRoom;
        private readonly List<DraftMarketCardView> marketCards = new List<DraftMarketCardView>();
        private readonly List<ActionCardSO> boughtBuffer = new List<ActionCardSO>();

        /// <summary>The supply room's runtime state (null until the first draft opens).</summary>
        public SupplyRoom Supply => supplyRoom;

        // -------------------------------------------------------------------------
        // Event Listeners
        // -------------------------------------------------------------------------

        private void Start()
        {
            // The panel stays visible in the scene for editing; a run always begins in the shift phase.
            if (draftPanel != null) draftPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFundingChanged += OnFundingChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFundingChanged -= OnFundingChanged;
        }

        private void OnFundingChanged(int newFunding)
        {
            RefreshAffordability();
        }

        // -------------------------------------------------------------------------
        // Phase Control
        // -------------------------------------------------------------------------

        /// <summary>
        /// Opens the draft panel: unlocks piles for the escalation stage reached, restocks open piles and
        /// rebuilds the market cards.
        /// </summary>
        public void OpenDraftPhase()
        {
            if (endShiftButton != null)
            {
                endShiftButton.SetActive(false);
            }
            draftPanel.SetActive(true);

            supplyRoom ??= new SupplyRoom(supplyPiles);
            var reached = PatientDeckManager.Instance != null ? PatientDeckManager.Instance.CurrentEscalation : EscalationType.WalkIn;
            supplyRoom.OpenDraft(reached);

            BuildMarketCards();
            OnSupplyChanged?.Invoke();
        }

        /// <summary>Spawns one <see cref="DraftMarketCardView"/> per pile, in the order the piles are configured.</summary>
        private void BuildMarketCards()
        {
            foreach (var old in marketCards) if (old != null) Destroy(old.gameObject);
            marketCards.Clear();
            if (marketCardPrefab == null || marketContainer == null) return;

            for (int i = 0; i < supplyRoom.Piles.Count; i++)
            {
                var pile = supplyRoom.Piles[i];
                DraftMarketCardView view = Instantiate(marketCardPrefab, marketContainer);
                int index = i; // captured for the click callbacks
                view.Setup(pile.Card, pile.Price, () => Buy(index), () => Undo(index));
                marketCards.Add(view);
            }
            RefreshAffordability();
        }

        // -------------------------------------------------------------------------
        // Purchase Logic
        // -------------------------------------------------------------------------

        /// <summary>Buys one copy from a pile into the cart, spending its Funding. Ignored if locked, empty or unaffordable.</summary>
        public void Buy(int pileIndex)
        {
            if (supplyRoom == null || pileIndex < 0 || pileIndex >= supplyRoom.Piles.Count) return;
            var pile = supplyRoom.Piles[pileIndex];
            if (!pile.Unlocked || pile.Stock <= 0) return;

            if (!GameManager.Instance.TrySpendFunding(pile.Price))
            {
                GameLog.Info("Not enough Funding!");
                return;
            }
            supplyRoom.TryReserve(pileIndex);
            GameLog.Info($"Reserved {pile.Card.cardName} for {pile.Price} Funding.");
            RefreshAffordability();
            OnSupplyChanged?.Invoke();
        }

        /// <summary>Returns one copy from the cart to its pile and refunds its Funding.</summary>
        public void Undo(int pileIndex)
        {
            if (supplyRoom == null || !supplyRoom.TryUnreserve(pileIndex)) return;
            var pile = supplyRoom.Piles[pileIndex];
            GameManager.Instance.AddFunding(pile.Price);
            GameLog.Info($"Refunded {pile.Card.cardName}, {pile.Price} Funding back.");
            RefreshAffordability();
            OnSupplyChanged?.Invoke();
        }

        /// <summary>
        /// Commits all pending purchases to the player's discard pile, closes the draft panel,
        /// and triggers the next shift via <see cref="GameManager.StartNewShift"/>.
        /// </summary>
        public void CloseDraftAndStartShift()
        {
            boughtBuffer.Clear();
            supplyRoom?.CommitCart(boughtBuffer);
            foreach (ActionCardSO card in boughtBuffer)
            {
                DeckManager.Instance.AddCardToDiscardPile(card);
                GameLog.Info($"COMMITTED: Added {card.cardName} to deck.");
            }
            boughtBuffer.Clear();

            draftPanel.SetActive(false);
            if (endShiftButton != null)
            {
                endShiftButton.SetActive(true);
            }
            GameManager.Instance.StartNewShift();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>Updates every pile's view for its lock state, stock, cart and the player's current Funding.</summary>
        private void RefreshAffordability()
        {
            if (supplyRoom == null) return;
            int funding = GameManager.Instance != null ? GameManager.Instance.CurrentFunding : 0;
            for (int i = 0; i < marketCards.Count && i < supplyRoom.Piles.Count; i++)
            {
                if (marketCards[i] == null) continue;
                var pile = supplyRoom.Piles[i];
                marketCards[i].SetPileState(pile.Unlocked, StageLabel(pile.Config.unlockAt), pile.Stock,
                    pile.Config.maxStock, pile.InCart, funding >= pile.Price);
            }
        }

        /// <summary>Player-facing name of an escalation stage, as used on locked piles.</summary>
        public static string StageLabel(EscalationType stage) => stage switch
        {
            EscalationType.Ambulance => "Ambulances",
            EscalationType.TraumaFlight => "Trauma Flights",
            _ => "Walk-Ins"
        };
    }

}
