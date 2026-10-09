using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Editor.Tools
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class AutoFixDraftPool
    {
        [MenuItem("Chief Complaint/Data Tools/Rebuild Draft Pool")]
        public static void FixPool()
        {
            var draftManager = Object.FindFirstObjectByType<DraftManager>();
            if (draftManager != null)
            {
                draftManager.draftableCardsPool.Clear();
                string[] guids = AssetDatabase.FindAssets("t:ActionCardSO", new[] { "Assets/Data/Actions/Draft" });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    ActionCardSO card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(path);
                    if (card != null) draftManager.draftableCardsPool.Add(card);
                }
                EditorUtility.SetDirty(draftManager);
                EditorSceneManager.MarkSceneDirty(draftManager.gameObject.scene);
                Debug.Log("Draft Pool Automatically Fixed! Added " + draftManager.draftableCardsPool.Count + " draft cards. Upgrade cards excluded.");
            }
        }
    }

}
