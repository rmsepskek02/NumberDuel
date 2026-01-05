using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 화면 설정 관리자
    /// - 해상도 변경
    /// - 화면 모드 변경
    /// - PlayerPrefs 저장/로드
    /// </summary>
    public class DisplaySettingsManager : SingletonDontDestroy<DisplaySettingsManager>
    {
        #region Fields and Properties
        // PlayerPrefs 키
        private const string PREF_SCREEN_WIDTH = "Display_ScreenWidth";
        private const string PREF_SCREEN_HEIGHT = "Display_ScreenHeight";
        private const string PREF_SCREEN_MODE = "Display_ScreenMode";

        // 기본값
        private const int DEFAULT_WIDTH = 1920;
        private const int DEFAULT_HEIGHT = 1080;
        private const FullScreenMode DEFAULT_MODE = FullScreenMode.ExclusiveFullScreen;

        // 현재 설정
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }
        public FullScreenMode ScreenMode { get; private set; }

        /// <summary>
        /// 사용 가능한 해상도 목록 (명세서에 따른 고정 목록)
        /// </summary>
        public static readonly List<Resolution> AvailableResolutions = new List<Resolution>
        {
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 2560, height = 1440 }
        };
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            // 설정만 로드 (화면 적용은 하지 않음)
            // 실제 화면 적용은 게임 시작 시 ScreenManager나 다른 곳에서 명시적으로 호출해야 함
            LoadSettings();
        }
        #endregion

        #region Settings Management
        /// <summary>
        /// 설정 로드
        /// </summary>
        public void LoadSettings()
        {
            ScreenWidth = PlayerPrefs.GetInt(PREF_SCREEN_WIDTH, DEFAULT_WIDTH);
            ScreenHeight = PlayerPrefs.GetInt(PREF_SCREEN_HEIGHT, DEFAULT_HEIGHT);
            ScreenMode = (FullScreenMode)PlayerPrefs.GetInt(PREF_SCREEN_MODE, (int)DEFAULT_MODE);

            // 로드한 설정 적용 (Awake 시에만)
            // UI 탭에서 LoadSettings 호출 시에는 적용하지 않음
        }

        /// <summary>
        /// 설정 로드 및 적용
        /// </summary>
        public void LoadAndApplySettings()
        {
            LoadSettings();
            ApplySettings();
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetInt(PREF_SCREEN_WIDTH, ScreenWidth);
            PlayerPrefs.SetInt(PREF_SCREEN_HEIGHT, ScreenHeight);
            PlayerPrefs.SetInt(PREF_SCREEN_MODE, (int)ScreenMode);
            PlayerPrefs.Save();
        }
        #endregion

        #region Display Settings
        /// <summary>
        /// 해상도 설정 (즉시 적용 + 자동 저장)
        /// </summary>
        public void SetResolution(int width, int height)
        {
            ScreenWidth = width;
            ScreenHeight = height;
            ApplySettings();
            SaveSettings();
        }

        /// <summary>
        /// 화면 모드 설정 (즉시 적용 + 자동 저장)
        /// </summary>
        public void SetScreenMode(FullScreenMode mode)
        {
            ScreenMode = mode;
            ApplySettings();
            SaveSettings();
        }

        /// <summary>
        /// 현재 설정을 Unity Screen API에 적용
        /// </summary>
        public void ApplySettings()
        {
            Debug.Log($"[DisplaySettingsManager] ApplySettings 호출: {ScreenWidth}x{ScreenHeight}, Mode={ScreenMode}\nStackTrace: {System.Environment.StackTrace}");
            Screen.SetResolution(ScreenWidth, ScreenHeight, ScreenMode);
        }

        /// <summary>
        /// 기본값으로 복원
        /// </summary>
        public void ResetToDefault()
        {
            ScreenWidth = DEFAULT_WIDTH;
            ScreenHeight = DEFAULT_HEIGHT;
            ScreenMode = DEFAULT_MODE;
            ApplySettings();
            SaveSettings();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 현재 해상도가 사용 가능한 목록에 있는지 확인
        /// </summary>
        public bool IsCurrentResolutionValid()
        {
            return AvailableResolutions.Any(r => r.width == ScreenWidth && r.height == ScreenHeight);
        }

        /// <summary>
        /// 해상도를 인덱스로 반환 (Dropdown에서 사용)
        /// </summary>
        public int GetCurrentResolutionIndex()
        {
            for (int i = 0; i < AvailableResolutions.Count; i++)
            {
                if (AvailableResolutions[i].width == ScreenWidth && AvailableResolutions[i].height == ScreenHeight)
                {
                    return i;
                }
            }
            return 2; // 기본값 (1920x1080)
        }

        /// <summary>
        /// 화면 모드를 인덱스로 반환 (Dropdown에서 사용)
        /// </summary>
        public int GetCurrentScreenModeIndex()
        {
            return ScreenMode switch
            {
                FullScreenMode.ExclusiveFullScreen => 0,
                FullScreenMode.Windowed => 1,
                FullScreenMode.FullScreenWindow => 2,
                _ => 0
            };
        }

        /// <summary>
        /// 인덱스를 화면 모드로 변환
        /// </summary>
        public static FullScreenMode GetScreenModeFromIndex(int index)
        {
            return index switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.ExclusiveFullScreen
            };
        }
        #endregion
    }
}
