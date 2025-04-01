using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드가 배치될 수 있는 영역 (손패, 필드 등)을 관리하는 클래스
    /// </summary>
    public class CardZone : MonoBehaviour
    {
        // 카드 영역의 종류: 손패 or 필드
        public enum ZoneType { Hand, Field }
        // 소유자 구분: 플레이어 or 상대
        public enum OwnerType { Player, Opponent }

        [Header("Zone Settings")]
        public ZoneType zoneType;
        public OwnerType ownerType;

        [Header("Layout Settings")]
        public float spacing = 2f;         // 카드 간 간격 (필드용)
        public float fanRadius = 5f;       // 손패에서 원형 배열 반지름
        public float fanAngle = 30f;       // 손패에서 펼쳐지는 각도
        public int maxFieldCards = 5;      // 필드에 올릴 수 있는 최대 카드 수

        private readonly List<Transform> cards = new(); // 현재 이 영역에 있는 카드 리스트

        // 카드를 이 영역에 추가
        public void AddCard(Transform card)
        {
            if (!cards.Contains(card))
            {
                cards.Add(card);
                card.SetParent(transform);
                UpdateLayout();

                if (zoneType == ZoneType.Hand)
                {
                    // card가 CardSprite인 경우에만 Hover 추가
                    var target = card.GetComponentInChildren<SpriteRenderer>()?.transform;
                    if (target != null)
                        AddHover(target);
                }
            }
        }


        // 카드를 이 영역에서 제거
        public void RemoveCard(Transform card)
        {
            if (cards.Contains(card))
            {
                cards.Remove(card);
                UpdateLayout();

                // 손패일 경우 hover 효과 제거
                if (zoneType == ZoneType.Hand)
                    RemoveHover(card);
            }
        }

        // 카드에 HoverCardMotion 컴포넌트를 추가
        private void AddHover(Transform card)
        {
            if (!card.TryGetComponent(out HoverCardMotion hover))
                hover = card.gameObject.AddComponent<HoverCardMotion>();

            // 다음 프레임에서 초기 위치 저장 (레이아웃 반영 후 정확하게)
            StartCoroutine(DelaySetInitialState(hover));
        }

        private System.Collections.IEnumerator DelaySetInitialState(HoverCardMotion hover)
        {
            yield return null; // 1 프레임 대기
            hover.SetInitialState();
        }

        // HoverCardMotion 제거
        private void RemoveHover(Transform card)
        {
            if (card.TryGetComponent(out HoverCardMotion hover))
                Destroy(hover);
        }

        // 영역의 카드 레이아웃을 다시 배치
        public void UpdateLayout()
        {
            if (zoneType == ZoneType.Hand)
                ArrangeFanLayout(); // 부채꼴 배열
            else
                ArrangeFieldLayout(); // 일렬 배열
        }

        // 손패 레이아웃: 카드를 부채꼴로 배치
        private void ArrangeFanLayout()
        {
            int count = cards.Count;
            float angleStep = fanAngle / Mathf.Max(count - 1, 1); // 카드 간 각도
            float startAngle = -fanAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 localPos = new Vector3(Mathf.Sin(rad), i * 0.01f + 0.01f, Mathf.Cos(rad)) * fanRadius;
                Quaternion rotation = Quaternion.Euler(0, angle, 0);

                Transform card = cards[i];
                card.localPosition = localPos;
                card.localRotation = rotation;
            }
        }

        // 필드 레이아웃: 카드를 일렬로 배치
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
