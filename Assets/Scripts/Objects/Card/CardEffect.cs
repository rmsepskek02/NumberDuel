using System.Collections;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드에 Glow 효과를 부여하고, MaterialPropertyBlock으로 값 제어
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CardEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private MaterialPropertyBlock block;
        private Coroutine glowRoutine;
        private Coroutine colorRoutine;

        [SerializeField] private string glowProperty = "_GlowToggle";
        [SerializeField] private string outlineColorProperty = "_OutlineColor";
        [SerializeField] private float fadeDuration = 0.3f;

        private float currentGlow = 0f;
        private Color currentColor;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            block = new MaterialPropertyBlock();
            sr.GetPropertyBlock(block);

            // 초기화 상태 적용
            currentGlow = 0f;
            block.SetFloat(glowProperty, currentGlow);
            currentColor = sr.sharedMaterial.GetColor(outlineColorProperty); // 초기값 추출
            block.SetColor(outlineColorProperty, currentColor);

            sr.SetPropertyBlock(block);
        }

        public void SetGlow(bool enable)
        {
            float target = enable ? 1f : 0f;

            if (glowRoutine != null)
                StopCoroutine(glowRoutine);

            glowRoutine = StartCoroutine(GlowLerpRoutine(target));
        }

        public bool IsGlowing() => currentGlow > 0.5f;

        private IEnumerator GlowLerpRoutine(float target)
        {
            float start = currentGlow;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                currentGlow = Mathf.Lerp(start, target, elapsed / fadeDuration);
                block.SetFloat(glowProperty, currentGlow);
                sr.SetPropertyBlock(block);
                yield return null;
            }

            currentGlow = target;
            block.SetFloat(glowProperty, currentGlow);
            sr.SetPropertyBlock(block);
        }

        public void LerpGlowColor(Color targetColor, float duration)
        {
            if (colorRoutine != null)
                StopCoroutine(colorRoutine);

            colorRoutine = StartCoroutine(ColorLerpRoutine(currentColor, targetColor, duration));
        }

        private IEnumerator ColorLerpRoutine(Color from, Color to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Color current = Color.Lerp(from, to, elapsed / duration);
                block.SetColor(outlineColorProperty, current);
                sr.SetPropertyBlock(block);
                yield return null;
            }

            currentColor = to;
            block.SetColor(outlineColorProperty, currentColor);
            sr.SetPropertyBlock(block);
        }
    }
}