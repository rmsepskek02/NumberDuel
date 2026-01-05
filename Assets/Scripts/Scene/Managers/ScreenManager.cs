using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 화면 해상도를 관리하는 매니저
    /// PlayerPrefs를 사용하여 해상도 저장 및 복원
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        #region Unity Lifecycle
        void Start()
        {
            // DisplaySettingsManager를 통해 저장된 해상도 적용
            if (DisplaySettingsManager.Instance != null)
            {
                DisplaySettingsManager.Instance.ApplySettings();
            }
        }

        void Update()
        {
            // 사용자가 창 크기를 변경하면 저장
            if (Screen.width != PlayerPrefs.GetInt("ScreenWidth") || Screen.height != PlayerPrefs.GetInt("ScreenHeight"))
            {
                PlayerPrefs.SetInt("ScreenWidth", Screen.width);
                PlayerPrefs.SetInt("ScreenHeight", Screen.height);
                PlayerPrefs.Save();
            }
        }
        #endregion
    }
}