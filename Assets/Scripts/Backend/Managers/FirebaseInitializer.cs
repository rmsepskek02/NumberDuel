using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// Firebase 중앙 초기화 관리자
    /// 모든 Firebase 서비스의 의존성 체크를 한 번만 수행하여 초기화 시간 단축
    /// </summary>
    public class FirebaseInitializer : SingletonDontDestroy<FirebaseInitializer>
    {
        #region Fields and Properties
        private static bool isFirebaseReady = false;
        private static DependencyStatus lastDependencyStatus = DependencyStatus.UnavailableOther;

        private const float INIT_TIMEOUT = 15f; // 초기화 타임아웃 (15초)

        /// <summary>
        /// Firebase 초기화 완료 여부
        /// </summary>
        public static bool IsFirebaseReady => isFirebaseReady;

        /// <summary>
        /// 마지막 의존성 체크 결과
        /// </summary>
        public static DependencyStatus DependencyStatus => lastDependencyStatus;
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            // 에디터에서 빌드 중일 때는 Firebase 초기화 스킵
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                Debug.Log("[FirebaseInitializer] 빌드 중 - Firebase 초기화 스킵");
                return;
            }
#endif

            InitializeFirebaseOnce();
        }

        /// <summary>
        /// Firebase 의존성 체크 및 초기화 (한 번만 수행)
        /// </summary>
        private async void InitializeFirebaseOnce()
        {
            // 이미 초기화 중이거나 완료된 경우 중복 호출 방지
            if (isFirebaseReady)
            {
                Debug.Log("[FirebaseInitializer] 이미 초기화 완료됨");
                return;
            }

            Debug.Log("[FirebaseInitializer] Firebase 초기화 시작...");

            try
            {
                // Firebase 의존성 체크 (비동기)
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                lastDependencyStatus = dependencyStatus;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    isFirebaseReady = true;
                    Debug.Log("[FirebaseInitializer] ✅ Firebase 초기화 완료!");
                }
                else
                {
                    isFirebaseReady = false;
                    Debug.LogError($"[FirebaseInitializer] ❌ Firebase 초기화 실패: {dependencyStatus}");
                }
            }
            catch (System.Exception ex)
            {
                isFirebaseReady = false;
                lastDependencyStatus = DependencyStatus.UnavailableOther;
                Debug.LogError($"[FirebaseInitializer] ❌ Firebase 초기화 예외: {ex.Message}");
            }
        }

        /// <summary>
        /// Firebase 초기화 완료 대기 (타임아웃 적용)
        /// </summary>
        /// <param name="timeout">타임아웃 시간 (초)</param>
        /// <returns>초기화 성공 여부</returns>
        public static async Task<bool> WaitForInitialization(float timeout = INIT_TIMEOUT)
        {
            float elapsedTime = 0f;

            while (!isFirebaseReady && elapsedTime < timeout)
            {
                await Task.Delay(100); // 0.1초마다 체크
                elapsedTime += 0.1f;
            }

            if (!isFirebaseReady)
            {
                Debug.LogError($"⏱️ FirebaseInitializer 초기화 타임아웃 ({timeout}초)");
                Debug.LogError($"   마지막 상태: {lastDependencyStatus}");
            }

            return isFirebaseReady;
        }

        /// <summary>
        /// Firebase 재초기화 시도 (초기화 실패 시 호출)
        /// </summary>
        public void RetryInitialization()
        {
            if (isFirebaseReady)
            {
                Debug.Log("[FirebaseInitializer] 이미 초기화 완료됨 - 재초기화 불필요");
                return;
            }

            Debug.Log("[FirebaseInitializer] Firebase 재초기화 시도...");
            InitializeFirebaseOnce();
        }
        #endregion
    }
}
