using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Core
{
    /// <summary>
    /// Symptom upgrade tokens: one colour per department (<see cref="SymptomCategory"/>), earned by treating a
    /// symptom there and spent, with Funding, on card upgrades. Plain C# so the rules can be tested without a scene.
    /// </summary>
    public class UpgradeTokenWallet
    {
        private readonly int[] counts = new int[4];

        public int Get(SymptomCategory colour) => counts[(int)colour];

        public int Total => counts[0] + counts[1] + counts[2] + counts[3];

        public void Add(SymptomCategory colour, int amount = 1)
        {
            if (amount > 0) counts[(int)colour] += amount;
        }

        /// <summary><c>true</c> if <paramref name="cost"/> tokens can be paid: of <paramref name="colour"/>, or of any colours when <paramref name="anyColour"/>.</summary>
        public bool CanPay(int cost, bool anyColour, SymptomCategory colour) =>
            cost <= 0 || (anyColour ? Total : Get(colour)) >= cost;

        /// <summary>
        /// Pays <paramref name="cost"/> tokens if possible. An any-colour cost is taken from whichever colours the
        /// player holds most of, so it never drains a colour they're short of.
        /// </summary>
        public bool TryPay(int cost, bool anyColour, SymptomCategory colour)
        {
            if (!CanPay(cost, anyColour, colour)) return false;
            if (!anyColour)
            {
                counts[(int)colour] -= cost;
                return true;
            }
            for (int paid = 0; paid < cost; paid++)
            {
                int most = 0;
                for (int i = 1; i < counts.Length; i++)
                    if (counts[i] > counts[most]) most = i;
                counts[most]--;
            }
            return true;
        }

        /// <summary>Player-facing name of a token colour, after its department.</summary>
        public static string ColourName(SymptomCategory colour) => colour switch
        {
            SymptomCategory.Cardiopulmonary => "Cardiology",
            SymptomCategory.Infectious => "Isolation",
            SymptomCategory.Internal => "Internal Med",
            _ => "Trauma"
        };
    }
}
