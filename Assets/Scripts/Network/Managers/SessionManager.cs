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
        private string cachedUserUID; // 캐시된 사용자 UID (OnDestroy에서 AuthManager 참조 불필요)
        private bool isInitialized = false;
        private ListenerRegistration sessionListener; // Firestore 실시간 리스너
        private string currentMonitoringUid; // 현재 모니터링 중인 UID
        private bool isFirstListenerCall = true; // 리스너 첫 호출 여부 (초기 호출 무시용)

        private const string SESSIONS_COLLECTION = "sessions";
        private const float INIT_TIMEOUT = 10f; // 초기화 타임아웃 (10초)

        /// <summary>
        /// 강제 로그아웃 이벤트 (다른 곳에서 로그인하여 세션이 종료될 때 호출)
        /// </summary>
        public event Action OnForceLogout;

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
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;
                    isInitialized = true;
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
                return;
            }

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
            // 1. Firestore 리스너 중지
            StopSessionMonitoring();

            // 2. 세션 정리 (캐시된 UID 사용 - AuthManager 참조 불필요!)
            if (!string.IsNullOrEmpty(cachedUserUID) &&
                !string.IsNullOrEmpty(currentSessionId))
            {
                _ = ClearSession(cachedUserUID);
            }
        }

        protected override void OnDestroy()
        {
            // Firestore 리스너 중지 (중복 호출 안전)
            StopSessionMonitoring();

            // 세션 정리 (OnApplicationQuit에서 실패했을 경우 재시도)
            if (!string.IsNullOrEmpty(cachedUserUID) &&
                !string.IsNullOrEmpty(currentSessionId))
            {
                _ = ClearSession(cachedUserUID);
            }

            // 베이스 클래스의 OnDestroy 호출 (싱글톤 인스턴스 정리)
            base.OnDestroy();
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
                // UID 캐싱 (OnDestroy에서 사용)
                cachedUserUID = uid;

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
                cachedUserUID = null; // 캐시도 정리
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 세션 정리 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Public Methods - Force Login
        /// <summary>
        /// 기존 세션 강제 종료 및 로그인
        /// 팝업에서 '기존 접속 해제하고 로그인' 선택 시 사용
        ///
        /// 방안 1: 세션 문서 덮어쓰기 방식
        /// - 기존 세션을 삭제하지 않고 SetAsync()로 덮어씀
        /// - Client A의 Firestore 리스너가 sessionId 변경을 감지하여 강제 로그아웃 트리거
        /// - 삭제-생성 타이밍 이슈 해결
        /// </summary>
        /// <param name="uid">사용자 UID</param>
        public async Task<bool> ForceLogin(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                // UID 캐싱 (OnDestroy에서 사용)
                cachedUserUID = uid;

                // 새로운 세션 ID 생성 (GUID 사용)
                currentSessionId = Guid.NewGuid().ToString();

                // 현재 타임스탬프
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

                // Firestore에 세션 정보 덮어쓰기 (기존 세션 삭제하지 않음)
                Dictionary<string, object> sessionData = new Dictionary<string, object>
                {
                    { "sessionId", currentSessionId },
                    { "lastActiveTime", timestamp },
                    { "deviceInfo", SystemInfo.deviceModel }
                };

                DocumentReference docRef = db.Collection(SESSIONS_COLLECTION).Document(uid);
                await docRef.SetAsync(sessionData); // SetAsync로 덮어쓰기

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 강제 로그인 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Public Methods - Session Monitoring
        /// <summary>
        /// 세션 실시간 모니터링 시작
        /// 다른 곳에서 로그인하여 세션이 변경/삭제되면 자동으로 강제 로그아웃
        /// </summary>
        /// <param name="uid">모니터링할 사용자 UID</param>
        public void StartSessionMonitoring(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("❌ Firestore가 초기화되지 않았습니다.");
                return;
            }

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("⚠️ UID가 비어있습니다.");
                return;
            }

            // 이미 모니터링 중이면 중지 후 재시작
            if (sessionListener != null)
            {
                StopSessionMonitoring();
            }

            currentMonitoringUid = uid;
            isFirstListenerCall = true; // 첫 호출 플래그 초기화

            // Firestore 실시간 리스너 등록
            DocumentReference docRef = db.Collection(SESSIONS_COLLECTION).Document(uid);
            sessionListener = docRef.Listen(snapshot =>
            {
                OnSessionDocumentChanged(snapshot, uid);
            });
        }

        /// <summary>
        /// 세션 모니터링 중지
        /// </summary>
        public void StopSessionMonitoring()
        {
            if (sessionListener != null)
            {
                sessionListener.Stop();
                sessionListener = null;
                currentMonitoringUid = null;
            }
        }

        /// <summary>
        /// Firestore 세션 문서 변경 감지 콜백
        /// </summary>
        private void OnSessionDocumentChanged(DocumentSnapshot snapshot, string uid)
        {
            if (snapshot == null)
            {
                Debug.LogWarning("⚠️ 세션 스냅샷이 null입니다.");
                return;
            }

            // 🔥 리스너 첫 호출은 무시 (모니터링 시작 시 현재 문서를 읽어오는 것이므로)
            if (isFirstListenerCall)
            {
                isFirstListenerCall = false;
                return;
            }

            // 세션 문서가 삭제되었거나 존재하지 않는 경우
            if (!snapshot.Exists)
            {
                Debug.LogWarning("⚠️ 세션이 삭제되었습니다. 강제 로그아웃 처리합니다.");
                HandleForceLogout();
                return;
            }

            // 세션 ID가 변경된 경우 (다른 곳에서 로그인)
            Dictionary<string, object> data = snapshot.ToDictionary();
            if (data.ContainsKey("sessionId"))
            {
                string newSessionId = data["sessionId"].ToString();

                // 현재 세션 ID와 다르면 강제 로그아웃
                if (!string.IsNullOrEmpty(currentSessionId) && newSessionId != currentSessionId)
                {
                    Debug.LogWarning($"⚠️ 세션 ID가 변경되었습니다. (현재: {currentSessionId}, 새로운: {newSessionId})");
                    Debug.LogWarning("⚠️ 다른 곳에서 로그인되었습니다. 강제 로그아웃 처리합니다.");
                    HandleForceLogout();
                }
            }
        }

        /// <summary>
        /// 강제 로그아웃 처리
        /// </summary>
        private void HandleForceLogout()
        {
            // 모니터링 중지
            StopSessionMonitoring();

            // 이벤트 호출 (UI 등에서 구독하여 처리)
            OnForceLogout?.Invoke();
        }
        #endregion
    }
}
