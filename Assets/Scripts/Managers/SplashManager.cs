using Objects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// Splash 화면을 관리하는 매니저
    /// 게임 시작 시 자동으로 JoinScene으로 이동
    /// </summary>
    public class SplashManager : MonoBehaviour
    {
        #region Unity Lifecycle
        void Start()
        {
            SceneManager.LoadScene(SceneNameExtensions.GetSceneName (SceneName.JoinScene));
        }
        #endregion
    }
}
