using System;
using UnityEngine;
using ChiefComplaint.Core;

namespace ChiefComplaint.UI
{
    public static class TutorialManager
    {
        private static readonly string[] TutorialSteps = new string[]
        {
            "Welcome to the Emergency Department, Chief.\n\nYour goal is to treat Patients arriving in Triage before their Stability drops to 0.",
            "Every Patient arrives with hidden Symptoms. At the end of every Shift, the Patient's Stability decays.\n\nIf a Patient's Stability hits 0, they die. You will suffer a severe Reputation penalty. Lose all Reputation, and it's Game Over.",
            "You have 3 Action Points (AP) per Shift. You spend AP by playing Action Cards from your hand (bottom) to interact with Patients. You must carefully manage your AP to save as many patients as possible.",
            "First, you must Diagnose a Patient. Drag a Diagnostics card onto a Patient to reveal their Symptoms. Without a diagnosis, you can't treat them or move them to a Specialty Department. Some symptoms are more severe (Tier II or III) and require stronger diagnostic cards.",
            "Once diagnosed, the Patient's required Medical Specialties (e.g., Trauma, Internal) are revealed.\n\nYou can now drag the Patient's card from Triage into a matching Specialty Department (the large central zones).",
            "With the Patient in the correct Department, you can play Treatment cards onto them to cure their Symptoms. But beware: each Specialty Department can only be used ONCE per shift!",
            "Need to clear Triage quickly? You can drag a Patient into a Ward bed (the side zones) to stabilise them. Hospitalised Patients decay half as fast, but they still need to visit a Department for treatment.",
            "When all of a Patient's Symptoms are cured, they are successfully discharged! You will earn Reputation and have the chance to draft powerful new cards for your deck to help you handle tougher shifts.",
            "When you run out of AP or can't make a move, click 'End Shift'.\n\nThis triggers Patient decay, discards your remaining hand, draws 5 new cards, and brings new Patients into the ER. Good luck, Chief!"
        };

        public static void ShowStartPrompt(Action onComplete)
        {
            ChoicePrompt.Show(
                "Welcome to Chief Complaint",
                "Play Tutorial", 
                () => StartTutorial(onComplete),
                "Skip", 
                () => FinishTutorial(onComplete)
            );
        }

        private static void StartTutorial(Action onComplete)
        {
            TutorialDialog.Show(TutorialSteps, () => FinishTutorial(onComplete));
        }

        private static void FinishTutorial(Action onComplete)
        {
            PlayerPrefs.SetInt(ChiefComplaint.Core.GameManager.TutorialCompletedKey, 1);
            PlayerPrefs.Save();
            onComplete?.Invoke();
        }
    }
}
