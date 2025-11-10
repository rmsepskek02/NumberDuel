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
            InitializeFirestore();
        }

        private void InitializeFirestore()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;

                    // 오프라인 캐시 비활성화 (멀티 인스턴스 지원)
                    try
                    {
                        db.Settings.PersistenceEnabled = false;
                        Debug.Log("✅ Firestore 오프라인 캐시 비활성화");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"⚠️ Firestore 설정 변경 실패 (이미 사용 중): {ex.Message}");
                    }

                    isInitialized = true;
                    Debug.Log("✅ DatabaseManager 초기화 완료");
                }
                else
                {
                    Debug.LogError($"❌ Firestore 초기화 실패: {task.Result}");
                }
            });
        }
        #endregion

        #region User Profile Methods
        /// <summary>
        /// 새로운 사용자 프로필 생성
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="email">이메일</param>
        /// <param name="nickname">닉네임</param>
        public async Task<bool> CreateUserProfile(string uid, string email, string nickname)
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
                UserProfile profile = new UserProfile(email, nickname);

                // Firestore에 저장
                DocumentReference docRef = db.Collection(USERS_COLLECTION).Document(uid);
                await docRef.SetAsync(profile);

                Debug.Log($"✅ 사용자 프로필 생성 완료: {nickname} ({uid})");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 사용자 프로필 생성 실패: {ex.Message}");
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
                    Debug.Log($"✅ 사용자 프로필 로드 완료: {profile.Nickname}");
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
                Debug.LogError($"❌ 사용자 프로필 로드 실패: {ex.Message}");
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

                Debug.Log($"✅ 마지막 로그인 시간 업데이트 완료: {uid}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 마지막 로그인 시간 업데이트 실패: {ex.Message}");
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

                Debug.Log($"✅ 닉네임 업데이트 완료: {newNickname}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 닉네임 업데이트 실패: {ex.Message}");
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

                Debug.Log($"✅ 게임 결과 저장 완료");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 게임 결과 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 플레이어 통계 업데이트
        /// </summary>
        /// <param name="uid">Firebase UID</param>
        /// <param name="isWin">승리 여부</param>
        private async Task UpdatePlayerStats(string uid, bool isWin)
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

                    Debug.Log($"✅ 플레이어 통계 업데이트 완료: {uid} (승리: {isWin})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ 플레이어 통계 업데이트 실패: {ex.Message}");
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
                Debug.LogError($"❌ 프로필 존재 확인 실패: {ex.Message}");
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
        #endregion
    }
}
