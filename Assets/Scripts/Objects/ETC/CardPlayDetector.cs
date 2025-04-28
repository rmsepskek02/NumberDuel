using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 드래그 해제 시 카드가 이 Detector 안에 있는지를 판별하는 도우미
    /// </summary>
    public class CardPlayDetector : MonoBehaviour
    {
        [Tooltip("카드가 낼 때 이동할 Zone")]
        [SerializeField] private CardZone targetZone;

        [Tooltip("모든 Zone들이 등록된 상위 오브젝트")]
        [SerializeField] private Transform allZonesRoot;

        private BoxCollider detectorCollider;

        private void Awake()
        {
            detectorCollider = GetComponent<BoxCollider>();
        }

        /// <summary>
        /// 해당 카드가 이 Detector 영역 안에 있는지 판단
        /// </summary>
        public bool IsCardInside(Transform card)
        {
            if (detectorCollider == null) return false;

            return detectorCollider.bounds.Contains(card.position);
        }

        /// <summary>
        /// 카드 낸 처리 수행
        /// </summary>
        public void TryPlayCard(Transform card)
        {
            var motion = card.GetComponentInChildren<CardMotion>();
            if (motion != null)
            {
                motion?.LockAndReset();
            }
            else
            {
                Debug.LogWarning(">>> CardMotion NOT FOUND");
            }

            var fromZone = FindCurrentZone(card);
            if (fromZone != null)
                fromZone.RemoveCard(card);

            RemoveGameplayComponents(card);
            targetZone.AddCard(card);
        }

        /// <summary>
        /// 카드가 현재 속해있는 CardZone 부모 오브젝트 찾는 함수
        /// </summary>
        /// <param name="card">드래그하여 움직인 카드</param>
        /// <returns>CardZone 부모 오브젝트</returns>
        private CardZone FindCurrentZone(Transform card)
        {
            if (allZonesRoot == null) return null;

            foreach (var zone in allZonesRoot.GetComponentsInChildren<CardZone>())
            {
                if (zone.Contains(card))
                    return zone;
            }

            return null;
        }

        /// <summary>
        /// DragHandler와 CardMotion 제거 하는 함수
        /// </summary>
        /// <param name="card">드래그하여 움직인 카드</param>
        private void RemoveGameplayComponents(Transform card)
        {
            // 카드 루트에 붙은 DragHandler 제거
            if (card.TryGetComponent(out DragHandler drag))
                Destroy(drag);

            // 카드 내부에 있는 Hover용 CardMotion 제거
            var motion = card.GetComponentInChildren<CardMotion>();
            if (motion != null)
            {
                Destroy(motion);
            }
        }
    }
}
