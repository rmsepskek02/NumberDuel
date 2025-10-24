using System.Collections;
using UnityEngine;
using Objects;

namespace Manager
{
    /// <summary>
    /// �ʵ� ī�� �� ���� �ý����� �����ϴ� �Ŵ���
    /// ������ ���� �� ������ ���� �� ���� ���� �� ��� ó�� �帧�� ���
    /// Secret ī�� ������ ������ ��Ʈ��ũ ����ȭ ����
    /// </summary>
    public class FieldAttackManager : MonoBehaviour
    {
        [SerializeField] private bool enableDebugLog = false;

        private Card currentAttacker;

        // ���� ����ȭ�� ĳ��
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
        /// ���� �����ڰ� ���õ� �������� Ȯ��
        /// </summary>
        public bool HasAttackerSelected() => currentAttacker != null;

        /// <summary>
        /// ���� ������ ��ȯ
        /// </summary>
        public Card GetCurrentAttacker() => currentAttacker;

        /// <summary>
        /// ���� ���� ���� �ʱ�ȭ (�� ����, �ٸ� ���μ��� ���� �� ��)
        /// </summary>
        public void ForceResetAttackState()
        {
            if (currentAttacker != null)
            {
                ResetAttackState();
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] ���� ���� ���� �ʱ�ȭ");
            }
        }
        #endregion

        #region Attack Flow
        /// <summary>
        /// ī�� Ŭ�� �̺�Ʈ ó��
        /// </summary>
        private void HandleCardClick(Card clickedCard)
        {
            if (!CanProcessAttack()) return;

            // ������ ���� (�� �ʵ� ī��)
            if (IsValidAttacker(clickedCard))
            {
                if (currentAttacker != null) return; // �̹� ������ ���õ�
                SelectAttacker(clickedCard);
            }
            // ���� ���� (��� �ʵ� ī��)
            else if (IsValidTarget(clickedCard))
            {
                if (currentAttacker == null) return; // ������ �̼���
                StartCoroutine(ExecuteAttack(currentAttacker, clickedCard));
            }
        }

        /// <summary>
        /// ������ ���� ó��
        /// </summary>
        private void SelectAttacker(Card attacker)
        {
            currentAttacker = attacker;
            SetupExpressionZone(attacker);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] ������ ����: {attacker.name}");

            // ��� �ʵ尡 ����ִ��� üũ
            if (IsOpponentFieldEmpty())
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] ��� �ʵ尡 ������� - ��� �� �ʵ� ���� ����");

                // ��� �� �ʵ� ���� ����
                StartCoroutine(ExecuteEmptyFieldAttack(attacker));
            }
            else
            {
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] ��� �ʵ忡 ī�� ���� - ��� ���� ���");

                // ���� ����: ��� ���� ���
                UpdateGlowStatesForTargetSelection();
            }
        }

        /// <summary>
        /// �� �ʵ� ���� ���� (Secret ���� ���� ����)
        /// </summary>
        private IEnumerator ExecuteEmptyFieldAttack(Card attacker)
        {
            // ������ ����Ǿ��ٸ� ���� �ߴ�
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] �� �ʵ� ���� ����: {attacker.name}");

            // Secret ����: �����ڰ� Secret ���¶�� ���ÿ��� ����
            bool attackerWasSecret = attacker.IsSecret;
            if (attackerWasSecret)
            {
                attacker.RevealSecret();
                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] ������ Secret ���� ����: {attacker.name}");
            }

            // ���� ���� ǥ��
            attacker.ShowAttackIcon();

            // ���� ������ �� �̸� ���� (Secret ���� ��)
            float originalAttackerValue = GetCardValue(attacker);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] ���� ������ ��: {originalAttackerValue}");

            // ExpressionZone�� ���� ����� ���� (���� ������ "0")
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.SetEmptyFieldDefender(CardZone.OwnerType.Opponent);

            yield return new WaitForSeconds(1.5f);

            // ���� ���� ��üũ
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // ���� ��� ��� �� ǥ��
            ezManager.ShowEmptyFieldResult(originalAttackerValue);

            yield return new WaitForSeconds(1f);

            // ���� ���� ��üũ
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // �� �ʵ� ���� ��� ���� (���� �� ���)
            int damage = ApplyEmptyFieldResult(attacker, originalAttackerValue);

            // ���� �Ϸ� ǥ��
            attacker.SetHasAttackedThisTurn(true);

            // ����: ��ü ���� �׼� ����ȭ (ExpressionZone + HP ����)
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncCombatAction(
                    attacker,
                    null,  // defender�� null (�� �ʵ�)
                    originalAttackerValue,
                    0f,    // ����� �� 0
                    damage // ���� ������
                );
            }

            // ���� ���� ����
            attacker.HideAllIcons();

            // ���� �ʱ�ȭ
            currentAttacker = null;
            yield return new WaitForSeconds(0.8f);

            // ������ ������� �ʾҴٸ� GLOW ���� ����
            if (!InGameManager.Instance.IsGameEnded)
            {
                RestoreDefaultGlowStates();
            }

            if (enableDebugLog)
                Debug.Log("[FieldAttackManager] �� �ʵ� ���� �Ϸ�");
        }

        /// <summary>
        /// ���� ���� �� ��� ó�� (Secret ���� ���� ����)
        /// </summary>
        private IEnumerator ExecuteAttack(Card attacker, Card defender)
        {
            // ������ ����Ǿ��ٸ� ���� �ߴ�
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] ���� ����: {attacker.name} �� {defender.name}");

            // Secret ����: �����ڿ� ����� ��� Secret ���¶�� ���ÿ��� ����
            bool attackerWasSecret = attacker.IsSecret;
            bool defenderWasSecret = defender.IsSecret;

            if (attackerWasSecret)
            {
                attacker.RevealSecret();
                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] ������ Secret ���� ����: {attacker.name}");
            }

            if (defenderWasSecret)
            {
                defender.RevealSecret();
                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] ����� Secret ���� ����: {defender.name}");
            }

            // ���� ���� ǥ��
            attacker.ShowAttackIcon();
            defender.ShowDefenseIcon();

            // ���� ī�� �� �̸� ���� (Secret ���� �� �� ����)
            var (originalAttackerValue, originalDefenderValue) = GetCardValues(attacker, defender);

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] ���� �� ����: ������={originalAttackerValue}, �����={originalDefenderValue}");

            // �������� ������ ����
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.SetDefenderCard(defender);

            yield return new WaitForSeconds(1.5f);

            // ���� ���� ��üũ
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // ���� ��� ��� �� ǥ��
            ezManager.ShowResult(originalAttackerValue, originalDefenderValue);

            yield return new WaitForSeconds(1f);

            // ���� ���� ��üũ
            if (InGameManager.Instance.IsGameEnded)
            {
                yield break;
            }

            // ��� ���� (���� �� ����) - ���� ������ ���
            int damage = ApplyBattleResult(attacker, defender, originalAttackerValue, originalDefenderValue, defenderWasSecret);

            // ���� �Ϸ� ǥ��
            attacker.SetHasAttackedThisTurn(true);

            // ����: ��ü ���� �׼� ����ȭ (ExpressionZone + HP ����)
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SyncCombatAction(
                    attacker,
                    defender,
                    originalAttackerValue,
                    originalDefenderValue,
                    damage // ���� ������
                );
            }

            // ���� ���� ����
            attacker.HideAllIcons();
            defender.HideAllIcons();

            // ���� �ʱ�ȭ
            currentAttacker = null;
            yield return new WaitForSeconds(0.8f);

            // ������ ������� �ʾҴٸ� GLOW ���� ����
            if (!InGameManager.Instance.IsGameEnded)
            {
                RestoreDefaultGlowStates();
            }
        }

        /// <summary>
        /// �� �ʵ� ���� ��� ����
        /// </summary>
        /// <param name="attacker">������ ī��</param>
        /// <param name="damage">��뿡�� ���� ������</param>
        private int ApplyEmptyFieldResult(Card attacker, float damage)
        {
            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] �� �ʵ� ���� ��� ���� - ������: {damage}");

            // DamageCalculator�� ���� ���� ������ ���
            int finalDamage = Utills.DamageCalculator.CalculateEmptyFieldDamage(damage);

            // HealthManager�� ���� ���� ������ ����
            if (HealthManager.Instance != null)
            {
                int actualDamage = HealthManager.Instance.ApplyDamage(finalDamage, CardZone.OwnerType.Opponent);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] ��뿡�� {actualDamage} ������ ���� �Ϸ�");

                return actualDamage; // �߰�: ������ ��ȯ
            }
            else
            {
                Debug.LogError("[FieldAttackManager] HealthManager�� ã�� �� �����ϴ�!");
                return 0;
            }
        }


        /// <summary>
        /// ���� ��� ���� (���� �� ���)
        /// </summary>
        private int ApplyBattleResult(Card attacker, Card defender, float originalAttackerValue, float originalDefenderValue, bool defenderWasSecret = false)
        {
            var attackerText = attacker.GetComponentInChildren<CardText>();
            var defenderText = defender.GetComponentInChildren<CardText>();

            float result = originalAttackerValue - originalDefenderValue;
            int actualDamage = 0; // �߰�: ������ ����

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] ���� ��� ���: {originalAttackerValue} - {originalDefenderValue} = {result}");

            if (result > 0) // ������ �¸�
            {
                // ���� ������ ��� (ī�� ���� �� ���� �� ���)
                // ������: ��ũ�� ī�带 ���ݿ��� �̰��� ������ ���� ����
                if (HealthManager.Instance != null && !defenderWasSecret)
                {
                    int damage = Utills.DamageCalculator.CalculateAttackDamage(originalAttackerValue, originalDefenderValue);
                    actualDamage = HealthManager.Instance.ApplyDamage(damage, CardZone.OwnerType.Opponent);

                    if (enableDebugLog)
                        Debug.Log($"[FieldAttackManager] ���� ���� - ��뿡�� {actualDamage} ������");
                }
                else if (defenderWasSecret && enableDebugLog)
                {
                    Debug.Log($"[FieldAttackManager] ��ũ�� ī�� ���� - ������ ����");
                }

                // ī�� ���� ó�� (������ ���� ��)
                attackerText.SetRawValue(result);
                DestroyCard(defender);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] {attacker.name} �¸� (�� ��: {result})");
            }
            else if (result < 0) // ������ �¸�
            {
                // ������ �¸� �ÿ��� �÷��̾�� ������ ���� (���� �䱸����)
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] ������ �¸� - �÷��̾� ������ ����");

                // ī�� ���� ó��
                defenderText.SetRawValue(Mathf.Abs(result));
                DestroyCard(attacker);

                if (enableDebugLog)
                    Debug.Log($"[FieldAttackManager] {defender.name} �¸� (�� ��: {Mathf.Abs(result)})");
            }
            else // ���º�
            {
                // ���º� �ÿ��� ������ ����
                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] ���º� - ������ ����");

                // ī�� ���� ó��
                DestroyCard(attacker);
                StartCoroutine(DelayedDestroy(defender, 0.2f));

                if (enableDebugLog)
                    Debug.Log("[FieldAttackManager] ��ȣ �ı�");
            }

            return actualDamage; // �߰�: ������ ��ȯ
        }
        #endregion

        #region Expression Zone Management
        /// <summary>
        /// ���ݿ� ������ ����
        /// </summary>
        private void SetupExpressionZone(Card attacker)
        {
            var ezManager = ExpressionZoneManager.Instance;
            ezManager.InitForAttack();
            ezManager.SetAttackerCard(attacker);
            ezManager.StartAttackMode();
        }

        /// <summary>
        /// ���� ���� �ʱ�ȭ
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
        /// ���� ��� ������ ���� GLOW ���� ����
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
        /// �⺻ GLOW ���·� ����
        /// </summary>
        private void RestoreDefaultGlowStates()
        {
            UpdateFieldCache();

            foreach (var card in fieldCardsCache)
            {
                // ���� ���� ���� �� �ڵ� ����ϵ��� ����
                card.ClearGlowOverride();
            }
        }

        /// <summary>
        /// �ʵ� ī�� ĳ�� ������Ʈ (���� ����ȭ)
        /// </summary>
        private void UpdateFieldCache()
        {
            fieldCardsCache.Clear();
            fieldCardsCache.AddRange(InGameManager.Instance.GetAllFieldCards());
        }
        #endregion

        #region Validation
        /// <summary>
        /// ���� ó�� ���� ���� Ȯ��
        /// </summary>
        private bool CanProcessAttack()
        {
            // �ٸ� ���μ��� ���� ���̸� ���� ����
            if (InGameManager.Instance.IsProcessing) return false;

            // ������ ��� ���̸� ���� ����
            if (OperatorManager.Instance.IsInOperatorMode) return false;

            // ù ���忡���� ���� ����
            if (TurnManager.Instance.IsFirstRound)
            {
                Debug.Log("[FieldAttackManager] ù ���忡���� ������ �Ұ����մϴ�.");
                return false;
            }

            // �� ����
            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                Debug.Log("[FieldAttackManager] �� ���� �ƴմϴ�.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ��ȿ�� ���������� Ȯ��
        /// </summary>
        private bool IsValidAttacker(Card card)
        {
            return card.CurrentOwnerType == CardZone.OwnerType.Player &&
                   card.CurrentZoneType == CardZone.ZoneType.Field &&
                   card.CanAttack;
        }

        /// <summary>
        /// ��ȿ�� ���� ������� Ȯ��
        /// </summary>
        private bool IsValidTarget(Card card)
        {
            return card.CurrentOwnerType == CardZone.OwnerType.Opponent &&
                   card.CurrentZoneType == CardZone.ZoneType.Field;
        }

        /// <summary>
        /// ��� �ʵ尡 ����ִ��� Ȯ��
        /// </summary>
        /// <returns>��� �ʵ忡 ī�尡 ������ true</returns>
        private bool IsOpponentFieldEmpty()
        {
            // CardZone.AllZonesRoot���� ��� �ʵ� Zone ã��
            if (CardZone.AllZonesRoot == null) return false;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            var opponentFieldZone = System.Linq.Enumerable.FirstOrDefault(zones, z =>
                z.Zone == CardZone.ZoneType.Field &&
                z.Owner == CardZone.OwnerType.Opponent);

            if (opponentFieldZone == null)
            {
                Debug.LogWarning("[FieldAttackManager] ��� �ʵ� Zone�� ã�� �� �����ϴ�.");
                return false;
            }

            bool isEmpty = opponentFieldZone.GetCardCount() == 0;

            if (enableDebugLog)
                Debug.Log($"[FieldAttackManager] ��� �ʵ� ī�� ��: {opponentFieldZone.GetCardCount()}, �������: {isEmpty}");

            return isEmpty;
        }
        #endregion

        #region Utility
        /// <summary>
        /// ���� ī���� �� ��ȯ
        /// </summary>
        /// <param name="card">���� ������ ī��</param>
        /// <returns>ī���� ���� ��</returns>
        private float GetCardValue(Card card)
        {
            return card.GetComponentInChildren<CardText>()?.RawValue ?? 0;
        }

        /// <summary>
        /// �� ī���� �� ��ȯ
        /// </summary>
        private (float attacker, float defender) GetCardValues(Card attacker, Card defender)
        {
            float attackerValue = attacker.GetComponentInChildren<CardText>()?.RawValue ?? 0;
            float defenderValue = defender.GetComponentInChildren<CardText>()?.RawValue ?? 0;
            return (attackerValue, defenderValue);
        }

        /// <summary>
        /// ī�� �ı� (�ִϸ��̼� ����)
        /// </summary>
        private void DestroyCard(Card card)
        {
            var zone = card.GetComponentInParent<CardZone>();

            // �ִϸ��̼� �Ϸ� �� Zone���� �����ϵ��� ����
            StartCoroutine(card.AnimateRemoval(() => {
                zone?.RemoveCard(card.transform);
                Destroy(card.gameObject);
            }));
        }

        /// <summary>
        /// ������ ī�� �ı�
        /// </summary>
        private IEnumerator DelayedDestroy(Card card, float delay)
        {
            yield return new WaitForSeconds(delay);
            DestroyCard(card);
        }

        /// <summary>
        /// ����� �α� Ȱ��ȭ/��Ȱ��ȭ
        /// </summary>
        public void SetDebugMode(bool enable) => enableDebugLog = enable;
        #endregion
    }
}