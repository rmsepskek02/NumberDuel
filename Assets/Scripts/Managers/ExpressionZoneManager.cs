using UnityEngine;
using Objects;
using System.Linq;
using Expression;
using Utills;

namespace Manager
{
    /// <summary>
    /// ExpressionZone의 5개 슬롯을 관리하여 수식을 시각적으로 표현하는 매니저
    /// 슬롯 구조: [0:내카드] [1:연산자] [2:상대카드] [3:등호] [4:결과]
    /// </summary>
    public class ExpressionZoneManager : Singleton<ExpressionZoneManager>
    {
        [Header("Expression Zone 설정")]
        [SerializeField] private CardZone expressionZone;
        [SerializeField] private bool enableDebugLog = false;

        private ExpressionCard[] slots;
        private Sprite neutralSprite;

        // 성능 최적화용 캐시
        private readonly Color[] cachedColors = new Color[5];
        private readonly bool[] cachedActiveStates = new bool[5];

        #region Unity Lifecycle
        private void Start()
        {
            InitializeSlots();
            SubscribeEvents();
            ResetAllSlots();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Initialization
        private void InitializeSlots()
        {
            if (expressionZone == null)
                expressionZone = GetComponentInParent<CardZone>();

            slots = GetComponentsInChildren<ExpressionCard>(includeInactive: true)
                .OrderBy(card => card.name)
                .ToArray();

            foreach (var slot in slots)
                expressionZone.AddCard(slot.transform);

            expressionZone.UpdateLayout();

            CacheNeutralSprite();

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] 초기화 완료 - 슬롯 수: {slots.Length}");
        }

        private void CacheNeutralSprite()
        {
            neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack) ??
                           ResourcesManager.Instance.GetSprite(Global.Card, "color_back");
        }

        private void SubscribeEvents()
        {
            ExpressionCard.onClicked -= HandleSlotClicked;
            ExpressionCard.onClicked += HandleSlotClicked;
        }

        private void UnsubscribeEvents()
        {
            ExpressionCard.onClicked -= HandleSlotClicked;
        }
        #endregion

        #region Core Slot Management
        /// <summary>
        /// 슬롯 업데이트 (변경사항이 있을 때만 실제 업데이트)
        /// </summary>
        private void UpdateSlot(int index, string text, Sprite sprite, bool showText, bool canCancel = false)
        {
            if (!IsValidSlotIndex(index) || index == 3) return; // 3번 슬롯 보호

            var slot = slots[index];
            var targetSprite = sprite ?? neutralSprite;
            var targetColor = Global.GetColorByName(targetSprite.name);

            // 변경사항 확인 후 필요시에만 업데이트
            bool needsUpdate = false;

            if (slot.GetComponentInChildren<SpriteRenderer>().sprite != targetSprite)
            {
                slot.SetSprite(targetSprite);
                needsUpdate = true;
            }

            if (slot.CurrentText != text || cachedActiveStates[index] != showText)
            {
                slot.SetValue(text);
                slot.SetTextVisible(showText);
                cachedActiveStates[index] = showText;
                needsUpdate = true;
            }

            if (cachedColors[index] != targetColor)
            {
                slot.SetTextColor(targetColor);
                cachedColors[index] = targetColor;
                needsUpdate = true;
            }

            // 취소 가능 상태 설정
            if (canCancel) slot.SetCancelable(true);
            else slot.ClearGlow();

            if (enableDebugLog && needsUpdate)
                Debug.Log($"[ExpressionZoneManager] 슬롯 {index} 업데이트: '{text}'");
        }

        /// <summary>
        /// 3번 슬롯 (등호) 고정 설정
        /// </summary>
        private void FixEqualSlot()
        {
            const int equalIndex = 3;
            var slot = slots[equalIndex];

            // 이미 올바른 상태인지 확인
            if (slot.CurrentText == "=" && slot.GetComponentInChildren<SpriteRenderer>().sprite == neutralSprite)
                return;

            slot.ClearGlow();
            slot.SetSprite(neutralSprite);
            slot.SetValue("=");
            slot.SetTextVisible(true);
            slot.SetTextColor(Color.white);

            cachedColors[equalIndex] = Color.white;
            cachedActiveStates[equalIndex] = true;
        }

        /// <summary>
        /// 유효한 슬롯 인덱스인지 확인
        /// </summary>
        private bool IsValidSlotIndex(int index) => index >= 0 && index < slots.Length;
        #endregion

        #region Public Interface
        /// <summary>
        /// 공격자 카드를 0번 슬롯에 설정
        /// </summary>
        public void SetAttackerCard(Card card)
        {
            SetCardToSlot(card, 0, "SetAttackerCard");
        }

        /// <summary>
        /// 수비자 카드를 2번 슬롯에 설정
        /// </summary>
        public void SetDefenderCard(Card card)
        {
            SetCardToSlot(card, 2, "SetDefenderCard");
        }

        /// <summary>
        /// 첫 번째 연산 대상을 0번 슬롯에 설정
        /// </summary>
        public void SetFirstOperand(Card card)
        {
            SetCardToSlot(card, 0, "SetFirstOperand");
        }

        /// <summary>
        /// 두 번째 연산 대상을 2번 슬롯에 설정
        /// </summary>
        public void SetSecondOperand(Card card)
        {
            SetCardToSlot(card, 2, "SetSecondOperand");
        }

        /// <summary>
        /// 연산자를 1번 슬롯에 설정
        /// </summary>
        public void SetOperator(Card operatorCard)
        {
            if (operatorCard?.CardType != CardType.Operator)
            {
                Debug.LogError("[ExpressionZoneManager] 유효하지 않은 연산자 카드");
                return;
            }

            string symbol = GetOperatorSymbol(operatorCard.OperatorType);
            var sprite = operatorCard.GetComponentInChildren<SpriteRenderer>()?.sprite;
            UpdateSlot(1, symbol, sprite, true);
        }

        /// <summary>
        /// 공격용 빼기 기호를 1번 슬롯에 설정
        /// </summary>
        public void SetAttackOperator()
        {
            UpdateSlot(1, "-", neutralSprite, true);
        }

        /// <summary>
        /// 연산 결과를 4번 슬롯에 표시
        /// </summary>
        public void ShowResult(float a, float b, OperatorType? operatorType = null)
        {
            float result = CalculateResult(a, b, operatorType);
            string resultText = FormatResultText(result, operatorType);
            Sprite resultSprite = GetResultSprite(result, operatorType);

            UpdateSlot(4, resultText, resultSprite, true);

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] 결과 표시: {a} {GetOperatorSymbol(operatorType)} {b} = {result}");
        }
        #endregion

        #region Process Control
        /// <summary>
        /// 공격 프로세스용 초기화
        /// </summary>
        public void InitForAttack()
        {
            ClearAllCancelable();
            UpdateSlot(1, "-", neutralSprite, true);
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// 연산 프로세스용 초기화
        /// </summary>
        public void InitForOperation()
        {
            ClearAllCancelable();
            UpdateSlot(0, "", neutralSprite, false);
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// 모든 슬롯 초기화
        /// </summary>
        public void ResetAllSlots()
        {
            ClearAllCancelable();
            UpdateSlot(0, "", neutralSprite, false);
            UpdateSlot(1, "-", neutralSprite, true);
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// 첫 번째 선택만 초기화 (연산 중 재선택용)
        /// </summary>
        public void ResetFirstSelection()
        {
            UpdateSlot(0, "", neutralSprite, false);
        }
        #endregion

        #region Cancellation System
        /// <summary>
        /// 지정된 슬롯들을 취소 가능하게 설정
        /// </summary>
        public void EnableCancellation(params int[] slotIndices)
        {
            ClearAllCancelable();

            foreach (int index in slotIndices)
            {
                if (IsValidSlotIndex(index) && slots[index].IsActive)
                {
                    slots[index].SetCancelable(true);
                }
            }
        }

        /// <summary>
        /// 모든 슬롯의 취소 기능 비활성화
        /// </summary>
        public void ClearAllCancelable()
        {
            foreach (var slot in slots)
                slot.ClearGlow();
        }

        /// <summary>
        /// 슬롯 클릭 이벤트 처리
        /// </summary>
        private void HandleSlotClicked(ExpressionCard clickedSlot)
        {
            int slotIndex = clickedSlot.SlotIndex;

            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool isAttacking = attackManager?.HasAttackerSelected() ?? false;

            if (isAttacking)
                HandleAttackCancellation(slotIndex);
            else if (InGameManager.Instance.CurrentProcess == GameProcessState.OperatorCalculation)
                HandleOperationCancellation(slotIndex);
        }

        /// <summary>
        /// 공격 취소 처리
        /// </summary>
        private void HandleAttackCancellation(int slotIndex)
        {
            if (slotIndex == 0) // 공격자 슬롯 클릭시 공격 취소
            {
                var attackManager = FindAnyObjectByType<FieldAttackManager>();
                attackManager?.ForceResetAttackState();
                ResetAllSlots();
            }
        }

        /// <summary>
        /// 연산 취소 처리
        /// </summary>
        private void HandleOperationCancellation(int slotIndex)
        {
            var operatorManager = OperatorManager.Instance;
            if (operatorManager?.IsInOperatorMode != true) return;

            switch (slotIndex)
            {
                case 0: // 첫 번째 선택 재설정
                    operatorManager.ResetFirstCardSelection();
                    EnableCancellation(1);
                    break;
                case 1: // 연산 완전 취소
                    operatorManager.CancelOperatorMode();
                    break;
            }
        }
        #endregion

        #region State Management for External Managers
        public void StartAttackMode() => EnableCancellation(0);
        public void StartOperationMode() => EnableCancellation(1);
        public void UpdateOperationFirstSelected() => EnableCancellation(0, 1);
        #endregion

        #region Utility Methods
        /// <summary>
        /// 카드 정보를 슬롯에 설정하는 공통 메소드
        /// </summary>
        private void SetCardToSlot(Card card, int slotIndex, string methodName)
        {
            var cardText = card?.GetComponentInChildren<CardText>();
            var spriteRenderer = card?.GetComponentInChildren<SpriteRenderer>();

            if (cardText?.TextValue == null || spriteRenderer?.sprite == null)
            {
                Debug.LogError($"[ExpressionZoneManager] {methodName} - 카드 데이터가 유효하지 않음");
                return;
            }

            UpdateSlot(slotIndex, cardText.TextValue, spriteRenderer.sprite, true);
        }

        /// <summary>
        /// 연산 결과 계산
        /// </summary>
        private float CalculateResult(float a, float b, OperatorType? operatorType)
        {
            return operatorType switch
            {
                OperatorType.Plus => a + b,
                OperatorType.Minus => a - b,
                OperatorType.Multiply => a * b,
                OperatorType.Divide => b != 0 ? Mathf.Floor(a / b) : 0, // 몫만 반환
                _ => a - b // 공격 (기본 빼기)
            };
        }

        /// <summary>
        /// 결과 텍스트 포맷팅
        /// </summary>
        private string FormatResultText(float result, OperatorType? operatorType)
        {
            return operatorType switch
            {
                null => Mathf.Abs(Mathf.FloorToInt(result)).ToString(), // 공격은 절댓값의 정수 부분
                OperatorType.Minus => Mathf.FloorToInt(result).ToString(), // 빼기는 정수 부분 (음수 포함)
                _ => Mathf.FloorToInt(result).ToString() // 나머지는 정수 부분
            };
        }

        /// <summary>
        /// 결과에 맞는 스프라이트 반환
        /// </summary>
        private Sprite GetResultSprite(float result, OperatorType? operatorType)
        {
            if (result == 0) return neutralSprite;

            if (operatorType == null) // 공격 프로세스
            {
                return result > 0
                    ? slots[0].GetComponentInChildren<SpriteRenderer>().sprite
                    : slots[2].GetComponentInChildren<SpriteRenderer>().sprite;
            }

            return slots[0].GetComponentInChildren<SpriteRenderer>().sprite;
        }

        /// <summary>
        /// 연산자 타입을 기호로 변환
        /// </summary>
        private string GetOperatorSymbol(OperatorType? operatorType)
        {
            return operatorType switch
            {
                OperatorType.Plus => "+",
                OperatorType.Minus => "-",
                OperatorType.Multiply => "×",
                OperatorType.Divide => "÷",
                _ => "?"
            };
        }

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public void SetDebugMode(bool enable) => enableDebugLog = enable;
        #endregion
    }
}