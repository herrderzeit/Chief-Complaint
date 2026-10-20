using System.Collections.Generic;
using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Bureaucracy
{
    /// <summary>Pure accreditation rules: each department is accredited after enough treatments in it.</summary>
    public class AccreditationTracker
    {
        public int threshold = 4;

        private readonly Dictionary<SymptomCategory, int> treatments = new Dictionary<SymptomCategory, int>();

        public static readonly SymptomCategory[] Departments =
            { SymptomCategory.Cardiopulmonary, SymptomCategory.Internal, SymptomCategory.Infectious, SymptomCategory.Trauma };

        public int Treatments(SymptomCategory department) => treatments.TryGetValue(department, out int n) ? n : 0;
        public bool IsAccredited(SymptomCategory department) => Treatments(department) >= threshold;

        public int AccreditedCount
        {
            get { int n = 0; foreach (var d in Departments) if (IsAccredited(d)) n++; return n; }
        }

        public bool AllAccredited => AccreditedCount == Departments.Length;

        /// <summary>Records a treatment; returns <c>true</c> if this treatment just accredited the department.</summary>
        public bool Record(SymptomCategory department)
        {
            bool before = IsAccredited(department);
            treatments[department] = Treatments(department) + 1;
            return !before && IsAccredited(department);
        }
    }

    /// <summary>
    /// Tracks department accreditation from treatments. An accredited department's fallback treatment
    /// costs 1 AP less. Accrediting all four is required to win at an inspection.
    /// </summary>
    public class AccreditationManager : Singleton<AccreditationManager>
    {
        [Tooltip("Treatments needed in a department to accredit it.")]
        [Min(1)] public int treatmentsToAccredit = 4;
        [Tooltip("AP discount on an accredited department's fallback treatment button.")]
        [Min(0)] public int fallbackDiscount = 1;

        public readonly AccreditationTracker tracker = new AccreditationTracker();

        /// <summary>Raised whenever a treatment is recorded (for the HUD).</summary>
        public event System.Action OnChanged;

        protected override void Awake()
        {
            base.Awake();
            tracker.threshold = treatmentsToAccredit;
            PatientCardView.OnSymptomTreated += HandleSymptomTreated;
            DepartmentFallbackButton.costAdjustment = FallbackAdjustment;
        }

        protected override void OnDestroy()
        {
            PatientCardView.OnSymptomTreated -= HandleSymptomTreated;
            if (DepartmentFallbackButton.costAdjustment == (System.Func<SymptomCategory, int>)FallbackAdjustment)
                DepartmentFallbackButton.costAdjustment = null;
            base.OnDestroy();
        }

        public bool IsAccredited(SymptomCategory department) => tracker.IsAccredited(department);

        private int FallbackAdjustment(SymptomCategory department) => tracker.IsAccredited(department) ? -fallbackDiscount : 0;

        private void HandleSymptomTreated(PatientCardView patient, SymptomCategory department)
        {
            tracker.threshold = treatmentsToAccredit;
            if (tracker.Record(department))
            {
                string name = DirectiveManager.DepartmentName(department);
                GameLog.Info($"ACCREDITED: {name}");
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.ShowNotification($"{name} ACCREDITED!\n({tracker.AccreditedCount}/4 departments)", new Color(0.4f, 1f, 0.6f));
            }
            OnChanged?.Invoke();
        }
    }
}
