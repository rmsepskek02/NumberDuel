using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private ZoneType zoneType;
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
                    var target = card.GetComponentInChildren<SpriteRenderer>()?.transform;
                    if (target != null)
                        AddHover(target);
                }
            }
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

                if (zoneType == ZoneType.Hand)
                    RemoveHover(card);
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

            StartCoroutine(DelaySetInitialState(hover));
        }

        private System.Collections.IEnumerator DelaySetInitialState(CardMotion hover)
        {
            yield return null;
            hover.SetInitialState();
        }

        /// <summary>
        /// 카드의 Hover 애니메이션 제거
        /// </summary>
        private void RemoveHover(Transform card)
        {
            if (card.TryGetComponent(out CardMotion hover))
                Destroy(hover);
        }
    }
}
