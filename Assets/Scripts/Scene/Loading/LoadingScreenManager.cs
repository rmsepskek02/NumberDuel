using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utills;
using TMPro;

namespace Manager
{
    /// <summary>
    /// 로딩 화면 상태
    /// </summary>
    public enum LoadingState
    {
        Hidden,      // 완전히 숨김 (alpha=0)
        FadingIn,    // 페이드인 진행 중
        Visible,     // 완전히 표시됨 (alpha=1, 로딩바 진행 가능)
        FadingOut    // 페이드아웃 진행 중
    }

    /// <summary>
    /// 로딩 화면 매니저 (상태 머신 + 전략 패턴)
    ///
    /// ■ 상태 머신: Hidden → FadingIn → Visible → FadingOut → Hidden
    /// ■ 전략 패턴: AutoFadeOut / ManualControl / Conditional
    ///
    /// [기본 사용법]
    /// 1. 씬 전환 (자동 페이드아웃): ShowThenLoadLocal(sceneName)
    /// 2. 수동 제어 (Photon 연결 추적):
    ///    - ShowManual("연결 중...")
    ///    - UpdateProgress(0.5f, "인증 중...")
    ///    - FadeOutManually()
    ///
    /// [고급 사용법]
    /// 3. 수동 제어 모드 전환: SetManualControl()
    /// 4. 긴급 복구: ForceHide()
    /// 5. 사용자 취소: CancelLoading()
    /// </summary>
    public class LoadingScreenManager : SingletonDontDestroy<LoadingScreenManager>
    {
        private const float FIXED_FADE_SECONDS = 1f;
        private const float FIXED_PROGRESS_SECONDS = 1f;
        private const float LOADING_TEXT_ANIMATION_INTERVAL = 0.5f;

        [Header("UI Settings")]
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 1.0f);
        [SerializeField] private Color progressColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Vector2 progressBarSize = new Vector2(600f, 22f);

        [Header("Font")]
        [Tooltip("Assets/Model/Fonts/Maplestory Bold SDF_OutLine 폰트를 할당하세요")]
        [SerializeField] private TMP_FontAsset tmpFont;

        private Canvas loadingCanvas;
        private CanvasGroup canvasGroup;
        private RectTransform progressBg;
        private RectTransform progressFill;
        private TextMeshProUGUI percentText;
        private TextMeshProUGUI statusText; // 상태 텍스트 UI (예: "서버 연결 중...")

        // 상태 머신
        private LoadingState currentState = LoadingState.Hidden;

        // 전략 패턴
        private ILoadingStrategy currentStrategy = new AutoFadeOutStrategy();

        // 취소 플래그 (사용자 명시적 취소 시에만 사용)
        // CancelLoading() 호출 시 true로 설정되며, OnSceneLoaded에서 페이드아웃 스킵용
        // 일시적 플래그로, OnSceneLoaded 또는 HideImmediate에서 false로 리셋됨
        private bool isCancelled = false;

        /// <summary>
        /// 현재 로딩 화면 상태 (읽기 전용)
        /// </summary>
        public LoadingState CurrentState => currentState;

        /// <summary>
        /// 로딩 화면이 표시 중인지 (Visible 또는 FadingIn/Out 상태)
        /// </summary>
        public bool IsShowing => currentState != LoadingState.Hidden;

        #region Unity Lifecycle
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

        protected override void OnDestroy()
        {
            // SceneManager 이벤트 해제 (안전장치)
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // 동적으로 생성한 LoadingCanvas GameObject 파괴
            // OnDestroy 중에는 Destroy()가 아닌 DestroyImmediate() 사용 필요
            if (loadingCanvas != null)
            {
                DestroyImmediate(loadingCanvas.gameObject);
                loadingCanvas = null;
            }

            // 베이스 클래스의 OnDestroy 호출 (싱글톤 인스턴스 정리)
            base.OnDestroy();
        }
        #endregion

        #region State Machine
        /// <summary>
        /// 상태 전환 가능 여부 확인
        /// </summary>
        private bool CanTransitionTo(LoadingState newState)
        {
            switch (currentState)
            {
                case LoadingState.Hidden:
                    return newState == LoadingState.FadingIn;

                case LoadingState.FadingIn:
                    return newState == LoadingState.Visible;

                case LoadingState.Visible:
                    return newState == LoadingState.FadingOut;

                case LoadingState.FadingOut:
                    return newState == LoadingState.Hidden;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 상태 전환 (안전하게)
        /// </summary>
        private void TransitionTo(LoadingState newState)
        {
            if (!CanTransitionTo(newState))
            {
                Debug.LogError($"[LoadingScreen] 잘못된 상태 전환: {currentState} → {newState}");
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"[LoadingScreen] State: {currentState} → {newState} (Scene: {sceneName})");
            currentState = newState;
        }
        #endregion

        #region Public API
        /// <summary>
        /// 로딩 화면 표시 후 로컬 씬 로드 (자동 페이드아웃)
        /// 전략: AutoFadeOutStrategy
        /// 흐름: Hidden → FadingIn → Visible → [씬 로드] → OnSceneLoaded → FadingOut → Hidden
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름</param>
        public void ShowThenLoadLocal(string sceneName)
        {
            // Hidden 상태일 때만 시작 가능
            if (currentState != LoadingState.Hidden)
            {
                return; // 이미 진행 중이면 무시
            }

            // 자동 페이드아웃 전략 설정
            currentStrategy = new AutoFadeOutStrategy();

            StartCoroutine(FadeInThenLoadLocalRoutine(sceneName));
        }

        private IEnumerator FadeInThenLoadLocalRoutine(string sceneName)
        {
            // State: Hidden → FadingIn
            TransitionTo(LoadingState.FadingIn);
            CreateUIIfMissing();

            UpdateProgressUI(0f);
            UpdateStatusText("로딩 중"); // 기본 로딩 텍스트로 초기화
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var fade = StartCoroutine(FadeCanvas(0f, 1f, FIXED_FADE_SECONDS));
            var prog = StartCoroutine(AnimateProgressOverSeconds(FIXED_PROGRESS_SECONDS));
            var loadingAnim = StartCoroutine(AnimateLoadingText());
            yield return fade;
            yield return prog;

            // State: FadingIn → Visible
            TransitionTo(LoadingState.Visible);

            // 씬 로딩
            SceneManager.LoadScene(sceneName);
            // OnSceneLoaded에서 페이드아웃 처리
        }

        /// <summary>
        /// 로딩 화면 표시 후 외부 액션 실행
        /// 전략: 호출자가 설정 (보통 AutoFadeOutStrategy)
        /// 사용 사례: PhotonNetwork.LoadLevel() 같은 네트워크 씬 로드
        /// 흐름: Hidden → FadingIn → Visible → [onFadeInComplete 실행] → [씬 로드] → OnSceneLoaded → FadingOut → Hidden
        /// </summary>
        /// <param name="onFadeInComplete">페이드인 완료 후 실행할 액션 (씬 로드 등)</param>
        public void FadeInThenAction(Action onFadeInComplete)
        {
            // Hidden 상태일 때만 시작 가능
            if (currentState != LoadingState.Hidden)
            {
                return; // 이미 진행 중이면 무시
            }

            // 자동 페이드아웃 전략 설정
            currentStrategy = new AutoFadeOutStrategy();

            StartCoroutine(FadeInThenActionRoutine(onFadeInComplete));
        }

        private IEnumerator FadeInThenActionRoutine(Action onFadeInComplete)
        {
            // State: Hidden → FadingIn
            TransitionTo(LoadingState.FadingIn);
            CreateUIIfMissing();

            UpdateProgressUI(0f);
            UpdateStatusText("로딩 중");
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            var fade = StartCoroutine(FadeCanvas(0f, 1f, FIXED_FADE_SECONDS));
            var prog = StartCoroutine(AnimateProgressOverSeconds(FIXED_PROGRESS_SECONDS));
            var loadingAnim = StartCoroutine(AnimateLoadingText());
            yield return fade;
            yield return prog;

            // State: FadingIn → Visible
            TransitionTo(LoadingState.Visible);

            onFadeInComplete?.Invoke();
            // 씬 로딩은 호출자(onFadeInComplete)에서 처리 -> OnSceneLoaded에서 페이드아웃 처리
        }

        /// <summary>
        /// 씬 로드 완료 콜백 (Unity 자동 호출)
        /// 로딩 화면 전략에 따라 자동 페이드아웃 또는 수동 제어 대기
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 1. 취소 플래그 체크 (CancelLoading 호출된 경우)
            if (isCancelled)
            {
                isCancelled = false; // 플래그 리셋
                return;
            }

            // 2. 상태 체크 (Visible 상태가 아니면 아무것도 안 함)
            if (currentState != LoadingState.Visible)
            {
                return;
            }

            // 3. 전략 패턴: 자동 페이드아웃 여부 결정
            if (currentStrategy.ShouldAutoFadeOut(scene))
            {
                UpdateProgressUI(1f);
                StartCoroutine(FadeOutAndHide());
            }
        }

        private IEnumerator FadeOutAndHide()
        {
            // State: Visible → FadingOut
            TransitionTo(LoadingState.FadingOut);

            yield return new WaitForSecondsRealtime(0.05f);
            yield return StartCoroutine(FadeCanvas(1f, 0f, FIXED_FADE_SECONDS));

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            UpdateProgressUI(0f);
            UpdateStatusText(""); // 상태 텍스트 초기화

            // State: FadingOut → Hidden
            TransitionTo(LoadingState.Hidden);

            // 전략 리셋 (다음 사용을 위해 기본 전략으로)
            currentStrategy = new AutoFadeOutStrategy();
        }

        /// <summary>
        /// 로딩 화면 취소 (사용자 요청 취소)
        /// 진행 중인 페이드 인/아웃을 즉시 중단하고 화면을 즉시 숨김 (애니메이션 없음)
        /// 사용 사례:
        ///   - 매칭 취소 버튼 클릭
        ///   - 사용자가 명시적으로 로딩 중단 요청
        /// OnSceneLoaded에서 이 취소를 감지하여 페이드아웃 스킵
        /// </summary>
        public void CancelLoading()
        {
            // 모든 코루틴 중단
            StopAllCoroutines();

            // 취소 플래그 설정 (OnSceneLoaded에서 페이드아웃 스킵용)
            isCancelled = true;

            // 즉시 숨김 (애니메이션 없음)
            HideImmediate();
        }

        #region Manual Control Methods
        /// <summary>
        /// 로딩 화면을 수동으로 표시 (수동 제어 모드)
        /// 전략: ManualControlStrategy
        /// 흐름: Hidden → FadingIn → Visible → [UpdateProgress 반복] → [FadeOutManually 호출] → FadingOut → Hidden
        /// 사용 사례:
        ///   - JoinScene: Photon 연결 상태 추적
        ///   - LobbyScene: 재연결 확인
        /// </summary>
        /// <param name="initialStatusText">초기 상태 텍스트 (예: "서버 연결 중...")</param>
        public void ShowManual(string initialStatusText = "")
        {
            // Hidden 상태일 때만 시작 가능
            if (currentState != LoadingState.Hidden)
            {
                return; // 이미 진행 중이면 무시
            }

            // 수동 제어 전략 설정
            currentStrategy = new ManualControlStrategy();

            StartCoroutine(ShowManualRoutine(initialStatusText));
        }

        private IEnumerator ShowManualRoutine(string initialStatusText)
        {
            // State: Hidden → FadingIn
            TransitionTo(LoadingState.FadingIn);
            CreateUIIfMissing();

            UpdateProgressUI(0f);
            UpdateStatusText(initialStatusText);

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 페이드인만 실행
            yield return StartCoroutine(FadeCanvas(0f, 1f, FIXED_FADE_SECONDS));

            // State: FadingIn → Visible
            TransitionTo(LoadingState.Visible);
        }

        /// <summary>
        /// 진행률과 상태 텍스트를 수동으로 업데이트
        /// ShowManual() 이후에 반복 호출하여 실시간 진행 상태 표시
        /// </summary>
        /// <param name="normalizedProgress">진행률 (0.0 ~ 1.0)</param>
        /// <param name="statusText">상태 텍스트 (예: "인증 중...", "로비 연결 중...")</param>
        public void UpdateProgress(float normalizedProgress, string statusText = "")
        {
            // FadingIn 또는 Visible 상태일 때만 업데이트 가능
            if (currentState != LoadingState.FadingIn && currentState != LoadingState.Visible)
            {
                return; // 경고 없이 무시 (정상 동작)
            }

            UpdateProgressUI(normalizedProgress);
            if (!string.IsNullOrEmpty(statusText))
            {
                UpdateStatusText(statusText);
            }
        }

        /// <summary>
        /// 수동으로 페이드아웃 실행 (수동 제어 모드 종료)
        /// ShowManual() → UpdateProgress() 이후 작업 완료 시 호출
        /// 사용 사례:
        ///   - JoinScene: Photon 로비 입장 완료 후
        ///   - LobbyScene: 재연결 확인 완료 후
        /// </summary>
        public void FadeOutManually()
        {
            // Visible 상태일 때만 페이드아웃 가능
            if (currentState != LoadingState.Visible)
            {
                return; // 경고 없이 무시 (정상 동작)
            }

            UpdateProgressUI(1f);
            StartCoroutine(FadeOutAndHide());
        }

        /// <summary>
        /// 수동 제어 모드로 전환
        /// 씬 로드 후 자동 페이드아웃하지 않고, FadeOutManually() 호출 대기
        /// 사용 사례: LobbyScene에서 Photon 재연결 확인 후 수동 페이드아웃
        /// </summary>
        public void SetManualControl()
        {
            currentStrategy = new ManualControlStrategy();
        }


        /// <summary>
        /// 로딩스크린을 즉시 강제로 숨김 (긴급 복구용)
        /// ⚠️ 주의: 상태 머신 검증을 우회하고 Hidden 상태로 강제 전환
        /// 사용 사례:
        ///   - 씬 시작 시 이전 씬의 로딩 화면이 남아있는 경우
        ///   - 예외 상황으로 로딩 화면이 멈춘 경우
        /// 정상 흐름에서는 FadeOutManually() 사용 권장
        /// </summary>
        public void ForceHide()
        {
            StopAllCoroutines();
            HideImmediate();
        }

        /// <summary>
        /// 상태 텍스트 업데이트
        /// </summary>
        private void UpdateStatusText(string text)
        {
            if (statusText != null)
            {
                statusText.text = text;
            }
        }
        #endregion
        #endregion

        #region UI 생성/관리 (런타임 코드 생성)
        private void CreateUIIfMissing()
        {
            if (loadingCanvas != null) return;

            // 폰트가 Inspector에서 할당되지 않았을 경우 기본 폰트 사용
            if (tmpFont == null)
            {
                // 기본 폰트 시도 (폴백)
                tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

                if (tmpFont == null)
                {
                    Debug.LogError("[LoadingScreenManager] ⚠️ 폰트가 할당되지 않았습니다!\n" +
                                   "→ SplashScene에서 LoadingScreenManager GameObject를 찾아\n" +
                                   "→ Inspector의 'Tmp Font' 필드에 'Assets/Model/Fonts/Maplestory Bold SDF_OutLine' 폰트를 할당하세요.");
                }
                else
                {
                    Debug.LogWarning("[LoadingScreenManager] 기본 폰트를 사용합니다.\n" +
                                     "Inspector에서 'Maplestory Bold SDF_OutLine' 폰트를 할당하면 더 나은 텍스트를 볼 수 있습니다.");
                }
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

            // Status text (로딩바 위쪽 중앙)
            GameObject statusGO = new GameObject("StatusText");
            statusGO.transform.SetParent(progressBg, false);
            var statusRT = statusGO.AddComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0.5f, 1f);
            statusRT.anchorMax = new Vector2(0.5f, 1f);
            statusRT.pivot = new Vector2(0.5f, 0f);
            statusRT.anchoredPosition = new Vector2(0f, 8f); // 로딩바 위 8px
            statusRT.sizeDelta = new Vector2(600f, 40f);
            statusText = statusGO.AddComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 18;
            statusText.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            if (tmpFont != null) statusText.font = tmpFont;
            statusText.text = "";

            // Percent text (로딩바 오른쪽 끝 위쪽)
            GameObject percentGO = new GameObject("PercentText");
            percentGO.transform.SetParent(progressBg, false);
            var percentRT = percentGO.AddComponent<RectTransform>();
            percentRT.anchorMin = new Vector2(1f, 1f);
            percentRT.anchorMax = new Vector2(1f, 1f);
            percentRT.pivot = new Vector2(1f, 0f);
            percentRT.anchoredPosition = new Vector2(0f, 8f); // 로딩바 위 8px
            percentRT.sizeDelta = new Vector2(100f, 30f);
            percentText = percentGO.AddComponent<TextMeshProUGUI>();
            percentText.alignment = TextAlignmentOptions.Right;
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
            UpdateStatusText(""); // 상태 텍스트 초기화

            // 상태 머신 강제 리셋 (긴급 상황용)
            currentState = LoadingState.Hidden;
            currentStrategy = new AutoFadeOutStrategy();
            isCancelled = false; // 취소 플래그도 리셋
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

        private IEnumerator AnimateLoadingText()
        {
            string[] dots = { "로딩 중", "로딩 중.", "로딩 중..", "로딩 중..." };
            int index = 0;

            while (currentState == LoadingState.FadingIn || currentState == LoadingState.Visible)
            {
                if (statusText != null)
                {
                    UpdateStatusText(dots[index]);
                    index = (index + 1) % dots.Length;
                }
                yield return new WaitForSecondsRealtime(LOADING_TEXT_ANIMATION_INTERVAL);
            }
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