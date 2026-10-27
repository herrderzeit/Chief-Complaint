using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Shows how many upgrade tokens of each department colour the player holds: a coloured pip and a count per
    /// department, tinted to match the department plates (<see cref="DepartmentFallbackButton.DepartmentColour"/>).
    /// Used in the HUD top bar and on the staff room Upgrades tab; each instance listens to
    /// <see cref="GameManager.OnUpgradeTokensChanged"/> and refreshes from <see cref="GameManager.UpgradeTokens"/>
    /// whenever it is enabled, so a hidden tab is never stale when it reopens.
    /// </summary>
    public class UpgradeTokenDisplay : MonoBehaviour
    {
        [Serializable]
        public struct Badge
        {
            public SymptomCategory colour;
            [Tooltip("Coloured circle tinted with the department colour.")]
            public Image pip;
            [Tooltip("Token count next to the pip.")]
            public TextMeshProUGUI countText;
        }

        [Tooltip("One badge per department, in display order (Cardiology, Internal Med, Isolation, Trauma).")]
        public Badge[] badges = Array.Empty<Badge>();

        [Tooltip("Alpha applied to a badge when the player holds none of that colour, so held colours stand out.")]
        [Range(0f, 1f)] public float emptyAlpha = 0.35f;

        private GameManager subscribedTo;

        private void OnEnable()
        {
            TrySubscribe();
            RefreshAll();
        }

        // GameManager may not have run Awake yet when this enables on scene load; try again once everything exists.
        private void Start()
        {
            TrySubscribe();
            RefreshAll();
        }

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void TrySubscribe()
        {
            if (subscribedTo != null || GameManager.Instance == null) return;
            subscribedTo = GameManager.Instance;
            subscribedTo.OnUpgradeTokensChanged += HandleTokensChanged;
        }

        private void Unsubscribe()
        {
            if (subscribedTo == null) return;
            subscribedTo.OnUpgradeTokensChanged -= HandleTokensChanged;
            subscribedTo = null;
        }

        private void HandleTokensChanged(SymptomCategory colour, int count)
        {
            for (int i = 0; i < badges.Length; i++)
                if (badges[i].colour == colour) ApplyBadge(badges[i], count);
        }

        /// <summary>Re-reads every colour from the wallet (zero when there is no GameManager, e.g. in edit mode).</summary>
        public void RefreshAll()
        {
            UpgradeTokenWallet wallet = GameManager.Instance != null ? GameManager.Instance.UpgradeTokens : null;
            for (int i = 0; i < badges.Length; i++)
                ApplyBadge(badges[i], wallet != null ? wallet.Get(badges[i].colour) : 0);
        }

        /// <summary>Shows <paramref name="count"/> on the badge for <paramref name="colour"/> (also used for edit-mode previews).</summary>
        public void ShowCount(SymptomCategory colour, int count) => HandleTokensChanged(colour, count);

        private void ApplyBadge(Badge badge, int count)
        {
            float alpha = count > 0 ? 1f : emptyAlpha;
            if (badge.pip != null)
            {
                Color c = DepartmentFallbackButton.DepartmentColour(badge.colour);
                c.a = alpha;
                badge.pip.color = c;
            }
            if (badge.countText != null)
            {
                badge.countText.text = count.ToString();
                badge.countText.alpha = alpha;
            }
        }
    }
}
