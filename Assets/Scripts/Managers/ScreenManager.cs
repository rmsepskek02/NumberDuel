using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 해상도 관리하는 매니저
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        void Start()
        {
            // 저장된 해상도가 있으면 불러오기
            int width = PlayerPrefs.GetInt("ScreenWidth", 1280);
            int height = PlayerPrefs.GetInt("ScreenHeight", 720);
            Screen.SetResolution(width, height, false);
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
    }
}
