using System.Collections.Generic;

namespace ChiefComplaint.Bureaucracy
{
    public enum CriterionType
    {
        DepartmentsAccredited,
        PatientsCured,
        NoDeaths,
        CitationsAtMost,
        RedTapeAtMost,
        NoCodeBlue
    }

    public enum InspectionOutcome
    {
        Pass,
        Conditional,
        Fail
    }

    public struct InspectionCriterion
    {
        public CriterionType type;
        public int target;

        public string Describe() => type switch
        {
            CriterionType.DepartmentsAccredited => $"At least {target} department{(target == 1 ? "" : "s")} accredited",
            CriterionType.PatientsCured => $"At least {target} patients cured since the last inspection",
            CriterionType.NoDeaths => "No patient deaths since the last inspection",
            CriterionType.CitationsAtMost => $"No more than {target} citations since the last inspection",
            CriterionType.RedTapeAtMost => $"No more than {target} Red Tape cards in your deck",
            _ => "No Code Blue patients on the board"
        };
    }

    /// <summary>The hospital's state at inspection time.</summary>
    public struct InspectionSnapshot
    {
        public int accreditedDepartments;
        public bool allAccredited;
        public int curedSinceLast;
        public int deathsSinceLast;
        public int citationsSinceLast;
        public int redTapeInDeck;
        public int codeBlues;
    }

    /// <summary>Pure inspection rules: which criteria are checked, whether they pass, and the outcome.</summary>
    public static class InspectionRules
    {
        /// <summary>
        /// Criteria for inspection number <paramref name="n"/> (1-based). Accreditation progress is always
        /// checked (targets 1, 2, 3, then 4); two more are drawn from the rest of the pool.
        /// </summary>
        public static List<InspectionCriterion> Draw(int n, System.Random rng)
        {
            var list = new List<InspectionCriterion>
            {
                new InspectionCriterion { type = CriterionType.DepartmentsAccredited, target = System.Math.Min(4, n) }
            };

            var pool = new List<InspectionCriterion>
            {
                new InspectionCriterion { type = CriterionType.PatientsCured, target = System.Math.Min(7, 1 + 2 * n) },
                new InspectionCriterion { type = CriterionType.NoDeaths },
                new InspectionCriterion { type = CriterionType.CitationsAtMost, target = 3 },
                new InspectionCriterion { type = CriterionType.RedTapeAtMost, target = System.Math.Max(0, 3 - n) },
                new InspectionCriterion { type = CriterionType.NoCodeBlue }
            };

            for (int i = 0; i < 2 && pool.Count > 0; i++)
            {
                int pick = rng.Next(pool.Count);
                list.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            return list;
        }

        public static bool Passes(InspectionCriterion c, InspectionSnapshot s) => c.type switch
        {
            CriterionType.DepartmentsAccredited => s.accreditedDepartments >= c.target,
            CriterionType.PatientsCured => s.curedSinceLast >= c.target,
            CriterionType.NoDeaths => s.deathsSinceLast == 0,
            CriterionType.CitationsAtMost => s.citationsSinceLast <= c.target,
            CriterionType.RedTapeAtMost => s.redTapeInDeck <= c.target,
            _ => s.codeBlues == 0
        };

        public static int PassedCount(IList<InspectionCriterion> criteria, InspectionSnapshot s)
        {
            int passed = 0;
            foreach (var c in criteria) if (Passes(c, s)) passed++;
            return passed;
        }

        /// <summary>All passed is a Pass, all but one is Conditional, anything less is a Fail.</summary>
        public static InspectionOutcome Evaluate(IList<InspectionCriterion> criteria, InspectionSnapshot s)
        {
            int passed = PassedCount(criteria, s);
            if (passed >= criteria.Count) return InspectionOutcome.Pass;
            if (passed == criteria.Count - 1) return InspectionOutcome.Conditional;
            return InspectionOutcome.Fail;
        }

        /// <summary>The run is won by passing (fully or conditionally) while every department is accredited.</summary>
        public static bool IsWin(InspectionOutcome outcome, InspectionSnapshot s) => outcome != InspectionOutcome.Fail && s.allAccredited;
    }
}
