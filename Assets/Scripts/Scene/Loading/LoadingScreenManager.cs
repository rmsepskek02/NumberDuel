using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utills;
using TMPro;

namespace Manager
{
    /// <summary>
    /// 콘솔 로딩 매니저 (Photon 분리)
    /// - 로컬 씬 로딩은 ShowThenLoadLocal
    /// - 외부 액션(예: PhotonNetwork.LoadLevel)을 호출하는 FadeInThenAction
    /// - 씬 로딩 완료 시 페이드아웃
    /// </summary>
    public class LoadingScreenManager : SingletonDontDestroy<LoadingScreenManager>
    {
        private const float FIXED_FADE_SECONDS = 1f;
        private const float FIXED_PROGRESS_SECONDS = 1f;

        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 1.0f);
        [SerializeField] private Color progressColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Vector2 progressBarSize = new Vector2(600f, 22f);
        [SerializeField] private TMP_FontAsset tmpFont;

        private Canvas loadingCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform progressBg;
        private RectTransform progressFill;
        private TextMeshProUGUI percentText;

        private bool isShowing = false;
        private bool isCancelled = false; // 매칭 취소 등으로 인한 취소 플래그

        protected override void Awake()
        {
            base.Awake();
            CreateUIIfMissing();
            HideImmediate();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // 1) 로컬 씬 로딩(로컬 전용)
        public void ShowThenLoadLocal(string sceneName)
        {
            if (isShowing) return;
            StartCoroutine(FadeInThenLoadLocalRoutine(sceneName));
        }

        private IEnumerator FadeInThenLoadLocalRoutine(string sceneName)
        {
            isShowing = true;
            CreateUIIfMissing();

            UpdateProgressUI(0f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var fade = StartCoroutine(FadeCanvas(0f, 1f, FIXED_FADE_SECONDS));
            var prog = StartCoroutine(AnimateProgressOverSeconds(FIXED_PROGRESS_SECONDS));
            yield return fade;
            yield return prog;

            // 씬 로딩
            SceneManager.LoadScene(sceneName);
            // OnSceneLoaded에서 페이드아웃 처리
        }

        // 2) 페이드인 후 외부 액션 호출 (PhotonNetwork.LoadLevel 같은 네트워크 호출을 여기서 처리)
        public void FadeInThenAction(Action onFadeInComplete)
        {
            if (isShowing) return;
            StartCoroutine(FadeInThenActionRoutine(onFadeInComplete));
        }

        private IEnumerator FadeInThenActionRoutine(Action onFadeInComplete)
        {
            isShowing = true;
            CreateUIIfMissing();

            UpdateProgressUI(0f);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var fade = StartCoroutine(FadeCanvas(0f, 1f, FIXED_FADE_SECONDS));
            var prog = StartCoroutine(AnimateProgressOverSeconds(FIXED_PROGRESS_SECONDS));
            yield return fade;
            yield return prog;

            onFadeInComplete?.Invoke();
            // 씬 로딩은 호출자(onFadeInComplete)에서 처리 -> OnSceneLoaded에서 페이드아웃 처리
        }

        // 페이드 인 후 다음으로 씬이 로드되면(로컬 또는 네트워크) 자동으로 페이드아웃
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 취소된 경우 무시
            if (isCancelled)
            {
                isCancelled = false;
                return;
            }

            if (!isShowing) return;

            // 씬이 로드되면 바로 페이드아웃
            UpdateProgressUI(1f);
            StartCoroutine(FadeOutAndHide());
        }

        private IEnumerator FadeOutAndHide()
        {
            yield return new WaitForSecondsRealtime(0.05f);
            yield return StartCoroutine(FadeCanvas(1f, 0f, FIXED_FADE_SECONDS));

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            isShowing = false;
            UpdateProgressUI(0f);
        }

        /// <summary>
        /// 로딩 화면 취소 (매칭 취소 등)
        /// 진행 중인 페이드 인/아웃을 즉시 중단하고 화면을 즉시 숨김 (애니메이션 없음)
        /// </summary>
        public void CancelLoading()
        {
            // 모든 코루틴 중단
            StopAllCoroutines();

            // 취소 플래그 설정
            isCancelled = true;

            // 즉시 숨김 (애니메이션 없음)
            HideImmediate();
        }

        #region UI 생성/관리 (런타임 코드 생성)
        private void CreateUIIfMissing()
        {
            if (loadingCanvas != null) return;

            if (tmpFont == null)
            {
                tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            GameObject canvasGO = new GameObject("LoadingCanvas");
            DontDestroyOnLoad(canvasGO);
            loadingCanvas = canvasGO.AddComponent<Canvas>();
            loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            loadingCanvas.sortingOrder = 10000;
            canvasGroup = canvasGO.AddComponent<CanvasGroup>();

            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Overlay
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvasGO.transform, false);
            var overlayRT = overlay.AddComponent<RectTransform>();
            overlayRT.anchorMin = Vector2.zero;
            overlayRT.anchorMax = Vector2.one;
            overlayRT.offsetMin = Vector2.zero;
            overlayRT.offsetMax = Vector2.zero;
            var overlayImage = overlay.AddComponent<UnityEngine.UI.Image>();
            overlayImage.color = overlayColor;

            // Progress background
            GameObject bg = new GameObject("ProgressBackground");
            bg.transform.SetParent(canvasGO.transform, false);
            progressBg = bg.AddComponent<RectTransform>();
            progressBg.sizeDelta = progressBarSize;
            progressBg.anchorMin = new Vector2(0.5f, 0.12f);
            progressBg.anchorMax = progressBg.anchorMin;
            progressBg.pivot = new Vector2(0.5f, 0.5f);
            progressBg.anchoredPosition = Vector2.zero;
            var bgImage = bg.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.08f);

            // Progress fill
            GameObject fill = new GameObject("ProgressFill");
            fill.transform.SetParent(progressBg, false);
            progressFill = fill.AddComponent<RectTransform>();
            progressFill.pivot = new Vector2(0f, 0.5f);
            progressFill.anchorMin = new Vector2(0f, 0f);
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.anchoredPosition = Vector2.zero;
            progressFill.sizeDelta = new Vector2(0f, 0f);
            var fillImg = fill.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = progressColor;

            // Percent text
            GameObject percentGO = new GameObject("PercentText");
            percentGO.transform.SetParent(canvasGO.transform, false);
            var percentRT = percentGO.AddComponent<RectTransform>();
            percentRT.anchorMin = new Vector2(0.5f, 0.2f);
            percentRT.anchorMax = percentRT.anchorMin;
            percentRT.pivot = new Vector2(0.5f, 0.5f);
            percentRT.anchoredPosition = Vector2.zero;
            percentRT.sizeDelta = new Vector2(200f, 30f);
            percentText = percentGO.AddComponent<TextMeshProUGUI>();
            percentText.alignment = TextAlignmentOptions.Center;
            percentText.fontSize = 20;
            percentText.color = Color.white;
            if (tmpFont != null) percentText.font = tmpFont;
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            UpdateProgressUI(0f);
            isShowing = false;
        }

        private IEnumerator AnimateProgressOverSeconds(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(t / seconds);
                UpdateProgressUI(normalized);
                yield return null;
            }
            UpdateProgressUI(1f);
        }

        private void UpdateProgressUI(float normalized)
        {
            if (progressBg == null || progressFill == null || percentText == null) return;
            float totalWidth = progressBg.sizeDelta.x;
            progressFill.sizeDelta = new Vector2(totalWidth * Mathf.Clamp01(normalized), 0f);
            percentText.text = $"{Mathf.RoundToInt(normalized * 100f)}%";
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;
            float t = 0f;
            canvasGroup.alpha = from;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, duration <= 0f ? 1f : (t / duration));
                yield return null;
            }
            canvasGroup.alpha = to;
            canvasGroup.interactable = to > 0.5f;
            canvasGroup.blocksRaycasts = to > 0.5f;
        }
        #endregion
    }
}