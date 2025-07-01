using System.Collections;
using UnityEngine;
using Objects;

namespace Manager
{
    /// <summary>
    /// 필드 카드 간 공격 시스템을 관리하는 매니저
    /// 공격자 선택 → 수비자 선택 → 공격 실행 → 결과 처리 흐름을 담당
    /// </summary>
    public class FieldAttackManager : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLog = false;

        private Card currentAttacker;

        // 성능 최적화용 캐시
        private readonly System.Collections.Generic.List<Card> fieldCardsCache = new System.Collections.Generic.List<Card>();

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
        /// 공격 상태 강제 초기화 (턴 종료, 다른 프로세스 시작 시 등)
        /// </summary>
        public void ForceResetAttackState()
        {
            if (currentAttacker != null)
            {
                ResetAttackState();
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 공격 상태 강제 초기화");
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
            // 공격 실행 (상대 필드 카드)
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
            currentAttacker = attacker;
            SetupExpressionZone(attacker);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격자 선택: {attacker.name}");

            // 상대 필드가 비어있는지 체크
            if (IsOpponentFieldEmpty())
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 상대 필드가 비어있음 - 즉시 빈 필드 공격 실행");

                // 즉시 빈 필드 공격 실행
                StartCoroutine(ExecuteEmptyFieldAttack(attacker));
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 상대 필드에 카드 존재 - 대상 선택 대기");

                // 기존 로직: 대상 선택 대기
                UpdateGlowStatesForTargetSelection();
            }
        }

        /// <summary>
        /// 빈 필드 공격 실행 (원본 값 사용하도록 수정)
        /// </summary>
        private IEnumerator ExecuteEmptyFieldAttack(Card attacker)
        {
            // 게임이 종료되었다면 공격 중단
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 빈 필드 공격 시작: {attacker.name}");

            // 원본 공격자 값 미리 저장
            float originalAttackerValue = GetCardValue(attacker);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 원본 공격자 값: {originalAttackerValue}");

            // ExpressionZone에 가상 방어자 설정 (상대방 색상의 "0")
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

            // 빈 필드 공격 결과 적용 (원본 값 사용)
            ApplyEmptyFieldResult(attacker, originalAttackerValue);

            // 상태 초기화
            currentAttacker = null;
            yield return new WaitForSeconds(0.8f);

            // 게임이 종료되지 않았다면 GLOW 상태 복원
            if (!InGameManager.Instance.IsGameEnded)
            {
                RestoreDefaultGlowStates();
            }

            if (enableDebugLog)
                Debug.Log("[FieldAttackManager] 빈 필드 공격 완료");
        }

        /// <summary>
        /// 빈 필드 공격 결과 적용 (수정된 버전)
        /// </summary>
        /// <param name="attacker">공격한 카드</param>
        /// <param name="damage">상대에게 가할 데미지</param>
        private void ApplyEmptyFieldResult(Card attacker, float damage)
        {
            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 빈 필드 공격 결과 적용 - 데미지: {damage}");

            // DamageCalculator를 통해 최종 데미지 계산
            int finalDamage = Utills.DamageCalculator.CalculateEmptyFieldDamage(damage);

            // HealthManager를 통해 실제 데미지 적용
            if (HealthManager.Instance != null)
            {
                int actualDamage = HealthManager.Instance.ApplyDamage(finalDamage, CardZone.OwnerType.Opponent);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 상대에게 {actualDamage} 데미지 적용 완료");
            }
            else
            {
                Debug.LogError("[FieldAttackManager] HealthManager를 찾을 수 없습니다!");
            }

            // 공격자 카드는 수정됨 표시 (재공격 방지)
            attacker.SetWasModifiedThisTurn(true);
        }

        /// <summary>
        /// 단일 카드의 값 반환
        /// </summary>
        /// <param name="card">값을 가져올 카드</param>
        /// <returns>카드의 숫자 값</returns>
        private float GetCardValue(Card card)
        {
            return card.GetComponentInChildren<CardText>()?.RawValue ?? 0;
        }

        /// <summary>
        /// 공격 실행 및 결과 처리 (수정된 버전)
        /// </summary>
        private IEnumerator ExecuteAttack(Card attacker, Card defender)
        {
            // 게임이 종료되었다면 공격 중단
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격 실행: {attacker.name} → {defender.name}");

            // 원본 카드 값 미리 저장 (카드 변경 전)
            var (originalAttackerValue, originalDefenderValue) = GetCardValues(attacker, defender);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 원본 값 저장: 공격자={originalAttackerValue}, 방어자={originalDefenderValue}");

            // 수식존에 수비자 설정
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

            // 결과 적용 (원본 값 전달)
            ApplyBattleResult(attacker, defender, originalAttackerValue, originalDefenderValue);

            // 상태 초기화
            currentAttacker = null;
            yield return new WaitForSeconds(0.8f);

            // 게임이 종료되지 않았다면 GLOW 상태 복원
            if (!InGameManager.Instance.IsGameEnded)
            {
                RestoreDefaultGlowStates();
            }
        }

        /// <summary>
        /// 전투 결과 적용 (수정된 버전 - 원본 값 사용)
        /// </summary>
        private void ApplyBattleResult(Card attacker, Card defender, float originalAttackerValue, float originalDefenderValue)
        {
            var attackerText = attacker.GetComponentInChildren<CardText>();
            var defenderText = defender.GetComponentInChildren<CardText>();

            float result = originalAttackerValue - originalDefenderValue;

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 전투 결과 계산: {originalAttackerValue} - {originalDefenderValue} = {result}");

            if (result > 0) // 공격자 승리
            {
                // 실제 데미지 계산 (카드 변경 전 원본 값 사용)
                if (HealthManager.Instance != null)
                {
                    int damage = Utills.DamageCalculator.CalculateAttackDamage(originalAttackerValue, originalDefenderValue);
                    int actualDamage = HealthManager.Instance.ApplyDamage(damage, CardZone.OwnerType.Opponent);

                    if (enableDebugLog)
                        Debug.Log($"[FieldAttackManager] 공격 성공 - 상대에게 {actualDamage} 데미지");
                }

                // 카드 전투 처리 (데미지 적용 후)
                attackerText.SetRawValue(result);
                attacker.SetWasModifiedThisTurn(true);
                DestroyCard(defender);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] {attacker.name} 승리 (새 값: {result})");
            }
            else if (result < 0) // 수비자 승리
            {
                // 수비자 승리 시에는 플레이어에게 데미지 없음 (설계 요구사항)
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 수비자 승리 - 플레이어 데미지 없음");

                // 카드 전투 처리
                defenderText.SetRawValue(Mathf.Abs(result));
                DestroyCard(attacker);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] {defender.name} 승리 (새 값: {Mathf.Abs(result)})");
            }
            else // 무승부
            {
                // 무승부 시에도 데미지 없음
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 무승부 - 데미지 없음");

                // 카드 전투 처리
                DestroyCard(attacker);
                StartCoroutine(DelayedDestroy(defender, 0.2f));

                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] 상호 파괴");
            }
        }
        
        #endregion

        #region Expression Zone Management
        /// <summary>
        /// 공격용 수식존 설정
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
        /// 공격 대상 선택을 위한 GLOW 상태 설정
        /// </summary>
        private void UpdateGlowStatesForTargetSelection()
        {
            UpdateFieldCache();

            foreach (var card in fieldCardsCache)
            {
                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    // 내 필드카드는 GLOW 끄기
                    card.SetCardState(false);
                }
                else if (card.CurrentOwnerType == CardZone.OwnerType.Opponent)
                {
                    // 상대 필드카드는 빨간색 GLOW
                    card.SetCardState(true, Global.GlowRed);
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
                if (card.CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    // 내 카드: 공격 가능하면 초록색 GLOW
                    bool canAttack = card.IsAttackableThisTurn();
                    card.SetCardState(canAttack, canAttack ? Global.GlowGreen : null);
                }
                else
                {
                    // 상대 카드: GLOW 끄기
                    card.SetCardState(false);
                }
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
        /// 공격 처리 가능 여부 확인
        /// </summary>
        private bool CanProcessAttack()
        {
            // 다른 프로세스 진행 중이면 공격 차단
            if (InGameManager.Instance.IsProcessing) return false;

            // 연산자 모드 중이면 공격 차단
            if (OperatorManager.Instance.IsInOperatorMode) return false;

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
        #endregion

        /// <summary>
        /// 상대 필드가 비어있는지 확인
        /// </summary>
        /// <returns>상대 필드에 카드가 없으면 true</returns>
        private bool IsOpponentFieldEmpty()
        {
            // CardZone.AllZonesRoot에서 상대 필드 Zone 찾기
            if (CardZone.AllZonesRoot == null) return false;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            var opponentFieldZone = System.Linq.Enumerable.FirstOrDefault(zones, z =>
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

        #region Utility
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

            // 애니메이션 완료 후 Zone에서 제거하도록 수정
            StartCoroutine(card.AnimateRemoval(() => {
                zone?.RemoveCard(card.transform); // 애니메이션 후 정렬 발생
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