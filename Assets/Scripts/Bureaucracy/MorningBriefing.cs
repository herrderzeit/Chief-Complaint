using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>
    /// Start-of-shift memo: the morning headline, today's Board directive, and any notices other
    /// systems add (e.g. the inspection countdown). Runs as the first shift-start step.
    /// </summary>
    public class MorningBriefing : Singleton<MorningBriefing>
    {
        [Tooltip("Skip the briefing on shift 1 (the tutorial already explains things).")]
        public bool skipFirstShift = false;

        /// <summary>Builds the headline for a shift. Replaced by the headline generator; this is the fallback.</summary>
        public Func<int, string> headlineProvider = shift => "Hospital opens its doors for another shift. Coffee machine still broken.";

        /// <summary>Extra notice lines (e.g. "Inspection in 3 shifts"). Each returns null/empty to stay hidden.</summary>
        public readonly List<Func<string>> noticeProviders = new List<Func<string>>();

        protected override void Awake()
        {
            base.Awake();
            if (GameManager.Instance != null) GameManager.Instance.shiftStartSteps.Register(10, ShowBriefing);
        }

        protected override void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.shiftStartSteps.Unregister(ShowBriefing);
            base.OnDestroy();
        }

        private void ShowBriefing(Action next)
        {
            int shift = GameManager.Instance != null ? GameManager.Instance.CurrentShift : 1;
            if (skipFirstShift && shift <= 1) { next(); return; }

            var body = new StringBuilder();
            body.AppendLine("<b><color=#9fd7ff>THE DAILY CHART</color></b>");
            body.AppendLine($"<i>\"{headlineProvider(shift)}\"</i>");
            body.AppendLine();

            var directives = DirectiveManager.Instance;
            if (directives != null && directives.Current != null)
            {
                body.AppendLine($"<b><color=#ffb347>BOARD DIRECTIVE: {directives.Current.title}</color></b>");
                body.AppendLine(directives.FormatMemo(directives.Current));
            }
            else
            {
                body.AppendLine("<b><color=#ffb347>BOARD DIRECTIVE:</color></b> None today. Enjoy it while it lasts.");
            }

            foreach (var provider in noticeProviders)
            {
                string line = provider();
                if (!string.IsNullOrEmpty(line)) { body.AppendLine(); body.AppendLine(line); }
            }

            BureaucracyDialog.Show($"MORNING BRIEFING - SHIFT {shift}", body.ToString(),
                new BureaucracyDialog.Option("Start Shift", next));
        }
    }
}
