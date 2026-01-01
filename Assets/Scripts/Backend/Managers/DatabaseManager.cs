using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Objects.Data;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// Firebase Firestore를 관리하는 싱글톤 매니저
    /// 사용자 프로필 및 게임 전적 데이터 처리 담당
    /// </summary>
    public class DatabaseManager : SingletonDontDestroy<DatabaseManager>
    {
        #region Fields and Properties
        private FirebaseFirestore db;
        private bool isInitialized = false;

        // Collection 이름 상수
        private const string USERS_COLLECTION = "users";
        private const string GAME_RECORDS_COLLECTION = "gameRecords";
        #endregion

        #region Initialization
        protected override void Awake()
        {
            base.Awake();

#if UNITY_EDITOR
            // 에디터에서 빌드 중일 때는 Firestore 초기화 스킵
            if (UnityEditor.BuildPipeline.isBuildingPlayer)
            {
                return;
            }
#endif

            InitializeFirestore();
        }

        private void InitializeFirestore()
        {
            // FirebaseInitializer가 이미 초기화를 완료했는지 확인
            if (FirebaseInitializer.IsFirebaseReady)
            {
                // 즉시 사용 가능
                InitializeFirestoreInstance();
            }
            else
            {
                // FirebaseInitializer 초기화 대기
                StartCoroutine(WaitForFirebaseInitializer());
            }
        }

        /// <summary>
        /// Firestore 인스턴스 초기화 (FirebaseInitializer 초기화 완료 후 호출)
        /// </summary>
        private void InitializeFirestoreInstance()
        {
            try
            {
                db = FirebaseFirestore.DefaultInstance;

                // 플랫폼별 오프라인 캐시 설정
                try
                {
#if UNITY_ANDROID || UNITY_IOS
                    // 모바일: 캐시 활성화 (성능 향상, 오프라인 지원)
                    db.Settings.PersistenceEnabled = true;
#else
                    // PC/에디터: 캐시 비활성화 (멀티 클라이언트 테스트 지원)
                    db.Settings.PersistenceEnabled = false;
#endif
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DatabaseManager] Firestore 설정 변경 실패 (이미 사용 중): {ex.Message}");
                }

                isInitialized = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DatabaseManager] ❌ Firestore 인스턴스 초기화 실패: {ex.Message}");
                isInitialized = false;
            }
        }

        /// <summary>
        /// FirebaseInitializer 초기화 완료 대기
        /// </summary>
        private System.Collections.IEnumerator WaitForFirebaseInitializer()
        {
            float elapsedTime = 0f;
            const float timeout = 15f; // 15초 타임아웃

            while (!FirebaseInitializer.IsFirebaseReady && elapsedTime < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsedTime += 0.1f;
            }

            if (FirebaseInitializer.IsFirebaseReady)
            {
                InitializeFirestoreInstance();
            }
            else
            {
                Debug.LogError($"[DatabaseManager] ⏱️ FirebaseInitializer 초기화 타임아웃 ({timeout}초)");
                Debug.LogError($"[DatabaseManager]    마지막 상태: {FirebaseInitializer.DependencyStatus}");
                isInitialized = false;
            }
        }
        #endregion

        #region User Profile Methods
        /// <summary>
        /// 닉네임 사용 가능 여부 확인 (중복 체크)
        /// </summary>
        /// <param name="nickname">확인할 닉네임</param>
        /// <returns>사용 가능하면 true, 중복이면 false</returns>
        public async Task<bool> IsNicknameAvailable(string nickname)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다. 초기화를 시도합니다...");
                await WaitForInitialization();

                if (!isInitialized)
                {
                    Debug.LogError("Firestore 초기화 실패.");
                    return false;
                }
            }

            try
            {
                // Firestore에서 닉네임으로 쿼리
                Query query = db.Collection(USERS_COLLECTION).WhereEqualTo("Nickname", nickname);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                // 결과가 없으면 사용 가능
                return snapshot.Count == 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"닉네임 중복 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 이메일 등록 여부 확인
        /// </summary>
        /// <param name="email">확인할 이메일</param>
        /// <returns>등록되어 있으면 true, 등록되지 않았으면 false</returns>
        public async Task<bool> IsEmailRegistered(string email)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다. 초기화를 시도합니다...");
                await WaitForInitialization();

                if (!isInitialized)
                {
                    Debug.LogError("Firestore 초기화 실패.");
                    return false;
                }
            }

            try
            {
                // Firestore에서 이메일로 쿼리
                Query query = db.Collection(USERS_COLLECTION).WhereEqualTo("Email", email);
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                // 결과가 있으면 등록된 이메일
                return snapshot.Count > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"이메일 등록 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 새로운 사용자 프로필 생성 (이메일 로그인용)
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="email">이메일</param>
        /// <param name="nickname">닉네임</param>
        /// <param name="emailVerified">이메일 인증 상태 (기본값: false)</param>
        /// <param name="authProvider">인증 제공자 (기본값: "Email")</param>
        public async Task<bool> CreateUserProfile(string uid, string email, string nickname, bool emailVerified = false, string authProvider = "Email")
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다. 초기화를 시도합니다...");
                await WaitForInitialization();

                if (!isInitialized)
                {
                    Debug.LogError("Firestore 초기화 실패.");
                    return false;
                }
            }

            try
            {
                // 새 프로필 생성
                UserProfile profile = new UserProfile(email, nickname, emailVerified, authProvider);

                // Firestore에 저장
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                await docRef.SetAsync(profile);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"사용자 프로필 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 소셜 로그인용 사용자 프로필 생성
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="email">이메일</param>
        /// <param name="nickname">닉네임</param>
        /// <param name="authProvider">인증 제공자 ("Google", "Kakao" 등)</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> CreateSocialUserProfile(string uid, string email, string nickname, string authProvider)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다. 초기화를 시도합니다...");
                await WaitForInitialization();

                if (!isInitialized)
                {
                    Debug.LogError("Firestore 초기화 실패.");
                    return false;
                }
            }

            try
            {
                // 소셜 로그인용 프로필 생성 (소셜 로그인은 기본적으로 이메일 인증됨)
                UserProfile profile = new UserProfile(email, nickname, emailVerified: true, authProvider: authProvider);

                // Firestore에 저장
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                await docRef.SetAsync(profile);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] 소셜 로그인 프로필 생성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 사용자 프로필 가져오기
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <returns>사용자 프로필 또는 null</returns>
        public async Task<UserProfile> GetUserProfile(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다. 초기화를 시도합니다...");
                await WaitForInitialization();

                if (!isInitialized)
                {
                    Debug.LogError("Firestore 초기화 실패.");
                    return null;
                }
            }

            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    UserProfile profile = snapshot.ConvertTo<UserProfile>();
                    return profile;
                }
                else
                {
                    Debug.LogWarning($"사용자 프로필을 찾을 수 없습니다: {uid}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"사용자 프로필 로드 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 마지막 로그인 시간 업데이트
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        public async Task<bool> UpdateLastLogin(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                await docRef.UpdateAsync("LastLoginAt", DateTime.UtcNow);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"마지막 로그인 시간 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 사용자 닉네임 업데이트
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="newNickname">새 닉네임</param>
        public async Task<bool> UpdateNickname(string uid, string newNickname)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                await docRef.UpdateAsync("Nickname", newNickname);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"닉네임 업데이트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 사용자의 AuthProvider 업데이트 (게스트 → SNS 전환 시)
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="newAuthProvider">새로운 AuthProvider 값 (예: "Google", "Email")</param>
        public async Task UpdateUserAuthProvider(string uid, string newAuthProvider)
        {
            if (!isInitialized)
            {
                Debug.LogError("[DatabaseManager] Firestore가 초기화되지 않았습니다.");
                throw new InvalidOperationException("Firestore가 초기화되지 않았습니다.");
            }

            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);

                var updates = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "AuthProvider", newAuthProvider },
                    { "LastLoginAt", DateTime.UtcNow }
                };

                await docRef.UpdateAsync(updates);

                Debug.Log($"[DatabaseManager] AuthProvider 업데이트 완료: {uid} → {newAuthProvider}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] AuthProvider 업데이트 실패: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 이메일 인증 상태 업데이트 (로그인 시 호출)
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="verified">이메일 인증 상태</param>
        public async Task<bool> UpdateEmailVerified(string uid, bool verified)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                await docRef.UpdateAsync(new System.Collections.Generic.Dictionary<string, object>
                {
                    { "EmailVerified", verified },
                    { "LastLoginAt", DateTime.UtcNow }
                });

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"이메일 인증 상태 업데이트 실패: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Game Record Methods
        /// <summary>
        /// 게임 결과 저장
        /// </summary>
        /// <param name="record">게임 전적 데이터</param>
        public async Task<bool> SaveGameRecord(GameRecord record)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다.");
                return false;
            }

            try
            {
                // 게임 기록 저장
                CollectionReference gamesRef = db.Collection(GAME_RECORDS_COLLECTION);
                await gamesRef.AddAsync(record);

                // 두 플레이어의 통계 업데이트
                await UpdatePlayerStats(record.Player1UID, record.WinnerUID == record.Player1UID);
                await UpdatePlayerStats(record.Player2UID, record.WinnerUID == record.Player2UID);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"게임 결과 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 플레이어 통계 업데이트
        /// 연결 끊김 등 상대방 정보 없이 전적만 기록할 때 사용
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="isWin">승리 여부</param>
        public async Task UpdatePlayerStats(string uid, bool isWin)
        {
            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    UserProfile profile = snapshot.ConvertTo<UserProfile>();

                    // 통계 업데이트
                    profile.Stats.TotalGames++;
                    if (isWin)
                        profile.Stats.Wins++;
                    else
                        profile.Stats.Losses++;

                    // Firestore에 저장
                    await docRef.UpdateAsync(new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "Stats.TotalGames", profile.Stats.TotalGames },
                        { "Stats.Wins", profile.Stats.Wins },
                        { "Stats.Losses", profile.Stats.Losses }
                    });

                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"플레이어 통계 업데이트 실패: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 사용자 프로필이 존재하는지 확인
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <returns>존재 여부</returns>
        public async Task<bool> UserProfileExists(string uid)
        {
            if (!isInitialized)
            {
                Debug.LogError("Firestore가 초기화되지 않았습니다. 초기화를 시도합니다...");
                await WaitForInitialization();

                if (!isInitialized)
                {
                    Debug.LogError("Firestore 초기화 실패.");
                    return false;
                }
            }

            try
            {
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                return snapshot.Exists;
            }
            catch (Exception ex)
            {
                Debug.LogError($"프로필 존재 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Firestore 초기화 대기
        /// 최대 10초 대기
        /// </summary>
        private async Task WaitForInitialization()
        {
            int waitCount = 0;
            int maxWait = 100; // 10초 (100 * 100ms)

            while (!isInitialized && waitCount < maxWait)
            {
                await Task.Delay(100);
                waitCount++;
            }

            if (!isInitialized)
            {
                Debug.LogError("DatabaseManager 초기화 타임아웃");
            }
        }

        // TODO: 미인증 계정 자동 삭제 기능 (Firebase Cloud Functions로 구현 예정)
        //
        // Firebase Cloud Functions를 사용하여 매일 자정에 실행되는 스케줄러 구현:
        // 1. emailVerified가 false이고 createdAt이 7일 이상 지난 계정 찾기
        // 2. Firestore 프로필 삭제
        // 3. Firebase Authentication 계정 삭제 (Admin SDK 사용)
        //
        // 구현 방법:
        // - Firebase CLI로 Cloud Functions 초기화: firebase init functions
        // - functions/index.js에 스케줄러 함수 작성
        // - firebase deploy --only functions로 배포
        //
        // 참고: Cloud Functions는 별도 비용이 발생할 수 있으므로 Firebase Console에서 요금제 확인 필요
        #endregion
    }
}
