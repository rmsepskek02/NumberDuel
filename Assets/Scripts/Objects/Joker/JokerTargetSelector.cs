using UnityEngine;
using Objects;
using System;

namespace Objects
{
    /// <summary>
    /// 조커 효과의 대상 카드를 선택하는 기능을 관리
    /// Delete와 Swap 효과에서 사용됨
    /// </summary>
    public class JokerTargetSelector : MonoBehaviour
    {
        private static JokerTargetSelector instance;
        public static JokerTargetSelector Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<JokerTargetSelector>();
                return instance;
            }
        }

        private JokerTargetMode currentMode = JokerTargetMode.None;
        private Action<Card> currentCallback;
        private Card firstSelectedCard;

        private void OnEnable()
        {
            // 카드 클릭 이벤트 구독
            Card.onClicked += HandleCardClicked;
        }

        private void OnDisable()
        {
            // 카드 클릭 이벤트 구독 해제
            Card.onClicked -= HandleCardClicked;
        }

        /// <summary>
        /// 대상 선택 모드 시작
        /// </summary>
        public void StartTargetSelection(JokerTargetMode mode, Action<Card> onTargetSelected)
        {
            currentMode = mode;
            currentCallback = onTargetSelected;
            firstSelectedCard = null;

            Debug.Log($"[JokerTargetSelector] {mode} 모드로 대상 선택 시작");
        }

        /// <summary>
        /// 대상 선택 모드 종료
        /// </summary>
        public void EndTargetSelection()
        {
            currentMode = JokerTargetMode.None;
            currentCallback = null;
            firstSelectedCard = null;

            Debug.Log("[JokerTargetSelector] 대상 선택 모드 종료");
        }

        /// <summary>
        /// 카드가 클릭되었을 때 처리
        /// </summary>
        private void HandleCardClicked(Card clickedCard)
        {
            if (currentMode == JokerTargetMode.None || clickedCard == null)
                return;

            // 조커 카드는 대상으로 선택 불가
            if (clickedCard.CardType == CardType.Joker)
            {
                Debug.Log("[JokerTargetSelector] 조커 카드는 대상으로 선택할 수 없습니다.");
                return;
            }

            switch (currentMode)
            {
                case JokerTargetMode.Delete:
                    HandleDeleteTarget(clickedCard);
                    break;

                case JokerTargetMode.SwapFirst:
                    HandleSwapFirstTarget(clickedCard);
                    break;

                case JokerTargetMode.SwapSecond:
                    HandleSwapSecondTarget(clickedCard);
                    break;
            }
        }

        /// <summary>
        /// 삭제 대상 처리
        /// </summary>
        private void HandleDeleteTarget(Card target)
        {
            // 상대 필드에 있는 카드만 삭제 가능
            if (target.CurrentZoneType != CardZone.ZoneType.Field ||
                target.CurrentOwnerType != CardZone.OwnerType.Opponent)
            {
                Debug.Log("[JokerTargetSelector] 상대 필드에 있는 카드만 삭제할 수 있습니다.");
                return;
            }

            // Glow 효과가 있는 카드만 선택 가능
            var effect = target.GetComponentInChildren<CardEffect>();
            if (effect == null || !effect.IsGlowing())
            {
                Debug.Log("[JokerTargetSelector] 선택할 수 없는 카드입니다.");
                return;
            }

            Debug.Log($"[JokerTargetSelector] 삭제 대상 선택됨: {target.name}");

            // 콜백 실행 후 모드 종료
            currentCallback?.Invoke(target);
            EndTargetSelection();
        }

        /// <summary>
        /// 교환 첫 번째 대상 처리 (내 필드 카드)
        /// </summary>
        private void HandleSwapFirstTarget(Card target)
        {
            // 내 필드 카드만 선택 가능
            if (target.CurrentZoneType != CardZone.ZoneType.Field ||
                target.CurrentOwnerType != CardZone.OwnerType.Player)
            {
                Debug.Log("[JokerTargetSelector] 내 필드의 카드만 선택할 수 있습니다.");
                return;
            }

            // Glow 효과가 있는 카드만 선택 가능
            var effect = target.GetComponentInChildren<CardEffect>();
            if (effect == null || !effect.IsGlowing())
            {
                Debug.Log("[JokerTargetSelector] 선택할 수 없는 카드입니다.");
                return;
            }

            // 숫자 카드만 교환 가능 (연산자는 제외)
            if (target.CardType != CardType.Number)
            {
                Debug.Log("[JokerTargetSelector] 숫자 카드만 교환할 수 있습니다.");
                return;
            }

            firstSelectedCard = target;
            Debug.Log($"[JokerTargetSelector] 교환 첫 번째 대상 선택됨: {target.name}");

            // 콜백 실행 (JokerModeSelector에서 두 번째 선택 준비)
            currentCallback?.Invoke(target);

            // 모드는 변경하지 않음 (JokerModeSelector에서 SwapSecond로 변경할 것)
        }

        /// <summary>
        /// 교환 두 번째 대상 처리 (상대 필드 카드)
        /// </summary>
        private void HandleSwapSecondTarget(Card target)
        {
            // 상대 필드 카드만 선택 가능
            if (target.CurrentZoneType != CardZone.ZoneType.Field ||
                target.CurrentOwnerType != CardZone.OwnerType.Opponent)
            {
                Debug.Log("[JokerTargetSelector] 상대 필드의 카드만 선택할 수 있습니다.");
                return;
            }

            // Glow 효과가 있는 카드만 선택 가능
            var effect = target.GetComponentInChildren<CardEffect>();
            if (effect == null || !effect.IsGlowing())
            {
                Debug.Log("[JokerTargetSelector] 선택할 수 없는 카드입니다.");
                return;
            }

            // 숫자 카드만 교환 가능 (연산자는 제외)
            if (target.CardType != CardType.Number)
            {
                Debug.Log("[JokerTargetSelector] 숫자 카드만 교환할 수 있습니다.");
                return;
            }

            // 첫 번째 카드와 같은 카드는 선택 불가 (안전장치)
            if (target == firstSelectedCard)
            {
                Debug.Log("[JokerTargetSelector] 같은 카드는 선택할 수 없습니다.");
                return;
            }

            Debug.Log($"[JokerTargetSelector] 교환 두 번째 대상 선택됨: {target.name}");

            // 콜백 실행 후 모드 종료
            currentCallback?.Invoke(target);
            EndTargetSelection();
        }

        /// <summary>
        /// 현재 선택 모드 확인
        /// </summary>
        public bool IsSelecting()
        {
            return currentMode != JokerTargetMode.None;
        }

        /// <summary>
        /// 현재 모드 가져오기
        /// </summary>
        public JokerTargetMode GetCurrentMode()
        {
            return currentMode;
        }

        /// <summary>
        /// 첫 번째 선택된 카드 가져오기 (디버깅용)
        /// </summary>
        public Card GetFirstSelectedCard()
        {
            return firstSelectedCard;
        }
    }
}