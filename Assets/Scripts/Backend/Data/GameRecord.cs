using System;
using Firebase.Firestore;

namespace Objects.Data
{
    /// <summary>
    /// Firestore에 저장되는 게임 전적 데이터
    /// </summary>
    [FirestoreData]
    public class GameRecord
    {
        [FirestoreProperty]
        public string Player1UID { get; set; }

        [FirestoreProperty]
        public string Player1Nickname { get; set; }

        [FirestoreProperty]
        public int Player1FinalHP { get; set; }

        [FirestoreProperty]
        public string Player2UID { get; set; }

        [FirestoreProperty]
        public string Player2Nickname { get; set; }

        [FirestoreProperty]
        public int Player2FinalHP { get; set; }

        [FirestoreProperty]
        public string WinnerUID { get; set; }

        [FirestoreProperty]
        public DateTime PlayedAt { get; set; }

        [FirestoreProperty]
        public int TurnCount { get; set; }

        public GameRecord()
        {
        }

        public GameRecord(
            string player1UID, string player1Nickname, int player1FinalHP,
            string player2UID, string player2Nickname, int player2FinalHP,
            string winnerUID, int turnCount)
        {
            Player1UID = player1UID;
            Player1Nickname = player1Nickname;
            Player1FinalHP = player1FinalHP;
            Player2UID = player2UID;
            Player2Nickname = player2Nickname;
            Player2FinalHP = player2FinalHP;
            WinnerUID = winnerUID;
            PlayedAt = DateTime.UtcNow;
            TurnCount = turnCount;
        }
    }
}
