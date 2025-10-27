using Manager;
using Objects;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Expression
{
    /// <summary>
    /// ExpressionZone 의 카드 표현을 담당하는 클래스
    /// - 숫자 또는 연산 기호 텍스트 표시
    /// - 카드 Sprite 설정
    /// - 텍스트 색상 및 표시 상태 관리
    /// - 취소 가능한 슬롯 클릭 이벤트 및 GLOW 효과
    /// </summary>
    public class ExpressionCard : MonoBehaviour
    {
        #region Events
        public static event Action<ExpressionCard> onClicked;
        #endregion

        #region Components
        private TextMeshPro cardText;
        private SpriteRenderer spriteRenderer;
        private CardEffect cardEffect;
        private Collider cardCollider; // 3D BoxCollider 용도
        #endregion

        #region Properties
        public int SlotIndex { get; private set; }
        public bool IsCancelable { get; private set; }
        public string CurrentText => cardText?.text ?? "";
        public bool IsActive => cardText != null && cardText.gameObject.activeSelf && !string.IsNullOrEmpty(CurrentText);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeComponents();
            SlotIndex = ExtractSlotIndexFromName();
        }

        private void Update()
        {
            // 마우스 클릭 감지
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                CheckForClick();
            }
        }
        #endregion

        #region Initialization
        private void InitializeComponents()
        {
            cardText = GetComponentInChildren<TextMeshPro>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            cardEffect = GetComponentInChildren<CardEffect>();

            // 콜라이더 확인 및 추가
            cardCollider = GetComponentInChildren<Collider>();
            if (cardCollider == null)
            {
                cardCollider = gameObject.AddComponent<BoxCollider>();
            }
        }

        private int ExtractSlotIndexFromName()
        {
            string name = gameObject.name;
            if (name.Contains("_"))
            {
                string[] parts = name.Split('_');
                if (parts.Length > 1 && int.TryParse(parts[1], out int index))
                {
                    return index - 1; // 1-based를 0-based로 변환
                }
            }
            return 0;
        }
        #endregion

        #region Click Detection
        private void CheckForClick()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreenPosition.x, mouseScreenPosition.y, Camera.main.nearClipPlane + 1f));

            // 3D BoxCollider를 위해 Z 좌표를 콜라이더 중심에 맞춤
            if (cardCollider != null)
            {
                mouseWorldPosition.z = cardCollider.bounds.center.z;

                // 이 카드의 콜라이더 영역에 마우스가 있는지 확인
                if (cardCollider.bounds.Contains(mouseWorldPosition))
                {
                    HandleClick();
                }
            }
        }

        private void HandleClick()
        {
            // 취소 가능한 상태가 아니면 무시
            if (!IsCancelable)
                return;

            // 이벤트 발생
            onClicked?.Invoke(this);
        }
        #endregion

        #region Display Functions
        /// <summary>
        /// 숫자나 값 텍스트를 설정합니다.
        /// </summary>
        public void SetValue(string value)
        {
            if (cardText != null)
                cardText.text = value;
        }

        /// <summary>
        /// 연산 기호 또는 기호를 설정합니다.
        /// </summary>
        public void SetSymbol(string symbol)
        {
            if (cardText != null)
                cardText.text = symbol;
        }

        /// <summary>
        /// 플레이어 또는 상대에 따라 Sprite를 설정하고, 텍스트 색상 반영합니다.
        /// </summary>
        public void SetSprite(CardZone.OwnerType owner)
        {
            if (spriteRenderer == null) return;

            Sprite sprite = owner switch
            {
                CardZone.OwnerType.Player => ResourcesManager.Instance.GetPlayerSprite(),
                CardZone.OwnerType.Opponent => ResourcesManager.Instance.GetOpponentSprite(),
                _ => null
            };

            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                SetTextColor(Global.GetColorByName(sprite.name));
            }
        }

        /// <summary>
        /// Sprite를 직접 설정하고 텍스트 색상 자동 적용합니다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer == null || sprite == null) return;
            spriteRenderer.sprite = sprite;
            SetTextColor(Global.GetColorByName(sprite.name));
        }

        /// <summary>
        /// 텍스트 색상을 설정합니다.
        /// </summary>
        public void SetTextColor(Color color)
        {
            if (cardText != null)
                cardText.color = color;
        }

        /// <summary>
        /// 텍스트 표시 상태를 설정합니다.
        /// </summary>
        public void SetTextVisible(bool visible)
        {
            if (cardText != null)
                cardText.gameObject.SetActive(visible);
        }
        #endregion

        #region Cancellation & GLOW Control
        /// <summary>
        /// 취소 가능 상태 설정 및 GLOW 효과 적용
        /// </summary>
        public void SetCancelable(bool cancelable)
        {
            IsCancelable = cancelable;

            if (cardEffect != null)
            {
                if (cancelable)
                {
                    cardEffect.SetGlow(true);
                    cardEffect.LerpGlowColor(Color.cyan, 0.3f);
                }
                else
                {
                    cardEffect.SetGlow(false);
                }
            }
        }

        /// <summary>
        /// GLOW 효과 해제
        /// </summary>
        public void ClearGlow()
        {
            IsCancelable = false;
            if (cardEffect != null)
                cardEffect.SetGlow(false);
        }

        /// <summary>
        /// 현재 GLOW 상태 확인
        /// </summary>
        public bool IsGlowing()
        {
            return cardEffect != null && cardEffect.IsGlowing();
        }
        #endregion

        #region Utility
        public string GetDebugInfo()
        {
            return $"Slot[{SlotIndex}] Text:'{CurrentText}' Active:{IsActive} Cancelable:{IsCancelable}";
        }
        #endregion
    }
}
