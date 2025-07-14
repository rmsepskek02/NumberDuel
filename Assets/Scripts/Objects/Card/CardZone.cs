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
        /// 현재 Zone에 있는 카드 개수 반환
        /// </summary>
        public int GetCardCount()
        {
            return cards.Count;
        }

        /// <summary>
        /// Zone이 가득 찬 상태인지 확인 (손패용)
        /// </summary>
        public bool IsHandFull()
        {
            return zoneType == ZoneType.Hand && cards.Count >= 10;
        }

        /// <summary>
        /// 카드 추가 가능 여부 확인
        /// </summary>
        public bool CanAddCard()
        {
            if (zoneType == ZoneType.Hand)
                return cards.Count < 10;
            else if (zoneType == ZoneType.Field)
                return cards.Count < 5;  // 필드 5장 제한 추가

            return true;
        }

        /// <summary>
        /// 필드가 가득 찬 상태인지 확인
        /// </summary>
        public bool IsFieldFull()
        {
            return zoneType == ZoneType.Field && cards.Count >= 5;
        }

        // AddCard 메서드 수정 (필드 제한 추가)
        public void AddCard(Transform card)
        {
            // 제한 체크들...
            if (zoneType == ZoneType.Hand && cards.Count >= 10) return;
            if (zoneType == ZoneType.Field && cards.Count >= 5) return;
            if (cards.Contains(card)) return;

            Card cardComponent = card.GetComponent<Card>();
            if (zoneType == ZoneType.Field && cardComponent?.CardType == CardType.Operator) return;

            cards.Add(card);
            card.SetParent(transform);

            // Opponent 손패 텍스트 비활성화
            if (ownerType == OwnerType.Opponent && zoneType == ZoneType.Hand)
            {
                var tmp = card.GetComponentInChildren<TMPro.TextMeshPro>();
                if (tmp != null) tmp.gameObject.SetActive(false);
            }

            // GLOW 설정
            if (zoneType == ZoneType.Hand)
            {
                var glow = card.GetComponentInChildren<CardEffect>();
                if (glow != null) glow.enabled = false;
            }

            // 필드 카드 등록 및 상태 설정 (SetInteraction 보다 먼저!)
            if (zoneType == ZoneType.Field && cardComponent != null)
            {
                InGameManager.Instance.RegisterFieldCard(cardComponent);

                if (ownerType == OwnerType.Player)
                {
                    cardComponent.SetWasPlayedThisTurn(true); // ← 먼저 설정!
                }
            }

            // 카드 인터랙션 권한 설정 (나중에!)
            ICard cardInterface = card.GetComponentInChildren<ICard>();
            cardInterface?.SetInteraction(zoneType, ownerType);

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
