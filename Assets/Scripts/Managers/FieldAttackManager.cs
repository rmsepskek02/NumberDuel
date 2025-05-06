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

        private IEnumerator ResolveAttack(Card attacker, Card defender)
        {
            // ExpressionZone에 상대 카드 표시
            expressionManager.SetOpponentCard(defender);

            yield return new WaitForSeconds(2f);

            expressionManager.DisplayResult(attacker, defender);

            // 공격 후 처리: 다시 Glow 초기화
            ResetAllGlow();

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
