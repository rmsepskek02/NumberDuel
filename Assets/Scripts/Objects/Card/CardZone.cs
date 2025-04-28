using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using static Objects.CardZone;

namespace Objects
{
    /// <summary>
    /// 카드가 배치되는 Zone (손패, 필드 등)을 관리하는 컴포넌트
    /// - 카드 리스트 관리
    /// - 배치는 CardLayoutHelper에 위임
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

        private readonly List<Transform> cards = new();

        /// <summary>
        /// 카드 추가 후 배치 갱신
        /// </summary>
        public void AddCard(Transform card)
        {
            if (!cards.Contains(card))
            {
                cards.Add(card);
                card.SetParent(transform);

                // 카드 인터랙션 권한 설정
                ICard cardInterface = card.GetComponentInChildren<ICard>();
                cardInterface?.SetInteraction(zoneType, ownerType);

                UpdateLayout();
                if (zoneType == ZoneType.Hand)
                {
                    var root = card; // 카드 루트
                    AddDragHandler(root); // DragHandler는 카드 루트에만 1회 추가

                    var target = card.GetComponentInChildren<SpriteRenderer>()?.transform;
                    if (target != null)
                    {
                        AddHover(target);
                    }
                }
            }
        }

        /// <summary>
        /// 카드 포함 여부 확인 (외부에서 조회용)
        /// </summary>
        public bool Contains(Transform card)
        {
            return cards.Contains(card);
        }

        /// <summary>
        /// 카드 제거 후 배치 갱신
        /// </summary>
        public void RemoveCard(Transform card)
        {
            if (cards.Contains(card))
            {
                cards.Remove(card);
                UpdateLayout();
            }
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
            if (!card.TryGetComponent(out CardMotion hover))
                hover = card.gameObject.AddComponent<CardMotion>();
        }

        /// <summary>
        /// 카드에 DragHandler 추가
        /// </summary>
        private void AddDragHandler(Transform card)
        {
            if (!card.TryGetComponent<DragHandler>(out _))
            {
                card.gameObject.AddComponent<DragHandler>();
            }
        }
    }
}
