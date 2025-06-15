using System.Collections;
using UnityEngine;
using Objects;

namespace Manager
{
    /// <summary>
    /// 필드에서의 공격 흐름을 처리하는 매니저 클래스 (수정 버전)
    /// - 공격자 선택 후 내 필드카드 선택 차단
    /// - 상대 필드카드만 빨간색 GLOW 효과
    /// - 내 필드카드들 GLOW 완전 제거
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
        /// 공격자 선택 후에는 내 필드카드 선택을 차단합니다.
        /// </summary>
        private void HandleCardClicked(Card clickedCard)
        {
            // 다른 프로세스가 진행 중이면 공격 차단
            if (InGameManager.Instance.IsProcessing)
            {
                Debug.Log($"[FieldAttackManager] {InGameManager.Instance.CurrentProcess} 진행 중이므로 공격 차단");
                return;
            }

            // 연산자 모드가 활성화된 경우 공격 차단
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                Debug.Log("[FieldAttackManager] 연산 중이므로 공격 차단");
                return;
            }

            // 내 필드 카드 클릭 처리
            if (clickedCard.CurrentOwnerType == CardZone.OwnerType.Player &&
                clickedCard.CurrentZoneType == CardZone.ZoneType.Field &&
                clickedCard.CanAttack)
            {
                // 이미 공격자가 선택된 상태라면 내 필드카드 선택 차단
                if (currentAttacker != null)
                {
                    Debug.Log("[FieldAttackManager] 공격자가 이미 선택됨. 내 필드카드 선택 차단");
                    return;
                }

                // 같은 카드 클릭 시 공격 취소
                if (currentAttacker == clickedCard)
                {
                    CancelAttack();
                    return;
                }

                // 새로운 공격자 선택
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
        /// 공격자 선택 후에는 내 필드카드들의 GLOW를 모두 끕니다.
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

            // 모든 내 필드카드의 GLOW 끄기 (공격자 포함)
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Player &&
                    card.CurrentZoneType == CardZone.ZoneType.Field)
                {
                    card.SetCardState(false); // 모든 내 필드카드 GLOW OFF
                }
            }

            // 상대 필드카드만 빨간색 GLOW ON
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                    card.CurrentZoneType == CardZone.ZoneType.Field)
                {
                    card.SetCardState(true, Global.GlowRed); // 빨간색 GLOW
                }
            }

            Debug.Log($"[FieldAttackManager] 공격자 선택: {attacker.name}. 상대 필드카드만 선택 가능");
        }

        /// <summary>
        /// 공격 취소 시 모든 GLOW 상태 및 수식존 상태 초기화
        /// </summary>
        private void CancelAttack()
        {
            Debug.Log("[FieldAttackManager] 공격 취소");

            currentAttacker = null;

            // 수식존 초기화
            expressionManager.ConfigureSlot(0, "", null, false); // 내 카드 초기화
            expressionManager.ConfigureSlot(1, "", null, false); // 연산자 초기화
            expressionManager.ConfigureSlot(2, "", null, false); // 상대 카드 초기화
            expressionManager.ConfigureSlot(4, "", null, false); // 결과 초기화

            // 모든 카드 GLOW 상태 초기화
            RefreshAllCardGlowStates();
        }

        /// <summary>
        /// 공격 실행 후 결과를 수식존과 카드에 반영합니다.
        /// </summary>
        private IEnumerator ResolveAttack(Card attacker, Card defender)
        {
            Debug.Log($"[FieldAttackManager] 공격 실행: {attacker.name} -> {defender.name}");

            // 수식존 2번 슬롯: 수비자 카드 시각화
            expressionManager.SetOpponentCard(defender);

            // 2초 대기 (시각적 연출)
            yield return new WaitForSeconds(2f);

            var myText = attacker.GetComponentInChildren<CardText>();
            var oppText = defender.GetComponentInChildren<CardText>();

            long myValue = myText?.RawValue ?? 0;
            long oppValue = oppText?.RawValue ?? 0;
            long result = myValue - oppValue;

            // 결과 표시
            expressionManager.DisplayResult(myValue, oppValue);

            yield return new WaitForSeconds(1f);

            var attackerZone = attacker.GetComponentInParent<CardZone>();
            var defenderZone = defender.GetComponentInParent<CardZone>();

            // 공격 결과 처리
            if (result > 0)
            {
                // 내 카드 생존, 상대 카드 파괴
                defenderZone?.RemoveCard(defender.transform);
                StartCoroutine(defender.AnimateRemoval(() => Destroy(defender.gameObject)));

                myText.SetRawValue(result);
                Debug.Log($"[FieldAttackManager] 공격 성공: {attacker.name} 생존 (수치: {result})");
            }
            else if (result < 0)
            {
                // 상대 카드 생존, 내 카드 파괴
                attackerZone?.RemoveCard(attacker.transform);
                StartCoroutine(attacker.AnimateRemoval(() => Destroy(attacker.gameObject)));

                oppText.SetRawValue(Mathf.Abs((int)result));
                Debug.Log($"[FieldAttackManager] 공격 실패: {defender.name} 생존 (수치: {Mathf.Abs(result)})");
            }
            else // result == 0
            {
                // 둘 다 파괴
                attackerZone?.RemoveCard(attacker.transform);
                defenderZone?.RemoveCard(defender.transform);

                StartCoroutine(attacker.AnimateRemoval(() => Destroy(attacker.gameObject)));
                StartCoroutine(defender.AnimateRemoval(() => Destroy(defender.gameObject), 0.2f));

                Debug.Log("[FieldAttackManager] 상호 파괴");
            }

            // 공격자가 생존했다면 이 턴엔 다시 공격할 수 없음
            if (result > 0)
            {
                attacker.SetWasModifiedThisTurn(true);
            }

            currentAttacker = null;

            // 1초 후 모든 카드 GLOW 상태 갱신
            yield return new WaitForSeconds(1f);
            RefreshAllCardGlowStates();
        }

        /// <summary>
        /// 모든 카드의 GLOW 상태를 기본 상태로 초기화
        /// </summary>
        private void RefreshAllCardGlowStates()
        {
            foreach (var card in FindObjectsByType<Card>(FindObjectsSortMode.None))
            {
                if (card.CurrentZoneType != CardZone.ZoneType.Field)
                    continue;

                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    // 내 필드카드: 공격 가능하면 초록색 GLOW
                    bool canAttack = card.IsAttackableThisTurn();
                    card.SetCardState(canAttack, canAttack ? Global.GlowGreen : null);
                }
                else
                {
                    // 상대 필드카드: GLOW 끄기
                    card.SetCardState(false);
                }
            }

            Debug.Log("[FieldAttackManager] 모든 카드 GLOW 상태 초기화 완료");
        }

        /// <summary>
        /// 현재 공격자가 선택된 상태인지 확인
        /// </summary>
        public bool HasAttackerSelected()
        {
            return currentAttacker != null;
        }

        /// <summary>
        /// 현재 공격자 정보 반환 (디버깅용)
        /// </summary>
        public Card GetCurrentAttacker()
        {
            return currentAttacker;
        }

        /// <summary>
        /// 강제로 공격 상태 초기화 (턴 종료 시 등)
        /// </summary>
        public void ForceResetAttackState()
        {
            if (currentAttacker != null)
            {
                CancelAttack();
                Debug.Log("[FieldAttackManager] 공격 상태 강제 초기화");
            }
        }
    }
}