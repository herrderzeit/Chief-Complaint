using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
    /// Manages the 3-tab UI during the Draft Phase:
    /// 1. Draft Market (Buy new cards)
    /// 2. Upgrades (Upgrade starter cards)
    /// 3. Deck View (View all cards in your deck)
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DeckUpgradeManager : MonoBehaviour
    {
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
        public enum DraftTab { Market, Upgrades, DeckView }

        [Header("Tab Containers")]
        public GameObject draftMarketContainer;
        public GameObject upgradesContainer;
        public GameObject deckViewContainer;

        [Header("Tab Buttons")]
        public Image marketTabButton;
        public Image upgradesTabButton;
        public Image deckViewTabButton;

        [Header("Tab Styling")]
        public Color activeTabColor = new Color(0.96f, 0.6f, 0.1f, 1f); // "Resource Allocation" Orange
        public Color inactiveTabColor = new Color(0.12f, 0.12f, 0.12f, 0.8f); // Dark translucent matching the UI

        [Header("Prefabs & Parents")]
        public Transform upgradesContentTransform;
        public Transform deckViewContentTransform;
        public GameObject cardRowPrefab; // Shared prefab for displaying a card

        [Header("Card Preview UI")]
        public UI_ActionCardView uiActionCardPrefab; // The dedicated UI prefab
        public Transform previewCardSpawnPoint; // Where to place the UI card prefab
        private UI_ActionCardView spawnedPreviewCard;

        private void OnEnable()
        {
            SwitchTab(0);
            HidePreview();
            if (GameManager.Instance != null) GameManager.Instance.OnFundingChanged += OnFundingChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnFundingChanged -= OnFundingChanged;
        }

        private void OnFundingChanged(int newFunding)
        {
            // If we are currently looking at the upgrades tab, refresh the list to grey out unaffordable buttons
            if (upgradesContainer != null && upgradesContainer.activeInHierarchy)
            {
                RefreshUpgradesList();
            }
        }

        public void SwitchTab(int tabIndex)
        {
            DraftTab tab = (DraftTab)tabIndex;

            if (draftMarketContainer != null) draftMarketContainer.SetActive(tab == DraftTab.Market);
            if (upgradesContainer != null) upgradesContainer.SetActive(tab == DraftTab.Upgrades);
            if (deckViewContainer != null) deckViewContainer.SetActive(tab == DraftTab.DeckView);

            // Update button colors visually
            if (marketTabButton != null) marketTabButton.color = (tab == DraftTab.Market) ? activeTabColor : inactiveTabColor;
            if (upgradesTabButton != null) upgradesTabButton.color = (tab == DraftTab.Upgrades) ? activeTabColor : inactiveTabColor;
            if (deckViewTabButton != null) deckViewTabButton.color = (tab == DraftTab.DeckView) ? activeTabColor : inactiveTabColor;

            if (tab == DraftTab.Upgrades) RefreshUpgradesList();
            else if (tab == DraftTab.DeckView) RefreshDeckList();
        }

        // Reused between refreshes so opening the panels doesn't allocate lists/LINQ iterators each time.
        private readonly List<ActionCardSO> cardBuffer = new List<ActionCardSO>();
        private readonly List<ActionCardSO> uniqueCardBuffer = new List<ActionCardSO>();
        private readonly Dictionary<string, int> cardCounts = new Dictionary<string, int>();

        private void FillAllCardsInDeck(List<ActionCardSO> buffer)
        {
            buffer.Clear();
            if (DeckManager.Instance != null)
            {
                buffer.AddRange(DeckManager.Instance.drawPile);
                buffer.AddRange(DeckManager.Instance.discardPile);
                buffer.AddRange(DeckManager.Instance.handData);
            }
        }

        public void RefreshUpgradesList()
        {
            foreach (Transform child in upgradesContentTransform) Destroy(child.gameObject);

            FillAllCardsInDeck(cardBuffer);
            cardBuffer.RemoveAll(c => c.directDraftUpgrade == null);
            
            cardCounts.Clear();
            uniqueCardBuffer.Clear();
            for (int i = 0; i < cardBuffer.Count; i++)
            {
                ActionCardSO card = cardBuffer[i];
                if (cardCounts.TryGetValue(card.cardName, out int count))
                {
                    cardCounts[card.cardName] = count + 1;
                }
                else
                {
                    cardCounts[card.cardName] = 1;
                    uniqueCardBuffer.Add(card);
                }
            }

            uniqueCardBuffer.Sort((a, b) => string.Compare(a.cardName, b.cardName));

            for (int i = 0; i < uniqueCardBuffer.Count; i++)
            {
                ActionCardSO card = uniqueCardBuffer[i];
                SpawnCardRow(card, upgradesContentTransform, true, cardCounts[card.cardName]);
            }
        }

        public void RefreshDeckList()
        {
            foreach (Transform child in deckViewContentTransform) Destroy(child.gameObject);

            FillAllCardsInDeck(cardBuffer);

            // Group identical cards together for the View Deck tab
            cardCounts.Clear();
            uniqueCardBuffer.Clear();
            for (int i = 0; i < cardBuffer.Count; i++)
            {
                ActionCardSO card = cardBuffer[i];
                if (cardCounts.TryGetValue(card.cardName, out int count))
                {
                    cardCounts[card.cardName] = count + 1;
                }
                else
                {
                    cardCounts[card.cardName] = 1;
                    uniqueCardBuffer.Add(card);
                }
            }

            uniqueCardBuffer.Sort((a, b) =>
            {
                int byType = a.cardType.CompareTo(b.cardType);
                return byType != 0 ? byType : string.Compare(a.cardName, b.cardName);
            });

            for (int i = 0; i < uniqueCardBuffer.Count; i++)
            {
                ActionCardSO card = uniqueCardBuffer[i];
                SpawnCardRow(card, deckViewContentTransform, false, cardCounts[card.cardName]);
            }
        }

        private void SpawnCardRow(ActionCardSO card, Transform parentTransform, bool showUpgradeButton, int count = 1)
        {
            if (cardRowPrefab == null) return;

            GameObject rowObj = Instantiate(cardRowPrefab, parentTransform);

            TextMeshProUGUI[] texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 1) 
            {
                texts[0].text = count > 1 ? $"{card.cardName} (x{count})" : card.cardName;
            }

            // --- HOVER LOGIC ---
            // Hovering the card name text displays the base card
            HoverRelay nameHover = texts[0].gameObject.GetComponent<HoverRelay>();
            if (nameHover == null) nameHover = texts[0].gameObject.AddComponent<HoverRelay>();

            nameHover.OnEnter = (data) => ShowPreview(card);
            nameHover.OnExit = (data) => HidePreview();
            // -------------------

            Button upgradeBtn = rowObj.GetComponentInChildren<Button>();
            if (upgradeBtn != null)
            {
                if (showUpgradeButton && card.directDraftUpgrade != null)
                {
                    upgradeBtn.gameObject.SetActive(true);

                    TextMeshProUGUI btnText = upgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = $"Upgrade to {card.directDraftUpgrade.cardName}";

                    if (texts.Length >= 3)
                    {
                        // The third text component in the prefab is the cost text
                        texts[2].text = $"{card.draftUpgradeCost} Funding";
                        texts[2].gameObject.SetActive(true);
                    }

                    bool canAfford = GameManager.Instance.CurrentFunding >= card.draftUpgradeCost;
                    upgradeBtn.interactable = canAfford;

                    // Attach specific hover logic to the button to show the UPGRADED card
                    HoverRelay btnHover = upgradeBtn.gameObject.GetComponent<HoverRelay>();
                    if (btnHover == null) btnHover = upgradeBtn.gameObject.AddComponent<HoverRelay>();

                    btnHover.OnEnter = (data) => ShowPreview(card.directDraftUpgrade);
                    btnHover.OnExit = (data) => HidePreview(); // Hide preview entirely when leaving button

                    upgradeBtn.onClick.AddListener(() => OnUpgradeClicked(card));
                }
                else
                {
                    upgradeBtn.gameObject.SetActive(false);
                    if (texts.Length >= 3) 
                    {
                        texts[2].text = "";
                        texts[2].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnUpgradeClicked(ActionCardSO baseCard)
        {
            if (baseCard.directDraftUpgrade == null) return;

            int cost = baseCard.draftUpgradeCost;
            if (GameManager.Instance.TrySpendFunding(cost))
            {

                if (DeckManager.Instance.drawPile.Contains(baseCard))
                {
                    DeckManager.Instance.drawPile.Remove(baseCard);
                    DeckManager.Instance.drawPile.Add(baseCard.directDraftUpgrade);
                }
                else if (DeckManager.Instance.discardPile.Contains(baseCard))
                {
                    DeckManager.Instance.discardPile.Remove(baseCard);
                    DeckManager.Instance.discardPile.Add(baseCard.directDraftUpgrade);
                }
                else if (DeckManager.Instance.handData.Contains(baseCard))
                {
                    DeckManager.Instance.handData.Remove(baseCard);
                    DeckManager.Instance.handData.Add(baseCard.directDraftUpgrade);
                }

                Debug.Log($"Upgraded {baseCard.cardName} to {baseCard.directDraftUpgrade.cardName} for {cost} Funding!");
                RefreshUpgradesList(); // Refresh to reflect the removal of the upgraded card
                HidePreview();
            }
        }

        private void ShowPreview(ActionCardSO card)
        {
            if (uiActionCardPrefab == null || previewCardSpawnPoint == null) return;

            // If we haven't spawned a preview card yet, clone one from the UI Prefab
            if (spawnedPreviewCard == null)
            {
                spawnedPreviewCard = Instantiate(uiActionCardPrefab, previewCardSpawnPoint);
                var rt = (RectTransform)spawnedPreviewCard.transform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.SetAsLastSibling();
            }

            spawnedPreviewCard.gameObject.SetActive(true);
            spawnedPreviewCard.InitialiseCard(card);
        }

        private void HidePreview()
        {
            if (spawnedPreviewCard != null)
            {
                spawnedPreviewCard.gameObject.SetActive(false);
            }
        }
    }

    // Custom hover component that DOES NOT block scroll events
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class HoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public System.Action<PointerEventData> OnEnter;
        public System.Action<PointerEventData> OnExit;

        public void OnPointerEnter(PointerEventData eventData) => OnEnter?.Invoke(eventData);
        public void OnPointerExit(PointerEventData eventData) => OnExit?.Invoke(eventData);
    }

}
