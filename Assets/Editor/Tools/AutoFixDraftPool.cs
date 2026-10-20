using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Drafting;

namespace ChiefComplaint.Editor.Tools
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class AutoFixDraftPool
    {
        /// <summary>
        /// Adds a supply-room pile for every card in Assets/Data/Actions/Draft that doesn't have one yet
        /// (Walk-In stage, 3 stock). Existing piles, with their stages and stock, are left untouched.
        /// </summary>
        [MenuItem("Chief Complaint/Data Tools/Add Missing Supply Piles")]
        public static void FixPool()
        {
            var draftManager = Object.FindAnyObjectByType<DraftManager>();
            if (draftManager == null) return;

            Undo.RecordObject(draftManager, "Add Missing Supply Piles");
            int added = 0;
            string[] guids = AssetDatabase.FindAssets("t:ActionCardSO", new[] { "Assets/Data/Actions/Draft" });
            foreach (string guid in guids)
            {
                ActionCardSO card = AssetDatabase.LoadAssetAtPath<ActionCardSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (card == null || draftManager.supplyPiles.Exists(p => p != null && p.card == card)) continue;
                draftManager.supplyPiles.Add(new SupplyPileConfig { card = card });
                added++;
            }
            EditorUtility.SetDirty(draftManager);
            EditorSceneManager.MarkSceneDirty(draftManager.gameObject.scene);
            Debug.Log($"Added {added} supply pile(s). Set each new pile's stage and stock on DraftManager.");
        }
    }
}
