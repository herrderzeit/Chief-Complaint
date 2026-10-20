using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

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
        /// Staff overtime = 1 per patient on the board (max 3); supplies = 1 per 2 treatments this shift;
        /// maintenance = 2 every 3rd shift. Zero-cost bills are left out. Listed in payment priority order.
        /// </summary>
        public static List<Bill> BillsFor(int patientsOnBoard, int treatmentsThisShift, int shift)
        {
            var bills = new List<Bill>();
            int staff = Mathf.Min(3, patientsOnBoard);
            int supplies = treatmentsThisShift / 2;
            int maintenance = shift % 3 == 0 ? 2 : 0;
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

    /// <summary>
    /// End Shift running costs, paid from Funding. Unpaid bills bring a penalty for the next shift,
    /// in the spirit of Papers, Please's end-of-day budget.
    /// </summary>
    public class UpkeepManager : Singleton<UpkeepManager>
    {
        private int treatmentsThisShift;
        private bool staffPenalty, suppliesPenalty, maintenancePenalty;
        private bool pendingStaff, pendingSupplies, pendingMaintenance;

        protected override void Awake()
        {
            base.Awake();
            PatientCardView.OnSymptomTreated += HandleSymptomTreated;
            CardCostModifiers.Add(ExtraCost);
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftEndSteps.Register(40, PresentBills);
                gm.OnShiftStarted += HandleShiftStarted;
            }
        }

        protected override void OnDestroy()
        {
            CardCostModifiers.Remove(ExtraCost);
            PatientCardView.OnSymptomTreated -= HandleSymptomTreated;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.shiftEndSteps.Unregister(PresentBills);
                gm.OnShiftStarted -= HandleShiftStarted;
            }
            base.OnDestroy();
        }

        private void HandleSymptomTreated(PatientCardView patient, SymptomCategory department) => treatmentsThisShift++;

        private int ExtraCost(ActionCardSO card)
        {
            if (staffPenalty && card.cardType == ActionCardType.Staff) return 1;
            if (suppliesPenalty && card.cardType == ActionCardType.Treatment) return 1;
            return 0;
        }

        private void HandleShiftStarted(int shift)
        {
            treatmentsThisShift = 0;
            staffPenalty = pendingStaff; suppliesPenalty = pendingSupplies; maintenancePenalty = pendingMaintenance;
            pendingStaff = pendingSupplies = pendingMaintenance = false;
            CardCostModifiers.NotifyChanged();

            if (maintenancePenalty && BoardView.Instance != null && BoardView.Instance.departmentSlots.Length > 0)
            {
                var slots = BoardView.Instance.departmentSlots;
                DepartmentSlot closed = slots[Random.Range(0, slots.Length)];
                closed.MarkAsUsed(); // closed for repairs: unusable this shift
                if (NotificationManager.Instance != null)
                    NotificationManager.Instance.ShowNotification($"{DirectiveManager.DepartmentName(closed.category)} closed for repairs (unpaid maintenance)", new Color(1f, 0.55f, 0.2f));
            }
        }

        private void PresentBills(System.Action next)
        {
            var gm = GameManager.Instance;
            var board = gm != null ? gm.boardManager : null;
            int patients = 0;
            if (board != null) foreach (var p in board.ActivePatients) if (p != null && !p.isRemoved) patients++;

            var bills = UpkeepRules.BillsFor(patients, treatmentsThisShift, gm.CurrentShift);
            if (bills.Count == 0) { next(); return; }

            int total = 0;
            var body = new StringBuilder();
            body.AppendLine($"<b>Funding available: {gm.CurrentFunding}</b>");
            body.AppendLine();
            foreach (var b in bills)
            {
                total += b.amount;
                body.AppendLine($"{b.Label}: <b>{b.amount}</b>   <size=24><color=#ff9f7a>(if unpaid: {b.UnpaidConsequence})</color></size>");
            }
            body.AppendLine();
            body.AppendLine($"<b>Total due: {total}</b>");
            if (gm.CurrentFunding < total)
                body.AppendLine("<color=#ffd166>You can't cover everything. Paying pays bills in order until the money runs out.</color>");

            BureaucracyDialog.Show("END OF SHIFT BILLS", body.ToString(),
                new BureaucracyDialog.Option($"Pay ({Mathf.Min(total, gm.CurrentFunding)})", () => { Settle(bills, payAll: true); next(); }, BureaucracyDialog.StampGreen),
                new BureaucracyDialog.Option("Skip bills", () => { Settle(bills, payAll: false); next(); }, BureaucracyDialog.StampRed));
        }

        private void Settle(List<Bill> bills, bool payAll)
        {
            var gm = GameManager.Instance;
            List<Bill> unpaid = bills;
            if (payAll)
            {
                unpaid = UpkeepRules.PayInOrder(bills, gm.CurrentFunding, out int spent);
                if (spent > 0) gm.TrySpendFunding(spent);
            }

            foreach (var b in unpaid)
            {
                if (b.type == BillType.StaffOvertime) pendingStaff = true;
                else if (b.type == BillType.Supplies) pendingSupplies = true;
                else pendingMaintenance = true;
                GameLog.Warn($"UNPAID: {b.Label} - {b.UnpaidConsequence}");
            }
        }
    }
}
