using UnityEngine;

/// <summary>Defines the five categories of playable action cards in the game.</summary>
public enum ActionCardType
{
    Staff,
    Diagnostic,
    Treatment,
    Utility,
    RedTape,
    Administration,
    Supply
}

/// <summary>Keywords used for synergistic combos across cards.</summary>
public enum CardTag
{
    None,
    Surgical,
    Pharmacy,
    Emergency,
    Training,
    Management,
    Policy,
    Lab,
    Rookie,
    /// <summary>Doctor ladder (Medical Student, Licensed Doctor, Chief of Medicine). Pairs with Nurse for Team Assessment.</summary>
    Doctor,
    /// <summary>Nurse ladder (Nursing Student, Registered Nurse, Chief Nurse). Pairs with Doctor for Team Assessment.</summary>
    Nurse
}

namespace ChiefComplaint.Cards.Data
{
    /// <summary>
    /// ScriptableObject data container for a single action card. Defines all static asset
    /// properties: identity, costs, mechanical effects, draft valuation, and visuals.
    /// Runtime mutable state (e.g. which pile the card is in) lives in <see cref="DeckManager"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Action Card", menuName = "Chief Complaint/Action Card")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class ActionCardSO : ScriptableObject
    {
        [Header("Card Identity")]
    public string cardName = "First-Year Resident";
    public ActionCardType cardType = ActionCardType.Staff;

    [Header("Synergy Tags")]
    public System.Collections.Generic.List<CardTag> tags = new System.Collections.Generic.List<CardTag>();

    [Header("Card Text")]
    [TextArea(2, 3)]
    [Tooltip("The actual mechanical effect (e.g., 'Unmask 1 hidden symptom.')")]
    public string rulesText;

    [TextArea(2, 3)]
    [Tooltip("The lore/story text (e.g., 'Rush a blood panel to the lab.')")]
    public string flavorText;

    [Header("Costs & Playability")]
    [Tooltip("If false, this card cannot be dragged or played (e.g., Insurance Paperwork).")]
    public bool isPlayable = true;
    
    [Range(0f, 1f)]
    [Tooltip("Probability that the card's effect successfully triggers (1.0 = 100%). If it fails, the AP and card are still consumed.")]
    public float successChance = 1.0f;

    [Range(0, 5)]
    public int apCost = 0;

    [Min(0)]
    [Tooltip("Reputation paid when the card is played. All or nothing: the card can't be played unless Reputation is above this cost, and it is only paid if the card's effect lands.")]
    public int reputationCost = 0;

    [Header("Card Draw & Hand Manipulation")]
    [Tooltip("How many cards the player draws when this card is played.")]
    public int cardsToDraw;

    [Header("Draft Valuation")]
    [Tooltip("1 = Basic, 2 = Specialised, 3 = Game-changing")]
    public int powerTier = 1;

    [Space(10)]
    [Tooltip("If true, this card is removed from the run entirely when played (doesn't go to discard pile).")]
    public bool exhaustsOnUse;

    [Tooltip("If true, this card automatically exhausts if it is left in your hand at the end of the shift.")]
    public bool exhaustsAtShiftEnd;

    [Header("Effects (Strategy Pattern)")]
    public System.Collections.Generic.List<ChiefComplaint.Cards.Effects.CardEffectSO> effects = new System.Collections.Generic.List<ChiefComplaint.Cards.Effects.CardEffectSO>();

    [Header("Draft Phase Upgrades")]
    [Tooltip("The card this upgrades into when paying Funding in the Draft screen.")]
    public ActionCardSO directDraftUpgrade;
    [Tooltip("The Funding cost to upgrade to the directDraftUpgrade card.")]
    public int draftUpgradeCost = 5;

    [Tooltip("Upgrade tokens needed for the direct upgrade, on top of Funding. Tokens are earned by treating symptoms in a department.")]
    [Min(0)] public int upgradeTokenCost = 0;
    [Tooltip("If set, any token colour pays; otherwise only Upgrade Token Colour does.")]
    public bool upgradeTokenAnyColour = false;
    [Tooltip("Department colour of the tokens this upgrade needs.")]
    public SymptomCategory upgradeTokenColour = SymptomCategory.Trauma;

    [Header("Visuals")]
    public Sprite cardArt;

    // -------------------------------------------------------------------------
    // Draft Valuation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calculates a dynamic Funding cost for the draft market based on the card's
    /// type, power tier, AP cost, and card-draw value.
    /// <list type="bullet">
    ///   <item><description>Base cost by card type (Staff = 4, Utility/Diagnostic/Treatment = 3, RedTape = 0).</description></item>
    ///   <item><description>Exponential power-tier scaling: +tier² (Tier 1 = +1, Tier 2 = +4, Tier 3 = +9).</description></item>
    ///   <item><description>AP economy adjustment: free cards (+2), expensive cards (≥2 AP, −1).</description></item>
    ///   <item><description>Card-draw tax: +2 per card drawn.</description></item>
    /// </list>
    /// RedTape cards always return 0. All other results are clamped to a minimum of 2.
    /// </summary>
    /// <returns>The Funding cost to purchase this card during a draft phase.</returns>
    /// <summary>
    /// What playing this card on the play zone gives, one result per line (e.g. "+3 AP" / "-2 Rep"), for the
    /// drag preview. Empty if the card does nothing without a target.
    /// </summary>
    public string PlayZonePreview()
    {
        var lines = new System.Collections.Generic.List<string>();
        foreach (var effect in effects)
        {
            if (effect == null) continue;
            string text = effect.DescribeOnPlayZone();
            if (!string.IsNullOrEmpty(text)) lines.Add(text);
        }
        if (cardsToDraw > 0) lines.Add($"+{cardsToDraw} Card{(cardsToDraw == 1 ? "" : "s")}");
        if (reputationCost > 0) lines.Add($"-{reputationCost} Rep");
        if (exhaustsOnUse && lines.Count > 0) lines.Add("Exhausts");
        if (lines.Count > 0 && successChance < 1f) lines.Add($"{Mathf.RoundToInt(successChance * 100f)}% chance");
        return string.Join("\n", lines);
    }

    public int CalculateDraftValue()
    {
        int cost = cardType switch
        {
            ActionCardType.Staff          => 4,
            ActionCardType.Utility        => 3,
            ActionCardType.Diagnostic     => 3,
            ActionCardType.Treatment      => 3,
            ActionCardType.Administration => 3,
            ActionCardType.Supply         => 2,
            ActionCardType.RedTape        => 0,
            _ => 2
        };

        // Exponential tier scaling: Tier 1 = +1, Tier 2 = +4, Tier 3 = +9.
        cost += (powerTier * powerTier);

        // Free cards are high-value; expensive cards impose a slight discount.
        if (apCost == 0) cost += 2;
        else if (apCost >= 2) cost -= 1;

        if (cardsToDraw > 0) cost += (cardsToDraw * 2);

        return cardType == ActionCardType.RedTape ? 0 : Mathf.Max(2, cost);
    }
}
}