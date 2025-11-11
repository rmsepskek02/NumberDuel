using Manager;
using Objects;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button에 클릭 사운드를 추가하는 컴포넌트
/// Inspector에서 사운드 타입과 볼륨 조절 가능
/// 기본값은 UI_ButtonClick 사용
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("재생할 사운드 타입 (기본값: UI_ButtonClick)")]
    [SerializeField] private SoundType soundType = SoundType.UI_ButtonClick;

    [Tooltip("사운드 볼륨 배율 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeScale = 1.0f;

    private Button button;

    #region Unity Lifecycle
    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
        else
        {
            Debug.LogError($"[ButtonSound] Button 컴포넌트를 찾을 수 없습니다: {gameObject.name}");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaySound);
        }
    }
    #endregion

    #region Sound Playback
    /// <summary>
    /// 버튼 클릭 사운드 재생
    /// </summary>
    private void PlaySound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(soundType, volumeScale);
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// 사운드 타입 변경 (런타임에서 호출 가능)
    /// </summary>
    public void SetSoundType(SoundType newSoundType)
    {
        soundType = newSoundType;
    }

    /// <summary>
    /// 볼륨 배율 변경 (런타임에서 호출 가능)
    /// </summary>
    public void SetVolumeScale(float newVolumeScale)
    {
        volumeScale = Mathf.Clamp01(newVolumeScale);
    }
    #endregion
}
