using UnityEngine;
using System.Collections;
using System.Linq;
using Objects;
using Utills;

namespace Manager
{
    /// <summary>
    /// 연산자 카드 사용 시 두 장의 카드를 선택하여 연산을 수행하는 매니저
    /// - 연산자별 첫 번째 카드 선택 제한 (더하기/곱하기: 내 필드, 빼기/나누기: 상대 필드)
    /// - 연산 결과를 수식존에 시각화 및 카드에 수치 반영
    /// - 나누기 연산 시 나머지 카드 생성 및 연산자 카드 삭제 처리
    /// </summary>
    public class OperatorManager : Singleton<OperatorManager>
    {
        #region Enums
        private enum OperatorState
        {
            Idle,
            OperatorSelected,
            FirstCardSelected,
            SecondCardSelected
        }
        #endregion

        #region Private Fields
        private OperatorState currentState = OperatorState.Idle;
        private OperatorType currentOperatorType;

        private Card selectedOperatorCard;
        private Card firstTargetCard;
        private Card secondTargetCard;
        #endregion

        #region Properties
        /// <summary>
        /// 현재 연산 프로세스 중인지 여부
        /// </summary>
        public bool IsInOperatorMode => currentState != OperatorState.Idle;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            Card.onClicked += OnCardClicked;
        }

        private void OnDestroy()
        {
            Card.onClicked -= OnCardClicked;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 연산자 카드 사용 시 연산 모드에 진입
        /// </summary>
        public void EnterOperatorMode(Card operatorCard)
        {
            if (!ValidateOperatorEntry(operatorCard)) return;

            InitializeOperatorMode(operatorCard);
            InitializeExpressionZone();
            SetupFirstSelectableCards();
        }
        #endregion

        #region Card Click Handling
        /// <summary>
        /// 카드 클릭 시 연산 흐름 처리
        /// </summary>
        public void OnCardClicked(Card card)
        {
            if (!CanProcessCardClick(card)) return;

            switch (currentState)
            {
                case OperatorState.OperatorSelected:
                    HandleFirstCardSelection(card);
                    break;
                case OperatorState.FirstCardSelected:
                    HandleSecondCardSelection(card);
                    break;
            }
        }
        #endregion

        #region Card Selection Logic
        /// <summary>
        /// 첫 번째 카드 선택 처리
        /// </summary>
        private void HandleFirstCardSelection(Card card)
        {
            if (!IsValidFirstCard(card)) return;

            firstTargetCard = card;
            currentState = OperatorState.FirstCardSelected;

            ExpressionZoneManager.Instance.SetMyCard(card);
            UpdateGlowForSecondSelection();
        }

        /// <summary>
        /// 두 번째 카드 선택 시 연산 실행
        /// </summary>
        private void HandleSecondCardSelection(Card card)
        {
            secondTargetCard = card;
            currentState = OperatorState.SecondCardSelected;
            StartCoroutine(ExecuteOperationSequence());
        }
        #endregion

        #region Operation Execution
        /// <summary>
        /// 전체 연산 시퀀스 실행
        /// </summary>
        private IEnumerator ExecuteOperationSequence()
        {
            SetupExpressionVisualization();
            yield return new WaitForSeconds(2f);

            yield return StartCoroutine(PerformCalculation());
            yield return StartCoroutine(RemoveOperatorCard());

            RestoreGlowStates();
            ResetOperatorState();
        }

        /// <summary>
        /// 실제 연산 계산 및 결과 적용
        /// </summary>
        private IEnumerator PerformCalculation()
        {
            var (first, second) = GetCardValues();
            var firstSprite = GetCardSprite(firstTargetCard);

            yield return StartCoroutine(ExecuteOperationByType(first, second, firstSprite));
        }

        /// <summary>
        /// 연산자 타입에 따른 연산 실행
        /// </summary>
        private IEnumerator ExecuteOperationByType(long first, long second, Sprite firstSprite)
        {
            var ez = ExpressionZoneManager.Instance;

            switch (currentOperatorType)
            {
                case OperatorType.Plus:
                    yield return StartCoroutine(HandleAddition(first, second, firstSprite, ez));
                    break;
                case OperatorType.Multiply:
                    yield return StartCoroutine(HandleMultiplication(first, second, firstSprite, ez));
                    break;
                case OperatorType.Minus:
                    yield return StartCoroutine(HandleSubtraction(first, second, firstSprite, ez));
                    break;
                case OperatorType.Divide:
                    yield return StartCoroutine(HandleDivision(first, second, firstSprite, ez));
                    break;
            }
        }
        #endregion

        #region Operation Types
        private IEnumerator HandleAddition(long first, long second, Sprite sprite, ExpressionZoneManager ez)
        {
            long result = first + second;
            ez.ConfigureSlot(4, result.ToString(), sprite, true);
            yield return new WaitForSeconds(1f);

            ApplyResultToCard(firstTargetCard, result);
        }

        private IEnumerator HandleMultiplication(long first, long second, Sprite sprite, ExpressionZoneManager ez)
        {
            long result = first * second;
            ez.ConfigureSlot(4, result.ToString(), sprite, true);
            yield return new WaitForSeconds(1f);

            ApplyResultToCard(firstTargetCard, result);
        }

        private IEnumerator HandleSubtraction(long first, long second, Sprite sprite, ExpressionZoneManager ez)
        {
            long result = first - second;
            ez.ConfigureSlot(4, result.ToString(), sprite, true);
            yield return new WaitForSeconds(1f);

            if (result > 0)
            {
                ApplyResultToCard(firstTargetCard, result);
            }
            else
            {
                RemoveCard(firstTargetCard);
            }
        }

        private IEnumerator HandleDivision(long first, long second, Sprite sprite, ExpressionZoneManager ez)
        {
            if (second == 0)
            {
                Debug.LogWarning("[OperatorManager] 0으로 나눌 수 없습니다.");
                yield break;
            }

            long result = first / second;
            long remainder = first % second;

            ez.ConfigureSlot(4, result.ToString(), sprite, true);
            yield return new WaitForSeconds(1f);

            if (result > 0)
            {
                ApplyResultToCard(firstTargetCard, result);
            }
            else
            {
                RemoveCard(firstTargetCard);
            }

            // 나머지 처리: 몫이 0이어도 나머지가 있으면 카드 생성
            if (remainder > 0)
            {
                CreateRemainderCard(remainder);
            }
        }
        #endregion

        #region Card Management
        /// <summary>
        /// 연산자 카드를 애니메이션과 함께 제거
        /// </summary>
        private IEnumerator RemoveOperatorCard()
        {
            if (selectedOperatorCard == null) yield break;

            CardZone zone = FindZoneOfCard(selectedOperatorCard.transform);
            yield return StartCoroutine(selectedOperatorCard.AnimateRemoval(() =>
            {
                zone?.RemoveCard(selectedOperatorCard.transform);
                Destroy(selectedOperatorCard.gameObject);
            }));
        }

        /// <summary>
        /// 나머지 값으로 내 필드에 숫자 카드 생성
        /// </summary>
        private void CreateRemainderCard(long remainderValue)
        {
            CardZone playerFieldZone = FindPlayerFieldZone();
            if (playerFieldZone == null)
            {
                Debug.LogError("[OperatorManager] 플레이어 필드 Zone을 찾을 수 없습니다.");
                return;
            }

            GameObject template = ResourcesManager.Instance.GetPlayerCardTemplate();
            if (template == null)
            {
                Debug.LogError("[OperatorManager] 플레이어 카드 템플릿을 가져올 수 없습니다.");
                return;
            }

            GameObject card = Instantiate(template);
            card.name = $"RemainderCard_{remainderValue}";
            card.SetActive(true);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            var cardComponent = card.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.InitializeAsNumber(remainderValue);
                cardComponent.SetWasModifiedThisTurn(true); // 해당 턴 공격 불가
            }

            playerFieldZone.AddCard(card.transform);
        }

        /// <summary>
        /// 카드에 연산 결과 적용
        /// </summary>
        private void ApplyResultToCard(Card card, long value)
        {
            card.GetComponentInChildren<CardText>().SetRawValue(value);
            card.SetWasModifiedThisTurn(true);
        }

        /// <summary>
        /// 카드 제거
        /// </summary>
        private void RemoveCard(Card card)
        {
            var zone = card.GetComponentInParent<CardZone>();
            zone?.RemoveCard(card.transform);
            Destroy(card.gameObject);
        }
        #endregion

        #region GLOW Management
        /// <summary>
        /// 첫 번째 카드 선택을 위한 GLOW 설정
        /// </summary>
        private void SetupFirstSelectableCards()
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in fieldCards)
            {
                bool isFirstSelectable = currentOperatorType switch
                {
                    OperatorType.Plus or OperatorType.Multiply => card.CurrentOwnerType == CardZone.OwnerType.Player,
                    OperatorType.Minus or OperatorType.Divide => card.CurrentOwnerType == CardZone.OwnerType.Opponent,
                    _ => false
                };

                card.SetCardState(isFirstSelectable, Global.GlowGreen);
            }
        }

        /// <summary>
        /// 두 번째 카드 선택을 위한 GLOW 업데이트
        /// </summary>
        private void UpdateGlowForSecondSelection()
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in fieldCards)
            {
                bool isSecondSelectable = (card != firstTargetCard);
                card.SetCardState(isSecondSelectable, Global.GlowGreen);
            }
        }

        /// <summary>
        /// GLOW 상태 복원
        /// </summary>
        private void RestoreGlowStates()
        {
            ClearAllGlow();
            ApplyGlowToAttackableCards();
        }

        /// <summary>
        /// 모든 카드의 GLOW 제거
        /// </summary>
        private void ClearAllGlow()
        {
            var cards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in cards)
                card.SetCardState(false);
        }

        /// <summary>
        /// 공격 가능한 카드에 GLOW 설정
        /// </summary>
        private void ApplyGlowToAttackableCards()
        {
            var cards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in cards)
            {
                bool canGlow = card.CurrentOwnerType == CardZone.OwnerType.Player &&
                               card.CurrentZoneType == CardZone.ZoneType.Field &&
                               card.IsAttackableThisTurn();

                card.SetCardState(canGlow);
            }
        }
        #endregion

        #region Validation & Utility
        /// <summary>
        /// 연산자 모드 진입 유효성 검증
        /// </summary>
        private bool ValidateOperatorEntry(Card operatorCard)
        {
            if (currentState != OperatorState.Idle) return false;
            if (operatorCard.CardType != CardType.Operator) return false;

            if (!ValidateOperatorConditions(operatorCard.OperatorType))
            {
                ShowOperatorConditionWarning(operatorCard.OperatorType);
                return false;
            }

            return InGameManager.Instance.StartProcess(GameProcessState.OperatorCalculation);
        }

        /// <summary>
        /// 연산자별 사용 조건 검증
        /// </summary>
        private bool ValidateOperatorConditions(OperatorType operatorType)
        {
            var fieldCards = InGameManager.Instance.GetAllFieldCards();
            var myFieldCards = fieldCards.FindAll(card => card.CurrentOwnerType == CardZone.OwnerType.Player);
            var opponentFieldCards = fieldCards.FindAll(card => card.CurrentOwnerType == CardZone.OwnerType.Opponent);

            if (fieldCards.Count < 2) return false;

            return operatorType switch
            {
                OperatorType.Plus or OperatorType.Multiply => myFieldCards.Count > 0,
                OperatorType.Minus or OperatorType.Divide => opponentFieldCards.Count > 0,
                _ => false
            };
        }

        /// <summary>
        /// 연산자 사용 조건 경고 메시지 표시
        /// </summary>
        private void ShowOperatorConditionWarning(OperatorType operatorType)
        {
            string operatorName = operatorType switch
            {
                OperatorType.Plus => "더하기(+)",
                OperatorType.Minus => "빼기(-)",
                OperatorType.Multiply => "곱하기(×)",
                OperatorType.Divide => "나누기(÷)",
                _ => "연산자"
            };

            string condition = operatorType switch
            {
                OperatorType.Plus or OperatorType.Multiply => "내 필드에 카드가 있고 전체 필드에 최소 2장의 카드가 필요합니다.",
                OperatorType.Minus or OperatorType.Divide => "상대 필드에 카드가 있고 전체 필드에 최소 2장의 카드가 필요합니다.",
                _ => "사용 조건을 만족해야 합니다."
            };

            Debug.LogWarning($"[OperatorManager] {operatorName} 카드를 사용하려면 {condition}");
        }

        /// <summary>
        /// 카드 클릭 처리 가능 여부 확인
        /// </summary>
        private bool CanProcessCardClick(Card card)
        {
            if (InGameManager.Instance.IsProcessing &&
                InGameManager.Instance.CurrentProcess != GameProcessState.OperatorCalculation)
                return false;

            if (!IsInOperatorMode) return false;
            if (card.CurrentZoneType == CardZone.ZoneType.Hand) return false;

            return true;
        }

        /// <summary>
        /// 첫 번째 카드 선택 유효성 검증
        /// </summary>
        private bool IsValidFirstCard(Card card)
        {
            return currentOperatorType switch
            {
                OperatorType.Plus or OperatorType.Multiply => card.CurrentOwnerType == CardZone.OwnerType.Player,
                OperatorType.Minus or OperatorType.Divide => card.CurrentOwnerType == CardZone.OwnerType.Opponent,
                _ => false
            };
        }

        /// <summary>
        /// 카드 값들 반환
        /// </summary>
        private (long first, long second) GetCardValues()
        {
            long first = firstTargetCard.GetComponentInChildren<CardText>().RawValue;
            long second = secondTargetCard.GetComponentInChildren<CardText>().RawValue;
            return (first, second);
        }

        /// <summary>
        /// 카드 스프라이트 반환
        /// </summary>
        private Sprite GetCardSprite(Card card)
        {
            return card.GetComponentInChildren<SpriteRenderer>()?.sprite;
        }

        /// <summary>
        /// 플레이어 필드 Zone 찾기
        /// </summary>
        private CardZone FindPlayerFieldZone()
        {
            if (CardZone.AllZonesRoot == null) return null;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            return zones.FirstOrDefault(z =>
                z.Owner == CardZone.OwnerType.Player &&
                z.Zone == CardZone.ZoneType.Field);
        }

        /// <summary>
        /// 카드가 속한 Zone 찾기
        /// </summary>
        private CardZone FindZoneOfCard(Transform card)
        {
            if (CardZone.AllZonesRoot == null || card == null) return null;

            foreach (var zone in CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>())
            {
                if (zone.Contains(card))
                    return zone;
            }
            return null;
        }
        #endregion

        #region Initialization & Cleanup
        /// <summary>
        /// 연산자 모드 초기화
        /// </summary>
        private void InitializeOperatorMode(Card operatorCard)
        {
            selectedOperatorCard = operatorCard;
            currentOperatorType = operatorCard.OperatorType;
            currentState = OperatorState.OperatorSelected;
        }

        /// <summary>
        /// 수식존 초기화
        /// </summary>
        private void InitializeExpressionZone()
        {
            var ez = ExpressionZoneManager.Instance;
            ez.ConfigureSlot(0, "", null, false);
            ez.ConfigureSlot(2, "", null, false);
            ez.ConfigureSlot(4, "", null, false);
            ez.SetOperatorCard(selectedOperatorCard);
        }

        /// <summary>
        /// 수식 시각화 설정
        /// </summary>
        private void SetupExpressionVisualization()
        {
            var ez = ExpressionZoneManager.Instance;
            ez.SetMyCard(firstTargetCard);
            ez.SetOperatorCard(selectedOperatorCard);
            ez.SetOpponentCard(secondTargetCard);
        }

        /// <summary>
        /// 연산자 상태 초기화
        /// </summary>
        private void ResetOperatorState()
        {
            currentState = OperatorState.Idle;
            selectedOperatorCard = null;
            firstTargetCard = null;
            secondTargetCard = null;

            InGameManager.Instance.EndProcess();
        }
        #endregion
    }
}