using UnityEngine;
using UnityEditor;
using ChiefComplaint.Cards.Data;

namespace ChiefComplaint.Editor
{
    public static class PatientCardAuditor
    {
        [MenuItem("Chief Complaint/Data Tools/Audit Max Symptoms")]
        public static void EnforceMaxSymptoms()
        {
            string[] guids = AssetDatabase.FindAssets("t:PatientCardSO");
            int flaggedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PatientCardSO patientCard = AssetDatabase.LoadAssetAtPath<PatientCardSO>(path);

                if (patientCard != null && patientCard.incomingCategories != null && patientCard.incomingCategories.Count > 3)
                {
                    Debug.LogWarning($"[Audit] Patient '{patientCard.name}' at {path} had {patientCard.incomingCategories.Count} symptoms. Truncating to 3.");
                    
                    Undo.RecordObject(patientCard, "Truncate Symptoms to 3");
                    while (patientCard.incomingCategories.Count > 3)
                    {
                        patientCard.incomingCategories.RemoveAt(patientCard.incomingCategories.Count - 1);
                    }
                    EditorUtility.SetDirty(patientCard);
                    flaggedCount++;
                }
            }

            if (flaggedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[Audit Complete] Truncated symptoms to 3 for {flaggedCount} PatientCardSO asset(s).");
            }
            else
            {
                Debug.Log("[Audit Complete] All PatientCardSO assets already conform to the 3-symptom maximum.");
            }
        }
    }
}
