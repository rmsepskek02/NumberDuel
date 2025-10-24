using UnityEngine;
using Objects;
using System.Linq;
using Expression;
using Utills;

namespace Manager
{
    /// <summary>
    /// ExpressionZone�� 5�� ������ �����Ͽ� ������ �ð������� ǥ���ϴ� �Ŵ���
    /// ���� ����: [0:��ī��] [1:������] [2:���ī��] [3:��ȣ] [4:���]
    /// </summary>
    public class ExpressionZoneManager : Singleton<ExpressionZoneManager>
    {
        [Header("Expression Zone ����")]
        [SerializeField] private CardZone expressionZone;
        [SerializeField] private bool enableDebugLog = false;

        private ExpressionCard[] slots;
        private Sprite neutralSprite;

        // ���� ����ȭ�� ĳ��
        private readonly Color[] cachedColors = new Color[5];
        private readonly bool[] cachedActiveStates = new bool[5];

        #region Unity Lifecycle
        private void Start()
        {
            InitializeSlots();
            SubscribeEvents();
            ResetAllSlots();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Initialization
        private void InitializeSlots()
        {
            if (expressionZone == null)
                expressionZone = GetComponentInParent<CardZone>();

            slots = GetComponentsInChildren<ExpressionCard>(includeInactive: true)
                .OrderBy(card => card.name)
                .ToArray();

            foreach (var slot in slots)
                expressionZone.AddCard(slot.transform);

            expressionZone.UpdateLayout();

            CacheNeutralSprite();

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] �ʱ�ȭ �Ϸ� - ���� ��: {slots.Length}");
        }

        private void CacheNeutralSprite()
        {
            neutralSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack) ??
                           ResourcesManager.Instance.GetSprite(Global.Card, "color_back");
        }

        private void SubscribeEvents()
        {
            ExpressionCard.onClicked -= HandleSlotClicked;
            ExpressionCard.onClicked += HandleSlotClicked;
        }

        private void UnsubscribeEvents()
        {
            ExpressionCard.onClicked -= HandleSlotClicked;
        }
        #endregion

        #region Core Slot Management
        /// <summary>
        /// ���� ������Ʈ (��������� ���� ���� ���� ������Ʈ)
        /// </summary>
        private void UpdateSlot(int index, string text, Sprite sprite, bool showText, bool canCancel = false)
        {
            if (!IsValidSlotIndex(index) || index == 3) return; // 3�� ���� ��ȣ

            var slot = slots[index];
            var targetSprite = sprite ?? neutralSprite;
            var targetColor = Global.GetColorByName(targetSprite.name);

            // ������� Ȯ�� �� �ʿ�ÿ��� ������Ʈ
            bool needsUpdate = false;

            if (slot.GetComponentInChildren<SpriteRenderer>().sprite != targetSprite)
            {
                slot.SetSprite(targetSprite);
                needsUpdate = true;
            }

            if (slot.CurrentText != text || cachedActiveStates[index] != showText)
            {
                slot.SetValue(text);
                slot.SetTextVisible(showText);
                cachedActiveStates[index] = showText;
                needsUpdate = true;
            }

            if (cachedColors[index] != targetColor)
            {
                slot.SetTextColor(targetColor);
                cachedColors[index] = targetColor;
                needsUpdate = true;
            }

            // ��� ���� ���� ����
            if (canCancel) slot.SetCancelable(true);
            else slot.ClearGlow();

            if (enableDebugLog && needsUpdate)
                Debug.Log($"[ExpressionZoneManager] ���� {index} ������Ʈ: '{text}'");
        }

        /// <summary>
        /// 3�� ���� (��ȣ) ���� ����
        /// </summary>
        private void FixEqualSlot()
        {
            const int equalIndex = 3;
            var slot = slots[equalIndex];

            // �̹� �ùٸ� �������� Ȯ��
            if (slot.CurrentText == "=" && slot.GetComponentInChildren<SpriteRenderer>().sprite == neutralSprite)
                return;

            slot.ClearGlow();
            slot.SetSprite(neutralSprite);
            slot.SetValue("=");
            slot.SetTextVisible(true);
            slot.SetTextColor(Color.white);

            cachedColors[equalIndex] = Color.white;
            cachedActiveStates[equalIndex] = true;
        }

        /// <summary>
        /// ��ȿ�� ���� �ε������� Ȯ��
        /// </summary>
        private bool IsValidSlotIndex(int index) => index >= 0 && index < slots.Length;
        #endregion

        #region Public Interface
        /// <summary>
        /// ������ ī�带 0�� ���Կ� ����
        /// </summary>
        public void SetAttackerCard(Card card)
        {
            SetCardToSlot(card, 0, "SetAttackerCard");
        }

        /// <summary>
        /// ������ ī�带 2�� ���Կ� ����
        /// </summary>
        public void SetDefenderCard(Card card)
        {
            SetCardToSlot(card, 2, "SetDefenderCard");
        }

        /// <summary>
        /// ù ��° ���� ����� 0�� ���Կ� ����
        /// </summary>
        public void SetFirstOperand(Card card)
        {
            SetCardToSlot(card, 0, "SetFirstOperand");
        }

        /// <summary>
        /// �� ��° ���� ����� 2�� ���Կ� ����
        /// </summary>
        public void SetSecondOperand(Card card)
        {
            SetCardToSlot(card, 2, "SetSecondOperand");
        }

        /// <summary>
        /// �����ڸ� 1�� ���Կ� ����
        /// </summary>
        public void SetOperator(Card operatorCard)
        {
            if (operatorCard?.CardType != CardType.Operator)
            {
                Debug.LogError("[ExpressionZoneManager] ��ȿ���� ���� ������ ī��");
                return;
            }

            string symbol = GetOperatorSymbol(operatorCard.OperatorType);

            // �ƴ��� ���ܽ�Ű�� ���� ��������Ʈ ã��
            Sprite sprite = null;
            var allRenderers = operatorCard.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var renderer in allRenderers)
            {
                if (!renderer.gameObject.name.Contains("Icon"))
                {
                    sprite = renderer.sprite;
                    break;
                }
            }

            UpdateSlot(1, symbol, sprite, true);
        }

        /// <summary>
        /// ���ݿ� ���� ��ȣ�� 1�� ���Կ� ����
        /// </summary>
        public void SetAttackOperator()
        {
            UpdateSlot(1, "-", neutralSprite, true);
        }

        /// <summary>
        /// ���� ����� 4�� ���Կ� ǥ��
        /// </summary>
        public void ShowResult(float a, float b, OperatorType? operatorType = null)
        {
            float result = CalculateResult(a, b, operatorType);
            string resultText = FormatResultText(result, operatorType);
            Sprite resultSprite = GetResultSprite(result, operatorType);

            UpdateSlot(4, resultText, resultSprite, true);

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] ��� ǥ��: {a} {GetOperatorSymbol(operatorType)} {b} = {result}");
        }
        #endregion

        #region Process Control
        /// <summary>
        /// ���� ���μ����� �ʱ�ȭ
        /// </summary>
        public void InitForAttack()
        {
            ClearAllCancelable();
            UpdateSlot(1, "-", neutralSprite, true);
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// ���� ���μ����� �ʱ�ȭ
        /// </summary>
        public void InitForOperation()
        {
            ClearAllCancelable();
            UpdateSlot(0, "", neutralSprite, false);
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// ��� ���� �ʱ�ȭ
        /// </summary>
        public void ResetAllSlots()
        {
            ClearAllCancelable();
            UpdateSlot(0, "", neutralSprite, false);
            UpdateSlot(1, "-", neutralSprite, true);
            UpdateSlot(2, "", neutralSprite, false);
            UpdateSlot(4, "", neutralSprite, false);
            FixEqualSlot();
        }

        /// <summary>
        /// ù ��° ���ø� �ʱ�ȭ (���� �� �缱�ÿ�)
        /// </summary>
        public void ResetFirstSelection()
        {
            UpdateSlot(0, "", neutralSprite, false);
        }
        #endregion

        #region Cancellation System
        /// <summary>
        /// ������ ���Ե��� ��� �����ϰ� ����
        /// </summary>
        public void EnableCancellation(params int[] slotIndices)
        {
            ClearAllCancelable();

            foreach (int index in slotIndices)
            {
                if (IsValidSlotIndex(index) && slots[index].IsActive)
                {
                    slots[index].SetCancelable(true);
                }
            }
        }

        /// <summary>
        /// ��� ������ ��� ��� ��Ȱ��ȭ
        /// </summary>
        public void ClearAllCancelable()
        {
            foreach (var slot in slots)
                slot.ClearGlow();
        }

        /// <summary>
        /// ���� Ŭ�� �̺�Ʈ ó��
        /// </summary>
        private void HandleSlotClicked(ExpressionCard clickedSlot)
        {
            int slotIndex = clickedSlot.SlotIndex;

            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool isAttacking = attackManager?.HasAttackerSelected() ?? false;

            if (isAttacking)
                HandleAttackCancellation(slotIndex);
            else if (InGameManager.Instance.CurrentProcess == GameProcessState.OperatorCalculation)
                HandleOperationCancellation(slotIndex);
        }

        /// <summary>
        /// ���� ��� ó��
        /// </summary>
        private void HandleAttackCancellation(int slotIndex)
        {
            if (slotIndex == 0) // ������ ���� Ŭ���� ���� ���
            {
                var attackManager = FindAnyObjectByType<FieldAttackManager>();
                attackManager?.ForceResetAttackState();
                ResetAllSlots();
            }
        }

        /// <summary>
        /// ���� ��� ó��
        /// </summary>
        private void HandleOperationCancellation(int slotIndex)
        {
            var operatorManager = OperatorManager.Instance;
            if (operatorManager?.IsInOperatorMode != true) return;

            switch (slotIndex)
            {
                case 0: // ù ��° ���� �缳��
                    operatorManager.ResetFirstCardSelection();
                    EnableCancellation(1);
                    break;
                case 1: // ���� ���� ���
                    operatorManager.CancelOperatorMode();
                    break;
            }
        }
        #endregion

        #region State Management for External Managers
        public void StartAttackMode() => EnableCancellation(0);
        public void StartOperationMode() => EnableCancellation(1);
        public void UpdateOperationFirstSelected() => EnableCancellation(0, 1);
        #endregion

        /// <summary>
        /// �� �ʵ� ���ݿ� - slot 2���� ���� ������ "0" ǥ��
        /// ���� ī�尡 �ƴ� ������ ���� �ð������� ǥ��
        /// </summary>
        /// <param name="opponentType">���� Ÿ�� (Player �Ǵ� Opponent)</param>
        public void SetEmptyFieldDefender(CardZone.OwnerType opponentType)
        {
            // ���� ���� ��������Ʈ ��������
            Sprite opponentSprite = GetSpriteByOwnerType(opponentType);

            if (opponentSprite == null)
            {
                Debug.LogWarning("[ExpressionZoneManager] ���� ��������Ʈ�� ã�� �� �����ϴ�. �⺻ ��������Ʈ ���");
                opponentSprite = neutralSprite;
            }

            // slot 2���� "0" �ؽ�Ʈ�� ���� �������� ����
            UpdateSlot(2, "0", opponentSprite, true);

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] �� �ʵ� ����� ����: {opponentType} �������� '0' ǥ��");
        }

        /// <summary>
        /// ������ Ÿ�Կ� ���� ��������Ʈ ��ȯ
        /// </summary>
        /// <param name="ownerType">ī�� ������ Ÿ��</param>
        /// <returns>�ش� �������� ��������Ʈ</returns>
        private Sprite GetSpriteByOwnerType(CardZone.OwnerType ownerType)
        {
            return ownerType switch
            {
                CardZone.OwnerType.Player => ResourcesManager.Instance.GetPlayerSprite(),
                CardZone.OwnerType.Opponent => ResourcesManager.Instance.GetOpponentSprite(),
                _ => neutralSprite
            };
        }

        /// <summary>
        /// �� �ʵ� ���� ��� ǥ�� (������ ���� �״�� �������� ��)
        /// </summary>
        /// <param name="attackerValue">������ ī�� ��</param>
        public void ShowEmptyFieldResult(float attackerValue)
        {
            // �� �ʵ� ����: ������ �� - 0 = ������ ��
            float result = attackerValue;
            string resultText = Mathf.FloorToInt(result).ToString();

            // ����� ������ �������� ǥ�� (����̹Ƿ�)
            Sprite resultSprite = slots[0].GetComponentInChildren<SpriteRenderer>().sprite;

            UpdateSlot(4, resultText, resultSprite, true);

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] �� �ʵ� ���� ���: {attackerValue} - 0 = {result}");
        }

        #region Utility Methods
        /// <summary>
        /// ī�� ������ ���Կ� �����ϴ� ���� �޼ҵ�
        /// </summary>
        private void SetCardToSlot(Card card, int slotIndex, string methodName)
        {
            var cardText = card?.GetComponentInChildren<CardText>();

            // �ƴ��� ���ܽ�Ű�� ���� ī�� ��������Ʈ���� SpriteRenderer ã��
            SpriteRenderer spriteRenderer = null;
            if (card != null)
            {
                var allRenderers = card.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var renderer in allRenderers)
                {
                    // "Icon"�� ���Ե� GameObject�� ����
                    if (!renderer.gameObject.name.Contains("Icon"))
                    {
                        spriteRenderer = renderer;
                        break;
                    }
                }
            }

            if (cardText?.TextValue == null || spriteRenderer?.sprite == null)
            {
                Debug.LogError($"[ExpressionZoneManager] {methodName} - ī�� �����Ͱ� ��ȿ���� ����");
                return;
            }

            UpdateSlot(slotIndex, cardText.TextValue, spriteRenderer.sprite, true);
        }

        /// <summary>
        /// ���� ��� ���
        /// </summary>
        private float CalculateResult(float a, float b, OperatorType? operatorType)
        {
            return operatorType switch
            {
                OperatorType.Plus => a + b,
                OperatorType.Minus => a - b,
                OperatorType.Multiply => a * b,
                OperatorType.Divide => b != 0 ? Mathf.Floor(a / b) : 0, // �� ��ȯ
                _ => a - b // ���� (�⺻ ����)
            };
        }

        /// <summary>
        /// ��� �ؽ�Ʈ ������
        /// </summary>
        private string FormatResultText(float result, OperatorType? operatorType)
        {
            return operatorType switch
            {
                null => Mathf.Abs(Mathf.FloorToInt(result)).ToString(), // ������ ������ ���� �κ�
                OperatorType.Minus => Mathf.FloorToInt(result).ToString(), // ����� ���� �κ� (���� ����)
                _ => Mathf.FloorToInt(result).ToString() // �������� ���� �κ�
            };
        }

        /// <summary>
        /// ����� �´� ��������Ʈ ��ȯ
        /// </summary>
        private Sprite GetResultSprite(float result, OperatorType? operatorType)
        {
            if (result == 0) return neutralSprite;

            if (operatorType == null) // ���� ���μ���
            {
                return result > 0
                    ? slots[0].GetComponentInChildren<SpriteRenderer>().sprite
                    : slots[2].GetComponentInChildren<SpriteRenderer>().sprite;
            }

            return slots[0].GetComponentInChildren<SpriteRenderer>().sprite;
        }

        /// <summary>
        /// ������ Ÿ���� ��ȣ�� ��ȯ
        /// </summary>
        private string GetOperatorSymbol(OperatorType? operatorType)
        {
            return operatorType switch
            {
                OperatorType.Plus => "+",
                OperatorType.Minus => "-",
                OperatorType.Multiply => "��",
                OperatorType.Divide => "��",
                _ => "?"
            };
        }

        /// <summary>
        /// ����� �α� Ȱ��ȭ/��Ȱ��ȭ
        /// </summary>
        public void SetDebugMode(bool enable) => enableDebugLog = enable;
        #endregion

        /// <summary>
        /// ������ Ÿ������ ���� ���� (��Ʈ��ũ ����ȭ��)
        /// ������ ī�� ��ü ���� Ÿ�Ը����� ������ ��ȣ�� ǥ��
        /// </summary>
        /// <param name="operatorType">ǥ���� ������ Ÿ��</param>
        public void SetOperatorByType(OperatorType operatorType)
        {
            string symbol = GetOperatorSymbol(operatorType);
            UpdateSlot(1, symbol, neutralSprite, true);

            if (enableDebugLog)
                Debug.Log($"[ExpressionZoneManager] ������ Ÿ�� ����: {operatorType} ({symbol})");
        }
    }
}