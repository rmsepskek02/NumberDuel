using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 배치 영역을 제어하는 컴포넌트.
    /// 하스스톤처럼 손패는 부채꼴로, 필드는 중앙 정렬로 배치.
    /// 3D 오브젝트 기반으로 구성되며, Photon을 활용한 멀티플레이에도 대응 가능.
    /// </summary>
    public class CardZone : MonoBehaviour
    {
        public enum ZoneType { Hand, Field }
        public enum OwnerType { Player, Opponent }

        [Header("Zone Settings")]
        public ZoneType zoneType;
        public OwnerType ownerType;

        [Header("Layout Settings")]
        public float spacing = 2f;            // 필드 정렬 간격
        public float fanRadius = 5f;          // 손패 부채꼴 반지름
        public float fanAngle = 30f;          // 전체 부채각도
        public int maxFieldCards = 5;         // 필드 카드 최대 개수

        private readonly List<Transform> cards = new();

        /// <summary>
        /// 카드 추가 후 레이아웃 업데이트
        /// </summary>
        public void AddCard(Transform card)
        {
            if (!cards.Contains(card))
            {
                cards.Add(card);
                card.SetParent(transform);
                UpdateLayout();
            }
        }

        /// <summary>
        /// 카드 제거 후 레이아웃 업데이트
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
        /// 현재 카드 정렬 상태를 갱신
        /// </summary>
        public void UpdateLayout()
        {
            if (zoneType == ZoneType.Hand)
                ArrangeFanLayout();
            else
                ArrangeFieldLayout();
        }

        /// <summary>
        /// 손패: 부채꼴 정렬
        /// </summary>
        private void ArrangeFanLayout()
        {
            int count = cards.Count;
            float angleStep = fanAngle / Mathf.Max(count - 1, 1);
            float startAngle = -fanAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 localPos = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * fanRadius;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);

                Transform card = cards[i];
                card.localPosition = localPos;
                card.localRotation = rotation;
            }
        }

        /// <summary>
        /// 필드: 가운데 정렬 (최대 5칸)
        /// </summary>
        private void ArrangeFieldLayout()
        {
            int count = cards.Count;
            float totalWidth = (maxFieldCards - 1) * spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                Vector3 localPos = new Vector3(startX + i * spacing, 0, 0);
                Transform card = cards[i];
                card.localPosition = localPos;
                card.localRotation = Quaternion.identity;
            }
        }
    }
}
