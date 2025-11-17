using System;
using UnityEngine;
using Objects;

namespace Objects
{
    /// <summary>
    /// 조커 효과의 대상 카드를 선택하는 시스템 관리
    /// Delete와 Swap 효과에서 사용
    /// </summary>
    public class JokerTargetSelector : MonoBehaviour
    {
        #region Singleton
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
        #endregion

        #region Fields and Properties
        private JokerTargetMode currentMode = JokerTargetMode.None;
        private Action<Card> currentCallback;
        private Card firstSelectedCard;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            // 카드 클릭 이벤트 등록
            Card.onClicked += HandleCardClicked;
        }

        private void OnDisable()
        {
            // 카드 클릭 이벤트 구독 해제
            Card.onClicked -= HandleCardClicked;
        }
        #endregion

        #region Selection Control
        /// <summary>
        /// 대상 선택 모드 시작
        /// </summary>
        public void StartTargetSelection(JokerTargetMode mode, Action<Card> onTargetSelected)
        {
            currentMode = mode;
            currentCallback = onTargetSelected;
            firstSelectedCard = null;
        }

        /// <summary>
        /// 대상 선택 모드 종료
        /// </summary>
        public void EndTargetSelection()
        {
            currentMode = JokerTargetMode.None;
            currentCallback = null;
            firstSelectedCard = null;
        }
        #endregion

        #region Card Click Handler
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
        #endregion

        #region Target Handlers
        /// <summary>
        /// 삭제 대상 처리
        /// </summary>
        private void HandleDeleteTarget(Card target)
        {
            // 상대 필드에 있는 카드만 삭제 가능
            if (target.CurrentZoneType != CardZone.ZoneType.Field ||
                target.CurrentOwnerType != CardZone.OwnerType.Opponent)
            {
                return;
            }

            // Glow 효과가 있는 카드만 선택 가능
            var effect = target.GetComponentInChildren<CardEffect>();
            if (effect == null || !effect.IsGlowing())
            {
                return;
            }

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
                return;
            }

            // Glow 효과가 있는 카드만 선택 가능
            var effect = target.GetComponentInChildren<CardEffect>();
            if (effect == null || !effect.IsGlowing())
            {
                return;
            }

            // 숫자 카드만 교환 가능 (연산자는 제외)
            if (target.CardType != CardType.Number)
            {
                return;
            }

            firstSelectedCard = target;

            // 콜백 실행 (JokerModeSelector에서 두 번째 단계 준비)
            currentCallback?.Invoke(target);

            // 모드 유지하지 않음 (JokerModeSelector에서 SwapSecond로 전환될 것)
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
                return;
            }

            // Glow 효과가 있는 카드만 선택 가능
            var effect = target.GetComponentInChildren<CardEffect>();
            if (effect == null || !effect.IsGlowing())
            {
                return;
            }

            // 숫자 카드만 교환 가능 (연산자는 제외)
            if (target.CardType != CardType.Number)
            {
                return;
            }

            // 첫 번째 카드와 같은 카드는 선택 불가 (안전장치)
            if (target == firstSelectedCard)
            {
                return;
            }

            // 콜백 실행 후 모드 종료
            currentCallback?.Invoke(target);
            EndTargetSelection();
        }
        #endregion

        #region Public Queries
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
        /// 첫 번째 선택된 카드 가져오기 (디버그)
        /// </summary>
        public Card GetFirstSelectedCard()
        {
            return firstSelectedCard;
        }
        #endregion
    }
}
