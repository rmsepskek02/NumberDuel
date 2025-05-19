using System.Collections;
using UnityEngine;
using Objects;

namespace Manager
{
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

        private void HandleCardClicked(Card clickedCard)
        {
            // 내 필드 카드 && 공격 가능할 경우
            if (clickedCard.CurrentOwnerType == CardZone.OwnerType.Player &&
                clickedCard.CurrentZoneType == CardZone.ZoneType.Field &&
                clickedCard.CanAttack)
            {
                // 같은 카드를 다시 클릭한 경우 → 공격 취소 처리
                if (currentAttacker == clickedCard)
                {
                    CancelAttack();
                    return;
                }

                SelectAttacker(clickedCard);
            }
            // 상대 카드 클릭 시
            else if (clickedCard.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                     clickedCard.CurrentZoneType == CardZone.ZoneType.Field &&
                     currentAttacker != null)
            {
                StartCoroutine(ResolveAttack(currentAttacker, clickedCard));
            }
        }

        private void SelectAttacker(Card attacker)
        {
            currentAttacker = attacker;

            expressionManager.ClearOpponentAndResult(); // 3번, 5번 카드 초기화

            // ExpressionZone에 내 카드 설정
            expressionManager.SetMyCard(attacker);

            // 내 필드 카드 중 나머지 Glow Off
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card == attacker) continue;

                if (card.CurrentOwnerType == CardZone.OwnerType.Player &&
                    card.CurrentZoneType == CardZone.ZoneType.Field)
                {
                    card.SetCanAttack(false); // Glow 끄기
                }
            }

            // 상대 카드 Glow 켜기 (붉은색)
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                    card.CurrentZoneType == CardZone.ZoneType.Field)
                {
                    card.SetCanAttack(true); // 내부적으로 붉은 Glow
                }
            }
        }

        /// <summary>
        /// 현재 공격자를 취소하고 수식 및 Glow 상태를 초기화합니다.
        /// </summary>
        private void CancelAttack()
        {
            currentAttacker = null;

            // ExpressionZone의 1번 카드 초기화
            expressionManager.ClearMyCard();

            // 모든 카드 순회
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                // 내 필드 카드 중 공격 가능한 카드: Glow 다시 설정
                if (card.CurrentOwnerType == CardZone.OwnerType.Player &&
                    card.CurrentZoneType == CardZone.ZoneType.Field &&
                    card.IsAttackableThisTurn())
                {
                    card.SetCanAttack(true); // 초록색 Glow
                }
                else
                {
                    // 상대 카드 또는 공격 불가능한 카드 → Glow 끄기
                    card.SetCanAttack(false);
                }
            }
        }

        private IEnumerator ResolveAttack(Card attacker, Card defender)
        {
            // 수비자 카드 수식 표현
            expressionManager.SetOpponentCard(defender);

            // 잠시 대기 후 결과 표시
            yield return new WaitForSeconds(2f);
            expressionManager.DisplayResult(attacker, defender);

            // 결과 수치 계산
            var myText = attacker.GetComponentInChildren<CardText>();
            var oppText = defender.GetComponentInChildren<CardText>();

            var defenderZone = defender.GetComponentInParent<CardZone>();
            var attackerZone = attacker.GetComponentInParent<CardZone>();

            if (myText == null || oppText == null)
            {
                Debug.LogWarning("[FieldAttackManager] 카드 텍스트 정보를 찾을 수 없습니다.");
                yield break;
            }

            long result = myText.RawValue - oppText.RawValue;

            yield return new WaitForSeconds(1f);

            // 결과 적용 규칙
            if (result > 0)
            {
                // 내 카드 생존, 상대 카드 파괴
                if (defenderZone != null)
                    defenderZone.RemoveCard(defender.transform);
                Destroy(defender.gameObject);
                myText.SetRawValue(result);
            }
            else if (result < 0)
            {
                // 상대 카드 생존, 내 카드 파괴
                if (attackerZone != null)
                    attackerZone.RemoveCard(attacker.transform);
                Destroy(attacker.gameObject);
                oppText.SetRawValue(System.Math.Abs(result));
            }
            else // result == 0
            {
                if (attackerZone != null)
                    attackerZone.RemoveCard(attacker.transform);
                if (defenderZone != null)
                    defenderZone.RemoveCard(defender.transform);
                Destroy(attacker.gameObject);
                Destroy(defender.gameObject);
            }

            // 공격자 카드 이번 턴에는 공격 불가
            attacker.SetWasModifiedThisTurn(true);

            // GLOW 초기화
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentZoneType != CardZone.ZoneType.Field) continue;

                // 상대 카드: GLOW 유지 없이 무시
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                {
                    card.SetCanAttack(false); // GLOW OFF
                }
                else
                {
                    // 내 카드: 공격 가능 여부에 따라 GLOW 복원
                    card.SetCanAttack(card.IsAttackableThisTurn());
                }
            }

            // 현재 공격자 비우기
            currentAttacker = null;
        }

        private void ResetAllGlow()
        {
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                card.SetCanAttack(false); // 전부 끄기
            }
        }
    }
}
