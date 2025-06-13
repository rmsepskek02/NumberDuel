using DG.Tweening;
using Manager;
using Objects;
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
        /// 카드 낸 처리 수행 (카드 타입에 따라 적절한 UI 표시)
        /// </summary>
        public void TryPlayCard(Transform card)
        {
            // 카드 컴포넌트 가져오기
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent == null)
            {
                Debug.LogError("[CardPlayDetector] Card 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            // 카드 움직임 정지 및 초기화
            var motion = card.GetComponentInChildren<CardMotion>();
            if (motion != null)
            {
                motion.LockAndReset();
            }

            // 카드 타입에 따라 처리 분기
            switch (cardComponent.CardType)
            {
                case CardType.Joker:
                    HandleJokerCard(cardComponent);
                    break;

                case CardType.Operator:
                    HandleOperatorCard(cardComponent);
                    break;

                case CardType.Number:
                    HandleNumberCard(card);
                    break;
            }
        }

        /// <summary>
        /// 조커 카드 처리 (JokerModeSelector 표시)
        /// </summary>
        private void HandleJokerCard(Card jokerCard)
        {
            Debug.Log("[CardPlayDetector] 조커 카드 드래그 감지 - JokerModeSelector 표시");

            // 조커 모드 선택기 표시
            if (JokerModeSelector.Instance != null)
            {
                JokerModeSelector.Instance.Show(jokerCard);
            }
            else
            {
                Debug.LogError("[CardPlayDetector] JokerModeSelector를 찾을 수 없습니다.");
            }

            // 원래 위치로 되돌리기 (옵션)
            // 조커는 필드에 놓이지 않으므로 원위치
            ReturnCardToHand(jokerCard.transform);
        }

        /// <summary>
        /// 연산자 카드 처리 (OperatorManager 호출)
        /// </summary>
        private void HandleOperatorCard(Card operatorCard)
        {
            Debug.Log("[CardPlayDetector] 연산자 카드 드래그 감지 - OperatorManager 호출");

            // 연산자 매니저 호출
            if (Manager.OperatorManager.Instance != null)
            {
                Manager.OperatorManager.Instance.EnterOperatorMode(operatorCard);
            }
            else
            {
                Debug.LogError("[CardPlayDetector] OperatorManager를 찾을 수 없습니다.");
            }

            // 원래 위치로 되돌리기
            ReturnCardToHand(operatorCard.transform);
        }

        /// <summary>
        /// 숫자 카드 처리 (기존 Open/Secret 선택)
        /// </summary>
        private void HandleNumberCard(Transform card)
        {
            Debug.Log("[CardPlayDetector] 숫자 카드 드래그 감지 - CardModeSelector 표시");

            // 카드 모드 선택 요청 이벤트 발행
            OnCardPlayRequested?.Invoke(card, targetZone);
        }

        /// <summary>
        /// 카드를 손패로 되돌리기
        /// </summary>
        private void ReturnCardToHand(Transform card)
        {
            // 카드의 현재 Zone 찾기
            Card cardComponent = card.GetComponent<Card>();
            if (cardComponent == null) return;

            // 손패 Zone 찾기
            CardZone handZone = FindHandZone(cardComponent.CurrentOwnerType);
            if (handZone != null)
            {
                // 레이아웃 업데이트 (카드가 원위치로 돌아감)
                handZone.UpdateLayout();
            }
        }

        /// <summary>
        /// 특정 소유자의 손패 Zone 찾기
        /// </summary>
        private CardZone FindHandZone(CardZone.OwnerType owner)
        {
            if (CardZone.AllZonesRoot == null) return null;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            return zones.FirstOrDefault(z =>
                z.Zone == CardZone.ZoneType.Hand &&
                z.Owner == owner);
        }
    }
}