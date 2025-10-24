using DG.Tweening;
using Manager;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// ���� ī�� ������Ʈ�� ���� �� Ŭ�� ������ �����ϴ� ������Ʈ
    /// Secret ī�� �ð��� ȿ���� CardEffect�� �����Ͽ� Material Property Block �浹 ����
    /// </summary>
    public class Card : MonoBehaviourPun, ICard
    {
        private TextMeshPro cardTMP;
        private CardText cardText;
        private SpriteRenderer spriteRenderer;

        // Combat Icon References
        [SerializeField] private GameObject swordIcon;
        [SerializeField] private GameObject shieldIcon;

        public static event Action<Card> onClicked;
        public static event Action<Transform> OnCardDropped;

        public CardZone.ZoneType CurrentZoneType { get; private set; }
        public CardZone.OwnerType CurrentOwnerType { get; private set; }
        public CardType CardType { get; private set; } = CardType.Number;
        public OperatorType OperatorType { get; private set; }
        public bool IsSecret { get; private set; }
        public bool CanAttack { get; private set; } = false;

        // �Ϻ� ���� ����
        public bool WasModifiedThisTurn { get; private set; } = false;
        public bool WasPlayedThisTurn { get; private set; } = false;
        public bool HasAttackedThisTurn { get; private set; } = false;

        // GLOW ���� ����
        private bool isGlowOverridden = false;
        private bool overrideGlowState = false;
        private Color? overrideGlowColor = null;

        public bool IsOpen => !IsSecret;

        private ObjectMouseEvent mouseEvent;

        #region Unity Lifecycle
        private void Awake()
        {
            mouseEvent = GetComponentInChildren<ObjectMouseEvent>();
            cardTMP = GetComponentInChildren<TextMeshPro>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (cardText == null)
                cardText = GetComponentInChildren<CardText>();
        }

        private void OnEnable()
        {
            RegisterEvents();
        }

        private void OnDisable()
        {
            UnregisterEvents();
        }

        private void RegisterEvents()
        {
            if (mouseEvent == null)
                return;

            mouseEvent.OnClickReleased += HandleClick;
            mouseEvent.OnEndDrag += HandleEndDrag;
        }

        private void UnregisterEvents()
        {
            if (mouseEvent == null)
                return;

            mouseEvent.OnClickReleased -= HandleClick;
            mouseEvent.OnEndDrag -= HandleEndDrag;
        }
        #endregion

        #region Card Initialization
        /// <summary>
        /// ���� ī��� �ʱ�ȭ
        /// </summary>
        /// <param name="value">ī�� ��</param>
        public void InitializeAsNumber(float value)
        {
            CardType = CardType.Number;
            cardText.SetRawValue(value);
        }

        /// <summary>
        /// ������ ī��� �ʱ�ȭ
        /// </summary>
        /// <param name="opType">������ Ÿ��</param>
        public void InitializeAsOperator(OperatorType opType)
        {
            CardType = CardType.Operator;
            OperatorType = opType;
            cardText.SetOperatorText(opType);
        }

        /// <summary>
        /// ��Ŀ ī��� �ʱ�ȭ
        /// </summary>
        public void InitializeAsJoker()
        {
            CardType = CardType.Joker;

            if (cardText == null)
                cardText = GetComponentInChildren<CardText>();

            cardText.SetJokerText();
        }
        #endregion

        #region Turn State Management
        public void SetWasPlayedThisTurn(bool played)
        {
            WasPlayedThisTurn = played;
            UpdateGlowState();
        }

        public void SetHasAttackedThisTurn(bool attacked)
        {
            HasAttackedThisTurn = attacked;
            UpdateGlowState();
        }

        public void SetWasModifiedThisTurn(bool modified)
        {
            WasModifiedThisTurn = modified;
            UpdateGlowState();
        }

        public void ResetTurnState()
        {
            WasPlayedThisTurn = false;
            HasAttackedThisTurn = false;
            UpdateGlowState();
        }

        public void ResetForNewTurn()
        {
            WasPlayedThisTurn = false;
            HasAttackedThisTurn = false;
            WasModifiedThisTurn = false;
            UpdateGlowState();
        }
        #endregion

        #region Attack Logic
        /// <summary>
        /// ���� ���� ���� üũ
        /// Secret ���¿� ������� ���� ���� (��ġ�� ����)
        /// </summary>
        public bool IsAttackableThisTurn()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsFirstRound)
            {
                return false;
            }

            if (WasPlayedThisTurn)
            {
                return false;
            }

            if (WasModifiedThisTurn)
            {
                return false;
            }

            if (HasAttackedThisTurn)
            {
                return false;
            }

            if (TurnManager.Instance != null && !TurnManager.Instance.IsLocalPlayerTurn)
            {
                return false;
            }

            if (CurrentOwnerType != CardZone.OwnerType.Player)
            {
                return false;
            }

            if (CurrentZoneType != CardZone.ZoneType.Field)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region GLOW Management
        public void SetGlowOverride(bool forceGlow, Color? glowColor = null)
        {
            isGlowOverridden = true;
            overrideGlowState = forceGlow;
            overrideGlowColor = glowColor;
            ApplyGlowState(forceGlow, glowColor);
        }

        public void ClearGlowOverride()
        {
            isGlowOverridden = false;
            overrideGlowState = false;
            overrideGlowColor = null;
            UpdateGlowState();
        }

        private void UpdateGlowState()
        {
            if (isGlowOverridden)
            {
                return;
            }

            bool canAttack = IsAttackableThisTurn();
            ApplyGlowState(canAttack, canAttack ? Global.GlowGreen : null);
        }

        private void ApplyGlowState(bool isGlowing, Color? glowColor = null)
        {
            CanAttack = isGlowing;

            var effect = GetComponentInChildren<CardEffect>();
            if (effect != null)
            {
                effect.SetGlow(isGlowing);

                if (isGlowing)
                {
                    Color colorToUse = glowColor ?? (
                        CurrentOwnerType == CardZone.OwnerType.Player
                            ? Global.GlowGreen
                            : Global.GlowRed
                    );

                    effect.LerpGlowColor(colorToUse, 0.2f);
                }
            }
        }

        public void SetCardState(bool isAttackable, Color? glowColor = null)
        {
            SetGlowOverride(isAttackable, glowColor);
        }
        #endregion

        #region Secret Management
        /// <summary>
        /// Secret ���� ����
        /// CardEffect�� �����Ͽ� Material Property Block �浹 ����
        /// </summary>
        /// <param name="isSecret">Secret ��� ����</param>
        public void SetSecret(bool isSecret)
        {
            IsSecret = isSecret;

            if (isSecret)
            {
                ApplySecretVisual();
            }
            else
            {
                RestoreOriginalVisual();
            }
        }

        /// <summary>
        /// Secret �ð��� ȿ�� ����
        /// CardEffect���� ��������Ʈ ������ �˷� Material Property Block ������Ʈ ����
        /// </summary>
        private void ApplySecretVisual()
        {
            // ��������Ʈ�� Secret ��������Ʈ�� ����
            if (spriteRenderer != null)
            {
                var secretSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);
                if (secretSprite != null)
                {
                    spriteRenderer.sprite = secretSprite;

                    // CardEffect���� ��������Ʈ ���� �˸� (Material Property Block ������Ʈ)
                    var cardEffect = GetComponentInChildren<CardEffect>();
                    if (cardEffect != null)
                    {
                        cardEffect.OnSpriteChanged();
                    }
                }
            }

            // �ؽ�Ʈ ó��
            if (cardTMP != null)
            {
                if (CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    // �÷��̾� Secret ī��: ��� �ؽ�Ʈ�� ǥ��
                    cardTMP.gameObject.SetActive(true);
                    cardTMP.color = Color.white;
                }
                else
                {
                    // ���� Secret ī��: �ؽ�Ʈ ����
                    cardTMP.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// ���� ���� ����
        /// CardEffect���� ��������Ʈ ������ �˷� Material Property Block ������Ʈ ����
        /// </summary>
        private void RestoreOriginalVisual()
        {
            // ��������Ʈ�� �������� ����
            if (spriteRenderer != null)
            {
                Sprite originalSprite;
                if (CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    originalSprite = ResourcesManager.Instance.GetPlayerSprite();
                }
                else
                {
                    originalSprite = ResourcesManager.Instance.GetOpponentSprite();
                }

                if (originalSprite != null)
                {
                    spriteRenderer.sprite = originalSprite;

                    // CardEffect���� ��������Ʈ ���� �˸� (Material Property Block ������Ʈ)
                    var cardEffect = GetComponentInChildren<CardEffect>();
                    if (cardEffect != null)
                    {
                        cardEffect.OnSpriteChanged();
                    }
                }
            }

            // �ؽ�Ʈ ����
            if (cardTMP != null)
            {
                cardTMP.gameObject.SetActive(true);

                // ���� �������� ����
                Color targetColor;
                if (CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    var playerSprite = ResourcesManager.Instance.GetPlayerSprite();
                    targetColor = ResourcesManager.Instance.MatchColorFromSprite(playerSprite?.name);
                }
                else
                {
                    var opponentSprite = ResourcesManager.Instance.GetOpponentSprite();
                    targetColor = ResourcesManager.Instance.MatchColorFromSprite(opponentSprite?.name);
                }

                cardTMP.color = targetColor;
            }
        }

        /// <summary>
        /// Secret ī�� ���� (���� �� ȣ��)
        /// </summary>
        public void RevealSecret()
        {
            if (!IsSecret) return;

            SetSecret(false);
            UpdateGlowState();
        }
        #endregion

        #region Zone Interaction
        /// <summary>
        /// ī���� Zone �� ������ ������ ��ȣ�ۿ� ���� ����
        /// </summary>
        /// <param name="zoneType">ī�尡 ��ġ�� Zone</param>
        /// <param name="ownerType">ī�� ������</param>
        public void SetInteraction(CardZone.ZoneType zoneType, CardZone.OwnerType ownerType)
        {
            CurrentZoneType = zoneType;
            CurrentOwnerType = ownerType;

            if (zoneType == CardZone.ZoneType.Hand && ownerType == CardZone.OwnerType.Player)
                ApplyInteraction(CardInteractionType.DragAndClick);
            else if (zoneType == CardZone.ZoneType.Field)
                ApplyInteraction(CardInteractionType.ClickOnly);
            else
                ApplyInteraction(CardInteractionType.None);

            UpdateGlowState();
        }

        private void ApplyInteraction(CardInteractionType type)
        {
            if (mouseEvent == null)
                mouseEvent = GetComponentInChildren<ObjectMouseEvent>();

            mouseEvent.isClickable = (type == CardInteractionType.ClickOnly || type == CardInteractionType.DragAndClick);
            mouseEvent.isDraggable = (type == CardInteractionType.DragAndClick);
        }
        #endregion

        #region Input Handling
        private void HandleClick()
        {
            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                return;
            }

            if (JokerTargetSelector.Instance != null && JokerTargetSelector.Instance.IsSelecting())
            {
                onClicked?.Invoke(this);
                return;
            }

            if (OperatorManager.Instance.IsInOperatorMode)
            {
                if (CurrentZoneType == CardZone.ZoneType.Hand)
                {
                    return;
                }

                onClicked?.Invoke(this);
                return;
            }

            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool hasAttackerSelected = attackManager != null && attackManager.HasAttackerSelected();

            if (hasAttackerSelected)
            {
                if (CurrentZoneType == CardZone.ZoneType.Hand &&
                    CurrentOwnerType == CardZone.OwnerType.Player &&
                    (CardType == CardType.Joker || CardType == CardType.Operator))
                {
                    return;
                }

                if (CurrentOwnerType == CardZone.OwnerType.Opponent && CurrentZoneType == CardZone.ZoneType.Field)
                {
                    onClicked?.Invoke(this);
                    return;
                }

                return;
            }

            if (InGameManager.Instance.IsProcessing)
            {
                return;
            }

            if (CardType == CardType.Joker &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                if (JokerModeSelector.Instance != null)
                {
                    JokerModeSelector.Instance.Show(this);
                }
                return;
            }

            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.StartOperation(this);
                return;
            }

            onClicked?.Invoke(this);
        }

        private void HandleEndDrag()
        {
            if (!TurnManager.Instance.IsLocalPlayerTurn)
            {
                return;
            }

            if (OperatorManager.Instance.IsInOperatorMode)
            {
                return;
            }

            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool hasAttackerSelected = attackManager != null && attackManager.HasAttackerSelected();

            if (hasAttackerSelected)
            {
                if (CurrentZoneType == CardZone.ZoneType.Hand &&
                    CurrentOwnerType == CardZone.OwnerType.Player &&
                    (CardType == CardType.Joker || CardType == CardType.Operator))
                {
                    return;
                }
            }

            if (InGameManager.Instance.IsProcessing)
            {
                return;
            }

            if (CardType == CardType.Joker &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OnCardDropped?.Invoke(transform);
                return;
            }

            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.StartOperation(this);
                return;
            }

            OnCardDropped?.Invoke(transform);
        }
        #endregion

        #region Animation
        /// <summary>
        /// ī�� ���� �ִϸ��̼� ����
        /// </summary>
        /// <param name="onComplete">�ִϸ��̼� �Ϸ� �� ������ �ݹ�</param>
        /// <param name="delay">���� �ð�</param>
        public IEnumerator AnimateRemoval(Action onComplete = null, float delay = 0f)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float animDuration = 0.5f;

            if (mouseEvent != null)
            {
                mouseEvent.isClickable = false;
                mouseEvent.isDraggable = false;
            }

            var cardEffect = GetComponentInChildren<CardEffect>();
            if (cardEffect != null)
            {
                cardEffect.SetGlow(false);
            }

            List<Tween> activeTweens = new List<Tween>();

            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            TextMeshPro[] allTexts = GetComponentsInChildren<TextMeshPro>();

            foreach (var sr in allSprites)
            {
                if (sr != null)
                {
                    Tween fadeTween = sr.DOFade(0f, animDuration)
                        .SetTarget(sr)
                        .OnKill(() => { });
                    activeTweens.Add(fadeTween);
                }
            }

            foreach (var text in allTexts)
            {
                if (text != null)
                {
                    Tween textTween = text.DOFade(0f, animDuration)
                        .SetTarget(text)
                        .OnKill(() => { });
                    activeTweens.Add(textTween);
                }
            }

            if (transform != null)
            {
                Tween scaleTween = transform.DOScale(Vector3.one * 0.8f, animDuration)
                    .SetEase(Ease.InQuad)
                    .SetTarget(transform)
                    .OnKill(() => { });

                Tween moveTween = transform.DOLocalMoveY(
                    transform.localPosition.y + 30f, animDuration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(transform)
                    .OnKill(() => { });

                activeTweens.Add(scaleTween);
                activeTweens.Add(moveTween);
            }

            yield return new WaitForSeconds(animDuration);

            foreach (var tween in activeTweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill(false);
                }
            }

            onComplete?.Invoke();
        }
        #endregion

        #region Combat Icons
        /// <summary>
        /// 공격 아이콘 표시 (Sword)
        /// </summary>
        public void ShowAttackIcon()
        {
            HideAllIcons();
            if (swordIcon != null)
            {
                swordIcon.SetActive(true);
                Debug.Log($"[Card] {name} - 공격 아이콘 표시");
            }
        }

        /// <summary>
        /// 방어 아이콘 표시 (Shield)
        /// </summary>
        public void ShowDefenseIcon()
        {
            HideAllIcons();
            if (shieldIcon != null)
            {
                shieldIcon.SetActive(true);
                Debug.Log($"[Card] {name} - 방어 아이콘 표시");
            }
        }

        /// <summary>
        /// 모든 아이콘 숨김
        /// </summary>
        public void HideAllIcons()
        {
            if (swordIcon != null) swordIcon.SetActive(false);
            if (shieldIcon != null) shieldIcon.SetActive(false);
        }
        #endregion

        #region Enums
        private enum CardInteractionType
        {
            None,
            ClickOnly,
            DragAndClick
        }
        #endregion
    }
}