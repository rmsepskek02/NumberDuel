using UnityEngine;

namespace Manager
{
    /// <summary>
    /// �ػ� �����ϴ� �Ŵ���
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        void Start()
        {
            // ����� �ػ󵵰� ������ �ҷ�����
            int width = PlayerPrefs.GetInt("ScreenWidth", 1280);
            int height = PlayerPrefs.GetInt("ScreenHeight", 720);
            Screen.SetResolution(width, height, false);
        }

        void Update()
        {
            // ����ڰ� â ũ�⸦ �����ϸ� ����
            if (Screen.width != PlayerPrefs.GetInt("ScreenWidth") || Screen.height != PlayerPrefs.GetInt("ScreenHeight"))
            {
                PlayerPrefs.SetInt("ScreenWidth", Screen.width);
                PlayerPrefs.SetInt("ScreenHeight", Screen.height);
                PlayerPrefs.Save();
            }
        }
    }
}
