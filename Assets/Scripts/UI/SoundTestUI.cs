using Manager;
using Objects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Utills;

/// <summary>
/// 사운드 시스템 테스트용 UI (DontDestroy 패턴)
/// F5 키로 토글, SplashScene에서 시작하여 모든 씬에서 사용 가능
/// 실제 설정 UI가 완성되면 이 UI는 제거될 예정
/// </summary>
public class SoundTestUI : SingletonDontDestroy<SoundTestUI>
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;

    [Header("Master Volume Section")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private Button masterMuteButton;
    [SerializeField] private Button masterSpeakerButton;

    [Header("BGM Volume Section")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmValueText;
    [SerializeField] private Button bgmMuteButton;
    [SerializeField] private Button bgmSpeakerButton;

    [Header("SFX Volume Section")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private Button sfxMuteButton;
    [SerializeField] private Button sfxSpeakerButton;

    [Header("Control Buttons")]
    [SerializeField] private Button cancelButton;

    private bool isInitialized = false;

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();

        // SplashScene에서는 숨김 처리
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "SplashScene")
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    private void Start()
    {
        InitializeUI();
    }

    private void Update()
    {
        // F5 키로 UI 토글 (New Input System)
        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            ToggleUI();
        }
    }
    #endregion

    #region UI Initialization
    /// <summary>
    /// UI 초기화 (버튼/슬라이더 리스너 등록)
    /// </summary>
    private void InitializeUI()
    {
        if (isInitialized)
            return;

        // SoundManager가 준비될 때까지 대기
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[SoundTestUI] SoundManager가 아직 초기화되지 않았습니다.");
            Invoke(nameof(InitializeUI), 0.5f);
            return;
        }

        // 현재 볼륨값으로 슬라이더 초기화
        masterSlider.value = SoundManager.Instance.MasterVolume;
        bgmSlider.value = SoundManager.Instance.BGMVolume;
        sfxSlider.value = SoundManager.Instance.SFXVolume;

        // 슬라이더 리스너 등록
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Mute/Speaker 버튼 리스너 등록 (클릭 사운드 포함)
        masterMuteButton.onClick.AddListener(() => { PlayButtonClickSound(); OnMuteButtonClicked(true, false, false); });
        masterSpeakerButton.onClick.AddListener(() => { PlayButtonClickSound(); OnSpeakerButtonClicked(true, false, false); });
        bgmMuteButton.onClick.AddListener(() => { PlayButtonClickSound(); OnMuteButtonClicked(false, true, false); });
        bgmSpeakerButton.onClick.AddListener(() => { PlayButtonClickSound(); OnSpeakerButtonClicked(false, true, false); });
        sfxMuteButton.onClick.AddListener(() => { PlayButtonClickSound(); OnMuteButtonClicked(false, false, true); });
        sfxSpeakerButton.onClick.AddListener(() => { PlayButtonClickSound(); OnSpeakerButtonClicked(false, false, true); });

        // Cancel 버튼 리스너
        cancelButton.onClick.AddListener(() => { PlayButtonClickSound(); OnCancel(); });

        // 초기 상태 업데이트
        UpdateAllUI();

        isInitialized = true;
    }
    #endregion

    #region UI Toggle
    /// <summary>
    /// UI 표시/숨김 토글
    /// </summary>
    public void ToggleUI()
    {
        if (panel == null)
        {
            Debug.LogError("[SoundTestUI] Panel이 할당되지 않았습니다.");
            return;
        }

        bool isActive = panel.activeSelf;
        panel.SetActive(!isActive);

        // UI가 표시될 때마다 최신 상태로 업데이트
        if (!isActive)
        {
            UpdateAllUI();
        }
    }

    /// <summary>
    /// UI 강제 표시
    /// </summary>
    public void ShowUI()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            UpdateAllUI();
        }
    }

    /// <summary>
    /// UI 강제 숨김
    /// </summary>
    public void HideUI()
    {
        if (panel != null)
            panel.SetActive(false);
    }
    #endregion

    #region Slider Callbacks
    /// <summary>
    /// 마스터 볼륨 변경 (자동 저장)
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        SoundManager.Instance.SetMasterVolume(value);
        UpdateValueText(masterValueText, value);
    }

    /// <summary>
    /// BGM 볼륨 변경 (자동 저장)
    /// </summary>
    private void OnBGMVolumeChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
        UpdateValueText(bgmValueText, value);
    }

    /// <summary>
    /// SFX 볼륨 변경 (자동 저장)
    /// </summary>
    private void OnSFXVolumeChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);
        UpdateValueText(sfxValueText, value);
    }
    #endregion

    #region Mute/Speaker Button Callbacks
    /// <summary>
    /// Mute 버튼 클릭 (음소거 활성화)
    /// </summary>
    private void OnMuteButtonClicked(bool isMaster, bool isBGM, bool isSFX)
    {
        if (isMaster)
        {
            SoundManager.Instance.SetMasterMute(true);
            UpdateMuteButtons(masterMuteButton, masterSpeakerButton, true);
        }
        else if (isBGM)
        {
            SoundManager.Instance.SetBGMMute(true);
            UpdateMuteButtons(bgmMuteButton, bgmSpeakerButton, true);
        }
        else if (isSFX)
        {
            SoundManager.Instance.SetSFXMute(true);
            UpdateMuteButtons(sfxMuteButton, sfxSpeakerButton, true);
        }
    }

    /// <summary>
    /// Speaker 버튼 클릭 (음소거 해제)
    /// </summary>
    private void OnSpeakerButtonClicked(bool isMaster, bool isBGM, bool isSFX)
    {
        if (isMaster)
        {
            SoundManager.Instance.SetMasterMute(false);
            UpdateMuteButtons(masterMuteButton, masterSpeakerButton, false);
        }
        else if (isBGM)
        {
            SoundManager.Instance.SetBGMMute(false);
            UpdateMuteButtons(bgmMuteButton, bgmSpeakerButton, false);
        }
        else if (isSFX)
        {
            SoundManager.Instance.SetSFXMute(false);
            UpdateMuteButtons(sfxMuteButton, sfxSpeakerButton, false);
        }
    }

    /// <summary>
    /// Mute/Speaker 버튼 활성화 상태 업데이트
    /// </summary>
    private void UpdateMuteButtons(Button muteButton, Button speakerButton, bool isMuted)
    {
        if (muteButton != null && speakerButton != null)
        {
            muteButton.gameObject.SetActive(!isMuted);   // 소리 나는 중 → Mute 버튼 표시
            speakerButton.gameObject.SetActive(isMuted);  // 음소거 중 → Speaker 버튼 표시
        }
    }
    #endregion

    #region Button Callbacks
    /// <summary>
    /// 버튼 클릭 사운드 재생
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(SoundType.UI_ButtonClick);
        }
    }

    /// <summary>
    /// UI 닫기
    /// </summary>
    private void OnCancel()
    {
        HideUI();
    }
    #endregion

    #region UI Update
    /// <summary>
    /// 모든 UI 요소 업데이트
    /// </summary>
    private void UpdateAllUI()
    {
        if (SoundManager.Instance == null)
            return;

        // 슬라이더 값 업데이트
        masterSlider.SetValueWithoutNotify(SoundManager.Instance.MasterVolume);
        bgmSlider.SetValueWithoutNotify(SoundManager.Instance.BGMVolume);
        sfxSlider.SetValueWithoutNotify(SoundManager.Instance.SFXVolume);

        // 텍스트 업데이트
        UpdateValueText(masterValueText, SoundManager.Instance.MasterVolume);
        UpdateValueText(bgmValueText, SoundManager.Instance.BGMVolume);
        UpdateValueText(sfxValueText, SoundManager.Instance.SFXVolume);

        // Mute/Speaker 버튼 상태 업데이트
        UpdateMuteButtons(masterMuteButton, masterSpeakerButton, SoundManager.Instance.IsMasterMuted);
        UpdateMuteButtons(bgmMuteButton, bgmSpeakerButton, SoundManager.Instance.IsBGMMuted);
        UpdateMuteButtons(sfxMuteButton, sfxSpeakerButton, SoundManager.Instance.IsSFXMuted);
    }

    /// <summary>
    /// 텍스트 업데이트 (0~100% 표시)
    /// </summary>
    private void UpdateValueText(TextMeshProUGUI text, float value)
    {
        if (text != null)
            text.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
    #endregion
}
