using UnityEngine;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.UI;

namespace ChiefComplaint.Cards.Effects
{
    [CreateAssetMenu(fileName = "TrashCardEffect", menuName = "Chief Complaint/Effects/Trash Card")]
    public class TrashCardEffectSO : CardEffectSO
    {
        [Tooltip("Only Red Tape cards can be trashed (e.g. Sharps Bin), so the card can't thin the player's useful cards.")]
        public bool redTapeOnly;

        public override bool RequiresPatientTarget => false;
        public override bool RequiresCardTarget => true;

        public override string Describe() => redTapeOnly ? "Dispose of a Red Tape card in hand" : "Trash a card in hand";

        public override bool CanApply(PatientCardView targetPatient, ActionCardView targetCard) =>
            targetCard != null && (!redTapeOnly ||
                (targetCard.currentCardData != null && targetCard.currentCardData.cardType == ActionCardType.RedTape));

        public override bool Apply(PatientCardView targetPatient, ActionCardView targetCard, bool rngFailed, Vector3 mousePos)
        {
            if (rngFailed)
            {
                Debug.Log("Card effect failed due to RNG!");
                return true;
            }
            
            if (DeckManager.Instance != null)
            {
                GameFeedback.ShowAt($"{targetCard.currentCardData.cardName} trashed", targetCard.transform.position, GameFeedback.CardColour);
                if (HandView.Instance != null) HandView.Instance.RemoveCardVisual(targetCard.gameObject);
                DeckManager.Instance.RemoveCardFromHand(targetCard.currentCardData, false);
            }
            return true;
        }
    }
}
