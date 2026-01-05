using Objects;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Utills;

namespace Manager
{
    /// <summary>
    /// Splash 화면을 관리하는 매니저
    /// 자동 로그인 체크 후 JoinScene 또는 LobbyScene으로 이동
    /// </summary>
    public class SplashManager : MonoBehaviour
    {
        #region Unity Lifecycle
        void Start()
        {
#if UNITY_EDITOR
            // 에디터에서 빌드 중일 때는 실행하지 않음
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                return;
            }
#endif

            // 모든 버튼에 클릭 사운드 자동 등록
            UIHelper.RegisterAllButtonSounds();

            StartCoroutine(CheckAutoLoginAndNavigate());
        }
        #endregion

        #region Auto Login Check
        /// <summary>
        /// 자동 로그인 체크 후 씬 전환
        /// </summary>
        private IEnumerator CheckAutoLoginAndNavigate()
        {
            // FirebaseInitializer 초기화 대기 (최우선)
            if (!FirebaseInitializer.IsFirebaseReady)
            {
                float elapsedTime = 0f;
                while (!FirebaseInitializer.IsFirebaseReady && elapsedTime < 15f)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                if (!FirebaseInitializer.IsFirebaseReady)
                {
                    Debug.LogError($"[SplashManager] ❌ FirebaseInitializer 초기화 실패 ({FirebaseInitializer.DependencyStatus})");
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }
            }

            // AuthManager 초기화 대기
            if (!AuthManager.Instance.IsInitialized)
            {
                float elapsedTime = 0f;
                while (!AuthManager.Instance.IsInitialized && elapsedTime < 10f)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                if (!AuthManager.Instance.IsInitialized)
                {
                    Debug.LogError("[SplashManager] ❌ AuthManager 초기화 타임아웃");
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }
            }

            // SessionManager 초기화 대기
            if (!SessionManager.Instance.IsInitialized)
            {
                float elapsedTime = 0f;
                while (!SessionManager.Instance.IsInitialized && elapsedTime < 10f)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsedTime += 0.1f;
                }

                if (!SessionManager.Instance.IsInitialized)
                {
                    Debug.LogError("[SplashManager] ❌ SessionManager 초기화 타임아웃");
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }
            }

            // 자동 로그인 가능 여부 확인
            if (AuthManager.Instance.CanAutoLogin())
            {
                // 이메일 전용 계정 + 인증 미완료 → 자동 로그인 차단
                if (AuthManager.Instance.IsEmailOnlyAccount() &&
                    !AuthManager.Instance.IsEmailVerified)
                {
                    AuthManager.Instance.SignOutWithoutSessionClear();
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }

                // 시스템 메시지: 세션 복원
                if (SystemMessageManager.Instance != null)
                {
                    SystemMessageManager.Instance.ShowMessage("SessionRestored");
                }

                // 세션 체크
                string uid = AuthManager.Instance.CurrentUserUID;
                var sessionCheckTask = SessionManager.Instance.CheckSession(uid);
                yield return new WaitUntil(() => sessionCheckTask.IsCompleted);

                var sessionCheck = sessionCheckTask.Result;

                if (sessionCheck.isDuplicate)
                {
                    AuthManager.Instance.Logout();
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }

                // 세션 생성
                var sessionCreateTask = SessionManager.Instance.CreateSession(uid);
                yield return new WaitUntil(() => sessionCreateTask.IsCompleted);

                if (!sessionCreateTask.Result)
                {
                    AuthManager.Instance.Logout();
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }

                // 프로필 로드
                var profileTask = DatabaseManager.Instance.GetUserProfile(uid);
                yield return new WaitUntil(() => profileTask.IsCompleted);

                var profile = profileTask.Result;

                if (profile == null)
                {
                    AuthManager.Instance.Logout();
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }

                // Photon 닉네임 설정
                PhotonNetwork.NickName = profile.Nickname;

                // Photon Custom Property에 Firebase UID 저장
                ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "FirebaseUID", uid }
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);

                // 한국 리전 설정
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";

                // Photon 연결
                PhotonNetwork.ConnectUsingSettings();

                // Photon 연결 대기
                float photonElapsedTime = 0f;
                while (!PhotonNetwork.IsConnectedAndReady && photonElapsedTime < 10f)
                {
                    yield return new WaitForSeconds(0.1f);
                    photonElapsedTime += 0.1f;
                }

                if (!PhotonNetwork.IsConnectedAndReady)
                {
                    Debug.LogError("[SplashManager] Photon 연결 실패 - 로그인 화면으로 이동");
                    AuthManager.Instance.Logout();
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }

                // Photon 로비 진입
                PhotonNetwork.JoinLobby();

                // 로비 진입 완료 대기 (OnJoinedLobby 콜백이 실행될 때까지)
                float joinLobbyElapsedTime = 0f;
                const float JOIN_LOBBY_TIMEOUT = 10f;

                while (!PhotonNetwork.InLobby && joinLobbyElapsedTime < JOIN_LOBBY_TIMEOUT)
                {
                    yield return new WaitForSeconds(0.1f);
                    joinLobbyElapsedTime += 0.1f;
                }

                if (!PhotonNetwork.InLobby)
                {
                    Debug.LogError("[SplashManager] 로비 진입 타임아웃 - 로그인 화면으로 이동");
                    AuthManager.Instance.Logout();
                    SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
                    yield break;
                }

                SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.LobbyScene));
            }
            else
            {
                SceneManager.LoadScene(SceneNameExtensions.GetSceneName(SceneName.JoinScene));
            }
        }
        #endregion
    }
}
