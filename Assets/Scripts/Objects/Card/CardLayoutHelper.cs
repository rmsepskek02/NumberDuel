using System.Collections.Generic;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 배치 관리 클래스
    /// - 손패는 부채꼴 배치
    /// - 필드는 일렬 배치
    /// </summary>
    public class CardLayoutHelper : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Fan Layout Settings (손패)")]
        [SerializeField] private float fanRadius = 5f;
        [SerializeField] private float fanAngle = 30f;

        [Header("Field Layout Settings (필드)")]
        [SerializeField] private float spacing = 2f;
        #endregion

        #region Fan Layout
        /// <summary>
        /// 손패를 부채꼴로 배치
        /// </summary>
        public void ArrangeFanLayout(List<Transform> cards)
        {
            int count = cards.Count;
            if (count == 0) return;

            // 카드가 1장일 때는 정중앙에 평평하게 배치
            if (count == 1)
            {
                Transform card = cards[0];
                // fanRadius를 적용하여 카메라에 보이도록 배치
                card.localPosition = new Vector3(0, 0.01f, fanRadius);
                card.localRotation = Quaternion.identity;

                var motion = card.GetComponentInChildren<CardMotion>();
                if (motion != null)
                {
                    motion?.ResetReturnMotion();
                }
                return;
            }

            // 카드 수에 따라 동적으로 각도 범위 조정 (카드가 적을 때는 좁게 모임)
            float minAngle = 10f; // 카드가 적을 때 사용할 최소 각도
            float dynamicAngle = Mathf.Lerp(minAngle, fanAngle, Mathf.Clamp01((count - 1) / 9f));

            float angleStep = dynamicAngle / (count - 1);
            float startAngle = -dynamicAngle / 2f;

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

                // 추가: 배치가 바뀐 뒤 CardMotion의 상태를 갱신 필요
                var motion = card.GetComponentInChildren<CardMotion>();
                if (motion != null)
                {
                    motion?.ResetReturnMotion();
                }
            }
        }

        /// <summary>
        /// 필드를 일렬 카드 배치 중앙 정렬해서 배치
        /// </summary>
        public void ArrangeFieldLayout(List<Transform> cards)
        {
            int count = cards.Count;
            if (count == 0) return;

            // 카드 간격 고려 전체 폭 계산 (간격 * (카드 수 - 1))
            float totalWidth = (count - 1) * spacing;
            float startX = -totalWidth / 2f;

            for (int i = 0; i < count; i++)
            {
                // 중앙 카드 위치 계산
                Vector3 position = new Vector3(startX + i * spacing, 0, 0);

                Transform card = cards[i];
                card.localPosition = position;
                card.localRotation = Quaternion.identity;
            }
        }
        #endregion
    }
}
