using UnityEngine;
using Objects;
using System.Linq;
using Expression;
using Utills;

namespace Manager
{
    /// <summary>
    /// ExpressionZone에 배치된 5개의 ExpressionCard를 제어하여 수식을 시각적으로 구성하는 매니저 클래스
    /// - 카드 배치 및 정렬
    /// - 연산자 및 숫자 표시
    /// - 결과값 계산 및 표시
    /// - 스프라이트에 따라 텍스트 색상 동기화
    /// - 취소 기능 처리
    /// </summary>
    public class ExpressionZoneManager : Singleton<ExpressionZoneManager>
    {
        [Header("카드 정렬 대상 Zone")]
        [SerializeField] private CardZone expressionZone;

        private ExpressionCard[] expressionCards;
        private Sprite neutralSprite;

        #region Unity Lifecycle
        /// <summary>
        /// 시작 시 표현식 카드 정렬 및 초기화 수행
        /// </summary>
        private void Start()
        {
            InitializeExpressionZone();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Initialization
        private void InitializeExpressionZone()
        {
            if (expressionZone == null)
                expressionZone = GetComponentInParent<CardZone>();

            // 슬롯들을 이름 기준으로 정렬
            expressionCards = GetComponentsInChildren<ExpressionCard>(includeInactive: true)
                .OrderBy(card => card.name)
                .ToArray();

            Debug.Log($"[ExpressionZoneManager] ExpressionCard 개수: {expressionCards.Length}");

            // Zone에 정렬 등록
            foreach (var card in expressionCards)
            {
                expressionZone.AddCard(card.transform);
            }

            expressionZone.UpdateLayout();

            // 중립 스프라이트 로드
            neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);

            // 수식존 초기화 (연산자 제외)
            ConfigureSlot(0, "", null, false); // 내 카드
            ConfigureSlot(2, "", null, false); // 상대 카드
            ConfigureSlot(4, "", null, false); // 결과
            SetEqualSymbol();
        }

        private void SubscribeToEvents()
        {
            ExpressionCard.onClicked -= HandleExpressionCardClicked;
            ExpressionCard.onClicked += HandleExpressionCardClicked;
            Debug.Log("[ExpressionZoneManager] ExpressionCard 이벤트 구독 완료");
        }

        private void UnsubscribeFromEvents()
        {
            ExpressionCard.onClicked -= HandleExpressionCardClicked;
        }
        #endregion

        #region Slot Configuration
        /// <summary>
        /// 지정한 슬롯에 텍스트/스프라이트/텍스트 활성 여부를 일괄 설정합니다.
        /// </summary>
        /// <param name="index">슬롯 인덱스 (0~4)</param>
        /// <param name="symbolText">표시할 텍스트나 기호</param>
        /// <param name="sprite">표시할 카드 Sprite (null이면 중립 사용)</param>
        /// <param name="showText">텍스트 표시 여부</param>
        public void ConfigureSlot(int index, string symbolText, Sprite sprite = null, bool showText = true)
        {
            if (index < 0 || index >= expressionCards.Length)
            {
                Debug.LogWarning($"[ExpressionZoneManager] 잘못된 슬롯 인덱스: {index}");
                return;
            }

            var slot = expressionCards[index];

            // 슬롯 구성 전에 GLOW 상태 초기화 (버그 수정)
            slot.ClearGlow();

            slot.SetValue(symbolText);
            slot.SetSprite(sprite ?? neutralSprite);
            slot.SetTextVisible(showText);

            // 텍스트 색상 설정 (Sprite가 있을 경우 색상 반영)
            if (sprite != null)
                slot.SetTextColor(Global.GetColorByName(sprite.name));
            else
                slot.SetTextColor(Color.white);
        }

        /// <summary>
        /// 내 카드의 값과 Sprite를 0번 슬롯에 표시합니다.
        /// </summary>
        public void SetMyCard(Card card)
        {
            var text = card.GetComponentInChildren<CardText>()?.TextValue;
            var sprite = card.GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (text == null || sprite == null) return;

            ConfigureSlot(0, text, sprite, true);
        }

        /// <summary>
        /// 상대 카드의 값과 Sprite를 2번 슬롯에 표시합니다.
        /// </summary>
        public void SetOpponentCard(Card card)
        {
            var text = card.GetComponentInChildren<CardText>()?.TextValue;
            var sprite = card.GetComponentInChildren<SpriteRenderer>()?.sprite;
            if (text == null || sprite == null) return;

            ConfigureSlot(2, text, sprite, true);
        }

        /// <summary>
        /// 연산자 카드(OperatorType)에 해당하는 기호와 스프라이트를 1번 슬롯에 표시합니다.
        /// </summary>
        public void SetOperatorCard(Card operatorCard)
        {
            if (operatorCard == null || operatorCard.CardType != CardType.Operator)
            {
                Debug.LogWarning("[ExpressionZoneManager] 잘못된 연산자 카드가 전달됨.");
                return;
            }

            string symbol = operatorCard.OperatorType switch
            {
                OperatorType.Plus => "+",
                OperatorType.Minus => "-",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "?"
            };

            var sprite = operatorCard.GetComponentInChildren<SpriteRenderer>()?.sprite;
            ConfigureSlot(1, symbol, sprite, true);
            SetEqualSymbol();
        }

        /// <summary>
        /// 연산자 없이 수동으로 기호만 1번 슬롯에 표시합니다. (공격 프로세스용)
        /// </summary>
        public void SetOperatorSymbol(string symbol)
        {
            ConfigureSlot(1, symbol, null, true);
        }

        /// <summary>
        /// 수식 표현용 '=' 기호를 3번 슬롯에 고정 표시합니다.
        /// </summary>
        public void SetEqualSymbol()
        {
            ConfigureSlot(3, "=", null, true);
        }

        /// <summary>
        /// 연산 또는 공격 결과를 4번 슬롯에 표시합니다.
        /// - type이 null이면 공격 처리 방식 (절댓값, 색상 분기)
        /// - type이 있으면 연산자 처리 방식 (결과 그대로 출력)
        /// </summary>
        public void DisplayResult(long a, long b, OperatorType? type = null, Sprite forceSprite = null)
        {
            long result = type switch
            {
                OperatorType.Plus => a + b,
                OperatorType.Minus => a - b,
                OperatorType.Multiply => a * b,
                OperatorType.Divide => b != 0 ? a / b : 0,
                _ => a - b // type == null이면 기본 공격 연산
            };

            // 텍스트 포맷: 연산자는 부호 포함, 공격은 절댓값
            string text = type == null ? Mathf.Abs(result).ToString() : result.ToString();

            // 스프라이트 처리
            Sprite sprite = forceSprite ?? (
                result == 0 ? neutralSprite :
                result > 0 ? ResourcesManager.Instance.GetPlayerSprite() :
                             ResourcesManager.Instance.GetOpponentSprite()
            );

            ConfigureSlot(4, text, sprite, true);
        }
        #endregion

        #region Cancellation Management
        /// <summary>
        /// 특정 슬롯들을 취소 가능하게 설정
        /// </summary>
        public void SetCancelableSlots(params int[] slotIndices)
        {
            Debug.Log($"[ExpressionZoneManager] SetCancelableSlots: [{string.Join(", ", slotIndices)}]");

            // 모든 슬롯 취소 불가능으로 초기화
            ClearAllCancelable();

            // 지정된 슬롯들만 취소 가능하게 설정
            foreach (int index in slotIndices)
            {
                if (index >= 0 && index < expressionCards.Length)
                {
                    var slot = expressionCards[index];
                    if (slot.IsActive)
                    {
                        slot.SetCancelable(true);
                        Debug.Log($"[ExpressionZoneManager] 슬롯 {index} 취소 가능 설정됨");
                    }
                }
            }
        }

        /// <summary>
        /// 모든 슬롯의 취소 가능 상태 해제
        /// </summary>
        public void ClearAllCancelable()
        {
            foreach (var card in expressionCards)
            {
                card.ClearGlow();
            }
        }

        /// <summary>
        /// ExpressionCard 클릭 이벤트 처리
        /// </summary>
        private void HandleExpressionCardClicked(ExpressionCard clickedCard)
        {
            Debug.Log($"[ExpressionZoneManager] ExpressionCard 클릭! - {clickedCard.name}, 슬롯: {clickedCard.SlotIndex}");

            int slotIndex = clickedCard.SlotIndex;

            // Unity 6 호환: FindAnyObjectByType 사용
            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool hasAttackerSelected = attackManager != null && attackManager.HasAttackerSelected();

            // 현재 진행중인 프로세스에 따라 취소 처리 분기
            if (hasAttackerSelected)
            {
                HandleAttackCancellation(slotIndex);
            }
            else if (InGameManager.Instance.CurrentProcess == GameProcessState.OperatorCalculation)
            {
                HandleOperatorCancellation(slotIndex);
            }
            else
            {
                Debug.LogWarning("[ExpressionZoneManager] 처리할 수 없는 상태에서 ExpressionCard 클릭됨");
            }
        }
        #endregion

        #region Attack Cancellation
        private void HandleAttackCancellation(int slotIndex)
        {
            if (slotIndex == 0) // 0번 슬롯(공격자) 클릭시 공격 취소
            {
                Debug.Log("[ExpressionZoneManager] 공격 취소 처리");
                CancelAttackProcess();
            }
            else
            {
                Debug.Log($"[ExpressionZoneManager] 슬롯 {slotIndex}는 공격 취소 불가");
            }
        }

        public void CancelAttackProcess()
        {
            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            if (attackManager != null)
            {
                attackManager.ForceResetAttackState();
                Debug.Log("[ExpressionZoneManager] 공격 프로세스 취소 완료");
            }

            ResetExpressionZone();
        }
        #endregion

        #region Operator Cancellation
        private void HandleOperatorCancellation(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0: // 0번 슬롯: 첫 번째 카드 재선택
                    Debug.Log("[ExpressionZoneManager] 연산 첫 번째 카드 재선택");
                    CancelOperatorFirstCard();
                    break;
                case 1: // 1번 슬롯: 연산 완전 취소
                    Debug.Log("[ExpressionZoneManager] 연산 프로세스 완전 취소");
                    CancelOperatorProcess();
                    break;
                default:
                    Debug.LogWarning($"[ExpressionZoneManager] 연산 중 취소 불가 슬롯: {slotIndex}");
                    break;
            }
        }

        public void CancelOperatorFirstCard()
        {
            var operatorManager = OperatorManager.Instance;
            if (operatorManager != null && operatorManager.IsInOperatorMode)
            {
                operatorManager.ResetFirstCardSelection();
                SetCancelableSlots(1); // 연산자 슬롯만 취소 가능하게
            }
        }

        public void CancelOperatorProcess()
        {
            var operatorManager = OperatorManager.Instance;
            if (operatorManager != null && operatorManager.IsInOperatorMode)
            {
                operatorManager.CancelOperatorMode();
            }
        }
        #endregion

        #region Process State Management
        public void StartAttackProcess()
        {
            Debug.Log("[ExpressionZoneManager] StartAttackProcess - 0번 슬롯 취소 가능");
            SetCancelableSlots(0);
        }

        public void StartOperatorProcess()
        {
            Debug.Log("[ExpressionZoneManager] StartOperatorProcess - 1번 슬롯 취소 가능");
            SetCancelableSlots(1);
        }

        public void UpdateOperatorFirstCardSelected()
        {
            Debug.Log("[ExpressionZoneManager] 연산 첫 번째 카드 선택됨 - 0,1번 슬롯 취소 가능");
            SetCancelableSlots(0, 1);
        }

        public void ResetExpressionZone()
        {
            Debug.Log("[ExpressionZoneManager] ResetExpressionZone");

            ClearAllCancelable();
            ConfigureSlot(0, "", null, false);
            ConfigureSlot(1, "", null, false);
            ConfigureSlot(2, "", null, false);
            ConfigureSlot(4, "", null, false);
            SetEqualSymbol();
        }
        #endregion
    }
}