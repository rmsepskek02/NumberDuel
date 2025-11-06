using UnityEngine;
using System.Collections;
using System.Linq;
using Objects;
using Utills;
using System.Collections.Generic;

namespace Manager
{
    /// <summary>
    /// 연산자 카드를 사용할 때 카드 간 연산 처리 매니저
    /// 연산자별 첫 카드 선택 규칙, 연산 처리, 결과 적용을 담당
    /// </summary>
    public class OperatorManager : Singleton<OperatorManager>
    {
        #region Fields and Properties
        /// <summary>
        /// 연산 진행 상태
        /// </summary>
        private enum State
        {
            Idle,
            OperatorSelected,
            FirstCardSelected,
            Processing
        }

        [SerializeField] private bool enableDebugLog = false;

        private State currentState = State.Idle;
        private OperatorType currentOperator;
        private Card operatorCard;
        private Card firstCard;
        private Card secondCard;

        /// <summary>
        /// 필드 최적화용 캐시
        /// </summary>
        private readonly List<Card> fieldCardsCache = new List<Card>();

        /// <summary>
        /// 현재 연산자 모드 활성화 여부
        /// </summary>
        public bool IsInOperatorMode => currentState != State.Idle;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            Card.onClicked += HandleCardClick;
        }

        private void OnDestroy()
        {
            Card.onClicked -= HandleCardClick;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 연산자 카드를 사용한 연산 시작
        /// </summary>
        public void StartOperation(Card operatorCard)
        {
            // 턴 검증: 내 턴인지 확인
            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                Debug.Log("[OperatorManager] 내 턴이 아닙니다.");
                return;
            }

            if (!CanStartOperation(operatorCard)) return;

            InitializeOperation(operatorCard);
            SetupFirstCardSelection();

            if (enableDebugLog)
                Debug.Log($"[OperatorManager] 연산 시작: {currentOperator}");
        }

        /// <summary>
        /// 첫 번째 카드 선택을 취소하고 재선택 가능하게 복귀
        /// </summary>
        public void ResetFirstCardSelection()
        {
            if (currentState != State.FirstCardSelected) return;

            // 첫 번째 카드 아이콘 숨김 및 네트워크 동기화
            if (firstCard != null)
            {
                firstCard.HideSelectionIcon();
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.HideCardIcon(firstCard);
                }
            }

            currentState = State.OperatorSelected;
            firstCard = null;

            ExpressionZoneManager.Instance.ResetFirstSelection();
            SetupFirstCardSelection();

            if (enableDebugLog)
                Debug.Log("[OperatorManager] 첫 번째 카드 선택 취소");
        }

        /// <summary>
        /// 연산 모드 완전 취소
        /// </summary>
        public void CancelOperatorMode()
        {
            // 아이콘 숨김 및 네트워크 동기화
            if (firstCard != null)
            {
                firstCard.HideSelectionIcon();
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.HideCardIcon(firstCard);
                }
            }
            if (secondCard != null)
            {
                secondCard.HideSelectionIcon();
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.HideCardIcon(secondCard);
                }
            }

            RestoreDefaultGlowStates();
            ExpressionZoneManager.Instance.ResetAllSlots();
            ResetOperationState();

            if (enableDebugLog)
                Debug.Log("[OperatorManager] 연산 모드 취소");
        }
        #endregion

        #region Card Selection Flow
        /// <summary>
        /// 카드 클릭 이벤트 처리
        /// </summary>
        private void HandleCardClick(Card card)
        {
            if (!CanProcessClick(card)) return;

            switch (currentState)
            {
                case State.OperatorSelected:
                    HandleFirstCardSelection(card);
                    break;
                case State.FirstCardSelected:
                    HandleSecondCardSelection(card);
                    break;
            }
        }

        /// <summary>
        /// 첫 번째 카드 선택 처리
        /// </summary>
        private void HandleFirstCardSelection(Card card)
        {
            if (!IsValidFirstCard(card)) return;

            firstCard = card;
            currentState = State.FirstCardSelected;

            // 첫 번째 카드에 연산자 아이콘 표시 (로컬만)
            CardIconType iconType = ResourcesManager.Instance.OperatorToIconType(currentOperator);
            firstCard.ShowSelectionIcon(iconType);

            // 네트워크 동기화는 ExecuteOperation()에서 수행 (취소 불가 시점)

            ExpressionZoneManager.Instance.SetFirstOperand(card);
            ExpressionZoneManager.Instance.UpdateOperationFirstSelected();

            SetupSecondCardSelection();
        }

        /// <summary>
        /// 두 번째 카드 선택 및 연산 실행
        /// </summary>
        private void HandleSecondCardSelection(Card card)
        {
            secondCard = card;
            currentState = State.Processing;

            // 두 번째 카드에 연산자 아이콘 표시 (로컬만)
            CardIconType iconType = ResourcesManager.Instance.OperatorToIconType(currentOperator);
            secondCard.ShowSelectionIcon(iconType);

            // 네트워크 동기화는 ExecuteOperation()에서 수행 (취소 불가 시점)

            StartCoroutine(ExecuteOperation());
        }
        #endregion

        #region Operation Execution
        /// <summary>
        /// 연산 처리 코루틴
        /// </summary>
        private IEnumerator ExecuteOperation()
        {
            ExpressionZoneManager.Instance.ClearAllCancelable();

            // 취소 불가능 시점 - 네트워크에 아이콘 동기화
            CardIconType iconType = ResourcesManager.Instance.OperatorToIconType(currentOperator);
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncCardIcon(firstCard, iconType);
                NetworkGameManager.Instance.SyncCardIcon(secondCard, iconType);
            }

            // 연산 시각화
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.SetFirstOperand(firstCard);
            ezManager.SetOperator(operatorCard);
            ezManager.SetSecondOperand(secondCard);

            yield return new WaitForSeconds(1.2f);

            // 연산 결과 표시 (Card 객체 전달 - 시크릿 카드 처리 포함)
            ezManager.ShowResult(firstCard, secondCard, currentOperator);

            yield return new WaitForSeconds(0.6f);

            // 카드 값 가져오기
            var (first, second) = GetCardValues();
            float result = CalculateResult(first, second);

            // 네트워크 동기화
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncOperationResult(
                    operatorCard, firstCard, secondCard, result, currentOperator
                );
            }

            // 결과 적용 (로컬 실행)
            ApplyOperationResult(first, second);

            // 연산자 카드 제거
            yield return StartCoroutine(RemoveOperatorCard());

            // 아이콘 숨김 및 네트워크 동기화
            if (firstCard != null)
            {
                firstCard.HideSelectionIcon();
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.HideCardIcon(firstCard);
                }
            }
            if (secondCard != null)
            {
                secondCard.HideSelectionIcon();
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.HideCardIcon(secondCard);
                }
            }

            // 상태 복귀
            RestoreDefaultGlowStates();
            ResetOperationState();
        }

        /// <summary>
        /// 연산 결과 적용
        /// </summary>
        private void ApplyOperationResult(float first, float second)
        {
            float result = CalculateResult(first, second);

            switch (currentOperator)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    UpdateCardValue(firstCard, result);
                    break;

                case OperatorType.Minus:
                    if (result > 0) UpdateCardValue(firstCard, result);
                    else DestroyCard(firstCard);
                    break;

                case OperatorType.Divide:
                    if (result > 0) UpdateCardValue(firstCard, result);
                    else DestroyCard(firstCard);

                    float remainder = first % second;
                    if (remainder > 0) CreateRemainderCard(remainder);
                    break;
            }

            if (enableDebugLog)
                Debug.Log($"[OperatorManager] 연산 완료: {first} {GetOperatorSymbol()} {second} = {result}");
        }

        /// <summary>
        /// 연산 결과 계산
        /// </summary>
        private float CalculateResult(float first, float second)
        {
            return currentOperator switch
            {
                OperatorType.Plus => first + second,
                OperatorType.Minus => first - second,
                OperatorType.Multiply => first * second,
                OperatorType.Divide => second != 0 ? Mathf.Floor(first / second) : 0, // 몫 반환
                _ => 0
            };
        }
        #endregion

        #region Card Management
        /// <summary>
        /// 연산자 카드 제거
        /// </summary>
        private IEnumerator RemoveOperatorCard()
        {
            if (operatorCard == null) yield break;

            var zone = operatorCard.GetComponentInParent<CardZone>();
            yield return StartCoroutine(operatorCard.AnimateRemoval(() =>
            {
                zone?.RemoveCard(operatorCard.transform);
                Destroy(operatorCard.gameObject);
            }));
        }

        /// <summary>
        /// 카드 값 업데이트
        /// </summary>
        private void UpdateCardValue(Card card, float newValue)
        {
            card.GetComponentInChildren<CardText>().SetRawValue(newValue);
            card.SetWasModifiedThisTurn(true);
        }

        /// <summary>
        /// 카드 파괴
        /// </summary>
        private void DestroyCard(Card card)
        {
            var zone = card.GetComponentInParent<CardZone>();
            zone?.RemoveCard(card.transform);
            Destroy(card.gameObject);
        }

        /// <summary>
        /// 나눗셈 나머지용 새 카드 생성
        /// </summary>
        private void CreateRemainderCard(float value)
        {
            var playerField = FindPlayerFieldZone();
            if (playerField == null) return;

            var template = ResourcesManager.Instance.GetPlayerCardTemplate();
            if (template == null) return;

            var newCard = Instantiate(template);

            // float 값이지만 정수로 변환
            int intValue = Mathf.FloorToInt(value);
            newCard.name = $"RemainderCard_{intValue}";
            newCard.SetActive(true);

            var cardComponent = newCard.GetComponent<Card>();
            cardComponent?.InitializeAsNumber(intValue); // 정수 값으로 초기화
            cardComponent?.SetWasModifiedThisTurn(true);

            playerField.AddCard(newCard.transform);
        }
        #endregion

        #region GLOW Management (선택가능)
        /// <summary>
        /// 첫 번째 카드 선택을 위한 GLOW 설정 (선택가능)
        /// </summary>
        private void SetupFirstCardSelection()
        {
            UpdateFieldCache();

            foreach (var card in fieldCardsCache)
            {
                bool canSelect = IsValidFirstCard(card);
                // 연산 프로세스 중이므로 GLOW 강제 설정
                card.SetGlowOverride(canSelect, canSelect ? Global.GlowGreen : null);
            }
        }

        /// <summary>
        /// 두 번째 카드 선택을 위한 GLOW 설정 (선택가능)
        /// </summary>
        private void SetupSecondCardSelection()
        {
            foreach (var card in fieldCardsCache)
            {
                bool canSelect = card != firstCard;
                // 연산 프로세스 중이므로 GLOW 강제 설정
                card.SetGlowOverride(canSelect, canSelect ? Global.GlowGreen : null);
            }
        }

        /// <summary>
        /// 기본 GLOW 상태로 복귀 (선택가능)
        /// </summary>
        private void RestoreDefaultGlowStates()
        {
            UpdateFieldCache();

            foreach (var card in fieldCardsCache)
            {
                // 연산 프로세스 종료 - GLOW 강제 설정 해제
                card.ClearGlowOverride();
            }
        }

        /// <summary>
        /// 필드 카드 캐시 업데이트
        /// </summary>
        private void UpdateFieldCache()
        {
            fieldCardsCache.Clear();
            fieldCardsCache.AddRange(InGameManager.Instance.GetAllFieldCards());
        }
        #endregion

        #region Validation
        /// <summary>
        /// 연산 시작 가능 여부 확인
        /// </summary>
        private bool CanStartOperation(Card card)
        {
            if (currentState != State.Idle) return false;
            if (card?.CardType != CardType.Operator) return false;
            if (!HasValidOperationConditions(card.OperatorType)) return false;

            return InGameManager.Instance.StartProcess(GameProcessState.OperatorCalculation);
        }

        /// <summary>
        /// 연산자별 실행 가능 확인
        /// </summary>
        private bool HasValidOperationConditions(OperatorType operatorType)
        {
            UpdateFieldCache();
            if (fieldCardsCache.Count < 2) return false;

            var myCards = fieldCardsCache.Count(c => c.CurrentOwnerType == CardZone.OwnerType.Player);
            var opponentCards = fieldCardsCache.Count(c => c.CurrentOwnerType == CardZone.OwnerType.Opponent);

            return operatorType switch
            {
                OperatorType.Plus or OperatorType.Multiply => myCards > 0,
                OperatorType.Minus or OperatorType.Divide => opponentCards > 0,
                _ => false
            };
        }

        /// <summary>
        /// 카드 클릭 처리 가능 여부 확인
        /// </summary>
        private bool CanProcessClick(Card card)
        {
            if (!IsInOperatorMode) return false;
            if (card.CurrentZoneType != CardZone.ZoneType.Field) return false;
            if (InGameManager.Instance.IsProcessing &&
                InGameManager.Instance.CurrentProcess != GameProcessState.OperatorCalculation) return false;

            return true;
        }

        /// <summary>
        /// 첫 번째 카드의 유효성 확인
        /// </summary>
        private bool IsValidFirstCard(Card card)
        {
            return currentOperator switch
            {
                OperatorType.Plus or OperatorType.Multiply => card.CurrentOwnerType == CardZone.OwnerType.Player,
                OperatorType.Minus or OperatorType.Divide => card.CurrentOwnerType == CardZone.OwnerType.Opponent,
                _ => false
            };
        }
        #endregion

        #region Utility
        /// <summary>
        /// 연산 모드 초기화
        /// </summary>
        private void InitializeOperation(Card card)
        {
            operatorCard = card;
            currentOperator = card.OperatorType;
            currentState = State.OperatorSelected;

            var ezManager = ExpressionZoneManager.Instance;
            ezManager.InitForOperation();
            ezManager.SetOperator(card);
            ezManager.StartOperationMode();
        }

        /// <summary>
        /// 연산 상태 초기화
        /// </summary>
        private void ResetOperationState()
        {
            currentState = State.Idle;
            operatorCard = null;
            firstCard = null;
            secondCard = null;
            InGameManager.Instance.EndProcess();
        }

        /// <summary>
        /// 카드 값들 반환
        /// </summary>
        private (float first, float second) GetCardValues()
        {
            float first = firstCard.GetComponentInChildren<CardText>().RawValue;
            float second = secondCard.GetComponentInChildren<CardText>().RawValue;
            return (first, second);
        }

        /// <summary>
        /// 플레이어 필드 Zone 찾기
        /// </summary>
        private CardZone FindPlayerFieldZone()
        {
            if (CardZone.AllZonesRoot == null) return null;

            return CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>()
                .FirstOrDefault(z => z.Owner == CardZone.OwnerType.Player && z.Zone == CardZone.ZoneType.Field);
        }

        /// <summary>
        /// 연산자 기호 반환
        /// </summary>
        private string GetOperatorSymbol()
        {
            return currentOperator switch
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