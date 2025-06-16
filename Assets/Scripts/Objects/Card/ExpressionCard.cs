using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Manager;
using Objects;
using System;

namespace Expression
{
    /// <summary>
    /// ExpressionZone 내 개별 카드 표현을 담당하는 클래스
    /// - 숫자 또는 연산 기호 텍스트 표시
    /// - 배경 Sprite 설정
    /// - 텍스트 색상 및 표시 여부 제어
    /// - 취소 기능을 위한 클릭 감지 및 GLOW 효과
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
        private Collider cardCollider; // 3D BoxCollider 사용
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
                Debug.Log($"[ExpressionCard] {gameObject.name}에 BoxCollider2D 추가됨");
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

            // 3D BoxCollider의 경우 Z 좌표를 콜라이더 중심에 맞춤
            if (cardCollider != null)
            {
                mouseWorldPosition.z = cardCollider.bounds.center.z;

                // 디버깅 정보
                if (IsCancelable) // 취소 가능한 상태일 때만 디버깅 로그
                {
                    Debug.Log($"[ExpressionCard] {gameObject.name} 마우스 체크 - Mouse: {mouseWorldPosition}, Bounds: {cardCollider.bounds}");
                }

                // 이 카드의 콜라이더 영역에 마우스가 있는지 확인
                if (cardCollider.bounds.Contains(mouseWorldPosition))
                {
                    HandleClick();
                }
            }
        }

        private void HandleClick()
        {
            Debug.Log($"[ExpressionCard] {gameObject.name} 클릭 감지! - Slot: {SlotIndex}, Cancelable: {IsCancelable}");

            // 취소 가능한 상태가 아니면 무시
            if (!IsCancelable)
            {
                Debug.Log($"[ExpressionCard] {gameObject.name} 취소 불가능한 상태");
                return;
            }

            // 이벤트 발생
            Debug.Log($"[ExpressionCard] {gameObject.name} 취소 이벤트 발생!");
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
        /// 연산 기호 또는 등호를 설정합니다.
        /// </summary>
        public void SetSymbol(string symbol)
        {
            if (cardText != null)
                cardText.text = symbol;
        }

        /// <summary>
        /// 플레이어 또는 상대에 따라 Sprite를 설정하고, 텍스트 색상도 함께 반영합니다.
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
        /// Sprite를 직접 지정하고 텍스트 색상도 자동 적용합니다.
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (spriteRenderer == null || sprite == null) return;
            spriteRenderer.sprite = sprite;
            SetTextColor(Global.GetColorByName(sprite.name));
        }

        /// <summary>
        /// 텍스트 색상을 지정합니다.
        /// </summary>
        public void SetTextColor(Color color)
        {
            if (cardText != null)
                cardText.color = color;
        }

        /// <summary>
        /// 텍스트 표시 여부를 제어합니다.
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
                    Debug.Log($"[ExpressionCard] {gameObject.name} 취소 가능 상태 - 시안 GLOW 적용");
                }
                else
                {
                    cardEffect.SetGlow(false);
                }
            }
            else
            {
                Debug.LogWarning($"[ExpressionCard] {gameObject.name} CardEffect 컴포넌트가 없습니다.");
            }
        }

        /// <summary>
        /// GLOW 효과 제거
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