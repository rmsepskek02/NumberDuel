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
    /// 메인 카드 컴포넌트로 생성 및 클릭 이벤트를 처리하는 컴포넌트
    /// Secret 카드 시각화 효과는 CardEffect를 사용하여 Material Property Block 충돌 방지
    /// </summary>
    public class Card : MonoBehaviourPun, ICard
    {
        #region Fields and Properties
        private TextMeshPro cardTMP;
        private CardText cardText;
        private SpriteRenderer spriteRenderer;

        /// <summary>
        /// 선택/상태 표시 아이콘 (공격, 방어, 연산자, 조커 등 모든 아이콘 통합)
        /// </summary>
        [SerializeField] private GameObject selectionIcon;

        /// <summary>
        /// 카드 클릭 이벤트
        /// </summary>
        public static event Action<Card> onClicked;

        /// <summary>
        /// 카드 드롭 이벤트
        /// </summary>
        public static event Action<Transform> OnCardDropped;

        /// <summary>
        /// 현재 카드가 위치한 Zone 타입
        /// </summary>
        public CardZone.ZoneType CurrentZoneType { get; private set; }

        /// <summary>
        /// 현재 카드 소유자 타입
        /// </summary>
        public CardZone.OwnerType CurrentOwnerType { get; private set; }

        /// <summary>
        /// 카드 타입 (Number, Operator, Joker)
        /// </summary>
        public CardType CardType { get; private set; } = CardType.Number;

        /// <summary>
        /// 연산자 타입 (Plus, Minus, Multiply, Divide)
        /// </summary>
        public OperatorType OperatorType { get; private set; }

        /// <summary>
        /// 조커 효과 타입 (Draw, Delete, Swap)
        /// </summary>
        public JokerEffectType JokerEffectType { get; private set; }

        /// <summary>
        /// Secret 상태 여부
        /// </summary>
        public bool IsSecret { get; private set; }

        /// <summary>
        /// 공격 가능 상태 여부
        /// </summary>
        public bool CanAttack { get; private set; } = false;

        /// <summary>
        /// 이번 턴에 수정되었는지 여부
        /// </summary>
        public bool WasModifiedThisTurn { get; private set; } = false;

        /// <summary>
        /// 이번 턴에 배치되었는지 여부
        /// </summary>
        public bool WasPlayedThisTurn { get; private set; } = false;

        /// <summary>
        /// 이번 턴에 공격했는지 여부
        /// </summary>
        public bool HasAttackedThisTurn { get; private set; } = false;

        /// <summary>
        /// 이번 턴에 시크릿에서 오픈으로 전환되었는지 여부
        /// </summary>
        public bool WasOpenedThisTurn { get; private set; } = false;

        /// <summary>
        /// GLOW 효과 강제 설정 여부
        /// </summary>
        private bool isGlowOverridden = false;
        private bool overrideGlowState = false;
        private Color? overrideGlowColor = null;

        /// <summary>
        /// 카드가 공개 상태인지 여부 (Secret의 반대)
        /// </summary>
        public bool IsOpen => !IsSecret;

        private ObjectMouseEvent mouseEvent;
        #endregion

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
        /// 숫자 카드 초기화
        /// </summary>
        /// <param name="value">카드 값</param>
        public void InitializeAsNumber(float value)
        {
            CardType = CardType.Number;
            cardText.SetRawValue(value);
        }

        /// <summary>
        /// 연산자 카드 초기화
        /// </summary>
        /// <param name="opType">연산자 타입</param>
        public void InitializeAsOperator(OperatorType opType)
        {
            CardType = CardType.Operator;
            OperatorType = opType;
            cardText.SetOperatorText(opType);
        }

        /// <summary>
        /// 조커 카드 초기화
        /// </summary>
        /// <param name="effectType">조커 효과 타입 (옵션)</param>
        public void InitializeAsJoker(JokerEffectType effectType = JokerEffectType.Draw)
        {
            CardType = CardType.Joker;
            JokerEffectType = effectType;

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

        public void SetWasOpenedThisTurn(bool opened)
        {
            WasOpenedThisTurn = opened;
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
            WasOpenedThisTurn = false;
            UpdateGlowState();
        }
        #endregion

        #region Attack Logic
        /// <summary>
        /// 공격 가능 상태 체크
        /// Secret 상태에 상관없이 공격 가능 (위치와 조건)
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

            if (WasOpenedThisTurn)
            {
                return false;
            }

            if (TurnManager.Instance != null && !TurnManager.Instance.IsLocalPlayerTurn)
            {
                return false;
            }

            // 원격 액션 진행 중에는 공격 불가 (상대방의 액션을 시청 중)
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.HasPendingRemoteActions)
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
        /// Secret 상태 설정
        /// CardEffect를 사용하여 Material Property Block 충돌 방지
        /// </summary>
        /// <param name="isSecret">Secret 모드 여부</param>
        public void SetSecret(bool isSecret)
        {
            bool wasSecret = IsSecret;
            IsSecret = isSecret;

            if (isSecret)
            {
                ApplySecretVisual();
            }
            else
            {
                RestoreOriginalVisual();

                // ★ 시크릿이 공개되는 순간 사운드 이벤트 발생
                if (wasSecret)
                {
                    GameEventManager.Instance?.TriggerSecretRevealed();
                }
            }
        }

        /// <summary>
        /// Secret 시각화 효과 적용
        /// CardEffect에게 스프라이트 변경을 알려 Material Property Block 업데이트 진행
        /// </summary>
        private void ApplySecretVisual()
        {
            // 스프라이트를 Secret 스프라이트로 변경
            if (spriteRenderer != null)
            {
                var secretSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);
                if (secretSprite != null)
                {
                    spriteRenderer.sprite = secretSprite;

                    // CardEffect에게 스프라이트 변경 알림 (Material Property Block 업데이트)
                    var cardEffect = GetComponentInChildren<CardEffect>();
                    if (cardEffect != null)
                    {
                        cardEffect.OnSpriteChanged();
                    }
                }
            }

            // 텍스트 처리
            if (cardTMP != null)
            {
                if (CurrentOwnerType == CardZone.OwnerType.Player)
                {
                    // 플레이어 Secret 카드: 자신 텍스트만 표시
                    cardTMP.gameObject.SetActive(true);
                    cardTMP.color = Color.white;
                }
                else
                {
                    // 상대 Secret 카드: "?" 표시 (RawValue는 유지됨)
                    cardTMP.gameObject.SetActive(true);
                    cardTMP.text = "?";
                    cardTMP.color = Color.white;
                }
            }
        }

        /// <summary>
        /// 원본 모습 복원
        /// CardEffect에게 스프라이트 변경을 알려 Material Property Block 업데이트 진행
        /// </summary>
        private void RestoreOriginalVisual()
        {
            // 스프라이트를 원래대로 복원
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

                    // CardEffect에게 스프라이트 변경 알림 (Material Property Block 업데이트)
                    var cardEffect = GetComponentInChildren<CardEffect>();
                    if (cardEffect != null)
                    {
                        cardEffect.OnSpriteChanged();
                    }
                }
            }

            // 텍스트 복원
            if (cardTMP != null)
            {
                cardTMP.gameObject.SetActive(true);

                // RawValue를 실제 숫자로 표시 (시크릿에서 오픈으로 전환)
                if (cardText != null)
                {
                    float rawValue = cardText.RawValue;
                    cardTMP.text = Mathf.FloorToInt(rawValue).ToString();
                }

                // 색상 원래대로 복원
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
        /// Secret 카드 공개 (전투 시 호출)
        /// </summary>
        public void RevealSecret()
        {
            if (!IsSecret) return;

            SetSecret(false);
            UpdateGlowState();
        }

        /// <summary>
        /// Secret 카드를 수동으로 오픈으로 전환 (플레이어 클릭 시 호출)
        /// 오픈한 턴에는 공격 불가능
        /// </summary>
        public void OpenSecret()
        {
            if (!IsSecret) return;

            SetSecret(false); // Sprite와 Text 색상 자동 복원
            SetWasOpenedThisTurn(true); // 이번 턴 공격 불가

            // Selection 아이콘을 3초간 표시
            StartCoroutine(ShowOpenSecretIconTemporary(3f));
        }

        /// <summary>
        /// 시크릿 오픈 아이콘을 일정 시간 동안 표시 후 자동으로 숨김
        /// </summary>
        /// <param name="duration">아이콘 표시 지속 시간 (초)</param>
        public IEnumerator ShowOpenSecretIconTemporary(float duration = 3f)
        {
            ShowSelectionIcon(CardIconType.Selection);
            yield return new WaitForSeconds(duration);
            HideSelectionIcon();
        }
        #endregion

        #region Zone Interaction
        /// <summary>
        /// 카드가 Zone 에 들어갈 때마다 상호작용 상태 설정
        /// </summary>
        /// <param name="zoneType">카드가 위치한 Zone</param>
        /// <param name="ownerType">카드 소유자</param>
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
                // 내 턴이 아닐 때 메시지 표시
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("NotYourTurn");
                }
                return;
            }

            // 원격 액션 진행 중에는 카드 클릭 차단 (상대방의 액션을 시청 중)
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.HasPendingRemoteActions)
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
                // 내 턴이 아닐 때 메시지 표시
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("NotYourTurn");
                }
                return;
            }

            // 원격 액션 진행 중에는 카드 드래그 차단 (상대방의 액션을 시청 중)
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.HasPendingRemoteActions)
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
        /// 카드 제거 애니메이션 실행
        /// </summary>
        /// <param name="onComplete">애니메이션 완료 시 실행할 콜백</param>
        /// <param name="delay">지연 시간</param>
        public IEnumerator AnimateRemoval(Action onComplete = null, float delay = 0f)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // 카드 파괴 이벤트 발생 (사운드 재생)
            GameEventManager.Instance?.TriggerCardDestroyed();

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

        #region Selection Icons
        /// <summary>
        /// 선택 아이콘 표시 (CardIconType enum 사용)
        /// 모든 아이콘을 ResourcesManager에서 로드
        /// </summary>
        /// <param name="iconType">아이콘 타입</param>
        public void ShowSelectionIcon(CardIconType iconType)
        {
            if (selectionIcon == null || iconType == CardIconType.None) return;

            var spriteRenderer = selectionIcon.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError($"[Card] SelectionIcon에 SpriteRenderer가 없습니다!");
                return;
            }

            // ResourcesManager에서 아이콘 스프라이트 로드
            Sprite iconSprite = Manager.ResourcesManager.Instance?.GetIconSprite(iconType);

            // 스프라이트 설정 및 활성화
            if (iconSprite != null)
            {
                spriteRenderer.sprite = iconSprite;
                spriteRenderer.color = Color.white;
                selectionIcon.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[Card] 아이콘 스프라이트를 찾을 수 없습니다: {iconType}");
            }
        }

        /// <summary>
        /// 선택 아이콘 숨김
        /// </summary>
        public void HideSelectionIcon()
        {
            if (selectionIcon != null)
            {
                selectionIcon.SetActive(false);
            }
        }

        /// <summary>
        /// 공격 아이콘 표시 (호환성 유지)
        /// </summary>
        public void ShowAttackIcon()
        {
            ShowSelectionIcon(CardIconType.Attack);
        }

        /// <summary>
        /// 방어 아이콘 표시 (호환성 유지)
        /// </summary>
        public void ShowDefenseIcon()
        {
            ShowSelectionIcon(CardIconType.Defense);
        }

        /// <summary>
        /// 모든 아이콘 숨김 (호환성 유지)
        /// </summary>
        public void HideAllIcons()
        {
            HideSelectionIcon();
        }

        /// <summary>
        /// 현재 카드 타입에 맞는 아이콘 자동 표시
        /// 연산자 카드 → 연산자 아이콘 (+, -, ×, ÷)
        /// 조커 카드 → 조커 효과 아이콘 (드로우, 삭제, 교환)
        /// </summary>
        public void ShowCardTypeIcon()
        {
            if (CardType == CardType.Operator)
            {
                var iconType = Manager.ResourcesManager.Instance?.OperatorToIconType(OperatorType);
                if (iconType.HasValue && iconType.Value != CardIconType.None)
                {
                    ShowSelectionIcon(iconType.Value);
                }
            }
            else if (CardType == CardType.Joker)
            {
                var iconType = Manager.ResourcesManager.Instance?.JokerToIconType(JokerEffectType);
                if (iconType.HasValue && iconType.Value != CardIconType.None)
                {
                    ShowSelectionIcon(iconType.Value);
                }
            }
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