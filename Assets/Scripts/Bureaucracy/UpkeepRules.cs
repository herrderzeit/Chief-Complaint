using System.Collections.Generic;
using UnityEngine;

namespace ChiefComplaint.Bureaucracy
{
    public enum BillType
    {
        StaffOvertime,
        Supplies,
        Maintenance
    }

    public struct Bill
    {
        public BillType type;
        public int amount;

        public string Label => type switch
        {
            BillType.StaffOvertime => "Staff overtime",
            BillType.Supplies => "Medical supplies",
            _ => "Building maintenance"
        };

        public string UnpaidConsequence => type switch
        {
            BillType.StaffOvertime => "Staff cards cost +1 AP next shift",
            BillType.Supplies => "Treatment cards cost +1 AP next shift",
            _ => "A random department is closed next shift"
        };
    }

    /// <summary>Pure upkeep rules: the bills due at End Shift and which ones a balance can cover.</summary>
    public static class UpkeepRules
    {
        /// <summary>
        /// Staff overtime = 1 per 2 patients on the board, rounded up (max 2); supplies = 1 per 2 treatments
        /// this shift; maintenance = 2 every 4th shift. Zero-cost bills are left out. Listed in payment priority order.
        /// </summary>
        public static List<Bill> BillsFor(int patientsOnBoard, int treatmentsThisShift, int shift)
        {
            var bills = new List<Bill>();
            int staff = Mathf.Min(2, (patientsOnBoard + 1) / 2);
            int supplies = treatmentsThisShift / 2;
            int maintenance = shift % 4 == 0 ? 2 : 0;
            if (staff > 0) bills.Add(new Bill { type = BillType.StaffOvertime, amount = staff });
            if (supplies > 0) bills.Add(new Bill { type = BillType.Supplies, amount = supplies });
            if (maintenance > 0) bills.Add(new Bill { type = BillType.Maintenance, amount = maintenance });
            return bills;
        }

        /// <summary>Pays bills in order while <paramref name="funding"/> lasts. Returns the unpaid ones and the amount spent.</summary>
        public static List<Bill> PayInOrder(IList<Bill> bills, int funding, out int spent)
        {
            var unpaid = new List<Bill>();
            spent = 0;
            foreach (var b in bills)
            {
                if (funding - spent >= b.amount) spent += b.amount;
                else unpaid.Add(b);
            }
            return unpaid;
        }
    }
}
