using System.Collections.Generic;
using Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Tabs
{
    /// <summary>
    /// 화면 설정 탭 UI
    /// DisplaySettingsManager와 연동하여 해상도/화면모드 제어
    /// </summary>
    public class DisplaySettingsTab : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Resolution")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Header("Screen Mode")]
        [SerializeField] private TMP_Dropdown screenModeDropdown;

        [Header("Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;

        private bool isInitializing = false; // 초기화 중 플래그
        private bool isChanged = false; // 변경사항 추적

        // 변경 전 설정값 저장
        private int originalResolutionIndex = -1;
        private int originalScreenModeIndex = -1;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 버튼 이벤트 등록
            saveButton?.onClick.AddListener(OnSaveClicked);
            resetButton?.onClick.AddListener(OnResetClicked);

            // 드롭다운 이벤트 등록
            resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
            screenModeDropdown?.onValueChanged.AddListener(OnScreenModeChanged);

            // 현재 설정 로드 (Inspector Options 사용)
            LoadCurrentSettings();
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            saveButton?.onClick.RemoveListener(OnSaveClicked);
            resetButton?.onClick.RemoveListener(OnResetClicked);
            resolutionDropdown?.onValueChanged.RemoveListener(OnResolutionChanged);
            screenModeDropdown?.onValueChanged.RemoveListener(OnScreenModeChanged);
        }

        private void OnEnable()
        {
            // 탭 활성화 시 현재 설정 다시 로드
            LoadCurrentSettings();
        }
        #endregion


        #region Settings Management
        /// <summary>
        /// DisplaySettingsManager에서 현재 설정 로드
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (Manager.DisplaySettingsManager.Instance == null)
            {
                Debug.LogWarning("[DisplaySettingsTab] DisplaySettingsManager가 없습니다!");
                return;
            }

            isInitializing = true; // 이벤트 중복 방지

            var displayManager = Manager.DisplaySettingsManager.Instance;

            // 해상도 드롭다운 설정
            if (resolutionDropdown != null)
            {
                int resolutionIndex = displayManager.GetCurrentResolutionIndex();
                resolutionDropdown.value = resolutionIndex;
                originalResolutionIndex = resolutionIndex; // 원본 저장
            }

            // 화면 모드 드롭다운 설정
            if (screenModeDropdown != null)
            {
                int screenModeIndex = displayManager.GetCurrentScreenModeIndex();
                screenModeDropdown.value = screenModeIndex;
                originalScreenModeIndex = screenModeIndex; // 원본 저장
            }

            // 변경사항 초기화
            isChanged = false;
            UpdateSaveButtonState();

            isInitializing = false;
        }

        /// <summary>
        /// 변경사항 확인
        /// </summary>
        private void CheckForChanges()
        {
            bool resolutionChanged = resolutionDropdown != null && resolutionDropdown.value != originalResolutionIndex;
            bool screenModeChanged = screenModeDropdown != null && screenModeDropdown.value != originalScreenModeIndex;

            isChanged = resolutionChanged || screenModeChanged;
            UpdateSaveButtonState();
        }

        /// <summary>
        /// 저장 버튼 상태 업데이트
        /// </summary>
        private void UpdateSaveButtonState()
        {
            if (saveButton != null)
            {
                saveButton.interactable = isChanged;
            }
        }

        /// <summary>
        /// 변경사항이 있는지 확인 (외부에서 호출)
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return isChanged;
        }

        /// <summary>
        /// 변경사항 저장 확인 요청
        /// </summary>
        public void PromptSaveChanges(System.Action onSave, System.Action onDiscard)
        {
            if (!isChanged)
            {
                onDiscard?.Invoke();
                return;
            }

            // ConfirmationPopup 표시
            UI.Shared.ConfirmationPopup.Show(
                "변경사항을 저장하시겠습니까?",
                () =>
                {
                    // 저장
                    ApplySettings();
                    onSave?.Invoke();
                },
                () =>
                {
                    // 취소 (변경사항 버림)
                    LoadCurrentSettings(); // 원래 설정으로 되돌림
                    onDiscard?.Invoke();
                }
            );
        }
        #endregion

        #region Dropdown Handlers
        /// <summary>
        /// 해상도 드롭다운 변경
        /// </summary>
        private void OnResolutionChanged(int index)
        {
            if (isInitializing)
                return;

            // 변경사항 체크
            CheckForChanges();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 화면 모드 드롭다운 변경
        /// </summary>
        private void OnScreenModeChanged(int index)
        {
            if (isInitializing)
                return;

            // 변경사항 체크
            CheckForChanges();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        #endregion

        #region Save Button
        /// <summary>
        /// 저장 버튼 클릭
        /// </summary>
        private void OnSaveClicked()
        {
            if (!isChanged)
                return;

            // ConfirmationPopup 표시
            UI.Shared.ConfirmationPopup.Show(
                "정말 변경하시겠습니까?",
                () =>
                {
                    // 확인 클릭 시 설정 적용
                    ApplySettings();
                }
            );
        }

        /// <summary>
        /// 설정 적용
        /// </summary>
        private void ApplySettings()
        {
            if (Manager.DisplaySettingsManager.Instance == null)
                return;

            // 해상도 적용
            if (resolutionDropdown != null && resolutionDropdown.value != originalResolutionIndex)
            {
                int index = resolutionDropdown.value;
                if (index >= 0 && index < Manager.DisplaySettingsManager.AvailableResolutions.Count)
                {
                    var selectedResolution = Manager.DisplaySettingsManager.AvailableResolutions[index];
                    Manager.DisplaySettingsManager.Instance.SetResolution(selectedResolution.width, selectedResolution.height);
                    originalResolutionIndex = index;

                    Debug.Log($"[DisplaySettingsTab] 해상도 적용: {selectedResolution.width}x{selectedResolution.height}");
                }
            }

            // 화면 모드 적용
            if (screenModeDropdown != null && screenModeDropdown.value != originalScreenModeIndex)
            {
                int index = screenModeDropdown.value;
                FullScreenMode mode = Manager.DisplaySettingsManager.GetScreenModeFromIndex(index);
                Manager.DisplaySettingsManager.Instance.SetScreenMode(mode);
                originalScreenModeIndex = index;

                Debug.Log($"[DisplaySettingsTab] 화면 모드 적용: {mode}");
            }

            // 변경사항 초기화
            isChanged = false;
            UpdateSaveButtonState();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        #endregion

        #region Reset Button
        /// <summary>
        /// 기본값으로 복원 버튼 클릭
        /// </summary>
        private void OnResetClicked()
        {
            if (Manager.DisplaySettingsManager.Instance == null)
                return;

            // ConfirmationPopup 표시
            UI.Shared.ConfirmationPopup.Show(
                "기본 설정으로 복원하시겠습니까?",
                () =>
                {
                    isInitializing = true; // 이벤트 중복 방지

                    // DisplaySettingsManager의 기본값으로 복원
                    Manager.DisplaySettingsManager.Instance.ResetToDefault();

                    // UI 업데이트
                    if (resolutionDropdown != null)
                    {
                        int resolutionIndex = Manager.DisplaySettingsManager.Instance.GetCurrentResolutionIndex();
                        resolutionDropdown.value = resolutionIndex;
                        originalResolutionIndex = resolutionIndex;
                    }

                    if (screenModeDropdown != null)
                    {
                        int screenModeIndex = Manager.DisplaySettingsManager.Instance.GetCurrentScreenModeIndex();
                        screenModeDropdown.value = screenModeIndex;
                        originalScreenModeIndex = screenModeIndex;
                    }

                    // 변경사항 초기화
                    isChanged = false;
                    UpdateSaveButtonState();

                    isInitializing = false;

                    // 사운드 재생
                    Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);

                    Debug.Log("[DisplaySettingsTab] 화면 설정이 기본값으로 복원되었습니다.");
                }
            );
        }
        #endregion
    }
}
