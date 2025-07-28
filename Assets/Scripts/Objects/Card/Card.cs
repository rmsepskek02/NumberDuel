using Manager;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace Objects
{
    /// <summary>
    /// 개별 카드 오브젝트의 상태 및 클릭 반응을 관리하는 컴포넌트
    /// - ICard 구현을 통해 Zone에서 인터랙션 설정을 받을 수 있음
    /// - ObjectMouseEvent로부터 클릭 이벤트를 수신함
    /// - 완전 재정립된 공격 시스템 및 GLOW 관리
    /// </summary>
    public class Card : MonoBehaviour, ICard
    {
        private TextMeshPro cardTMP;
        private CardText cardText;
        private SpriteRenderer spriteRenderer;

        public static event Action<Card> onClicked; // 외부에서 구독 가능한 카드 클릭 이벤트
        public static event Action<Transform> OnCardDropped; // 카드가 드래그에서 해제됐을 때 알림

        public CardZone.ZoneType CurrentZoneType { get; private set; }
        public CardZone.OwnerType CurrentOwnerType { get; private set; }
        public CardType CardType { get; private set; } = CardType.Number;
        public OperatorType OperatorType { get; private set; }
        public bool IsSecret { get; private set; }
        public bool CanAttack { get; private set; } = false;

        // 턴별 상태 관리 (재정립)
        public bool WasModifiedThisTurn { get; private set; } = false;    // 연산으로 수정됨
        public bool WasPlayedThisTurn { get; private set; } = false;      // 이번 턴에 필드 배치됨
        public bool HasAttackedThisTurn { get; private set; } = false;    // 이번 턴에 공격함

        // GLOW 제어 관리
        private bool isGlowOverridden = false;  // 외부에서 GLOW 강제 설정 중인지
        private bool overrideGlowState = false; // 강제 설정된 GLOW 상태
        private Color? overrideGlowColor = null; // 강제 설정된 GLOW 색상

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
        // 카드 초기화 함수: 숫자 카드
        public void InitializeAsNumber(float value)
        {
            CardType = CardType.Number;
            cardText.SetRawValue(value);
        }

        // 카드 초기화 함수: 연산자 카드
        public void InitializeAsOperator(OperatorType opType)
        {
            CardType = CardType.Operator;
            OperatorType = opType;
            cardText.SetOperatorText(opType);
        }

        // 카드 초기화 함수: 조커 카드
        public void InitializeAsJoker()
        {
            CardType = CardType.Joker;

            if (cardText == null)
                cardText = GetComponentInChildren<CardText>();

            cardText.SetJokerText();
        }
        #endregion

        #region Turn State Management (완전 재정립)
        /// <summary>
        /// 이번 턴에 필드에 배치되었음을 표시
        /// </summary>
        public void SetWasPlayedThisTurn(bool played)
        {
            WasPlayedThisTurn = played;
            UpdateGlowState(); // GLOW 상태 즉시 업데이트
        }

        /// <summary>
        /// 이번 턴에 공격했음을 표시
        /// </summary>
        public void SetHasAttackedThisTurn(bool attacked)
        {
            HasAttackedThisTurn = attacked;
            UpdateGlowState(); // GLOW 상태 즉시 업데이트
        }

        /// <summary>
        /// 연산으로 수정되었음을 표시
        /// </summary>
        public void SetWasModifiedThisTurn(bool modified)
        {
            WasModifiedThisTurn = modified;
            UpdateGlowState(); // GLOW 상태 즉시 업데이트
        }

        /// <summary>
        /// 턴 시작 시 상태 초기화 (WasModified는 유지)
        /// </summary>
        public void ResetTurnState()
        {
            WasPlayedThisTurn = false;
            HasAttackedThisTurn = false;
            // WasModifiedThisTurn은 그대로 유지 (다음 턴까지 공격 불가)
            UpdateGlowState();
        }

        /// <summary>
        /// 새 턴 시작 시 완전 초기화
        /// </summary>
        public void ResetForNewTurn()
        {
            WasPlayedThisTurn = false;
            HasAttackedThisTurn = false;
            WasModifiedThisTurn = false;
            UpdateGlowState();
        }
        #endregion

        #region Attack Logic (완전 재정립)
        /// <summary>
        /// 공격 가능 여부 체크 (모든 조건 통합)
        /// </summary>
        public bool IsAttackableThisTurn()
        {
            // 기본 조건: 오픈되어 있어야 함
            if (!IsOpen)
            {
                return false;
            }

            // 1. 첫라운드 공격불가
            if (TurnManager.Instance != null && TurnManager.Instance.IsFirstRound)
            {
                return false;
            }

            // 2. 카드를 낸 턴에 공격 불가
            if (WasPlayedThisTurn)
            {
                return false;
            }

            // 3. 연산으로 수치가 바뀐 경우 공격 불가
            if (WasModifiedThisTurn)
            {
                return false;
            }

            // 4. 이미 공격한 경우 공격 불가
            if (HasAttackedThisTurn)
            {
                return false;
            }

            // 5. 상대 턴에 공격 불가
            if (TurnManager.Instance != null && !TurnManager.Instance.IsLocalPlayerTurn)
            {
                return false;
            }

            // 6. 내 카드가 아니면 공격 불가
            if (CurrentOwnerType != CardZone.OwnerType.Player)
            {
                return false;
            }

            // 7. 필드에 있지 않으면 공격 불가
            if (CurrentZoneType != CardZone.ZoneType.Field)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region GLOW Management (완전 재정립)
        /// <summary>
        /// GLOW 상태 강제 설정 (연산카드 프로세스 등에서 사용)
        /// </summary>
        /// <param name="forceGlow">강제로 설정할 GLOW 상태</param>
        /// <param name="glowColor">GLOW 색상 (null이면 기본 색상)</param>
        public void SetGlowOverride(bool forceGlow, Color? glowColor = null)
        {
            isGlowOverridden = true;
            overrideGlowState = forceGlow;
            overrideGlowColor = glowColor;

            // 즉시 GLOW 적용
            ApplyGlowState(forceGlow, glowColor);

            Debug.Log($"[Card] {name} GLOW 강제 설정: {forceGlow}");
        }

        /// <summary>
        /// GLOW 강제 설정 해제 (일반 모드로 복귀)
        /// </summary>
        public void ClearGlowOverride()
        {
            isGlowOverridden = false;
            overrideGlowState = false;
            overrideGlowColor = null;

            // 일반 GLOW 상태로 복원
            UpdateGlowState();

            Debug.Log($"[Card] {name} GLOW 강제 설정 해제");
        }

        /// <summary>
        /// GLOW 상태 업데이트 (통합 관리)
        /// </summary>
        private void UpdateGlowState()
        {
            // 강제 설정 중이면 무시
            if (isGlowOverridden) return;

            bool canAttack = IsAttackableThisTurn();
            ApplyGlowState(canAttack, canAttack ? Global.GlowGreen : null);
        }

        /// <summary>
        /// 실제 GLOW 적용
        /// </summary>
        private void ApplyGlowState(bool isGlowing, Color? glowColor = null)
        {
            CanAttack = isGlowing;

            var effect = GetComponentInChildren<CardEffect>();
            if (effect != null)
            {
                // GLOW 토글
                effect.SetGlow(isGlowing);

                // GLOW 색상 지정
                if (isGlowing)
                {
                    // 전달된 색상이 없으면 자동 분기
                    Color colorToUse = glowColor ?? (
                        CurrentOwnerType == CardZone.OwnerType.Player
                            ? Global.GlowGreen
                            : Global.GlowRed
                    );

                    effect.LerpGlowColor(colorToUse, 0.2f);
                }
            }
        }

        /// <summary>
        /// 기존 SetCardState 메서드 (호환성 유지)
        /// </summary>
        public void SetCardState(bool isAttackable, Color? glowColor = null)
        {
            // 강제 설정 모드로 전환
            SetGlowOverride(isAttackable, glowColor);
        }
        #endregion

        #region Secret Management
        /// <summary>
        /// 카드를 비밀 상태로 설정하거나 해제합니다.
        /// </summary>
        public void SetSecret(bool isSecret)
        {
            IsSecret = isSecret;

            if (cardTMP != null)
                cardTMP.gameObject.SetActive(!isSecret);

            if (spriteRenderer != null)
            {
                if (isSecret)
                {
                    var secretSprite = ResourcesManager.Instance.GetSprite(Global.Card, Global.SpriteColorBlack);
                    if (secretSprite != null)
                        spriteRenderer.sprite = secretSprite;
                    else
                        Debug.LogWarning($"[Card] Secret Sprite '{Global.SpriteColorBlack}' not found.");
                }
                else
                {
                    // 원래 Sprite로 되돌릴 로직이 필요하면 여기에 작성
                    spriteRenderer.sprite = ResourcesManager.Instance.GetPlayerSprite();
                }
            }
        }
        #endregion

        #region Zone Interaction
        /// <summary>
        /// Zone 정보에 따라 카드 상호작용 권한 설정
        /// </summary>
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

            // Zone 변경 시 GLOW 상태 재계산
            UpdateGlowState();
        }

        /// <summary>
        /// Interaction 유형에 따라 드래그/클릭 허용 여부 설정
        /// </summary>
        private void ApplyInteraction(CardInteractionType type)
        {
            if (mouseEvent == null)
                mouseEvent = GetComponentInChildren<ObjectMouseEvent>();

            mouseEvent.isClickable = (type == CardInteractionType.ClickOnly || type == CardInteractionType.DragAndClick);
            mouseEvent.isDraggable = (type == CardInteractionType.DragAndClick);
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// 클릭 시 실행되는 내부 로직 (기존 유지)
        /// </summary>
        private void HandleClick()
        {
            // 1. 조커 대상 선택 모드인 경우 항상 허용
            if (JokerTargetSelector.Instance != null && JokerTargetSelector.Instance.IsSelecting())
            {
                onClicked?.Invoke(this);
                return;
            }

            // 2. 연산자 모드 중인 경우 - 필드 카드만 허용
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                if (CurrentZoneType == CardZone.ZoneType.Hand)
                {
                    return;
                }

                onClicked?.Invoke(this);
                return;
            }

            // 3. 공격 프로세스 중인지 확인
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

            // 4. 기타 프로세스 진행 중이면 모든 새 프로세스 시작 차단
            if (InGameManager.Instance.IsProcessing)
            {
                return;
            }

            // 5. 프로세스가 진행 중이지 않을 때만 새 프로세스 시작 허용

            // 조커 카드일 경우: JokerModeSelector 호출
            if (CardType == CardType.Joker &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                if (JokerModeSelector.Instance != null)
                {
                    JokerModeSelector.Instance.Show(this);
                }
                else
                {
                    Debug.LogError("[Card] JokerModeSelector를 찾을 수 없습니다.");
                }
                return;
            }

            // 연산자 카드일 경우: OperatorManager 호출
            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.StartOperation(this);
                return;
            }

            // 일반 카드 클릭 이벤트
            onClicked?.Invoke(this);
        }

        /// <summary>
        /// 카드가 Drag가 끝난 시점에 호출 (기존 유지)
        /// </summary>
        private void HandleEndDrag()
        {
            // 1. 연산자 프로세스 중인 경우
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                return;
            }

            // 2. 공격 프로세스 중인지 확인
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

            // 3. 프로세스 진행 중일 때는 새로운 프로세스 시작 차단
            if (InGameManager.Instance.IsProcessing)
            {
                return;
            }

            // 4. 프로세스가 진행 중이지 않을 때만 새 프로세스 시작 허용

            // 조커 카드일 경우: 드롭 시 카드 배치 프로세스 시작
            if (CardType == CardType.Joker &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OnCardDropped?.Invoke(transform);
                return;
            }

            // 연산자 카드일 경우: 드롭 시 연산자 모드 진입
            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.StartOperation(this);
                return;
            }

            // 일반 카드 드롭 이벤트
            OnCardDropped?.Invoke(transform);
        }
        #endregion

        #region Animation
        /// <summary>
        /// 카드 삭제 애니메이션 (기존 유지)
        /// </summary>
        public IEnumerator AnimateRemoval(Action onComplete = null, float delay = 0f)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float animDuration = 0.5f;

            // 1. 상호작용 비활성화
            if (mouseEvent != null)
            {
                mouseEvent.isClickable = false;
                mouseEvent.isDraggable = false;
            }

            // 2. Glow 효과 제거
            var cardEffect = GetComponentInChildren<CardEffect>();
            if (cardEffect != null)
            {
                cardEffect.SetGlow(false);
            }

            // 3. 모든 트윈 저장을 위한 리스트
            List<Tween> activeTweens = new List<Tween>();

            // 4. 모든 시각적 요소 찾기
            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            TextMeshPro[] allTexts = GetComponentsInChildren<TextMeshPro>();

            // 5. 페이드 애니메이션
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

            // 6. 스케일 + 이동 애니메이션
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

            // 7. 애니메이션 완료 대기
            yield return new WaitForSeconds(animDuration);

            // 8. 트윈 정리
            foreach (var tween in activeTweens)
            {
                if (tween != null && tween.IsActive())
                {
                    tween.Kill(false);
                }
            }

            // 9. 콜백 실행
            onComplete?.Invoke();
        }
        #endregion

        #region Enums
        /// <summary>
        /// 카드 상호작용 종류를 정의하는 내부 열거형
        /// </summary>
        private enum CardInteractionType
        {
            None,
            ClickOnly,
            DragAndClick
        }
        #endregion
    }
}