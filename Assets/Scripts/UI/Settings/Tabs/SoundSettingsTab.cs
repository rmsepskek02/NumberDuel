using Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Tabs
{
    /// <summary>
    /// 사운드 설정 탭 UI
    /// SoundManager와 연동하여 볼륨/음소거 제어
    /// </summary>
    public class SoundSettingsTab : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Master Volume")]
        [SerializeField] private Slider masterVolumeSlider;      // MasterSlider
        [SerializeField] private TextMeshProUGUI masterVolumeText; // MasterValue
        [SerializeField] private Toggle masterMuteToggle;        // MasterToggle
        [SerializeField] private GameObject masterIconOn;        // 마스터 ON 아이콘
        [SerializeField] private GameObject masterIconOff;       // 마스터 OFF 아이콘

        [Header("BGM Volume")]
        [SerializeField] private Slider bgmVolumeSlider;         // BGMSlider
        [SerializeField] private TextMeshProUGUI bgmVolumeText;  // BGMValue
        [SerializeField] private Toggle bgmMuteToggle;           // BGMToggle
        [SerializeField] private GameObject bgmIconOn;           // BGM ON 아이콘
        [SerializeField] private GameObject bgmIconOff;          // BGM OFF 아이콘

        [Header("SFX Volume")]
        [SerializeField] private Slider sfxVolumeSlider;         // SFXSlider
        [SerializeField] private TextMeshProUGUI sfxVolumeText;  // SFXValue
        [SerializeField] private Toggle sfxMuteToggle;           // SFXToggle
        [SerializeField] private GameObject sfxIconOn;           // SFX ON 아이콘
        [SerializeField] private GameObject sfxIconOff;          // SFX OFF 아이콘

        [Header("Reset Button")]
        [SerializeField] private Button resetButton;

        // 슬라이더 색상 (캐싱)
        private static readonly Color normalColor = new Color(0x0A / 255f, 0xFF / 255f, 0x00 / 255f); // #0AFF00
        private static readonly Color mutedColor = Global.GlowRed; // #FF000A

        private bool isInitializing = false; // 초기화 중 플래그 (이벤트 중복 방지)
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 버튼 이벤트 등록
            resetButton?.onClick.AddListener(OnResetClicked);

            // 슬라이더 이벤트 등록
            masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
            bgmVolumeSlider?.onValueChanged.AddListener(OnBGMVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);

            // 토글 이벤트 등록
            masterMuteToggle?.onValueChanged.AddListener(OnMasterMuteChanged);
            bgmMuteToggle?.onValueChanged.AddListener(OnBGMMuteChanged);
            sfxMuteToggle?.onValueChanged.AddListener(OnSFXMuteChanged);

            // SoundManager에서 현재 값 로드
            LoadCurrentSettings();
        }

        private void OnDestroy()
        {
            // 이벤트 해제
            resetButton?.onClick.RemoveListener(OnResetClicked);

            masterVolumeSlider?.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            bgmVolumeSlider?.onValueChanged.RemoveListener(OnBGMVolumeChanged);
            sfxVolumeSlider?.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            masterMuteToggle?.onValueChanged.RemoveListener(OnMasterMuteChanged);
            bgmMuteToggle?.onValueChanged.RemoveListener(OnBGMMuteChanged);
            sfxMuteToggle?.onValueChanged.RemoveListener(OnSFXMuteChanged);
        }

        private void OnEnable()
        {
            // 탭 활성화 시 현재 설정 다시 로드
            LoadCurrentSettings();
        }
        #endregion

        #region Settings Management
        /// <summary>
        /// SoundManager에서 현재 설정 로드
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (Manager.SoundManager.Instance == null)
            {
                Debug.LogWarning("[SoundSettingsTab] SoundManager가 없습니다!");
                return;
            }

            isInitializing = true; // 이벤트 중복 방지

            var soundManager = Manager.SoundManager.Instance;

            // Master Volume
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = soundManager.MasterVolume;
                UpdateVolumeText(masterVolumeText, soundManager.MasterVolume);
            }

            if (masterMuteToggle != null)
            {
                masterMuteToggle.isOn = soundManager.IsMasterMuted;
                UpdateToggleIcons(masterIconOn, masterIconOff, soundManager.IsMasterMuted);
                UpdateSliderColor(masterVolumeSlider, soundManager.IsMasterMuted);
            }

            // BGM Volume
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = soundManager.BGMVolume;
                UpdateVolumeText(bgmVolumeText, soundManager.BGMVolume);
            }

            if (bgmMuteToggle != null)
            {
                bgmMuteToggle.isOn = soundManager.IsBGMMuted;
                UpdateToggleIcons(bgmIconOn, bgmIconOff, soundManager.IsBGMMuted);
                UpdateSliderColor(bgmVolumeSlider, soundManager.IsBGMMuted);
            }

            // SFX Volume
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = soundManager.SFXVolume;
                UpdateVolumeText(sfxVolumeText, soundManager.SFXVolume);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.isOn = soundManager.IsSFXMuted;
                UpdateToggleIcons(sfxIconOn, sfxIconOff, soundManager.IsSFXMuted);
                UpdateSliderColor(sfxVolumeSlider, soundManager.IsSFXMuted);
            }

            isInitializing = false;
        }
        #endregion

        #region Volume Slider Handlers
        /// <summary>
        /// 마스터 볼륨 슬라이더 변경
        /// </summary>
        private void OnMasterVolumeChanged(float value)
        {
            if (isInitializing)
                return;

            // SoundManager에 적용 (자동 저장됨)
            Manager.SoundManager.Instance?.SetMasterVolume(value);

            // UI 텍스트 업데이트
            UpdateVolumeText(masterVolumeText, value);
        }

        /// <summary>
        /// BGM 볼륨 슬라이더 변경
        /// </summary>
        private void OnBGMVolumeChanged(float value)
        {
            if (isInitializing)
                return;

            Manager.SoundManager.Instance?.SetBGMVolume(value);
            UpdateVolumeText(bgmVolumeText, value);
        }

        /// <summary>
        /// SFX 볼륨 슬라이더 변경
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            if (isInitializing)
                return;

            Manager.SoundManager.Instance?.SetSFXVolume(value);
            UpdateVolumeText(sfxVolumeText, value);

            // SFX 변경 시 테스트 사운드 재생
            PlayTestSound();
        }
        #endregion

        #region Mute Toggle Handlers
        /// <summary>
        /// 마스터 음소거 토글 변경
        /// </summary>
        private void OnMasterMuteChanged(bool isMuted)
        {
            if (isInitializing)
                return;

            Manager.SoundManager.Instance?.SetMasterMute(isMuted);
            UpdateToggleIcons(masterIconOn, masterIconOff, isMuted);
            UpdateSliderColor(masterVolumeSlider, isMuted);
        }

        /// <summary>
        /// BGM 음소거 토글 변경
        /// </summary>
        private void OnBGMMuteChanged(bool isMuted)
        {
            if (isInitializing)
                return;

            Manager.SoundManager.Instance?.SetBGMMute(isMuted);
            UpdateToggleIcons(bgmIconOn, bgmIconOff, isMuted);
            UpdateSliderColor(bgmVolumeSlider, isMuted);
        }

        /// <summary>
        /// SFX 음소거 토글 변경
        /// </summary>
        private void OnSFXMuteChanged(bool isMuted)
        {
            if (isInitializing)
                return;

            Manager.SoundManager.Instance?.SetSFXMute(isMuted);
            UpdateToggleIcons(sfxIconOn, sfxIconOff, isMuted);
            UpdateSliderColor(sfxVolumeSlider, isMuted);
        }
        #endregion

        #region Reset Button
        /// <summary>
        /// 기본값으로 복원 버튼 클릭
        /// </summary>
        private void OnResetClicked()
        {
            if (Manager.SoundManager.Instance == null)
                return;

            isInitializing = true; // 이벤트 중복 방지

            // 기본값 설정 (SoundManager의 기본값과 동일)
            float defaultMaster = 1.0f;
            float defaultBGM = 0.7f;
            float defaultSFX = 1.0f;

            // SoundManager에 적용
            Manager.SoundManager.Instance.SetMasterVolume(defaultMaster);
            Manager.SoundManager.Instance.SetBGMVolume(defaultBGM);
            Manager.SoundManager.Instance.SetSFXVolume(defaultSFX);

            Manager.SoundManager.Instance.SetMasterMute(false);
            Manager.SoundManager.Instance.SetBGMMute(false);
            Manager.SoundManager.Instance.SetSFXMute(false);

            // UI 업데이트
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = defaultMaster;
                UpdateVolumeText(masterVolumeText, defaultMaster);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = defaultBGM;
                UpdateVolumeText(bgmVolumeText, defaultBGM);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = defaultSFX;
                UpdateVolumeText(sfxVolumeText, defaultSFX);
            }

            if (masterMuteToggle != null)
            {
                masterMuteToggle.isOn = false;
                UpdateToggleIcons(masterIconOn, masterIconOff, false);
                UpdateSliderColor(masterVolumeSlider, false);
            }

            if (bgmMuteToggle != null)
            {
                bgmMuteToggle.isOn = false;
                UpdateToggleIcons(bgmIconOn, bgmIconOff, false);
                UpdateSliderColor(bgmVolumeSlider, false);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.isOn = false;
                UpdateToggleIcons(sfxIconOn, sfxIconOff, false);
                UpdateSliderColor(sfxVolumeSlider, false);
            }

            isInitializing = false;

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);

            Debug.Log("[SoundSettingsTab] 사운드 설정이 기본값으로 복원되었습니다.");
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 볼륨 텍스트 업데이트 (0~100% 형식)
        /// </summary>
        private void UpdateVolumeText(TextMeshProUGUI text, float value)
        {
            if (text != null)
            {
                int percentage = Mathf.RoundToInt(value * 100f);
                text.text = $"{percentage}%";
            }
        }

        /// <summary>
        /// 테스트 사운드 재생 (SFX 볼륨 변경 시)
        /// </summary>
        private void PlayTestSound()
        {
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 토글 아이콘 전환 (ON/OFF 상태에 따라 아이콘 활성화/비활성화)
        /// </summary>
        /// <param name="iconOn">ON 상태 아이콘 (음소거됨)</param>
        /// <param name="iconOff">OFF 상태 아이콘 (음소거 안됨)</param>
        /// <param name="isMuted">음소거 여부</param>
        private void UpdateToggleIcons(GameObject iconOn, GameObject iconOff, bool isMuted)
        {
            if (iconOn != null)
            {
                iconOn.SetActive(isMuted);
            }

            if (iconOff != null)
            {
                iconOff.SetActive(!isMuted);
            }
        }

        /// <summary>
        /// 슬라이더 색상 업데이트 (음소거 상태에 따라 Fill Area 색상 변경)
        /// </summary>
        /// <param name="slider">변경할 슬라이더</param>
        /// <param name="isMuted">음소거 여부</param>
        private void UpdateSliderColor(Slider slider, bool isMuted)
        {
            if (slider == null)
                return;

            Color targetColor = isMuted ? mutedColor : normalColor;

            // Fill Area의 Image 색상만 변경
            var fillArea = slider.fillRect;
            if (fillArea != null)
            {
                var fillImage = fillArea.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = targetColor;
                }
            }
        }
        #endregion
    }
}
