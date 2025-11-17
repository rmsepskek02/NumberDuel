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

        [Header("Reset Button")]
        [SerializeField] private Button resetButton;

        private bool isInitializing = false; // 초기화 중 플래그
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 버튼 이벤트 등록
            resetButton?.onClick.AddListener(OnResetClicked);

            // 드롭다운 이벤트 등록
            resolutionDropdown?.onValueChanged.AddListener(OnResolutionChanged);
            screenModeDropdown?.onValueChanged.AddListener(OnScreenModeChanged);

            // 드롭다운 옵션 초기화
            InitializeDropdowns();

            // 현재 설정 로드
            LoadCurrentSettings();
        }

        private void OnDestroy()
        {
            // 이벤트 해제
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

        #region Dropdown Initialization
        /// <summary>
        /// 드롭다운 옵션 초기화
        /// </summary>
        private void InitializeDropdowns()
        {
            // 해상도 드롭다운 초기화
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();

                List<string> resolutionOptions = new List<string>();
                foreach (var res in Manager.DisplaySettingsManager.AvailableResolutions)
                {
                    resolutionOptions.Add($"{res.width} x {res.height}");
                }

                resolutionDropdown.AddOptions(resolutionOptions);
            }

            // 화면 모드 드롭다운 초기화
            if (screenModeDropdown != null)
            {
                screenModeDropdown.ClearOptions();

                List<string> screenModeOptions = new List<string>
                {
                    "전체화면",
                    "창모드",
                    "테두리 없는 창"
                };

                screenModeDropdown.AddOptions(screenModeOptions);
            }
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
            }

            // 화면 모드 드롭다운 설정
            if (screenModeDropdown != null)
            {
                int screenModeIndex = displayManager.GetCurrentScreenModeIndex();
                screenModeDropdown.value = screenModeIndex;
            }

            isInitializing = false;
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

            if (Manager.DisplaySettingsManager.Instance == null)
                return;

            // 선택한 해상도 가져오기
            if (index < 0 || index >= Manager.DisplaySettingsManager.AvailableResolutions.Count)
                return;

            var selectedResolution = Manager.DisplaySettingsManager.AvailableResolutions[index];

            // DisplaySettingsManager에 적용 (즉시 적용 + 자동 저장)
            Manager.DisplaySettingsManager.Instance.SetResolution(selectedResolution.width, selectedResolution.height);

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);

            Debug.Log($"[DisplaySettingsTab] 해상도 변경: {selectedResolution.width}x{selectedResolution.height}");
        }

        /// <summary>
        /// 화면 모드 드롭다운 변경
        /// </summary>
        private void OnScreenModeChanged(int index)
        {
            if (isInitializing)
                return;

            if (Manager.DisplaySettingsManager.Instance == null)
                return;

            // 선택한 화면 모드 가져오기
            FullScreenMode mode = Manager.DisplaySettingsManager.GetScreenModeFromIndex(index);

            // DisplaySettingsManager에 적용 (즉시 적용 + 자동 저장)
            Manager.DisplaySettingsManager.Instance.SetScreenMode(mode);

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);

            Debug.Log($"[DisplaySettingsTab] 화면 모드 변경: {mode}");
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

            isInitializing = true; // 이벤트 중복 방지

            // DisplaySettingsManager의 기본값으로 복원
            Manager.DisplaySettingsManager.Instance.ResetToDefault();

            // UI 업데이트
            if (resolutionDropdown != null)
            {
                int resolutionIndex = Manager.DisplaySettingsManager.Instance.GetCurrentResolutionIndex();
                resolutionDropdown.value = resolutionIndex;
            }

            if (screenModeDropdown != null)
            {
                int screenModeIndex = Manager.DisplaySettingsManager.Instance.GetCurrentScreenModeIndex();
                screenModeDropdown.value = screenModeIndex;
            }

            isInitializing = false;

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);

            Debug.Log("[DisplaySettingsTab] 화면 설정이 기본값으로 복원되었습니다.");
        }
        #endregion
    }
}
