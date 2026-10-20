using UnityEngine;
using TMPro;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.UI
{
    /// <summary>
    /// Attached to a clickable 2D object on the DepartmentSlot prefab.
    /// Allows the player to spend raw AP to treat a patient's symptom directly,
    /// bypassing the need for a Treatment card. The cost scales with the symptom tier.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DepartmentFallbackButton : MonoBehaviour
    {
        public DepartmentSlot departmentSlot;

        [Tooltip("The TextMeshPro (World Space) component to show the AP cost.")]
        public TextMeshPro costText;

        [Tooltip("Optional: A sprite renderer to tint when disabled.")]
        public SpriteRenderer buttonSprite;

        private PatientCardView currentPatient;
        private int currentAPCost = 0;
        private SymptomTier? targetTier = null;
        private bool isInteractable = false;

        void Awake()
        {
            if (departmentSlot == null)
                departmentSlot = GetComponentInParent<DepartmentSlot>();
        }

        void Update()
        {
            if (departmentSlot == null || GameManager.Instance == null) return;

            // 1. Find the patient in this slot
            currentPatient = departmentSlot.GetComponentInChildren<PatientCardView>();

            if (currentPatient == null || !departmentSlot.isAvailable || !currentPatient.isFullyDiagnosed)
            {
                DisableButton("---");
                return;
            }

            // 2. Find the lowest tier symptom matching this department
            targetTier = currentPatient.GetLowestMatchingSymptomTier(departmentSlot.category);

            if (targetTier == null)
            {
                DisableButton("---");
                return;
            }

            // 3. Calculate AP Cost based on scaling rules (Tier 1 = 2 AP, Tier 2 = 3 AP, Tier 3 = 4 AP)
            currentAPCost = GetAPCostForTier(targetTier.Value);

            // 4. Check if player can afford it
            if (GameManager.Instance.CurrentAP >= currentAPCost)
            {
                EnableButton($"{currentAPCost} AP");
            }
            else
            {
                DisableButton($"{currentAPCost} AP");
            }
        }

        private int GetAPCostForTier(SymptomTier tier)
        {
            return tier switch
            {
                SymptomTier.Tier1 => 2,
                SymptomTier.Tier2 => 3,
                SymptomTier.Tier3 => 4,
                _ => 4
            };
        }

        private void DisableButton(string text)
        {
            isInteractable = false;
            if (costText != null) costText.text = text;
            if (buttonSprite != null) buttonSprite.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Grey out
        }

        private void EnableButton(string text)
        {
            isInteractable = true;
            if (costText != null) costText.text = text;
            if (buttonSprite != null) buttonSprite.color = Color.white;
        }

        private void OnMouseDown()
        {
            if (!isInteractable || currentPatient == null || targetTier == null) return;

            if (GameManager.Instance.CurrentAP >= currentAPCost)
            {
                // Treat first and only pay if it worked (TryCureSymptom marks the department "IN USE").
                int cost = currentAPCost;
                if (currentPatient.TryCureSymptom(targetTier.Value))
                    GameManager.Instance.SpendAP(cost);
            }
        }
    }

}
