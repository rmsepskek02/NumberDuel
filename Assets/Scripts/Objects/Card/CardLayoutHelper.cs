using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 배치 전담 클래스
    /// - 손패용 부채꼴 정렬
    /// - 필드용 일렬 정렬
    /// </summary>
    public class CardLayoutHelper : MonoBehaviour
    {
        [Header("Fan Layout Settings (손패용)")]
        [SerializeField] private float fanRadius = 5f;
        [SerializeField] private float fanAngle = 30f;

        [Header("Field Layout Settings (필드용)")]
        [SerializeField] private float spacing = 2f;
        [SerializeField] private int maxFieldCards = 5;

        /// <summary>
        /// 손패를 부채꼴로 배치
        /// </summary>
        public void ArrangeFanLayout(List<Transform> cards)
        {
            int count = cards.Count;
            if (count == 0) return;

            float angleStep = fanAngle / Mathf.Max(count - 1, 1);
            float startAngle = -fanAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                float rad = angle * Mathf.Deg2Rad;

                Vector3 position = new Vector3(
                    Mathf.Sin(rad),
                    i * 0.01f + 0.01f,
                    Mathf.Cos(rad)
                ) * fanRadius;

                Quaternion rotation = Quaternion.Euler(0, angle, 0);

                Transform card = cards[i];
                card.localPosition = position;
                card.localRotation = rotation;

                // 추가: 정렬이 끝난 후 CardMotion의 상태를 새로 저장
                var motion = card.GetComponentInChildren<CardMotion>();
                if (motion != null)
                {
                    motion?.ResetReturnMotion();
                }
            }
        }

        /// <summary>
        /// 필드를 현재 카드 수에 따라 가운데 정렬해서 배치
        /// </summary>
        public void ArrangeFieldLayout(List<Transform> cards)
        {
            int count = cards.Count;
            if (count == 0) return;

            // 카드 수에 따라 전체 폭 계산 (간격 * (카드 수 - 1))
            float totalWidth = (count - 1) * spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                // 현재 카드 위치 계산
                Vector3 position = new Vector3(startX + i * spacing, 0, 0);

                Transform card = cards[i];
                card.localPosition = position;
                card.localRotation = Quaternion.identity;
            }
        }

    }
}
