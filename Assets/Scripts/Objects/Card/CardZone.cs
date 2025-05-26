using Manager;
using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드가 배치되는 Zone (손패, 필드 등)을 관리하는 컴포넌트
    /// - 카드 리스트 관리
    /// - 배치는 CardLayoutHelper에 위임
    /// - 카드의 Sprite 및 Material 설정 포함
    /// </summary>
    public class CardZone : MonoBehaviour
    {
        public enum ZoneType { Hand, Field }
        public enum OwnerType { Player, Opponent }

        [Header("Zone Settings")]
        public ZoneType Zone => zoneType;
        [SerializeField] private ZoneType zoneType;
        public OwnerType Owner => ownerType;
        [SerializeField] private OwnerType ownerType;

        [Header("Layout Helper")]
        [SerializeField] private CardLayoutHelper layoutHelper;

        private readonly List<Transform> cards = new List<Transform>();

        public static Transform AllZonesRoot { get; private set; }

        private void Awake()
        {
            if (AllZonesRoot == null)
            {
                AllZonesRoot = transform.parent;
            }
        }

        /// <summary>
        /// 카드 추가 후 배치 갱신
        /// </summary>
        public void AddCard(Transform card)
        {
            if (cards.Contains(card)) return;

            Card cardComponent = card.GetComponent<Card>();

            // 연산자 카드는 Field Zone에 추가 금지
            if (zoneType == ZoneType.Field && cardComponent != null && cardComponent.CardType == CardType.Operator)
            {
                Debug.Log($"[CardZone] 연산자 카드는 필드에 배치할 수 없습니다: {card.name}");
                return;
            }

            cards.Add(card);
            card.SetParent(transform);

            // 카드 인터랙션 권한 설정
            ICard cardInterface = card.GetComponentInChildren<ICard>();
            cardInterface?.SetInteraction(zoneType, ownerType);

            // Opponent 손패일 경우 텍스트 비활성화
            if (ownerType == OwnerType.Opponent && zoneType == ZoneType.Hand)
            {
                var tmp = card.GetComponentInChildren<TMPro.TextMeshPro>();
                if (tmp != null)
                    tmp.gameObject.SetActive(false);
            }

            if (zoneType == ZoneType.Hand)
            {
                // Glow 효과 비활성화
                var glow = card.GetComponentInChildren<CardEffect>();
                if (glow != null)
                    glow.enabled = false;
            }

            if (zoneType == ZoneType.Field)
            {
                // Glow 효과 활성화
                var glow = card.GetComponentInChildren<CardEffect>();
                if (glow != null)
                    glow.enabled = true;

                if (cardComponent != null)
                    InGameManager.Instance.RegisterFieldCard(cardComponent);
            }

            UpdateLayout();

            if (zoneType == ZoneType.Hand)
            {
                AddDragHandler(card);
                AddHover(card.GetComponentInChildren<SpriteRenderer>()?.transform);
            }
        }

        /// <summary>
        /// 카드 포함 여부 확인 (외부에서 조회용)
        /// </summary>
        public bool Contains(Transform card) => cards.Contains(card);

        /// <summary>
        /// 카드 제거 후 배치 갱신
        /// </summary>
        public void RemoveCard(Transform card)
        {
            if (!cards.Contains(card)) return;

            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
                InGameManager.Instance.UnregisterFieldCard(cardComponent);
            cards.Remove(card);
            UpdateLayout();
        }

        /// <summary>
        /// 현재 카드 상태에 맞춰 레이아웃 재배치
        /// </summary>
        public void UpdateLayout()
        {
            if (layoutHelper == null)
            {
                Debug.LogWarning("[CardZone] LayoutHelper is not assigned.");
                return;
            }

            if (zoneType == ZoneType.Hand)
                layoutHelper.ArrangeFanLayout(cards);
            else
                layoutHelper.ArrangeFieldLayout(cards);
        }

        /// <summary>
        /// 카드에 Hover 애니메이션 추가
        /// </summary>
        private void AddHover(Transform card)
        {
            if (!card.TryGetComponent<CardMotion>(out _))
                card.gameObject.AddComponent<CardMotion>();
        }

        /// <summary>
        /// 카드에 DragHandler 추가
        /// </summary>
        private void AddDragHandler(Transform card)
        {
            if (!card.TryGetComponent<DragHandler>(out _))
                card.gameObject.AddComponent<DragHandler>();
        }
    }
}
