using TMPro;
using UnityEngine;
using Manager;
using Objects;

namespace Expression
{
    /// <summary>
    /// ExpressionZone 내 개별 카드 표현을 담당하는 클래스
    /// - 숫자 또는 연산 기호 텍스트 표시
    /// - 배경 Sprite 설정
    /// - 텍스트 색상 및 표시 여부 제어
    /// </summary>
    public class ExpressionCard : MonoBehaviour
    {
        private TextMeshPro cardText;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            cardText = GetComponentInChildren<TextMeshPro>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

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
    }
}
