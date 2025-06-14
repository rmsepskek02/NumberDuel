using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Objects;
using Utills;

namespace Manager
{
    /// <summary>
    /// 연산자 카드 사용 시 두 장의 카드를 선택하여 연산을 수행하고,
    /// 수식존에 결과를 시각화하며 카드에 수치 반영까지 처리하는 매니저
    /// </summary>
    public class OperatorManager : Singleton<OperatorManager>
    {
        private enum OperatorState
        {
            Idle,
            OperatorSelected,
            FirstCardSelected,
            SecondCardSelected
        }

        private OperatorState currentState = OperatorState.Idle;
        private OperatorType currentOperatorType;

        private Card selectedOperatorCard;
        private Card firstTargetCard;
        private Card secondTargetCard;

        /// <summary>
        /// 현재 연산 프로세스 중인지 여부를 외부에서 확인할 수 있음
        /// </summary>
        public bool IsInOperatorMode => currentState != OperatorState.Idle;

        protected override void Awake()
        {
            base.Awake();
            Card.onClicked += OnCardClicked;
        }

        private void OnDestroy()
        {
            Card.onClicked -= OnCardClicked;
        }

        /// <summary>
        /// 연산자 카드 사용 시 연산 모드에 진입합니다.
        /// InGameManager 프로세스도 함께 설정합니다.
        /// </summary>
        public void EnterOperatorMode(Card operatorCard)
        {
            if (currentState != OperatorState.Idle) return;
            if (operatorCard.CardType != CardType.Operator) return;

            // 프로세스 시작
            if (!InGameManager.Instance.StartProcess(GameProcessState.OperatorCalculation))
            {
                Debug.LogWarning("[OperatorManager] 다른 프로세스가 진행 중이므로 연산 모드 진입 실패");
                return;
            }

            selectedOperatorCard = operatorCard;
            currentOperatorType = operatorCard.OperatorType;
            currentState = OperatorState.OperatorSelected;

            Debug.Log($"[OperatorManager] 연산 시작: {currentOperatorType}");

            // 수식존 상태 초기화 후 연산자 기호 표시
            ExpressionZoneManager.Instance.ConfigureSlot(0, "", null, false);
            ExpressionZoneManager.Instance.ConfigureSlot(2, "", null, false);
            ExpressionZoneManager.Instance.ConfigureSlot(4, "", null, false);
            ExpressionZoneManager.Instance.SetOperatorCard(operatorCard);

            SetupFirstSelectableCards();
        }

        /// <summary>
        /// 첫 번째 카드 선택 시 가능한 카드에 초록색 GLOW 적용
        /// </summary>
        private void SetupFirstSelectableCards()
        {
            var cards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in cards)
            {
                bool selectable = currentOperatorType switch
                {
                    OperatorType.Plus or OperatorType.Multiply => card.CurrentOwnerType == CardZone.OwnerType.Player,
                    OperatorType.Minus or OperatorType.Divide => card.CurrentOwnerType == CardZone.OwnerType.Opponent,
                    _ => false
                };

                card.SetCardState(selectable, Global.GlowGreen);
            }
        }

        /// <summary>
        /// 두 번째 카드 선택 시 가능한 카드(GLOW)를 설정합니다.
        /// </summary>
        private void SetupSecondSelectableCards()
        {
            var cards = InGameManager.Instance.GetAllFieldCards();

            foreach (var card in cards)
            {
                if (card == firstTargetCard) continue;
                card.SetCardState(true, Global.GlowGreen);
            }
        }

        /// <summary>
        /// 카드 클릭 시 연산 흐름을 처리합니다.
        /// 다른 프로세스 진행 중이면 처리하지 않습니다.
        /// </summary>
        public void OnCardClicked(Card card)
        {
            // 다른 프로세스가 진행 중이면 연산 처리 차단
            if (InGameManager.Instance.IsProcessing &&
                InGameManager.Instance.CurrentProcess != GameProcessState.OperatorCalculation)
            {
                Debug.Log($"[OperatorManager] {InGameManager.Instance.CurrentProcess} 진행 중이므로 연산 처리 차단");
                return;
            }

            // 연산 모드가 아니면 처리하지 않음
            if (!IsInOperatorMode)
            {
                return;
            }

            // 손패 카드는 연산 대상이 아님
            if (card.CurrentZoneType == CardZone.ZoneType.Hand)
            {
                Debug.Log("[OperatorManager] 손패 카드는 연산 대상이 아닙니다.");
                return;
            }

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

        /// <summary>
        /// 첫 번째 카드 선택 처리 및 GLOW 업데이트
        /// </summary>
        private void HandleFirstCardSelection(Card card)
        {
            // 선택 제한 조건 추가
            bool isSelectable = currentOperatorType switch
            {
                OperatorType.Plus or OperatorType.Multiply => card.CurrentOwnerType == CardZone.OwnerType.Player,
                OperatorType.Minus or OperatorType.Divide => card.CurrentOwnerType == CardZone.OwnerType.Opponent,
                _ => false
            };

            if (!isSelectable)
            {
                Debug.Log("[OperatorManager] 잘못된 카드 선택: 연산자에 맞는 대상 아님");
                return;
            }

            // 선택 처리
            firstTargetCard = card;
            currentState = OperatorState.FirstCardSelected;

            ExpressionZoneManager.Instance.SetMyCard(card);

            ResetAllCardGlow();
            SetupSecondSelectableCards();
        }


        /// <summary>
        /// 두 번째 카드 선택 시 연산 실행
        /// </summary>
        private void HandleSecondCardSelection(Card card)
        {
            secondTargetCard = card;
            currentState = OperatorState.SecondCardSelected;

            StartCoroutine(ExecuteOperation());
        }

        /// <summary>
        /// 연산 수행 및 결과 시각화, 값 반영 처리
        /// </summary>
        private IEnumerator ExecuteOperation()
        {
            var ez = ExpressionZoneManager.Instance;

            // 1. 수식 시각화: 내 카드, 연산자, 상대 카드 설정
            ez.SetMyCard(firstTargetCard);
            ez.SetOperatorCard(selectedOperatorCard);
            ez.SetOpponentCard(secondTargetCard);

            // 2. 연산 시각화 대기
            yield return new WaitForSeconds(2f);

            // 3. 숫자 값 추출
            long first = firstTargetCard.GetComponentInChildren<CardText>().RawValue;
            long second = secondTargetCard.GetComponentInChildren<CardText>().RawValue;

            long result = 0;
            long remainder = 0;

            // 결과 Sprite는 항상 첫 번째 카드 기준
            Sprite firstSprite = firstTargetCard.GetComponentInChildren<SpriteRenderer>()?.sprite;

            switch (currentOperatorType)
            {
                case OperatorType.Plus:
                    result = first + second;
                    ez.ConfigureSlot(4, result.ToString(), firstSprite, true);

                    yield return new WaitForSeconds(1f);

                    firstTargetCard.GetComponentInChildren<CardText>().SetRawValue(result);
                    firstTargetCard.SetWasModifiedThisTurn(true);
                    break;

                case OperatorType.Multiply:
                    result = first * second;
                    ez.ConfigureSlot(4, result.ToString(), firstSprite, true);

                    yield return new WaitForSeconds(1f);

                    firstTargetCard.GetComponentInChildren<CardText>().SetRawValue(result);
                    firstTargetCard.SetWasModifiedThisTurn(true);
                    break;

                case OperatorType.Minus:
                    result = first - second;
                    ez.ConfigureSlot(4, result.ToString(), firstSprite, true);

                    yield return new WaitForSeconds(1f);

                    if (result > 0)
                    {
                        // 결과가 양수면 첫 번째 카드에 반영
                        firstTargetCard.GetComponentInChildren<CardText>().SetRawValue(result);
                        firstTargetCard.SetWasModifiedThisTurn(true);
                    }
                    else
                    {
                        // 결과가 0 이하이면 첫 번째 카드 제거
                        var zone = firstTargetCard.GetComponentInParent<CardZone>();
                        zone?.RemoveCard(firstTargetCard.transform);
                        Destroy(firstTargetCard.gameObject);
                    }
                    break;

                case OperatorType.Divide:
                    if (second == 0)
                    {
                        Debug.LogWarning("[OperatorManager] 0으로 나눌 수 없습니다.");
                        yield break;
                    }

                    result = first / second;
                    remainder = first % second;

                    ez.ConfigureSlot(4, result.ToString(), firstSprite, true);

                    yield return new WaitForSeconds(1f);

                    // 몫을 첫 번째 카드에 반영
                    firstTargetCard.GetComponentInChildren<CardText>().SetRawValue(result);
                    firstTargetCard.SetWasModifiedThisTurn(true);

                    // 나머지가 있을 경우 내 필드에 새로운 카드 생성
                    if (remainder > 0)
                    {
                        Debug.Log("나머지 발생 = " + remainder);
                        //InGameManager.Instance.SpawnCardToField(remainder);
                    }
                    break;
            }

            // 4. GLOW 상태 초기화
            ResetAllCardGlow();
            ApplyGlowToAttackableCards();

            // 5. 내부 상태 초기화
            ResetState();
        }

        /// <summary>
        /// 전체 GLOW 제거
        /// </summary>
        private void ResetAllCardGlow()
        {
            var cards = InGameManager.Instance.GetAllFieldCards();
            foreach (var card in cards)
            {
                card.GetComponentInChildren<CardEffect>()?.SetGlow(false);
            }
        }

        /// <summary>
        /// 공격 가능한 내 필드 카드에 GLOW 설정
        /// </summary>
        private void ApplyGlowToAttackableCards()
        {
            foreach (var card in InGameManager.Instance.GetAllFieldCards())
            {
                bool canGlow = card.CurrentOwnerType == CardZone.OwnerType.Player &&
                               card.CurrentZoneType == CardZone.ZoneType.Field &&
                               card.IsAttackableThisTurn();

                card.SetCardState(canGlow); // GLOW 색상 자동 분기
            }
        }

        /// <summary>
        /// 내부 상태 초기화 (개선 버전)
        /// InGameManager 프로세스도 함께 종료
        /// </summary>
        private void ResetState()
        {
            currentState = OperatorState.Idle;
            selectedOperatorCard = null;
            firstTargetCard = null;
            secondTargetCard = null;

            // 프로세스 종료
            InGameManager.Instance.EndProcess();
            Debug.Log("[OperatorManager] 연산 프로세스 종료");
        }
    }
}
