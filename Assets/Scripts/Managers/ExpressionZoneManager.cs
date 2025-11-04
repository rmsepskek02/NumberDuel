using Expression;
using Objects;
using System.Linq;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// ExpressionZone의 5개 슬롯을 관리하여 연산식 시각화를 표현하는 매니저
    /// 슬롯 구조: [0:좌카드] [1:연산자] [2:우카드] [3:등호] [4:결과]
    /// </summary>
    public class ExpressionZoneManager : Singleton<ExpressionZoneManager>
    {
        #region Fields and Properties
        [Header("Expression Zone 설정")]
        [SerializeField] private CardZone expressionZone;
        [SerializeField] private bool enableDebugLog = false;

        private ExpressionCard[] slots;
        private Sprite neutralSprite;

        /// <summary>
        /// 성능 최적화용 캐시
        /// </summary>
        private readonly Color[] cachedColors = new Color[5];
        private readonly bool[] cachedActiveStates = new bool[5];
        #endregion

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
        /// 슬롯 업데이트 (변경사항이 있을 경우 최소 업데이트)
        /// </summary>
        private void UpdateSlot(int index, string text, Sprite sprite, bool showText, bool canCancel = false)
        {
            if (!IsValidSlotIndex(index) || index == 3) return; // 3번 슬롯은 등호

            var slot = slots[index];
            var targetSprite = sprite ?? neutralSprite;
            var targetColor = Global.GetColorByName(targetSprite.name);

            // 변경점만 확인 후 필요할때만 업데이트
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

            // 취소 가능 여부 설정
            if (canCancel) slot.SetCancelable(true);
            else slot.ClearGlow();
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
        /// 공격자 카드를 0번 슬롯에 배치
        /// </summary>
        public void SetAttackerCard(Card card)
        {
            SetCardToSlot(card, 0, "SetAttackerCard");
        }

        /// <summary>
        /// 방어자 카드를 2번 슬롯에 배치
        /// </summary>
        public void SetDefenderCard(Card card)
        {
            SetCardToSlot(card, 2, "SetDefenderCard");
        }

        /// <summary>
        /// 첫 번째 연산 피연산자 0번 슬롯에 배치
        /// </summary>
        public void SetFirstOperand(Card card)
        {
            SetCardToSlot(card, 0, "SetFirstOperand");
        }

        /// <summary>
        /// 두 번째 연산 피연산자 2번 슬롯에 배치
        /// </summary>
        public void SetSecondOperand(Card card)
        {
            SetCardToSlot(card, 2, "SetSecondOperand");
        }

        /// <summary>
        /// 연산자를 1번 슬롯에 배치
        /// </summary>
        public void SetOperator(Card operatorCard)
        {
            if (operatorCard?.CardType != CardType.Operator)
            {
                return;
            }

            string symbol = GetOperatorSymbol(operatorCard.OperatorType);

            // 아이콘이 아닌 스프라이트를 가진 스프라이트 찾기
            Sprite sprite = null;
            var allRenderers = operatorCard.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in allRenderers)
            {
                if (!renderer.gameObject.name.Contains("Icon"))
                {
                    sprite = renderer.sprite;
                    break;
                }
            }

            UpdateSlot(1, symbol, sprite, true);
        }

        /// <summary>
        /// 공격에 대한 빼기 기호를 1번 슬롯에 설정
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
        /// 첫 번째 슬롯만 초기화 (연산 시 재선택용)
        /// </summary>
        public void ResetFirstSelection()
        {
            UpdateSlot(0, "", neutralSprite, false);
        }
        #endregion

        #region Cancellation System
        /// <summary>
        /// 지정한 슬롯들만 취소 가능하게 설정
        /// </summary>
        public void EnableCancellation(params int[] slotIndices)
        {
            ClearAllCancelable();

            foreach (int index in slotIndices)
            {
                if (IsValidSlotIndex(index))
                {
                    slots[index].SetCancelable(true);
                }
            }
        }

        /// <summary>
        /// 모든 슬롯의 취소 가능 비활성화
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
            else if (InGameManager.Instance.CurrentProcess == GameProcessState.JokerDeleteProcess)
                HandleJokerDeleteCancellation(slotIndex);
            else if (InGameManager.Instance.CurrentProcess == GameProcessState.JokerSwapProcess)
                HandleJokerSwapCancellation(slotIndex);
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
                case 0: // 첫 번째 피연산자 재선택
                    operatorManager.ResetFirstCardSelection();
                    EnableCancellation(1);
                    break;
                case 1: // 연산 모드 취소
                    operatorManager.CancelOperatorMode();
                    break;
            }
        }

        /// <summary>
        /// 조커 Delete 취소 처리
        /// </summary>
        private void HandleJokerDeleteCancellation(int slotIndex)
        {
            if (slotIndex == 1) // 슬롯 1 (삭제 이미지) 클릭 시 완전 취소
            {
                var jokerSelector = JokerModeSelector.Instance;
                jokerSelector?.CancelDeleteMode();
            }
        }

        /// <summary>
        /// 조커 Swap 취소 처리
        /// </summary>
        private void HandleJokerSwapCancellation(int slotIndex)
        {
            var jokerSelector = JokerModeSelector.Instance;
            if (jokerSelector == null) return;

            switch (slotIndex)
            {
                case 0: // 첫 번째 카드 재선택
                    jokerSelector.ResetSwapFirstSelection();
                    break;
                case 1: // Swap 모드 완전 취소
                    jokerSelector.CancelSwapMode();
                    break;
            }
        }
        #endregion

        #region State Management for External Managers
        public void StartAttackMode() => EnableCancellation(0);
        public void StartOperationMode() => EnableCancellation(1);
        public void UpdateOperationFirstSelected() => EnableCancellation(0, 1);
        #endregion

        #region Joker Effect Support
        /// <summary>
        /// 조커 삭제(Delete) 효과용 초기화
        /// 슬롯 1번에 삭제 이미지 카드 표시
        /// </summary>
        public void InitForJokerDelete()
        {
            ClearAllCancelable();

            // 삭제 이미지 스프라이트 가져오기
            Sprite deleteSprite = GetJokerEffectSprite("delete");

            // 슬롯 0: 비워둠 (선택될 카드 대기)
            UpdateSlot(0, "", neutralSprite, false);
            // 슬롯 1: 삭제 이미지
            UpdateSlot(1, "", deleteSprite ?? neutralSprite, false);
            // 슬롯 2, 4: 비워둠
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// 조커 삭제 대상 카드를 슬롯 0에 표시
        /// </summary>
        public void SetJokerDeleteTarget(Card target)
        {
            if (target == null) return;
            SetCardToSlot(target, 0, "SetJokerDeleteTarget");
        }

        /// <summary>
        /// 조커 교환(Swap) 효과용 초기화
        /// 슬롯 1번에 스왑 이미지 카드 표시
        /// </summary>
        public void InitForJokerSwap()
        {
            ClearAllCancelable();

            // 스왑 이미지 스프라이트 가져오기
            Sprite swapSprite = GetJokerEffectSprite("swap");

            // 슬롯 0: 비워둠 (첫 번째 카드 대기)
            UpdateSlot(0, "", neutralSprite, false);
            // 슬롯 1: 스왑 이미지
            UpdateSlot(1, "", swapSprite ?? neutralSprite, false);
            // 슬롯 2, 4: 비워둠
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// 조커 스왑 첫 번째 카드를 슬롯 0에 표시
        /// </summary>
        public void SetJokerSwapFirst(Card firstCard)
        {
            if (firstCard == null) return;
            SetCardToSlot(firstCard, 0, "SetJokerSwapFirst");
        }

        /// <summary>
        /// 조커 스왑 두 번째 카드를 슬롯 2에 표시
        /// </summary>
        public void SetJokerSwapSecond(Card secondCard)
        {
            if (secondCard == null) return;
            SetCardToSlot(secondCard, 2, "SetJokerSwapSecond");
        }

        /// <summary>
        /// 현재 플레이어 색상에 맞는 조커 효과 스프라이트 가져오기
        /// </summary>
        private Sprite GetJokerEffectSprite(string effectType)
        {
            if (ResourcesManager.Instance == null) return null;

            var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
            if (playerSprite == null) return null;

            // 색상 추출 (예: "color_green_1" -> "green")
            string colorName = ExtractColorFromSpriteName(playerSprite.name);

            // 조커 효과 스프라이트 이름 생성 (예: "color_green_delete")
            string spriteName = $"color_{colorName}_{effectType}";

            return ResourcesManager.Instance.GetSprite(Global.Joker, spriteName);
        }

        /// <summary>
        /// 스프라이트 이름에서 색상 추출
        /// </summary>
        private string ExtractColorFromSpriteName(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return "green";

            if (spriteName.StartsWith("color_"))
            {
                string remaining = spriteName.Substring(6);
                int underscoreIndex = remaining.IndexOf('_');
                if (underscoreIndex > 0)
                {
                    return remaining.Substring(0, underscoreIndex);
                }
                else
                {
                    return remaining;
                }
            }

            return "green";
        }

        /// <summary>
        /// 조커 Delete 모드에서 취소 가능 표시 (슬롯 1만)
        /// </summary>
        public void EnableJokerDeleteCancellation()
        {
            EnableCancellation(1); // 슬롯 1 (삭제 이미지) 클릭 가능
        }

        /// <summary>
        /// 조커 Swap 모드에서 취소 가능 표시 (슬롯 1만)
        /// </summary>
        public void EnableJokerSwapCancellation()
        {
            EnableCancellation(1); // 슬롯 1 (스왑 이미지) 클릭 가능
        }

        /// <summary>
        /// 조커 Swap 첫 번째 선택 후 취소 가능 표시 (슬롯 0, 1)
        /// </summary>
        public void EnableJokerSwapFirstCancellation()
        {
            EnableCancellation(0, 1); // 슬롯 0, 1 클릭 가능
        }

        /// <summary>
        /// 조커 첫 번째 슬롯만 초기화
        /// </summary>
        public void ResetJokerFirstSlot()
        {
            UpdateSlot(0, "", neutralSprite, false);
        }
        #endregion

        #region Empty Field Attack Support
        /// <summary>
        /// 빈 필드 공격용 - slot 2번에 상대 스프라이트 "0" 표시
        /// 실제 카드가 아닌 빈필드 방어를 시각적으로 표시
        /// </summary>
        /// <param name="opponentType">상대 타입 (Player 또는 Opponent)</param>
        public void SetEmptyFieldDefender(CardZone.OwnerType opponentType)
        {
            // 상대 색상 스프라이트 가져오기
            Sprite opponentSprite = GetSpriteByOwnerType(opponentType);

            if (opponentSprite == null)
            {
                opponentSprite = neutralSprite;
            }

            // slot 2번에 "0" 텍스트와 상대 스프라이트 설정
            UpdateSlot(2, "0", opponentSprite, true);
        }

        /// <summary>
        /// 소유자 타입에 따른 스프라이트 반환
        /// </summary>
        /// <param name="ownerType">카드 소유자 타입</param>
        /// <returns>해당 플레이어 스프라이트</returns>
        private Sprite GetSpriteByOwnerType(CardZone.OwnerType ownerType)
        {
            return ownerType switch
            {
                CardZone.OwnerType.Player => ResourcesManager.Instance.GetPlayerSprite(),
                CardZone.OwnerType.Opponent => ResourcesManager.Instance.GetOpponentSprite(),
                _ => neutralSprite
            };
        }

        /// <summary>
        /// 빈 필드 공격 결과 표시 (공격값 - 0 = 공격값을 그대로 표시)
        /// </summary>
        /// <param name="attackerValue">공격자 카드 값</param>
        public void ShowEmptyFieldResult(float attackerValue)
        {
            // 빈 필드 방어: 공격자 값 - 0 = 공격자 값
            float result = attackerValue;
            string resultText = Mathf.FloorToInt(result).ToString();

            // 결과는 공격자 스프라이트로 표시 (승리이므로)
            Sprite resultSprite = slots[0].GetComponentInChildren<SpriteRenderer>().sprite;

            UpdateSlot(4, resultText, resultSprite, true);
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 카드 정보를 슬롯에 설정하는 통합 메서드
        /// </summary>
        private void SetCardToSlot(Card card, int slotIndex, string methodName)
        {
            var cardText = card?.GetComponentInChildren<CardText>();

            // 아이콘이 아닌 실제 카드 오브젝트에서 SpriteRenderer 찾기
            SpriteRenderer spriteRenderer = null;
            if (card != null)
            {
                var allRenderers = card.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var renderer in allRenderers)
                {
                    // "Icon"이 포함된 GameObject는 제외
                    if (!renderer.gameObject.name.Contains("Icon"))
                    {
                        spriteRenderer = renderer;
                        break;
                    }
                }
            }

            if (cardText?.TextValue == null || spriteRenderer?.sprite == null)
            {
                return;
            }

            UpdateSlot(slotIndex, cardText.TextValue, spriteRenderer.sprite, true);
        }

        /// <summary>
        /// 결과 값 계산
        /// </summary>
        private float CalculateResult(float a, float b, OperatorType? operatorType)
        {
            return operatorType switch
            {
                OperatorType.Plus => a + b,
                OperatorType.Minus => a - b,
                OperatorType.Multiply => a * b,
                OperatorType.Divide => b != 0 ? Mathf.Floor(a / b) : 0, // 몫 반환
                _ => a - b // 전투 (기본 빼기)
            };
        }

        /// <summary>
        /// 결과 텍스트 포맷팅
        /// </summary>
        private string FormatResultText(float result, OperatorType? operatorType)
        {
            return operatorType switch
            {
                null => Mathf.Abs(Mathf.FloorToInt(result)).ToString(), // 전투는 절대값 정수 부분
                OperatorType.Minus => Mathf.FloorToInt(result).ToString(), // 빼기는 음수 정수 부분 (음수 가능)
                _ => Mathf.FloorToInt(result).ToString() // 나머지는 정수 부분
            };
        }

        /// <summary>
        /// 결과에 맞는 스프라이트 반환
        /// </summary>
        private Sprite GetResultSprite(float result, OperatorType? operatorType)
        {
            if (result == 0) return neutralSprite;

            if (operatorType == null) // 전투 프로세스
            {
                return result > 0
                    ? slots[0].GetComponentInChildren<SpriteRenderer>().sprite
                    : slots[2].GetComponentInChildren<SpriteRenderer>().sprite;
            }

            return slots[0].GetComponentInChildren<SpriteRenderer>().sprite;
        }

        /// <summary>
        /// 연산자 타입을 기호로 반환
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

        #region Network Synchronization Support
        /// <summary>
        /// 연산자 타입으로만 연산 기호 설정 (네트워크 동기화용)
        /// 연산자 카드 객체 없이 타입만으로도 연산 기호를 표시
        /// </summary>
        /// <param name="operatorType">표시할 연산자 타입</param>
        public void SetOperatorByType(OperatorType operatorType)
        {
            string symbol = GetOperatorSymbol(operatorType);
            UpdateSlot(1, symbol, neutralSprite, true);
        }
        #endregion
    }
}
