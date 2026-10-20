using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>Occasionally presents a dilemma at the start of a shift and applies the chosen effects.</summary>
    public class DilemmaManager : Singleton<DilemmaManager>
    {
        public List<DilemmaSO> pool = new List<DilemmaSO>();
        [Range(0f, 1f)] public float chancePerShift = 0.35f;
        [Min(1)] public int firstDilemmaShift = 2;
        [Tooltip("A dilemma won't come back until this many others have been shown.")]
        [Min(0)] public int noRepeatWindow = 3;

        private readonly Queue<DilemmaSO> recent = new Queue<DilemmaSO>();
        private int apNextShift;
        private int staffCostNextShift;
        private int staffCostThisShift;

        protected override void Awake()
        {
            base.Awake();
            CardCostModifiers.Add(ExtraCost);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftStartSteps.Register(20, MaybeShowDilemma);
                gm.OnShiftStarted += HandleShiftStarted;
            }
        }

        protected override void OnDestroy()
        {
            CardCostModifiers.Remove(ExtraCost);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftStartSteps.Unregister(MaybeShowDilemma);
                gm.OnShiftStarted -= HandleShiftStarted;
            }
            base.OnDestroy();
        }

        private int ExtraCost(ActionCardSO card) => card.cardType == ActionCardType.Staff ? staffCostThisShift : 0;

        private void HandleShiftStarted(int shift)
        {
            staffCostThisShift = staffCostNextShift;
            staffCostNextShift = 0;
            if (apNextShift != 0 && GameManager.Instance != null)
            {
                if (apNextShift > 0) GameManager.Instance.GainAP(apNextShift);
                else GameManager.Instance.SpendAP(-apNextShift);
            }
            apNextShift = 0;
            CardCostModifiers.NotifyChanged();
        }

        private void MaybeShowDilemma(System.Action next)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentShift < firstDilemmaShift || pool.Count == 0 || Random.value > chancePerShift) { next(); return; }

            var candidates = new List<DilemmaSO>();
            foreach (var d in pool) if (d != null && !recent.Contains(d)) candidates.Add(d);
            if (candidates.Count == 0) { next(); return; }

            var dilemma = candidates[Random.Range(0, candidates.Count)];
            recent.Enqueue(dilemma);
            while (recent.Count > noRepeatWindow) recent.Dequeue();

            var body = new StringBuilder(dilemma.description);
            body.AppendLine().AppendLine();
            body.AppendLine($"<b>{dilemma.optionALabel}:</b> {Summary(dilemma.optionAEffects)}");
            body.AppendLine($"<b>{dilemma.optionBLabel}:</b> {Summary(dilemma.optionBEffects)}");

            BureaucracyDialog.Show(dilemma.title, body.ToString(),
                new BureaucracyDialog.Option(dilemma.optionALabel, () => { Apply(dilemma.optionAEffects); next(); }),
                new BureaucracyDialog.Option(dilemma.optionBLabel, () => { Apply(dilemma.optionBEffects); next(); }));
        }

        private static string Summary(List<DilemmaEffect> effects)
        {
            if (effects == null || effects.Count == 0) return "Nothing happens.";
            var parts = new List<string>();
            foreach (var e in effects) parts.Add(e.Describe());
            return string.Join(", ", parts);
        }

        /// <summary>Applies a dilemma option's effects.</summary>
        public void Apply(List<DilemmaEffect> effects)
        {
            var gm = GameManager.Instance;
            if (effects == null || gm == null) return;
            foreach (var e in effects)
            {
                switch (e.type)
                {
                    case DilemmaEffectType.Funding:
                        if (e.amount >= 0) gm.AddFunding(e.amount);
                        else gm.TrySpendFunding(Mathf.Min(-e.amount, gm.CurrentFunding));
                        break;
                    case DilemmaEffectType.Reputation:
                        if (e.amount >= 0) gm.AddReputation(e.amount); else gm.SpendReputation(-e.amount);
                        break;
                    case DilemmaEffectType.APNow:
                        if (e.amount >= 0) gm.GainAP(e.amount); else gm.SpendAP(-e.amount);
                        break;
                    case DilemmaEffectType.APNextShift: apNextShift += e.amount; break;
                    case DilemmaEffectType.AddRedTape:
                        for (int i = 0; i < e.amount; i++) if (DeckManager.Instance != null) DeckManager.Instance.SpawnRandomRedTape();
                        break;
                    case DilemmaEffectType.StaffCostNextShift: staffCostNextShift += e.amount; break;
                }
            }
            GameLog.Info("Dilemma resolved: " + Summary(effects));
        }
    }
}
