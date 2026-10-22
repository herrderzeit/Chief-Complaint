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
    /// "Page the on-call team": a clickable plate under each department slot. Spends raw AP to treat a
    /// diagnosed patient's symptom in that department without a Treatment card. The cost scales with the
    /// symptom tier (see <see cref="PagingRules"/>), always 1 AP more than the matching card, because calling a
    /// team in is slower than having a prepared order.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DepartmentFallbackButton : MonoBehaviour, IBoardPointerHandler
    {
        public DepartmentSlot departmentSlot;

        [Tooltip("The TextMeshPro (World Space) component to show the AP cost.")]
        public TextMeshPro costText;

        [Tooltip("The plate background; tinted for available / unavailable.")]
        public SpriteRenderer buttonSprite;

        [Header("Page the On-Call Team")]
        [Tooltip("Top line of the plate, e.g. PAGE CARDIOLOGY. Set from the department at runtime.")]
        public TextMeshPro labelText;
        [Tooltip("Plate border, tinted with the department colour.")]
        public SpriteRenderer border;
        [Tooltip("Hover tooltip explaining the mechanic (shown after a short delay).")]
        public GameObject tooltipRoot;
        [Min(0f)] public float tooltipDelay = 0.5f;

        private static readonly Color PlateColour = new Color(0.06f, 0.09f, 0.13f, 0.95f);
        private static readonly Color PlateDisabledColour = new Color(0.06f, 0.09f, 0.13f, 0.6f);
        private static readonly Color DisabledTextColour = new Color(0.6f, 0.65f, 0.7f, 1f);
        private float hoverTime;

        private PatientCardView currentPatient;
        private int currentAPCost = 0;
        private SymptomTier? targetTier = null;
        private bool isInteractable = false;

        void Awake()
        {
            if (departmentSlot == null)
                departmentSlot = GetComponentInParent<DepartmentSlot>();
            if (labelText != null && departmentSlot != null) labelText.text = PageLabel(departmentSlot.category);
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        /// <summary>The plate's call-to-action for each department.</summary>
        public static string PageLabel(SymptomCategory category) => category switch
        {
            SymptomCategory.Cardiopulmonary => "PAGE CARDIOLOGY",
            SymptomCategory.Internal => "PAGE MEDICINE",
            SymptomCategory.Infectious => "PAGE INFECTION CONTROL",
            _ => "TRAUMA CALL"
        };

        /// <summary>The department's name as used in the "paged" popup.</summary>
        public static string DepartmentName(SymptomCategory category) => category switch
        {
            SymptomCategory.Cardiopulmonary => "Cardiology",
            SymptomCategory.Internal => "Medicine",
            SymptomCategory.Infectious => "Infection Control",
            _ => "Trauma team"
        };

        /// <summary>Department accent colour, matching the department artwork.</summary>
        public static Color DepartmentColour(SymptomCategory category) => category switch
        {
            SymptomCategory.Cardiopulmonary => new Color(0.2f, 0.75f, 1f),
            SymptomCategory.Internal => new Color(1f, 0.62f, 0.15f),
            SymptomCategory.Infectious => new Color(0.3f, 0.95f, 0.4f),
            _ => new Color(1f, 0.27f, 0.27f)
        };

        void Update()
        {
            if (departmentSlot == null || GameManager.Instance == null) return;
            UpdateTooltip();

            // 1. Find the patient in this slot
            currentPatient = departmentSlot.GetComponentInChildren<PatientCardView>();

            if (!GameManager.Instance.CanInteract || currentPatient == null || !departmentSlot.isAvailable || !currentPatient.isFullyDiagnosed)
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

        private int GetAPCostForTier(SymptomTier tier) => PagingRules.ApCost(tier);

        private void DisableButton(string text) => ApplyStyle(false, text);

        private void EnableButton(string text) => ApplyStyle(true, text);

        /// <summary>Available: dark plate, bright department border, white text. Unavailable: dimmed.</summary>
        private void ApplyStyle(bool available, string text)
        {
            isInteractable = available;
            Color accent = departmentSlot != null ? DepartmentColour(departmentSlot.category) : Color.white;

            if (costText != null)
            {
                costText.text = text;
                costText.color = available ? Color.white : DisabledTextColour;
            }
            if (labelText != null) labelText.color = available ? accent : DisabledTextColour;
            if (buttonSprite != null) buttonSprite.color = available ? PlateColour : PlateDisabledColour;
            if (border != null) border.color = new Color(accent.r, accent.g, accent.b, available ? 0.9f : 0.3f);
        }

        /// <summary>Shows the explanation tooltip after the mouse rests on the plate for a moment.</summary>
        private void UpdateTooltip()
        {
            if (tooltipRoot == null || buttonSprite == null) return;
            bool over = false;
            Camera cam = CameraUtil.Main;
            if (cam != null && GameManager.Instance.CanInteract)
            {
                over = GameInput.IsPointerOver(buttonSprite.bounds, cam);
            }
            hoverTime = over ? hoverTime + Time.deltaTime : 0f;
            bool show = over && hoverTime >= tooltipDelay;
            if (tooltipRoot.activeSelf != show) tooltipRoot.SetActive(show);
        }

        public void PointerDown()
        {
            if (!isInteractable || currentPatient == null || targetTier == null) return;
            if (!GameManager.Instance.CanInteract) return; // board locked (draft, choice prompt, pause)

            if (GameManager.Instance.CurrentAP >= currentAPCost)
            {
                // Treat first and only pay if it worked (TryCureSymptom marks the department "IN USE").
                int cost = currentAPCost;
                Vector3 popupAt = currentPatient.transform.position;
                SymptomCategory department = departmentSlot.category;
                if (currentPatient.TryCureSymptom(targetTier.Value))
                {
                    GameManager.Instance.SpendAP(cost);
                    GameFeedback.ShowAt($"{DepartmentName(department)} paged", popupAt, DepartmentColour(department));
                }
            }
        }
    }

}
