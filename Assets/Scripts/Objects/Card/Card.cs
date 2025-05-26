using Manager;
using System;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 개별 카드 오브젝트의 상태 및 클릭 반응을 관리하는 컴포넌트
    /// - ICard 구현을 통해 Zone에서 인터랙션 설정을 받을 수 있음
    /// - ObjectMouseEvent로부터 클릭 이벤트를 수신함
    /// </summary>

    public enum CardType { Number, Operator, Joker }
    public enum OperatorType { Plus, Minus, Multiply, Divide }

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
        public void InitializeAsNumber(long value)
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
        /// 클릭 시 실행되는 내부 로직
        /// </summary>
        private void HandleClick()
        {
            Debug.Log($"[Card] Clicked: {gameObject.name}");

            // 연산자 카드일 경우: 일반 클릭 이벤트 대신 OperatorManager 호출
            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.EnterOperatorMode(this);
                return; // 기본 onClicked 이벤트 방지
            }

            onClicked?.Invoke(this);
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
        /// 카드가 Drag가 끝난 시점에 호출
        /// </summary>
        private void HandleEndDrag()
        {
            // 연산 중이면 드롭 처리 무시
            if (OperatorManager.Instance.IsInOperatorMode)
            {
                Debug.Log("[Card] 연산 중이므로 드래그 무시");
                return;
            }

            // 연산자 카드일 경우: 드롭 시 OperatorManager 호출
            if (CardType == CardType.Operator &&
                CurrentZoneType == CardZone.ZoneType.Hand &&
                CurrentOwnerType == CardZone.OwnerType.Player)
            {
                OperatorManager.Instance.EnterOperatorMode(this);
                return;
            }

            OnCardDropped?.Invoke(transform); // Detector가 이걸 받아 처리
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
