using UnityEngine;
using UnityEditor;

namespace ChiefComplaint.Editor
{
    public static class TutorialDebug
    {
        [MenuItem("Chief Complaint/Debug/Reset Tutorial Flag")]
        public static void ResetTutorial()
        {
            PlayerPrefs.DeleteKey("TutorialCompleted");
            PlayerPrefs.Save();
            Debug.Log("[TutorialDebug] Tutorial flag reset. The prompt will appear on next play.");
        }
    }
}
