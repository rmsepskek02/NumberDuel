using System.Collections;
using UnityEngine;
using Objects;

namespace Manager
{
    /// <summary>
    /// 필드에서의 공격 흐름을 처리하는 매니저 클래스
    /// - 공격자 선택
    /// - 수식 시각화
    /// - 공격 실행 및 결과 반영
    /// - 카드 GLOW 상태 제어
    /// </summary>
    public class FieldAttackManager : MonoBehaviour
    {
        private Card currentAttacker;
        private ExpressionZoneManager expressionManager;

        private void Awake()
        {
            expressionManager = FindAnyObjectByType<ExpressionZoneManager>();
        }

        private void OnEnable()
        {
            Card.onClicked += HandleCardClicked;
        }

        private void OnDisable()
        {
            Card.onClicked -= HandleCardClicked;
        }

        /// <summary>
        /// 카드 클릭 시 공격 흐름을 제어합니다.
        /// </summary>
        private void HandleCardClicked(Card clickedCard)
        {
            // 연산자 모드가 활성화된 경우 공격 차단
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                Debug.Log("[FieldAttackManager] 연산 중이므로 공격 차단");
                return;
            }

            // 내 필드 카드 클릭 (공격자 선택)
            if (clickedCard.CurrentOwnerType == CardZone.OwnerType.Player &&
                clickedCard.CurrentZoneType == CardZone.ZoneType.Field &&
                clickedCard.CanAttack)
            {
                if (currentAttacker == clickedCard)
                {
                    CancelAttack();
                    return;
                }

                SelectAttacker(clickedCard);
            }
            // 상대 필드 카드 클릭 (공격 실행)
            else if (clickedCard.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                     clickedCard.CurrentZoneType == CardZone.ZoneType.Field &&
                     currentAttacker != null)
            {
                StartCoroutine(ResolveAttack(currentAttacker, clickedCard));
            }
        }

        /// <summary>
        /// 공격자 카드를 선택하고 수식존 시각화 및 GLOW 상태를 업데이트합니다.
        /// </summary>
        private void SelectAttacker(Card attacker)
        {
            currentAttacker = attacker;

            // 수식 표현 설정
            expressionManager.ConfigureSlot(0, attacker.GetComponentInChildren<CardText>().TextValue,
                attacker.GetComponentInChildren<SpriteRenderer>().sprite, true);

            expressionManager.ConfigureSlot(1, "-", null, true); // 연산자
            expressionManager.ConfigureSlot(2, "", null, false); // 상대 카드 초기화
            expressionManager.ConfigureSlot(4, "", null, false); // 결과 초기화
            expressionManager.SetEqualSymbol();

            // 내 카드 GLOW OFF
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card == attacker) continue;

                if (card.CurrentOwnerType == CardZone.OwnerType.Player &&
                    card.CurrentZoneType == CardZone.ZoneType.Field)
                {
                    bool isSelf = card == attacker;
                    card.SetCardState(!isSelf); // 나머지만 켜기
                }
            }

            // 상대 카드 GLOW ON (빨간색)
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                    card.CurrentZoneType == CardZone.ZoneType.Field)
                {
                    card.SetCardState(true, Global.GlowRed);
                }
            }
        }

        /// <summary>
        /// 공격 취소 시 모든 GLOW 상태 및 수식존 상태 초기화
        /// </summary>
        private void CancelAttack()
        {
            currentAttacker = null;

            expressionManager.ConfigureSlot(0, "", null, false); // 내 카드 초기화

            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentZoneType != CardZone.ZoneType.Field) continue;

                // 내 필드 카드: 공격 가능 시 GLOW ON
                if (card.CurrentOwnerType == CardZone.OwnerType.Player &&
                    card.IsAttackableThisTurn())
                {
                    card.SetCardState(card.IsAttackableThisTurn());
                }
                else
                {
                    card.SetCardState(false);
                }
            }
        }

        /// <summary>
        /// 공격 실행 후 결과를 수식존과 카드에 반영합니다.
        /// </summary>
        private IEnumerator ResolveAttack(Card attacker, Card defender)
        {
            // 수식존 2번 슬롯: 수비자 카드 시각화
            expressionManager.SetOpponentCard(defender);

            // 2초 대기 (시각적 연출)
            yield return new WaitForSeconds(2f);

            var myText = attacker.GetComponentInChildren<CardText>();
            var oppText = defender.GetComponentInChildren<CardText>();

            long myValue = myText?.RawValue ?? 0;
            long oppValue = oppText?.RawValue ?? 0;
            long result = myValue - oppValue;

            // 5번 슬롯 결과 표시: 공격자는 항상 절댓값 + Sprite 자동 판단
            expressionManager.DisplayResult(myValue, oppValue);

            yield return new WaitForSeconds(1f);

            var attackerZone = attacker.GetComponentInParent<CardZone>();
            var defenderZone = defender.GetComponentInParent<CardZone>();

            // 공격 결과 처리
            if (result > 0)
            {
                // 내 카드 생존, 상대 카드 파괴
                defenderZone?.RemoveCard(defender.transform);
                Destroy(defender.gameObject);

                myText.SetRawValue(result);
            }
            else if (result < 0)
            {
                // 상대 카드 생존, 내 카드 파괴
                attackerZone?.RemoveCard(attacker.transform);
                Destroy(attacker.gameObject);

                oppText.SetRawValue(Mathf.Abs((int)result));
            }
            else // result == 0
            {
                attackerZone?.RemoveCard(attacker.transform);
                defenderZone?.RemoveCard(defender.transform);
                Destroy(attacker.gameObject);
                Destroy(defender.gameObject);
            }

            // 공격자 카드: 이 턴엔 다시 공격할 수 없음
            attacker.SetWasModifiedThisTurn(true);
            currentAttacker = null;

            // 모든 카드 GLOW 초기화 및 공격 가능 상태 갱신
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentZoneType != CardZone.ZoneType.Field)
                    continue;

                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    card.SetCardState(card.IsAttackableThisTurn());
                }
                else
                {
                    card.SetCardState(false);
                }
            }
        }

    }
}
