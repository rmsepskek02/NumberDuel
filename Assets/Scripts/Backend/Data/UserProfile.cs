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
        public bool EmailVerified { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public DateTime LastLoginAt { get; set; }

        [FirestoreProperty]
        public UserStats Stats { get; set; }

        // ⭐ Google 로그인 지원 추가
        [FirestoreProperty]
        public string AuthProvider { get; set; } // "Email", "Google", "Email+Google"

        [FirestoreProperty]
        public string PhotoUrl { get; set; } // 프로필 사진 URL (Google 제공)

        // TODO: 프로필 사진 UI 표시 기능 구현 필요

        public UserProfile()
        {
            Stats = new UserStats();
            EmailVerified = false;
            AuthProvider = "Email"; // 기본값
            PhotoUrl = string.Empty;
        }

        public UserProfile(string email, string nickname, bool emailVerified = false)
        {
            Email = email;
            Nickname = nickname;
            EmailVerified = emailVerified;
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
            Stats = new UserStats();
            AuthProvider = "Email"; // 기본값
            PhotoUrl = string.Empty;
        }

        /// <summary>
        /// 소셜 로그인용 생성자
        /// </summary>
        public UserProfile(string email, string nickname, string authProvider, string photoUrl = "")
        {
            Email = email;
            Nickname = nickname;
            EmailVerified = true; // 소셜 로그인은 기본 인증됨
            CreatedAt = DateTime.UtcNow;
            LastLoginAt = DateTime.UtcNow;
            Stats = new UserStats();
            AuthProvider = authProvider;
            PhotoUrl = photoUrl ?? string.Empty;
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
