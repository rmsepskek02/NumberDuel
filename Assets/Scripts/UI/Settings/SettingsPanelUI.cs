using DG.Tweening;
using Objects;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace UI.Settings
{
    /// <summary>
    /// 설정 패널 UI 컨트롤러
    /// - 탭 전환
    /// - 애니메이션
    /// - 탭별 콘텐츠 활성화/비활성화
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Panel References")]
        [SerializeField] private GameObject background; // 배경 (Background)
        [SerializeField] private GameObject mainPanel;     // 실제 UI 패널

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panelRect;
        [SerializeField] private Button closeButton;

        [Header("Tab Buttons")]
        [SerializeField] private Button soundTabButton;
        [SerializeField] private Button displayTabButton;
        [SerializeField] private Button profileTabButton;

        [Header("Tab Contents")]
        [SerializeField] private GameObject soundContent;
        [SerializeField] private GameObject displayContent;
        [SerializeField] private GameObject profileContent;

        [Header("Tab Scripts")]
        [SerializeField] private Tabs.DisplaySettingsTab displaySettingsTab;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.2f;

        private SettingsTabType currentTab = SettingsTabType.Sound;
        private Tween showTween;
        private Tween hideTween;
        private string currentSceneName;

        // 애니메이션 진행 상태
        private bool isAnimating = false;

        /// <summary>
        /// 애니메이션 진행 중인지 여부
        /// </summary>
        public bool IsAnimating => isAnimating;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 현재 씬 이름 가져오기
            currentSceneName = SceneManager.GetActiveScene().name;

            // 버튼 이벤트 등록
            closeButton?.onClick.AddListener(OnCloseClicked);
            soundTabButton?.onClick.AddListener(() => SwitchTab(SettingsTabType.Sound));
            displayTabButton?.onClick.AddListener(() => SwitchTab(SettingsTabType.Display));
            profileTabButton?.onClick.AddListener(() => SwitchTab(SettingsTabType.Profile));

            // 초기 상태: 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // 패널들 초기 비활성화
            if (background != null)
                background.SetActive(false);

            if (mainPanel != null)
                mainPanel.SetActive(false);

            // 씬별 탭 표시 업데이트
            UpdateTabVisibility();
        }

        private void Update()
        {
            // 설정 패널이 활성화되어 있고 애니메이션 중이 아닐 때만 키 입력 처리
            if (!mainPanel.activeSelf || isAnimating)
                return;

            // Enter 키 - 설정 패널 닫기 (ESC와 동일)
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                Manager.SettingsManager.Instance?.HideSettings();
            }
        }

        private void OnDestroy()
        {
            // Tween 정리
            showTween?.Kill();
            hideTween?.Kill();

            // 버튼 이벤트 해제
            closeButton?.onClick.RemoveListener(OnCloseClicked);
            soundTabButton?.onClick.RemoveListener(() => SwitchTab(SettingsTabType.Sound));
            displayTabButton?.onClick.RemoveListener(() => SwitchTab(SettingsTabType.Display));
            profileTabButton?.onClick.RemoveListener(() => SwitchTab(SettingsTabType.Profile));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 설정 패널 표시
        /// </summary>
        public void Show()
        {
            // 현재 씬 확인
            currentSceneName = SceneManager.GetActiveScene().name;

            // 이전 Tween 정리
            hideTween?.Kill();

            // 패널들 활성화
            if (background != null)
                background.SetActive(true);

            if (mainPanel != null)
                mainPanel.SetActive(true);

            // 씬별 탭 표시 업데이트
            UpdateTabVisibility();

            // 기본 탭 활성화 (사운드)
            SwitchTab(SettingsTabType.Sound);

            // 애니메이션
            PlayShowAnimation();
        }

        /// <summary>
        /// 설정 패널 숨기기
        /// </summary>
        public void Hide()
        {
            // Display 탭의 변경사항 확인
            if (displaySettingsTab != null && displaySettingsTab.HasUnsavedChanges())
            {
                displaySettingsTab.PromptSaveChanges(
                    onSave: () =>
                    {
                        // 저장 후 닫기
                        HideInternal();
                    },
                    onDiscard: () =>
                    {
                        // 변경사항 버리고 닫기
                        HideInternal();
                    }
                );
                return;
            }

            // 변경사항 없으면 바로 닫기
            HideInternal();
        }

        /// <summary>
        /// 설정 패널 숨기기 (내부)
        /// </summary>
        private void HideInternal()
        {
            // 이전 Tween 정리
            showTween?.Kill();

            // 애니메이션
            PlayHideAnimation();
        }

        /// <summary>
        /// 탭 전환
        /// </summary>
        public void SwitchTab(SettingsTabType tab)
        {
            // Display 탭에서 다른 탭으로 전환 시 변경사항 확인
            if (currentTab == SettingsTabType.Display && tab != SettingsTabType.Display)
            {
                if (displaySettingsTab != null && displaySettingsTab.HasUnsavedChanges())
                {
                    displaySettingsTab.PromptSaveChanges(
                        onSave: () =>
                        {
                            // 저장 후 탭 전환
                            SwitchTabInternal(tab);
                        },
                        onDiscard: () =>
                        {
                            // 변경사항 버리고 탭 전환
                            SwitchTabInternal(tab);
                        }
                    );
                    return;
                }
            }

            // 변경사항 없으면 바로 전환
            SwitchTabInternal(tab);
        }

        /// <summary>
        /// 탭 전환 (내부)
        /// </summary>
        private void SwitchTabInternal(SettingsTabType tab)
        {
            currentTab = tab;

            // 모든 콘텐츠 비활성화
            soundContent?.SetActive(false);
            displayContent?.SetActive(false);
            profileContent?.SetActive(false);

            // 선택한 탭 활성화
            switch (tab)
            {
                case SettingsTabType.Sound:
                    soundContent?.SetActive(true);
                    break;

                case SettingsTabType.Display:
                    displayContent?.SetActive(true);
                    break;

                case SettingsTabType.Profile:
                    profileContent?.SetActive(true);
                    break;
            }

            // 탭 버튼 시각적 상태 업데이트 (선택된 탭 강조)
            UpdateTabButtonVisuals(tab);

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void OnCloseClicked()
        {
            Manager.SettingsManager.Instance?.HideSettings();
        }

        /// <summary>
        /// 탭 버튼 시각적 상태 업데이트
        /// </summary>
        private void UpdateTabButtonVisuals(SettingsTabType selectedTab)
        {
            // 모든 탭 버튼을 기본 상태로
            ResetTabButton(soundTabButton);
            ResetTabButton(displayTabButton);
            ResetTabButton(profileTabButton);

            // 선택된 탭 버튼 강조
            Button selectedButton = selectedTab switch
            {
                SettingsTabType.Sound => soundTabButton,
                SettingsTabType.Display => displayTabButton,
                SettingsTabType.Profile => profileTabButton,
                _ => soundTabButton
            };

            HighlightTabButton(selectedButton);
        }

        /// <summary>
        /// 탭 버튼을 기본 상태로 리셋
        /// </summary>
        private void ResetTabButton(Button button)
        {
            if (button == null)
                return;

            // ColorBlock을 사용하여 기본 색상으로 설정
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 연한 회색
            button.colors = colors;
        }

        /// <summary>
        /// 탭 버튼 강조
        /// </summary>
        private void HighlightTabButton(Button button)
        {
            if (button == null)
                return;

            // ColorBlock을 사용하여 강조 색상으로 설정
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white; // 흰색 (강조)
            button.colors = colors;
        }

        /// <summary>
        /// 씬별 탭 표시 여부 업데이트
        /// </summary>
        private void UpdateTabVisibility()
        {
            if (profileTabButton == null)
                return;

            // JoinScene에서는 Profile 탭 숨김
            if (currentSceneName == SceneName.JoinScene.GetSceneName())
            {
                profileTabButton.gameObject.SetActive(false);
            }
            else
            {
                profileTabButton.gameObject.SetActive(true);
            }
        }
        #endregion

        #region Animations
        /// <summary>
        /// 표시 애니메이션 (Scale 0.8 → 1.0 + Fade In)
        /// </summary>
        private void PlayShowAnimation()
        {
            if (canvasGroup == null || panelRect == null)
            {
                // CanvasGroup이 없으면 즉시 표시
                return;
            }

            // 애니메이션 시작
            isAnimating = true;

            // 초기 상태
            canvasGroup.alpha = 0f;
            panelRect.localScale = Vector3.one * 0.8f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence showSequence = DOTween.Sequence();

            // Fade In
            showSequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));

            // Scale Up
            showSequence.Join(panelRect.DOScale(1f, showDuration).SetEase(Ease.OutBack));

            // 완료 시 상호작용 활성화
            showSequence.OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                isAnimating = false; // 애니메이션 완료
            });

            showTween = showSequence;
        }

        /// <summary>
        /// 숨김 애니메이션 (Scale 1.0 → 0.8 + Fade Out)
        /// </summary>
        private void PlayHideAnimation()
        {
            if (canvasGroup == null || panelRect == null)
            {
                // CanvasGroup이 없으면 즉시 비활성화
                if (background != null)
                    background.SetActive(false);

                if (mainPanel != null)
                    mainPanel.SetActive(false);
                return;
            }

            // 애니메이션 시작
            isAnimating = true;

            // 상호작용 비활성화
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence hideSequence = DOTween.Sequence();

            // Fade Out
            hideSequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

            // Scale Down
            hideSequence.Join(panelRect.DOScale(0.8f, hideDuration).SetEase(Ease.InBack));

            // 완료 시 비활성화
            hideSequence.OnComplete(() =>
            {
                if (background != null)
                    background.SetActive(false);

                if (mainPanel != null)
                    mainPanel.SetActive(false);

                isAnimating = false; // 애니메이션 완료
            });

            hideTween = hideSequence;
        }
        #endregion
    }
}
