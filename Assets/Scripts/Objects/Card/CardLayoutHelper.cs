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
            }
        }

        /// <summary>
        /// 필드를 일렬로 배치
        /// </summary>
        public void ArrangeFieldLayout(List<Transform> cards)
        {
            int count = cards.Count;
            if (count == 0) return;

            float totalWidth = (maxFieldCards - 1) * spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3(startX + i * spacing, 0, 0);

                Transform card = cards[i];
                card.localPosition = position;
                card.localRotation = Quaternion.identity;
            }
        }
    }
}
