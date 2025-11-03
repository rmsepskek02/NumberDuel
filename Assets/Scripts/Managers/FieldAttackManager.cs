using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Objects;

namespace Manager
{
    /// <summary>
    /// 필드 카드 간 공격 시스템을 관리하는 매니저
    /// 공격자 선택 및 대상자 선택 후 공격 처리 및 결과 처리 흐름을 담당
    /// Secret 카드 공개와 네트워크 동기화 포함
    /// </summary>
    public class FieldAttackManager : MonoBehaviour
    {
        #region Fields and Properties
        [SerializeField] private bool enableDebugLog = false;

        private Card currentAttacker;

        // 성능 최적화용 캐시
        private readonly List<Card> fieldCardsCache = new List<Card>();
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            Card.onClicked += HandleCardClick;
        }

        private void OnDisable()
        {
            Card.onClicked -= HandleCardClick;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 현재 공격자가 선택된 상태인지 확인
        /// </summary>
        public bool HasAttackerSelected() => currentAttacker != null;

        /// <summary>
        /// 현재 공격자 반환
        /// </summary>
        public Card GetCurrentAttacker() => currentAttacker;

        /// <summary>
        /// 강제 공격 상태 초기화 (턴 종료, 다른 프로세스 취소 시 사용)
        /// </summary>
        public void ForceResetAttackState()
        {
            if (currentAttacker != null)
            {
                ResetAttackState();
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 강제 공격 상태 초기화");
            }
        }
        #endregion

        #region Attack Flow
        /// <summary>
        /// 카드 클릭 이벤트 처리
        /// </summary>
        private void HandleCardClick(Card clickedCard)
        {
            if (!CanProcessAttack()) return;

            // 공격자 선택 (내 필드 카드)
            if (IsValidAttacker(clickedCard))
            {
                if (currentAttacker != null) return; // 이미 공격자 선택됨
                SelectAttacker(clickedCard);
            }
            // 대상 선택 (상대 필드 카드)
            else if (IsValidTarget(clickedCard))
            {
                if (currentAttacker == null) return; // 공격자 미선택
                StartCoroutine(ExecuteAttack(currentAttacker, clickedCard));
            }
        }

        /// <summary>
        /// 공격자 선택 처리
        /// </summary>
        private void SelectAttacker(Card attacker)
        {
            // 공격 프로세스 시작 (턴 종료 차단 및 다른 프로세스와의 충돌 방지)
            if (!InGameManager.Instance.StartProcess(GameProcessState.CardAttackProcess))
            {
                if (enableDebugLog)
                    Debug.LogWarning("[FieldAttackManager] 다른 프로세스가 진행 중입니다.");
                return; // 다른 프로세스 진행 중이면 중단
            }

            currentAttacker = attacker;
            SetupExpressionZone(attacker);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격자 선택 및 프로세스 시작: {attacker.name}");

            // 상대 필드가 비어있는지 체크
            if (IsOpponentFieldEmpty())
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 상대 필드가 비어있음 - 상대 본 필드 직접 공격");

                // 상대 본 필드 직접 공격
                StartCoroutine(ExecuteEmptyFieldAttack(attacker));
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 상대 필드에 카드 존재 - 대상 선택 대기");

                // 대상 선택: 상대 카드 선택 대기
                UpdateGlowStatesForTargetSelection();
            }
        }

        /// <summary>
        /// 빈 필드 직접 공격 (Secret 카드 공개 포함)
        /// </summary>
        private IEnumerator ExecuteEmptyFieldAttack(Card attacker)
        {
            // 게임이 종료되었다면 공격 중단
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 빈 필드 직접 공격: {attacker.name}");

            // Secret 처리: 공격자가 Secret 상태라면 공개해야 함
            bool attackerWasSecret = attacker.IsSecret;
            if (attackerWasSecret)
            {
                attacker.RevealSecret();
                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 공격자 Secret 카드 공개: {attacker.name}");
            }

            // 공격 아이콘 표시
            attacker.ShowAttackIcon();

            // 공격 카드의 본 이름 저장 (Secret 공개 후)
            float originalAttackerValue = GetCardValue(attacker);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격 카드의 값: {originalAttackerValue}");

            // ExpressionZone에 공격 상황을 설정 (방어 카드는 "0")
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.SetEmptyFieldDefender(CardZone.OwnerType.Opponent);

            yield return new WaitForSeconds(1.5f);

            // 게임 종료 재체크
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // 공격 결과 계산 및 표시
            ezManager.ShowEmptyFieldResult(originalAttackerValue);

            yield return new WaitForSeconds(1f);

            // 게임 종료 재체크
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // 빈 필드 공격 결과 적용 (상대 본 체력 감소)
            int damage = ApplyEmptyFieldResult(attacker, originalAttackerValue);

            // 공격 완료 표시
            attacker.SetHasAttackedThisTurn(true);

            // 동기화: 전체 공격 액션 동기화 (ExpressionZone + HP 갱신)
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncCombatAction(
                    attacker,
                    null,  // defender는 null (빈 필드)
                    originalAttackerValue,
                    0f,    // 방어자 값 0
                    damage // 실제 데미지
                );
            }

            // 공격 아이콘 숨김
            attacker.HideAllIcons();

            yield return new WaitForSeconds(0.8f);

            // 공격 완료 후 상태 정리 (프로세스 종료 포함)
            if (!InGameManager.Instance.IsGameEnded)
            {
                ResetAttackState(); // currentAttacker = null, EndProcess, ExpressionZone 리셋, GLOW 복원 모두 처리
            }
            else
            {
                // 게임 종료 시에는 최소한의 정리만
                currentAttacker = null;
                if (InGameManager.Instance.IsProcessing &&
                    InGameManager.Instance.CurrentProcess == GameProcessState.CardAttackProcess)
                {
                    InGameManager.Instance.EndProcess();
                }
            }

            if (enableDebugLog)
                Debug.Log("[FieldAttackManager] 빈 필드 공격 완료 및 프로세스 종료");
        }

        /// <summary>
        /// 일반 카드 간 공격 처리 (Secret 카드 공개 포함)
        /// </summary>
        private IEnumerator ExecuteAttack(Card attacker, Card defender)
        {
            // 게임이 종료되었다면 공격 중단
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격 시작: {attacker.name} vs {defender.name}");

            // Secret 처리: 공격자와 방어자 모두 Secret 상태라면 공개해야 함
            bool attackerWasSecret = attacker.IsSecret;
            bool defenderWasSecret = defender.IsSecret;

            if (attackerWasSecret)
            {
                attacker.RevealSecret();
                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 공격자 Secret 카드 공개: {attacker.name}");
            }

            if (defenderWasSecret)
            {
                defender.RevealSecret();
                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 방어자 Secret 카드 공개: {defender.name}");
            }

            // 공격 아이콘 표시
            attacker.ShowAttackIcon();
            defender.ShowDefenseIcon();

            // 양쪽 카드 본 이름 저장 (Secret 공개 후 수 사용)
            var (originalAttackerValue, originalDefenderValue) = GetCardValues(attacker, defender);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 전투 본 수치: 공격자={originalAttackerValue}, 방어자={originalDefenderValue}");

            // 표현식으로 방어자 설정
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.SetDefenderCard(defender);

            yield return new WaitForSeconds(1.5f);

            // 게임 종료 재체크
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // 공격 결과 계산 및 표시
            ezManager.ShowResult(originalAttackerValue, originalDefenderValue);

            yield return new WaitForSeconds(1f);

            // 게임 종료 재체크
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // 결과 적용 (카드 값 변경) - 실제 데미지 반환
            int damage = ApplyBattleResult(attacker, defender, originalAttackerValue, originalDefenderValue, defenderWasSecret);

            // 공격 완료 표시
            attacker.SetHasAttackedThisTurn(true);

            // 동기화: 전체 공격 액션 동기화 (ExpressionZone + HP 갱신)
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncCombatAction(
                    attacker,
                    defender,
                    originalAttackerValue,
                    originalDefenderValue,
                    damage // 실제 데미지
                );
            }

            // 공격 아이콘 숨김
            attacker.HideAllIcons();
            defender.HideAllIcons();

            yield return new WaitForSeconds(0.8f);

            // 공격 완료 후 상태 정리 (프로세스 종료 포함)
            if (!InGameManager.Instance.IsGameEnded)
            {
                ResetAttackState(); // currentAttacker = null, EndProcess, ExpressionZone 리셋, GLOW 복원 모두 처리
            }
            else
            {
                // 게임 종료 시에는 최소한의 정리만
                currentAttacker = null;
                if (InGameManager.Instance.IsProcessing &&
                    InGameManager.Instance.CurrentProcess == GameProcessState.CardAttackProcess)
                {
                    InGameManager.Instance.EndProcess();
                }
            }

            if (enableDebugLog)
                Debug.Log("[FieldAttackManager] 공격 완료 및 프로세스 종료");
        }

        /// <summary>
        /// 빈 필드 공격 결과 적용
        /// </summary>
        /// <param name="attacker">공격자 카드</param>
        /// <param name="damage">상대에게 입힐 데미지</param>
        private int ApplyEmptyFieldResult(Card attacker, float damage)
        {
            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 빈 필드 공격 결과 적용 - 데미지: {damage}");

            // DamageCalculator를 통해 실제 데미지 계산
            int finalDamage = Utills.DamageCalculator.CalculateEmptyFieldDamage(damage);

            // HealthManager를 통해 상대 체력에 적용
            if (HealthManager.Instance != null)
            {
                int actualDamage = HealthManager.Instance.ApplyDamage(finalDamage, CardZone.OwnerType.Opponent);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 상대에게 {actualDamage} 데미지 적용 완료");

                return actualDamage; // 추가: 데미지 반환
            }
            else
            {
                Debug.LogError("[FieldAttackManager] HealthManager를 찾을 수 없습니다!");
                return 0;
            }
        }


        /// <summary>
        /// 전투 결과 적용 (카드 값 변경)
        /// </summary>
        private int ApplyBattleResult(Card attacker, Card defender, float originalAttackerValue, float originalDefenderValue, bool defenderWasSecret = false)
        {
            var attackerText = attacker.GetComponentInChildren<CardText>();
            var defenderText = defender.GetComponentInChildren<CardText>();

            float result = originalAttackerValue - originalDefenderValue;
            int actualDamage = 0; // 추가: 데미지 변수

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 전투 결과 계산: {originalAttackerValue} - {originalDefenderValue} = {result}");

            if (result > 0) // 공격자 승리
            {
                // 상대 체력에 피해 (카드 파괴 시 체력 본 차이 감소)
                // 예외처리: 시크릿 카드를 공격해서 이겼을 때는 체력 감소 없음
                if (HealthManager.Instance != null && !defenderWasSecret)
                {
                    int damage = Utills.DamageCalculator.CalculateAttackDamage(originalAttackerValue, originalDefenderValue);
                    actualDamage = HealthManager.Instance.ApplyDamage(damage, CardZone.OwnerType.Opponent);

                    if (enableDebugLog)
                        Debug.Log($"[FieldAttackManager] 카드 파괴 - 상대에게 {actualDamage} 데미지");
                }
                else if (defenderWasSecret && enableDebugLog)
                {
                    Debug.Log($"[FieldAttackManager] 시크릿 카드 파괴 - 데미지 없음");
                }

                // 카드 수치 처리 (공격자 남은 값)
                attackerText.SetRawValue(result);
                DestroyCard(defender);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] {attacker.name} 승리 (남은 값: {result})");
            }
            else if (result < 0) // 방어자 승리
            {
                // 방어자 승리 상황에서 플레이어는 데미지 받지 않음 (현재 요구사항)
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 방어자 승리 - 플레이어 데미지 없음");

                // 카드 수치 처리
                defenderText.SetRawValue(Mathf.Abs(result));
                DestroyCard(attacker);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] {defender.name} 승리 (남은 값: {Mathf.Abs(result)})");
            }
            else // 무승부
            {
                // 무승부 상황에서 데미지 없음
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 무승부 - 데미지 없음");

                // 카드 수치 처리
                DestroyCard(attacker);
                StartCoroutine(DelayedDestroy(defender, 0.2f));

                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 양측 파괴");
            }

            return actualDamage; // 추가: 데미지 반환
        }
        #endregion

        #region Expression Zone Management
        /// <summary>
        /// 공격에 맞춰 준비
        /// </summary>
        private void SetupExpressionZone(Card attacker)
        {
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.InitForAttack();
            ezManager.SetAttackerCard(attacker);
            ezManager.StartAttackMode();
        }

        /// <summary>
        /// 공격 상태 초기화
        /// </summary>
        private void ResetAttackState()
        {
            currentAttacker = null;

            if (InGameManager.Instance.IsProcessing &&
                InGameManager.Instance.CurrentProcess == GameProcessState.CardAttackProcess)
            {
                InGameManager.Instance.EndProcess();
            }

            ExpressionZoneManager.Instance.ResetAllSlots();
            RestoreDefaultGlowStates();
        }
        #endregion

        #region GLOW Management
        /// <summary>
        /// 대상 선택 모드로 변경 GLOW 상태 업데이트
        /// </summary>
        private void UpdateGlowStatesForTargetSelection()
        {
            UpdateFieldCache();

            foreach (var card in fieldCardsCache)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    card.SetGlowOverride(false);
                }
                else if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                {
                    card.SetGlowOverride(true, Global.GlowRed);
                }
            }
        }

        /// <summary>
        /// 기본 GLOW 상태로 복원
        /// </summary>
        private void RestoreDefaultGlowStates()
        {
            UpdateFieldCache();

            foreach (var card in fieldCardsCache)
            {
                // 공격 가능 상태 등 자동 판정하도록 복원
                card.ClearGlowOverride();
            }
        }

        /// <summary>
        /// 필드 카드 캐시 업데이트 (성능 최적화)
        /// </summary>
        private void UpdateFieldCache()
        {
            fieldCardsCache.Clear();
            fieldCardsCache.AddRange(InGameManager.Instance.GetAllFieldCards());
        }
        #endregion

        #region Validation
        /// <summary>
        /// 공격 처리 가능 상태 확인
        /// </summary>
        private bool CanProcessAttack()
        {
            // 공격 프로세스가 이미 진행 중이고 공격자가 선택된 상태라면 계속 진행 허용
            // (방어자 선택을 위해 필요)
            if (InGameManager.Instance.IsProcessing &&
                InGameManager.Instance.CurrentProcess == GameProcessState.CardAttackProcess &&
                currentAttacker != null)
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 공격 프로세스 진행 중 - 방어자 선택 가능");
                return true; // 공격 프로세스 내에서는 방어자 선택 가능
            }

            // 다른 프로세스 진행 중이면 공격 불가
            if (InGameManager.Instance.IsProcessing) return false;

            // 연산자 모드 중이면 공격 불가
            if (OperatorManager.Instance.IsInOperatorMode) return false;

            // 첫 라운드에서는 공격 불가
            if (TurnManager.Instance.IsFirstRound)
            {
                Debug.Log("[FieldAttackManager] 첫 라운드에서는 공격이 불가능합니다.");
                return false;
            }

            // 턴 체크
            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                Debug.Log("[FieldAttackManager] 내 턴이 아닙니다.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 유효한 공격자인지 확인
        /// </summary>
        private bool IsValidAttacker(Card card)
        {
            return card.CurrentOwnerType == CardZone.OwnerType.Player &&
                   card.CurrentZoneType == CardZone.ZoneType.Field &&
                   card.CanAttack;
        }

        /// <summary>
        /// 유효한 공격 대상인지 확인
        /// </summary>
        private bool IsValidTarget(Card card)
        {
            return card.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                   card.CurrentZoneType == CardZone.ZoneType.Field;
        }

        /// <summary>
        /// 상대 필드가 비어있는지 확인
        /// </summary>
        /// <returns>상대 필드에 카드가 없으면 true</returns>
        private bool IsOpponentFieldEmpty()
        {
            // CardZone.AllZonesRoot에서 상대 필드 Zone 찾기
            if (CardZone.AllZonesRoot == null) return false;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            var opponentFieldZone = zones.FirstOrDefault(z =>
                z.Zone == CardZone.ZoneType.Field &&
                z.Owner == CardZone.OwnerType.Opponent);

            if (opponentFieldZone == null)
            {
                Debug.LogWarning("[FieldAttackManager] 상대 필드 Zone을 찾을 수 없습니다.");
                return false;
            }

            bool isEmpty = opponentFieldZone.GetCardCount() == 0;

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 상대 필드 카드 수: {opponentFieldZone.GetCardCount()}, 비어있음: {isEmpty}");

            return isEmpty;
        }
        #endregion

        #region Utility
        /// <summary>
        /// 단일 카드의 값 반환
        /// </summary>
        /// <param name="card">값을 가져올 카드</param>
        /// <returns>카드의 수치 값</returns>
        private float GetCardValue(Card card)
        {
            return card.GetComponentInChildren<CardText>()?.RawValue ?? 0;
        }

        /// <summary>
        /// 두 카드의 값 반환
        /// </summary>
        private (float attacker, float defender) GetCardValues(Card attacker, Card defender)
        {
            float attackerValue = attacker.GetComponentInChildren<CardText>()?.RawValue ?? 0;
            float defenderValue = defender.GetComponentInChildren<CardText>()?.RawValue ?? 0;
            return (attackerValue, defenderValue);
        }

        /// <summary>
        /// 카드 파괴 (애니메이션 포함)
        /// </summary>
        private void DestroyCard(Card card)
        {
            var zone = card.GetComponentInParent<CardZone>();

            // 애니메이션 완료 후 Zone에서 제거하도록 처리
            StartCoroutine(card.AnimateRemoval(() => {
                zone?.RemoveCard(card.transform);
                Destroy(card.gameObject);
            }));
        }

        /// <summary>
        /// 지연된 카드 파괴
        /// </summary>
        private IEnumerator DelayedDestroy(Card card, float delay)
        {
            yield return new WaitForSeconds(delay);
            DestroyCard(card);
        }

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public void SetDebugMode(bool enable) => enableDebugLog = enable;
        #endregion
    }
}
