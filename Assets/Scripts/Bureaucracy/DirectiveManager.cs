using System.Collections.Generic;
using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>
    /// Picks one Board directive per shift, applies its modifiers, and issues citations when it is broken.
    /// </summary>
    public class DirectiveManager : Singleton<DirectiveManager>
    {
        [Tooltip("Directives that can be drawn. One is picked per shift, never the same twice in a row.")]
        public List<DirectiveSO> pool = new List<DirectiveSO>();
        [Tooltip("First shift that has a directive (shift 1 is a quiet start).")]
        [Min(1)] public int firstDirectiveShift = 2;

        /// <summary>Today's directive, or <c>null</c> on a quiet shift.</summary>
        public DirectiveSO Current { get; private set; }

        /// <summary>The department closed by a <see cref="DirectiveType.ClosedDepartment"/> directive.</summary>
        public SymptomCategory ClosedCategory { get; private set; }

        private DirectiveSO previous;

        protected override void Awake()
        {
            base.Awake();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnShiftStarted += HandleShiftStarted;
                gm.shiftEndSteps.Register(10, EndOfShiftChecks);
            }
            PatientCardView.OnSymptomTreated += HandleSymptomTreated;
            CardCostModifiers.Add(ExtraCost);
        }

        protected override void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnShiftStarted -= HandleShiftStarted;
                gm.shiftEndSteps.Unregister(EndOfShiftChecks);
            }
            PatientCardView.OnSymptomTreated -= HandleSymptomTreated;
            CardCostModifiers.Remove(ExtraCost);
            base.OnDestroy();
        }

        /// <summary><c>true</c> if today's directive is of <paramref name="type"/>.</summary>
        public bool IsActive(DirectiveType type) => Current != null && Current.type == type;

        /// <summary>The directive's memo with its placeholders filled in.</summary>
        public string FormatMemo(DirectiveSO directive)
        {
            if (directive == null) return "";
            return directive.memo
                .Replace("{department}", DepartmentName(ClosedCategory))
                .Replace("{value}", directive.value.ToString());
        }

        public static string DepartmentName(SymptomCategory category) => category switch
        {
            SymptomCategory.Cardiopulmonary => "Cardiology",
            SymptomCategory.Internal => "Internal Medicine",
            SymptomCategory.Infectious => "Isolation",
            _ => "the Trauma Bay"
        };

        private void HandleShiftStarted(int shift)
        {
            previous = Current;
            Current = shift >= firstDirectiveShift ? DirectivePicker.Pick(pool, previous) : null;

            if (IsActive(DirectiveType.ClosedDepartment))
                ClosedCategory = (SymptomCategory)Random.Range(0, 4);

            if (IsActive(DirectiveType.StaffingShortage) && GameManager.Instance != null)
                GameManager.Instance.SpendAP(Current.value);

            if (Current != null) GameLog.Info($"DIRECTIVE: {Current.title}");
            CardCostModifiers.NotifyChanged();
        }

        private int ExtraCost(ActionCardSO card)
        {
            if (IsActive(DirectiveType.DiagnosticsSurcharge) && card.cardType == ActionCardType.Diagnostic)
                return Current.value;
            return 0;
        }

        private void HandleSymptomTreated(PatientCardView patient, SymptomCategory department)
        {
            if (IsActive(DirectiveType.ClosedDepartment) && department == ClosedCategory)
                Cite($"Treated a patient in {DepartmentName(department)} while it was closed for audit");
        }

        private void EndOfShiftChecks(System.Action next)
        {
            var board = GameManager.Instance != null ? GameManager.Instance.boardManager : null;
            if (board != null && Current != null)
            {
                if (IsActive(DirectiveType.EmergenciesFirst))
                {
                    foreach (var p in board.ActivePatients)
                    {
                        if (p == null || p.isRemoved || p.assignedWardSlot != null) continue;
                        if (p.patientData.escalationPhase != EscalationType.WalkIn)
                            Cite($"{p.patientData.patientName} ({p.patientData.escalationPhase}) was left waiting in Triage");
                    }
                }

                if (IsActive(DirectiveType.BedCap))
                {
                    int occupied = 0;
                    foreach (var p in board.ActivePatients)
                        if (p != null && !p.isRemoved && p.assignedWardSlot != null) occupied++;
                    if (occupied > Current.value)
                        Cite($"{occupied} Ward beds occupied (cap is {Current.value})");
                }
            }
            next();
        }

        private static void Cite(string reason)
        {
            if (CitationManager.Instance != null) CitationManager.Instance.Issue(reason);
        }
    }

    /// <summary>Pure directive selection, kept separate so it can be unit tested.</summary>
    public static class DirectivePicker
    {
        /// <summary>Picks a random directive from <paramref name="pool"/>, avoiding <paramref name="previous"/> when possible.</summary>
        public static DirectiveSO Pick(IList<DirectiveSO> pool, DirectiveSO previous, System.Func<int, int> randomIndex = null)
        {
            if (pool == null || pool.Count == 0) return null;
            randomIndex ??= n => Random.Range(0, n);

            var candidates = new List<DirectiveSO>();
            foreach (var d in pool) if (d != null && d != previous) candidates.Add(d);
            if (candidates.Count == 0) return previous;

            return candidates[randomIndex(candidates.Count)];
        }
    }
}
