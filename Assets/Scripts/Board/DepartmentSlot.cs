using UnityEngine;
using ChiefComplaint.Board;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Interaction;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Core;
using ChiefComplaint.Drafting;
using ChiefComplaint.UI;
using ChiefComplaint.Utils;

namespace ChiefComplaint.Board
{
    /// <summary>
    /// Represents a single specialty department zone on the board (e.g. Trauma Bay, Cardiology).
    /// Tracks availability for the current shift and swaps the indicator light sprite
    /// between an active "IN USE" state and an idle state.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, null, null)]
    public class DepartmentSlot : MonoBehaviour
    {
        [Header("Visual References")]
        public SpriteRenderer indicatorLight;

        [Header("Specialty")]
        [Tooltip("The symptom category this department treats. Replaces matching by Unity tag.")]
        public SymptomCategory category;

        [Header("State")]
        public bool isAvailable = true;

        [Header("Sprites")]
        public Sprite availableSprite;
        public Sprite usedSprite;

        void Start()
        {
            ResetZone();
        }

        private SpriteRenderer _sr;
        private Color _originalColor;
        private Color _targetColor;
        private bool _isInitialized = false;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _originalColor = _sr.color;
                _targetColor = _originalColor;
                _isInitialized = true;
            }
        }

        void Update()
        {
            if (_isInitialized && _sr.color != _targetColor)
            {
                _sr.color = Color.Lerp(_sr.color, _targetColor, Time.deltaTime * 5f);
            }
        }

        public void SetTargetColor(Color target)
        {
            if (!_isInitialized) return;
            _targetColor = target;
        }

        public void ResetColor()
        {
            if (!_isInitialized) return;
            _targetColor = _originalColor;
        }

        /// <summary>
        /// Marks this department as used for the current shift.
        /// Sets <see cref="isAvailable"/> to <c>false</c> and swaps the indicator to the "IN USE" sprite.
        /// </summary>
        public void MarkAsUsed()
        {
            isAvailable = false;
            if (indicatorLight != null && usedSprite != null)
            {
                indicatorLight.sprite = usedSprite;
                indicatorLight.color = Color.white;
            }
        }

        /// <summary>
        /// Resets the department at the start of a new shift.
        /// Sets <see cref="isAvailable"/> to <c>true</c> and restores the indicator to the idle sprite.
        /// </summary>
        public void ResetZone()
        {
            isAvailable = true;
            if (indicatorLight != null && availableSprite != null)
            {
                indicatorLight.sprite = availableSprite;
                indicatorLight.color = Color.white;
            }
        }
    }
}
