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
        public bool WasModifiedThisTurn { get; private set; } = false;
        public bool IsOpen => !IsSecret;

        private ObjectMouseEvent mouseEvent;

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

        /// <summary>
        /// 카드의 공격 가능 상태와 GLOW 색상을 지정하는 일반화된 함수
        /// </summary>
        /// <param name="isAttackable">공격 가능 여부 (내 턴에 클릭 가능)</param>
        /// <param name="glowColor">GLOW 색상 (null이면 자동 지정)</param>
        public void SetCardState(bool isAttackable, Color? glowColor = null)
        {
            CanAttack = isAttackable;

            var effect = GetComponentInChildren<CardEffect>();
            if (effect != null)
            {
                // GLOW 토글
                effect.SetGlow(isAttackable);

                // GLOW 색상 지정
                if (isAttackable)
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

        public void SetWasModifiedThisTurn(bool modified)
        {
            WasModifiedThisTurn = modified;
            if (modified) SetCardState(false);
        }

        public bool IsAttackableThisTurn()
        {
            return IsOpen && !WasModifiedThisTurn;
        }

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

        /// <summary>
        /// 클릭 시 실행되는 내부 로직 (버그 수정 버전)
        /// 공격 프로세스 중 연산/조커 카드 사용 차단
        /// </summary>
        private void HandleClick()
        {
            Debug.Log($"[Card] Clicked: {gameObject.name}");

            // 디버깅: 프로세스 상태 출력
            Debug.Log($"[Card] IsProcessing: {InGameManager.Instance.IsProcessing}, CurrentProcess: {InGameManager.Instance.CurrentProcess}");
            Debug.Log($"[Card] IsInOperatorMode: {OperatorManager.Instance.IsInOperatorMode}");
            Debug.Log($"[Card] CurrentZoneType: {CurrentZoneType}, CardType: {CardType}");

            // 1. 조커 대상 선택 모드인 경우 항상 허용
            if (JokerTargetSelector.Instance != null && JokerTargetSelector.Instance.IsSelecting())
            {
                Debug.Log("[Card] 조커 대상 선택 모드 - 이벤트 허용");
                onClicked?.Invoke(this);
                return;
            }

            // 2. 연산자 모드 중인 경우 - 필드 카드만 허용 (프로세스 상태와 무관하게 체크)
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                Debug.Log("[Card] 연산자 모드 감지");

                // 손패의 카드는 연산 대상이 아니므로 클릭 이벤트 차단
                if (CurrentZoneType == CardZone.ZoneType.Hand)
                {
                    Debug.Log($"[Card] 연산자 모드 중 손패 카드 클릭 이벤트 차단: {gameObject.name}");
                    return; // UI 표시 완전 차단
                }

                // 필드 카드는 연산 대상이므로 허용
                Debug.Log("[Card] 연산자 모드 중 필드 카드 - 이벤트 허용");
                onClicked?.Invoke(this);
                return;
            }

            // 3. 공격 프로세스 중인지 확인 (새로 추가)
            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool hasAttackerSelected = attackManager != null && attackManager.HasAttackerSelected();

            if (hasAttackerSelected)
            {
                Debug.Log("[Card] 공격 프로세스 진행 중 감지");

                // 공격 프로세스 중에는 손패의 조커/연산자 카드 사용 차단
                if (CurrentZoneType == CardZone.ZoneType.Hand &&
                    CurrentOwnerType == CardZone.OwnerType.Player &&
                    (CardType == CardType.Joker || CardType == CardType.Operator))
                {
                    Debug.Log($"[Card] 공격 프로세스 중 {CardType} 카드 사용 차단: {gameObject.name}");
                    return;
                }

                // 상대 필드 카드 공격은 허용
                if (CurrentOwnerType == CardZone.OwnerType.Opponent && CurrentZoneType == CardZone.ZoneType.Field)
                {
                    Debug.Log("[Card] 공격 대상 선택 허용");
                    onClicked?.Invoke(this);
                    return;
                }

                // 기타 경우는 차단
                Debug.Log($"[Card] 공격 프로세스 중 기타 카드 클릭 차단: {gameObject.name}");
                return;
            }

            // 4. 기타 프로세스 진행 중이면 모든 새 프로세스 시작 차단
            if (InGameManager.Instance.IsProcessing)
            {
                Debug.Log($"[Card] 현재 {InGameManager.Instance.CurrentProcess} 진행 중이므로 모든 새 프로세스 차단");
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
                return; // 기본 onClicked 이벤트 방지
            }

            // 연산자 카드일 경우: OperatorManager 호출
            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.StartOperation(this);
                return; // 기본 onClicked 이벤트 방지
            }

            // 일반 카드 클릭 이벤트
            onClicked?.Invoke(this);
        }

        /// <summary>
        /// 카드가 Drag가 끝난 시점에 호출 (버그 수정 버전)
        /// 공격 프로세스 중 연산/조커 카드 드래그 차단
        /// </summary>
        private void HandleEndDrag()
        {
            Debug.Log($"[Card] EndDrag: {gameObject.name}");

            // 1. 연산자 프로세스 중인 경우
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                Debug.Log("[Card] 연산자 모드 중 드래그 차단");
                return; // 완전히 차단
            }

            // 2. 공격 프로세스 중인지 확인 (새로 추가)
            var attackManager = FindAnyObjectByType<FieldAttackManager>();
            bool hasAttackerSelected = attackManager != null && attackManager.HasAttackerSelected();

            if (hasAttackerSelected)
            {
                Debug.Log("[Card] 공격 프로세스 진행 중 - 드래그 차단");

                // 공격 프로세스 중에는 손패의 조커/연산자 카드 드래그 차단
                if (CurrentZoneType == CardZone.ZoneType.Hand &&
                    CurrentOwnerType == CardZone.OwnerType.Player &&
                    (CardType == CardType.Joker || CardType == CardType.Operator))
                {
                    Debug.Log($"[Card] 공격 프로세스 중 {CardType} 카드 드래그 차단: {gameObject.name}");
                    return;
                }
            }

            // 3. 프로세스 진행 중일 때는 새로운 프로세스 시작 차단
            if (InGameManager.Instance.IsProcessing)
            {
                Debug.Log("[Card] 프로세스 진행 중 드래그 차단");
                return; // 완전히 차단, 이벤트 발행 안 함
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

        /// <summary>
        /// 카드 삭제 애니메이션 (모든 카드 삭제 시 사용)
        /// </summary>
        /// <param name="onComplete">애니메이션 완료 후 실행할 콜백</param>
        /// <param name="delay">애니메이션 시작 전 대기 시간</param>
        public IEnumerator AnimateRemoval(Action onComplete = null, float delay = 0f)
        {
            // 대기 시간
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float animDuration = 0.5f; // Global에 정의되어 있다면 사용

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

        /// <summary>
        /// 카드 상호작용 종류를 정의하는 내부 열거형
        /// </summary>
        private enum CardInteractionType
        {
            None,
            ClickOnly,
            DragAndClick
        }
    }
}