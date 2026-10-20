using System.Collections.Generic;
using ChiefComplaint.Cards.Data;

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
}
