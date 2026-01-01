using UnityEngine;
using UMI;
using Utills;

namespace Manager
{
    /// <summary>
    /// UMI (Unity Mobile Input) 플러그인 초기화 관리자
    /// - MobileInput.Init() 최우선 실행
    /// - KeyboardManager보다 먼저 초기화되어야 함
    /// - DontDestroyOnLoad로 모든 Scene에서 유지
    /// </summary>
    public class MobileInputManager : SingletonDontDestroy<MobileInputManager>
    {
        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            // UMI 초기화 (모든 InputField 및 KeyboardManager보다 먼저 실행되어야 함!)
            InitializeMobileInput();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// UMI 플러그인 초기화
        /// </summary>
        private void InitializeMobileInput()
        {
#if UNITY_ANDROID || UNITY_IOS
            try
            {
                MobileInput.Init();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MobileInputManager] UMI 초기화 실패: {e.Message}");
            }
#endif
        }
        #endregion
    }
}
