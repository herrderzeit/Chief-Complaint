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

        [Header("Group Headers")]
        [Tooltip("Height of the headings placed above each tier (Upgrades tab) or card type (View Deck tab).")]
        public float tierHeaderHeight = 34f;
        public float tierHeaderFontSize = 22f;
        public Color tierHeaderColour = new Color(0.96f, 0.6f, 0.1f, 1f);
        [Tooltip("Strip behind each heading, so dark type colours stay readable on the grey list.")]
        public Color groupHeaderBackground = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        [Header("Card Preview UI")]
        public UI_ActionCardView uiActionCardPrefab; // The dedicated UI prefab
        public Transform previewCardSpawnPoint; // Where to place the UI card prefab
        private UI_ActionCardView spawnedPreviewCard;

        private void OnEnable()
        {
            SwitchTab(0);
            HidePreview();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFundingChanged += OnFundingChanged;
                GameManager.Instance.OnUpgradeTokensChanged += OnUpgradeTokensChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFundingChanged -= OnFundingChanged;
                GameManager.Instance.OnUpgradeTokensChanged -= OnUpgradeTokensChanged;
            }
        }

        // One upgrade can raise several Funding and token events in a row; rebuild the list once, after all of them.
        private bool upgradesListDirty;

        private void OnUpgradeTokensChanged(SymptomCategory colour, int count) => upgradesListDirty = true;

        private void OnFundingChanged(int newFunding) => upgradesListDirty = true;

        private void LateUpdate()
        {
            if (!upgradesListDirty) return;
            upgradesListDirty = false;
            // Only the visible Upgrades tab needs refreshing; SwitchTab rebuilds it when it opens.
            if (upgradesContainer != null && upgradesContainer.activeInHierarchy) RefreshUpgradesList();
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

            uniqueCardBuffer.Sort((a, b) =>
            {
                int byTier = a.powerTier.CompareTo(b.powerTier);
                return byTier != 0 ? byTier : string.Compare(a.cardName, b.cardName);
            });

            int currentTier = -1;
            for (int i = 0; i < uniqueCardBuffer.Count; i++)
            {
                ActionCardSO card = uniqueCardBuffer[i];
                if (card.powerTier != currentTier)
                {
                    currentTier = card.powerTier;
                    SpawnGroupHeader($"TIER {currentTier}", $"upgrades to Tier {currentTier + 1}", tierHeaderColour, upgradesContentTransform);
                }
                SpawnCardRow(card, upgradesContentTransform, true, cardCounts[card.cardName]);
            }
        }

        /// <summary>
        /// Adds a heading (e.g. "TIER 1" or "TREATMENT") above a group of rows: a dark strip with the title in
        /// <paramref name="colour"/> and a smaller subtitle, styled with the row prefab's font.
        /// </summary>
        private void SpawnGroupHeader(string title, string subtitle, Color colour, Transform parentTransform)
        {
            var go = new GameObject($"Header_{title}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parentTransform, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(0f, tierHeaderHeight);
            var strip = go.GetComponent<Image>();
            strip.color = groupHeaderBackground;
            strip.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRt = (RectTransform)textGo.transform;
            textRt.SetParent(go.transform, false);
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = textRt.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<TextMeshProUGUI>();
            var rowText = cardRowPrefab != null ? cardRowPrefab.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (rowText != null) text.font = rowText.font;
            text.text = $"{title}  <size=75%><color=#F0F0F0>{subtitle}</color></size>";
            text.fontSize = tierHeaderFontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = colour;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(12f, 0f, 0f, 0f);
            text.raycastTarget = false;
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
                if (i == 0 || card.cardType != uniqueCardBuffer[i - 1].cardType)
                {
                    int typeTotal = 0;
                    for (int j = i; j < uniqueCardBuffer.Count && uniqueCardBuffer[j].cardType == card.cardType; j++)
                        typeTotal += cardCounts[uniqueCardBuffer[j].cardName];
                    SpawnGroupHeader(CardTypeIconsSO.GetDisplayName(card.cardType).ToUpperInvariant(),
                        $"{typeTotal} card{(typeTotal == 1 ? "" : "s")}",
                        CardTypeIconsSO.GetTypeColor(card.cardType), deckViewContentTransform);
                }
                SpawnCardRow(card, deckViewContentTransform, false, cardCounts[card.cardName]);
            }
        }

        private void SpawnCardRow(ActionCardSO card, Transform parentTransform, bool showUpgradeButton, int count = 1)
        {
            if (cardRowPrefab == null) return;

            GameObject rowObj = Instantiate(cardRowPrefab, parentTransform);

            TextMeshProUGUI[] texts = rowObj.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length == 0)
            {
                Debug.LogWarning("DeckUpgradeManager: the card row prefab has no TextMeshProUGUI label.");
                return;
            }
            texts[0].text = count > 1 ? $"{card.cardName} (x{count})" : card.cardName;

            // --- HOVER LOGIC ---
            // Hovering the card name text displays the base card
            HoverRelay nameHover = texts[0].gameObject.GetComponent<HoverRelay>();
            if (nameHover == null) nameHover = texts[0].gameObject.AddComponent<HoverRelay>();

            nameHover.OnEnter = (data) => ShowPreview(card);
            nameHover.OnExit = (data) => HidePreview();
            // -------------------

            // Cost columns: Funding (right-aligned) and tokens (left-aligned), grouped on the right of the row.
            Transform costGroup = rowObj.transform.Find("CostGroup");
            TextMeshProUGUI fundingText = FindRowText(rowObj, "CostGroup/FundingText");
            TextMeshProUGUI tokenText = FindRowText(rowObj, "CostGroup/TokenText");

            Button upgradeBtn = rowObj.GetComponentInChildren<Button>();
            if (upgradeBtn != null)
            {
                if (showUpgradeButton && card.directDraftUpgrade != null)
                {
                    upgradeBtn.gameObject.SetActive(true);

                    TextMeshProUGUI btnText = upgradeBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null) btnText.text = $"Upgrade to {card.directDraftUpgrade.cardName}";

                    if (costGroup != null) costGroup.gameObject.SetActive(true);
                    if (fundingText != null) fundingText.text = $"{card.draftUpgradeCost} Funding";
                    if (tokenText != null) tokenText.text = UpgradeTokenRichText(card);

                    bool canAfford = GameManager.Instance != null && GameManager.Instance.CanAffordUpgrade(card);
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
                    if (costGroup != null) costGroup.gameObject.SetActive(false);
                }
            }
        }

        private static TextMeshProUGUI FindRowText(GameObject row, string path)
        {
            Transform t = row.transform.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        private void OnUpgradeClicked(ActionCardSO baseCard)
        {
            if (baseCard.directDraftUpgrade == null) return;

            if (GameManager.Instance == null || DeckManager.Instance == null) return;

            int cost = baseCard.draftUpgradeCost;
            if (GameManager.Instance.TryPayForUpgrade(baseCard))
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
                // The upgrade paid Funding and tokens, so the list rebuilds once in LateUpdate.
                upgradesListDirty = true;
                HidePreview();
            }
        }

        /// <summary>Cost line for an upgrade, e.g. "3 Funding + 1 Trauma token" or "5 Funding + 2 tokens (any)".</summary>
        public static string UpgradeCostText(ActionCardSO card)
        {
            string text = $"{card.draftUpgradeCost} Funding";
            if (card.upgradeTokenCost <= 0) return text;
            string plural = card.upgradeTokenCost == 1 ? "token" : "tokens";
            return card.upgradeTokenAnyColour
                ? $"{text} + {card.upgradeTokenCost} {plural} (any)"
                : $"{text} + {card.upgradeTokenCost} {UpgradeTokenWallet.ColourName(card.upgradeTokenColour)} {plural}";
        }

        /// <summary>
        /// Token part of the upgrade row's cost, e.g. "+ 1 Internal Med" with the colour name tinted like its
        /// department plate ("+ 2 any" for any-colour costs, empty when no tokens are needed).
        /// </summary>
        public static string UpgradeTokenRichText(ActionCardSO card)
        {
            if (card.upgradeTokenCost <= 0) return "";
            if (card.upgradeTokenAnyColour) return $"+ {card.upgradeTokenCost} any";
            string hex = ColorUtility.ToHtmlStringRGB(DepartmentFallbackButton.DepartmentColour(card.upgradeTokenColour));
            return $"+ {card.upgradeTokenCost} <color=#{hex}>{UpgradeTokenWallet.ColourName(card.upgradeTokenColour)}</color>";
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
