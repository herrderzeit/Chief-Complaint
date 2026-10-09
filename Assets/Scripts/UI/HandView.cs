using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;
using ChiefComplaint.Cards.Data;
using ChiefComplaint.Cards.Views;
using ChiefComplaint.Utils;

namespace ChiefComplaint.UI
{
    public class HandView : Singleton<HandView>
    {
        [Header("Visuals")]
        public GameObject actionCardPrefab;
        public Transform handZone;
        public float cardSpacing = 3.5f;
        public float handLiftHeight = 3.0f;
        public float handLiftSpeed = 8f;

        private Vector3 baseHandZonePos;
        [HideInInspector] public bool isDraggingCard = false;
        
        private IObjectPool<ActionCardView> cardPool;
        public List<ActionCardView> activeCards = new List<ActionCardView>();

        protected override void Awake()
        {
            base.Awake();
            if (handZone != null) baseHandZonePos = handZone.localPosition;
            
            cardPool = new ObjectPool<ActionCardView>(
                CreateCard,
                OnGetCard,
                OnReleaseCard,
                OnDestroyCard,
                false, 10, 20
            );
        }

        private ActionCardView CreateCard()
        {
            GameObject obj = Instantiate(actionCardPrefab, handZone);
            return obj.GetComponent<ActionCardView>();
        }

        private void OnGetCard(ActionCardView card)
        {
            card.gameObject.SetActive(true);
        }

        private void OnReleaseCard(ActionCardView card)
        {
            card.gameObject.SetActive(false);
        }

        private void OnDestroyCard(ActionCardView card)
        {
            Destroy(card.gameObject);
        }

        public void SpawnCardVisual(ActionCardSO cardData)
        {
            if (actionCardPrefab == null || handZone == null) return;
            ActionCardView cardView = cardPool.Get();
            cardView.InitialiseCard(cardData);
            activeCards.Add(cardView);
            UpdateHandLayout();
        }

        public void ClearHandVisuals()
        {
            foreach (var card in activeCards)
            {
                cardPool.Release(card);
            }
            activeCards.Clear();
        }

        public void RemoveCardVisual(GameObject cardObject)
        {
            ActionCardView view = cardObject.GetComponent<ActionCardView>();
            if (activeCards.Contains(view))
            {
                activeCards.Remove(view);
                cardPool.Release(view);
                UpdateHandLayout();
            }
        }

        public void RemoveCardVisualAtIndex(int index)
        {
            if (index >= 0 && index < activeCards.Count)
            {
                ActionCardView view = activeCards[index];
                activeCards.RemoveAt(index);
                cardPool.Release(view);
                UpdateHandLayout();
            }
        }

        public void UpdateHandLayout()
        {
            float totalWidth = (activeCards.Count - 1) * cardSpacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i] == null) continue;
                Vector3 targetPosition = new Vector3(startX + (i * cardSpacing), 0, -i * 0.1f);
                activeCards[i].transform.localPosition = targetPosition;
            }
        }

        private void Update()
        {
            if (handZone == null) return;

            bool shouldLift = false;
            if (!isDraggingCard && activeCards.Count > 0)
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                Camera cam = CameraUtil.Main;
                if (mouse != null && cam != null)
                {
                    Vector3 viewportPos = cam.ScreenToViewportPoint(mouse.position.ReadValue());
                    shouldLift = viewportPos.y < 0.22f && viewportPos.y >= 0f && viewportPos.x > 0.05f && viewportPos.x < 0.95f;
                }
            }

            Vector3 targetPos = shouldLift ? baseHandZonePos + new Vector3(0, handLiftHeight, 0) : baseHandZonePos;
            handZone.localPosition = Vector3.Lerp(handZone.localPosition, targetPos, Time.deltaTime * handLiftSpeed);
        }
    }
}
