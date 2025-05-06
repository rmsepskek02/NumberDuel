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
    public class Card : MonoBehaviour, ICard
    {
        private TextMeshPro cardText;
        private SpriteRenderer spriteRenderer;

        public static event Action<Card> onClicked; // 외부에서 구독 가능한 카드 클릭 이벤트
        public static event Action<Transform> OnCardDropped; // 카드가 드래그에서 해제됐을 때 알림

        public CardZone.ZoneType CurrentZoneType { get; private set; }
        public CardZone.OwnerType CurrentOwnerType { get; private set; }
        public bool IsSecret { get; private set; }
        public bool CanAttack { get; private set; } = false;
        public bool WasModifiedThisTurn { get; private set; } = false;
        public bool IsOpen => !IsSecret;

        private ObjectMouseEvent mouseEvent;

        private static readonly string SecretSpriteName = "color_back 1_0";

        private void Awake()
        {
            mouseEvent = GetComponentInChildren<ObjectMouseEvent>();
            cardText = GetComponentInChildren<TextMeshPro>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
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

        /// <summary>
        /// 카드를 비밀 상태로 설정하거나 해제합니다.
        /// </summary>
        public void SetSecret(bool isSecret)
        {
            IsSecret = isSecret;

            if (cardText != null)
                cardText.gameObject.SetActive(!isSecret);

            if (spriteRenderer != null)
            {
                if (isSecret)
                {
                    var secretSprite = ResourcesManager.Instance.GetSprite(Global.Card, SecretSpriteName);
                    if (secretSprite != null)
                        spriteRenderer.sprite = secretSprite;
                    else
                        Debug.LogWarning($"[Card] Secret Sprite '{SecretSpriteName}' not found.");
                }
                else
                {
                    // 원래 Sprite로 되돌릴 로직이 필요하면 여기에 작성
                    spriteRenderer.sprite = ResourcesManager.Instance.GetPlayerSprite();
                }
            }
        }

        public void SetCanAttack(bool canAttack)
        {
            CanAttack = canAttack;

            var effect = GetComponentInChildren<CardEffect>();
            if (effect != null)
            {
                effect.SetGlow(canAttack); // Glow 켜기/끄기

                // 색상 설정 (기본: 내 카드 = 연두색)
                if (canAttack)
                {
                    Color glowColor = CurrentOwnerType == CardZone.OwnerType.Player
                        ? Global.GlowGreen
                        : Global.GlowRed;

                    effect.LerpGlowColor(glowColor, 0.2f); // 부드럽게 색 변경
                }
            }
        }

        public void SetWasModifiedThisTurn(bool modified)
        {
            WasModifiedThisTurn = modified;
            if (modified) SetCanAttack(false);
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
