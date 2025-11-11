using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 세션 토큰 기반 중복 로그인 방지 매니저
    /// Firebase Firestore에 세션 정보 저장 및 관리
    /// </summary>
    public class SessionManager : SingletonDontDestroy<SessionManager>
    {
        #region Fields and Properties
        private FirebaseFirestore db;
        private string currentSessionId;
        private bool isInitialized = false;

        private const string SESSIONS_COLLECTION = "sessions";
        private const float INIT_TIMEOUT = 10f; // 초기화 타임아웃 (10초)

        /// <summary>
        /// 현재 세션 ID
        /// </summary>
        public string CurrentSessionId => currentSessionId;

        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized => isInitialized;
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            // 이미 초기화 중이거나 완료된 경우 중복 호출 방지
            if (isInitialized)
            {
                Debug.Log("[SessionManager] 이미 초기화 완료됨");
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;
                    isInitialized = true;
                    Debug.Log("✅ SessionManager 초기화 완료");
                }
                else
                {
                    Debug.LogError($"❌ Firestore 초기화 실패: {task.Result}");
                    isInitialized = false;
                }
            });
        }

        /// <summary>
        /// Firestore 재초기화 시도 (초기화 실패 시 호출)
        /// </summary>
        public void RetryInitialization()
        {
            if (isInitialized)
            {
                Debug.Log("[SessionManager] 이미 초기화 완료됨 - 재초기화 불필요");
                return;
            }

            Debug.Log("[SessionManager] Firestore 재초기화 시도...");
            InitializeDatabase();
        }

        /// <summary>
        /// Firebase 초기화 완료 대기 (타임아웃 적용)
        /// </summary>
        /// <param name="timeout">타임아웃 시간 (초)</param>
        /// <returns>초기화 성공 여부</returns>
        public async Task<bool> WaitForInitialization(float timeout = INIT_TIMEOUT)
        {
            float elapsedTime = 0f;

            while (!isInitialized && elapsedTime < timeout)
            {
                await Task.Delay(100); // 0.1초마다 체크
                elapsedTime += 0.1f;
            }

            if (!isInitialized)
            {
                Debug.LogError($"⏱️ SessionManager 초기화 타임아웃 ({timeout}초)");
            }

            return isInitialized;
        }

        private void OnApplicationQuit()
        {
            // 앱 종료 시 세션 정리
            if (!string.IsNullOrEmpty(currentSessionId))
            {
                string uid = AuthManager.Instance.CurrentUserUID;
                if (!string.IsNullOrEmpty(uid))
                {
                    ClearSession(uid);
                }
            }
        }
        #endregion

        #region Public Methods - Session Check
        /// <summary>
        /// 로그인 시 세션 체크 (중복 로그인 확인)
        /// </summary>
        /// <param name="uid">사용자 UID</param>
        /// <returns>(중복 로그인 여부, 메시지)</returns>
        public async Task<(bool isDuplicate, string message)> CheckSession(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Firestore가 초기화되지 않았습니다.");
                return (false, "세션 체크 중 오류 발생");
            }

            try
            {
                // Firestore에서 현재 세션 정보 가져오기
                DocumentReference docRef = db.Collection(SESSIONS_COLLECTION).Document(uid);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    // 기존 세션이 존재함
                    Dictionary<string, object> data = snapshot.ToDictionary();
                    string existingSessionId = data.ContainsKey("sessionId") ? data["sessionId"].ToString() : "";
                    string lastActiveTime = data.ContainsKey("lastActiveTime") ? data["lastActiveTime"].ToString() : "";

                    Debug.Log($"⚠️ 이미 활성 세션이 존재합니다. SessionId: {existingSessionId}");

                    // TODO: 추후 팝업 처리 로직 추가
                    // 현재는 중복 로그인으로 판단
                    return (true, "이미 로그인 되어 있는 아이디입니다.");
                }

                // 세션이 없으면 정상 처리
                return (false, "로그인 가능");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 세션 체크 실패: {ex.Message}");
                return (false, "세션 체크 중 오류 발생");
            }
        }

        /// <summary>
        /// 새로운 세션 생성 (로그인 성공 시 호출)
        /// </summary>
        /// <param name="uid">사용자 UID</param>
        public async Task<bool> CreateSession(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                // 고유한 세션 ID 생성 (GUID 사용)
                currentSessionId = Guid.NewGuid().ToString();

                // 현재 타임스탬프
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // Firestore에 세션 정보 저장
                Dictionary<string, object> sessionData = new Dictionary<string, object>
                {
                    { "sessionId", currentSessionId },
                    { "lastActiveTime", timestamp },
                    { "deviceInfo", SystemInfo.deviceModel }
                };

                DocumentReference docRef = db.Collection(SESSIONS_COLLECTION).Document(uid);
                await docRef.SetAsync(sessionData);

                Debug.Log($"✅ 세션 생성 완료: {currentSessionId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 세션 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션 갱신 (주기적으로 호출하여 활성 상태 유지)
        /// </summary>
        /// <param name="uid">사용자 UID</param>
        public async Task<bool> RefreshSession(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(currentSessionId))
                {
                    Debug.LogWarning("⚠️ 현재 세션 ID가 없습니다.");
                    return false;
                }

                // 타임스탬프 업데이트
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                DocumentReference docRef = db.Collection(SESSIONS_COLLECTION).Document(uid);
                await docRef.UpdateAsync("lastActiveTime", timestamp);

                Debug.Log($"✅ 세션 갱신 완료: {timestamp}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 세션 갱신 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 세션 정리 (로그아웃 시 호출)
        /// </summary>
        /// <param name="uid">사용자 UID</param>
        public async Task<bool> ClearSession(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(uid))
                {
                    Debug.LogWarning("⚠️ UID가 비어있습니다.");
                    return false;
                }

                // Firestore에서 세션 정보 삭제
                DocumentReference docRef = db.Collection(SESSIONS_COLLECTION).Document(uid);
                await docRef.DeleteAsync();

                currentSessionId = null;
                Debug.Log($"✅ 세션 정리 완료: {uid}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 세션 정리 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Public Methods - Force Login (추후 구현용)
        /// <summary>
        /// [추후 구현] 기존 세션 강제 종료 및 로그인
        /// 팝업에서 '기존 접속 해제하고 로그인' 선택 시 사용
        /// </summary>
        /// <param name="uid">사용자 UID</param>
        public async Task<bool> ForceLogin(string uid)
        {
            try
            {
                // 기존 세션 삭제
                await ClearSession(uid);

                // 새로운 세션 생성
                return await CreateSession(uid);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 강제 로그인 실패: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}
