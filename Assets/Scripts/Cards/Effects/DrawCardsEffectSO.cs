using UnityEngine;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    /// <summary>Draws cards from the deck. Useful as one option of a <see cref="ChoiceEffectSO"/>.</summary>
    [CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "Chief Complaint/Effects/Draw Cards")]
    public class DrawCardsEffectSO : CardEffectSO
    {
        [Min(1)] public int cardsToDraw = 1;

        public override bool RequiresPatientTarget => false;
        public override bool RequiresCardTarget => false;

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) => DeckManager.Instance != null;

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                GameFeedback.WarnAt("Failed!", mousePos);
                return true;
            }

            DeckManager.Instance.DrawCards(cardsToDraw);
            return true;
        }
    }
}
