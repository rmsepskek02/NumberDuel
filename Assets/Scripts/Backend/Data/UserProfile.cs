using System;
using Firebase.Firestore;

namespace Objects.Data
{
    /// <summary>
    /// Firestore에 저장되는 사용자 프로필 데이터
    /// </summary>
    [FirestoreData]
    public class UserProfile
    {
        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string Nickname { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public DateTime LastLoginAt { get; set; }

        [FirestoreProperty]
        public UserStats Stats { get; set; }

        public UserProfile()
        {
            Stats = new UserStats();
        }

        public UserProfile(string email, string nickname)
        {
            Email = email;
            Nickname = nickname;
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
            Stats = new UserStats();
        }
    }

    /// <summary>
    /// 사용자 게임 통계
    /// </summary>
    [FirestoreData]
    public class UserStats
    {
        [FirestoreProperty]
        public int TotalGames { get; set; }

        [FirestoreProperty]
        public int Wins { get; set; }

        [FirestoreProperty]
        public int Losses { get; set; }

        [FirestoreProperty]
        public float WinRate
        {
            get
            {
                if (TotalGames == 0) return 0f;
                return (float)Wins / TotalGames * 100f;
            }
            set { } // Firestore 직렬화를 위한 빈 setter
        }

        public UserStats()
        {
            TotalGames = 0;
            Wins = 0;
            Losses = 0;
        }
    }
}
