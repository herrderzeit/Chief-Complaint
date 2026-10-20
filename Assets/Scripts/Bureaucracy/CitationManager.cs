using UnityEngine;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>
    /// Issues citations for rule breaks (directives, paperwork) and applies their penalties:
    /// warnings, then a Funding fine, then Red Tape cards. Resets each shift.
    /// </summary>
    public class CitationManager : Singleton<CitationManager>
    {
        [Tooltip("Free warnings per shift before penalties start.")]
        [Min(0)] public int warningsPerShift = 2;
        [Tooltip("Funding taken by the first citation after the warnings.")]
        [Min(0)] public int fineAmount = 3;

        public readonly CitationLedger ledger = new CitationLedger();

        /// <summary>Raised after every citation with its reason and penalty.</summary>
        public event System.Action<string, CitationPenalty> OnCitationIssued;

        private static readonly Color CitationColour = new Color(1f, 0.55f, 0.2f);

        protected override void Awake()
        {
            base.Awake();
            if (GameManager.Instance != null) GameManager.Instance.OnShiftStarted += HandleShiftStarted;
        }

        protected override void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnShiftStarted -= HandleShiftStarted;
            base.OnDestroy();
        }

        private void HandleShiftStarted(int shift) => ledger.ResetShift();

        /// <summary>Issues a citation for <paramref name="reason"/> and applies its penalty.</summary>
        public CitationPenalty Issue(string reason)
        {
            ledger.warningsPerShift = warningsPerShift;
            CitationPenalty penalty = ledger.Issue();
            string detail;

            switch (penalty)
            {
                case CitationPenalty.Warning:
                    detail = $"Warning {ledger.ShiftCount}/{warningsPerShift}";
                    break;
                case CitationPenalty.Fine:
                    int fine = GameManager.Instance != null ? Mathf.Min(fineAmount, GameManager.Instance.CurrentFunding) : 0;
                    if (fine > 0) GameManager.Instance.TrySpendFunding(fine);
                    detail = $"Fined {fine} Funding";
                    break;
                default:
                    if (DeckManager.Instance != null) DeckManager.Instance.SpawnRandomRedTape();
                    detail = "Red Tape added to your deck";
                    break;
            }

            GameLog.Warn($"CITATION: {reason} ({detail})");
            if (NotificationManager.Instance != null)
                NotificationManager.Instance.ShowNotification($"CITATION: {reason}\n{detail}", CitationColour);

            OnCitationIssued?.Invoke(reason, penalty);
            return penalty;
        }
    }
}
