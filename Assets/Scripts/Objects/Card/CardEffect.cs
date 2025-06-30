using System.Collections;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드에 Glow 효과를 부여하고, MaterialPropertyBlock으로 값 제어
    /// 최적화된 버전 - 필요할 때만 업데이트
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

        // 최적화용 캐시
        private Texture2D lastTexture;
        private bool needsUpdate = true;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            block = new MaterialPropertyBlock();

            // 초기화
            currentGlow = 0f;
            currentColor = Color.white;
            lastTexture = sr.sprite?.texture;

            // 초기 설정
            UpdatePropertyBlockIfNeeded(true); // 강제 업데이트
        }

        /// <summary>
        /// 필요할 때만 MaterialPropertyBlock 업데이트 (최적화)
        /// </summary>
        private void UpdatePropertyBlockIfNeeded(bool forceUpdate = false)
        {
            // 텍스처가 바뀌었는지 확인
            Texture2D currentTexture = sr.sprite?.texture;
            bool textureChanged = currentTexture != lastTexture;

            // 업데이트가 필요한 경우만 실행
            if (forceUpdate || needsUpdate || textureChanged)
            {
                // _MainTex 설정 (텍스처가 바뀌었을 때만)
                if (textureChanged || forceUpdate)
                {
                    if (currentTexture != null)
                    {
                        block.SetTexture("_MainTex", currentTexture);
                        lastTexture = currentTexture;
                    }
                }

                // GLOW 프로퍼티 설정
                block.SetFloat(glowProperty, currentGlow);
                block.SetColor(outlineColorProperty, currentColor);

                // GPU에 적용
                sr.SetPropertyBlock(block);

                needsUpdate = false;
            }
        }

        public void SetGlow(bool enable)
        {
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[CardEffect] {gameObject.name} is inactive. Cannot start Coroutine.");
                return;
            }

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
                float newGlow = Mathf.Lerp(start, target, elapsed / fadeDuration);

                // 값이 실제로 바뀌었을 때만 업데이트
                if (Mathf.Abs(newGlow - currentGlow) > 0.001f)
                {
                    currentGlow = newGlow;
                    needsUpdate = true;
                    UpdatePropertyBlockIfNeeded();
                }

                yield return null;
            }

            // 최종값 설정
            if (Mathf.Abs(target - currentGlow) > 0.001f)
            {
                currentGlow = target;
                needsUpdate = true;
                UpdatePropertyBlockIfNeeded();
            }
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
                Color newColor = Color.Lerp(from, to, elapsed / duration);

                // 색상이 실제로 바뀌었을 때만 업데이트
                if (Vector4.Distance(newColor, currentColor) > 0.001f)
                {
                    currentColor = newColor;
                    needsUpdate = true;
                    UpdatePropertyBlockIfNeeded();
                }

                yield return null;
            }

            // 최종 색상 설정
            if (Vector4.Distance(to, currentColor) > 0.001f)
            {
                currentColor = to;
                needsUpdate = true;
                UpdatePropertyBlockIfNeeded();
            }
        }

        /// <summary>
        /// 외부에서 스프라이트가 변경되었을 때 호출 (선택사항)
        /// </summary>
        public void OnSpriteChanged()
        {
            needsUpdate = true;
            UpdatePropertyBlockIfNeeded();
        }
    }
}