using System;
using System.Linq;
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

        [Tooltip("카드 모드 선택을 표시하는 셀렉터")]
        [SerializeField] private CardModeSelector cardModeSelector;

        private BoxCollider detectorCollider;

        public static event Action<Transform, CardZone> OnCardPlayRequested;

        private void Awake()
        {
            detectorCollider = GetComponent<BoxCollider>();

            cardModeSelector = FindFirstObjectByType<CardModeSelector>();
            if (cardModeSelector == null)
            {
                Debug.LogError("[CardPlayDetector] CardModeSelector를 씬에서 찾을 수 없습니다.");
            }
        }

        private void OnEnable()
        {
            Card.OnCardDropped += HandleCardDropped;
            Card.onClicked += HandleCardClicked;
        }

        private void OnDisable()
        {
            Card.OnCardDropped -= HandleCardDropped;
            Card.onClicked -= HandleCardClicked;
        }

        private void HandleCardDropped(Transform card)
        {
            if (!IsCardInside(card))
                return;

            TryPlayCard(card);
        }

        private void HandleCardClicked(Card card)
        {
            // 카드가 손패이고 플레이어 소유일 때만 플레이 요청 처리
            if (card.CurrentZoneType == CardZone.ZoneType.Hand 
                && card.CurrentOwnerType == CardZone.OwnerType.Player)
            {
                TryPlayCard(card.transform);
            }
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
        /// 카드 낸 처리 수행 (모드 선택을 위해 CardModeSelector를 활성화)
        /// </summary>
        public void TryPlayCard(Transform card)
        {
            // 카드 움직임 정지 및 초기화
            var motion = card.GetComponentInChildren<CardMotion>();
            if (motion != null)
            {
                motion.LockAndReset();
            }

            // 드래그와 Hover 컴포넌트 제거
            RemoveGameplayComponents(card);

            // 카드 모드 선택 요청 이벤트 발행
            OnCardPlayRequested?.Invoke(card, targetZone);
        }

        /// <summary>
        /// DragHandler와 CardMotion 제거 하는 함수
        /// </summary>
        private void RemoveGameplayComponents(Transform card)
        {
            if (card.TryGetComponent(out DragHandler drag))
                Destroy(drag);

            var motion = card.GetComponentInChildren<CardMotion>();
            if (motion != null)
                Destroy(motion);
        }
    }
}
