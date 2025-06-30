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
            UpdateGlowStatesForTargetSelection();

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격자 선택: {attacker.name}");
        }

        /// <summary>
        /// 공격 실행 및 결과 처리
        /// </summary>
        private IEnumerator ExecuteAttack(Card attacker, Card defender)
        {
            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] 공격 실행: {attacker.name} → {defender.name}");

            // 수식존에 수비자 설정
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.SetDefenderCard(defender);

            yield return new WaitForSeconds(1.5f);

            // 공격 결과 계산 및 표시
            var (attackerValue, defenderValue) = GetCardValues(attacker, defender);
            ezManager.ShowResult(attackerValue, defenderValue);

            yield return new WaitForSeconds(1f);

            // 결과 적용
            ApplyBattleResult(attacker, defender, attackerValue - defenderValue);

            // 상태 초기화
            currentAttacker = null;
            yield return new WaitForSeconds(0.8f);

            RestoreDefaultGlowStates();
        }

        /// <summary>
        /// 전투 결과 적용
        /// </summary>
        private void ApplyBattleResult(Card attacker, Card defender, float result)
        {
            var attackerText = attacker.GetComponentInChildren<CardText>();
            var defenderText = defender.GetComponentInChildren<CardText>();

            if (result > 0) // 공격자 승리
            {
                attackerText.SetRawValue(result);
                attacker.SetWasModifiedThisTurn(true);
                DestroyCard(defender);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 공격 성공 - {attacker.name} 생존 (값: {result})");
            }
            else if (result < 0) // 수비자 승리
            {
                defenderText.SetRawValue(Mathf.Abs(result));
                DestroyCard(attacker);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] 공격 실패 - {defender.name} 생존 (값: {Mathf.Abs(result)})");
            }
            else // 무승부
            {
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
            zone?.RemoveCard(card.transform);
            StartCoroutine(card.AnimateRemoval(() => Destroy(card.gameObject)));
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