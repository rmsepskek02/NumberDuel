using UnityEngine;
using System;
using Objects;
using Utills;

namespace Manager
{
    /// <summary>
    /// 플레이어별 체력을 관리하는 매니저
    /// 데미지 적용, 게임 종료 조건 체크, UI 이벤트 발생
    /// </summary>
    public class HealthManager : Singleton<HealthManager>
    {
        [Header("체력 설정")]
        [SerializeField] private int maxHP = 30;
        [SerializeField] private bool enableDebugLog = true;

        // 현재 체력
        private int playerCurrentHP;
        private int opponentCurrentHP;

        // 체력 변경 이벤트
        public static event Action<CardZone.OwnerType, int, int> OnHealthChanged; // (플레이어, 이전HP, 현재HP)
        public static event Action<CardZone.OwnerType> OnPlayerDefeated; // (패배한 플레이어)

        #region Unity Lifecycle
        private void Start()
        {
            InitializeHealth();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 체력 초기화 (게임 시작 시)
        /// </summary>
        public void InitializeHealth()
        {
            playerCurrentHP = maxHP;
            opponentCurrentHP = maxHP;

            if (enableDebugLog)
                Debug.Log($"[HealthManager] 체력 초기화 완료 - Player: {playerCurrentHP}, Opponent: {opponentCurrentHP}");

            // 초기 상태 UI 업데이트
            OnHealthChanged?.Invoke(CardZone.OwnerType.Player, maxHP, playerCurrentHP);
            OnHealthChanged?.Invoke(CardZone.OwnerType.Opponent, maxHP, opponentCurrentHP);
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 지정된 플레이어에게 데미지 적용
        /// </summary>
        /// <param name="damage">가할 데미지 (양수)</param>
        /// <param name="target">데미지를 받을 플레이어</param>
        /// <returns>실제로 적용된 데미지</returns>
        public int ApplyDamage(int damage, CardZone.OwnerType target)
        {
            if (damage <= 0)
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[HealthManager] 잘못된 데미지 값: {damage}");
                return 0;
            }

            int previousHP = GetCurrentHP(target);
            int newHP = Mathf.Max(0, previousHP - damage);
            int actualDamage = previousHP - newHP;

            // 체력 업데이트
            SetHP(target, newHP);

            if (enableDebugLog)
                Debug.Log($"[HealthManager] {target}에게 {actualDamage} 데미지 적용 ({previousHP} → {newHP})");

            // 이벤트 발생
            OnHealthChanged?.Invoke(target, previousHP, newHP);

            // 게임 종료 체크
            if (newHP <= 0)
            {
                if (enableDebugLog)
                    Debug.Log($"[HealthManager] {target} 패배! HP가 0이 되었습니다.");

                OnPlayerDefeated?.Invoke(target);
            }

            return actualDamage;
        }

        /// <summary>
        /// 현재 체력 반환
        /// </summary>
        /// <param name="player">체력을 확인할 플레이어</param>
        /// <returns>현재 체력</returns>
        public int GetCurrentHP(CardZone.OwnerType player)
        {
            return player switch
            {
                CardZone.OwnerType.Player => playerCurrentHP,
                CardZone.OwnerType.Opponent => opponentCurrentHP,
                _ => 0
            };
        }

        /// <summary>
        /// 최대 체력 반환
        /// </summary>
        public int GetMaxHP() => maxHP;

        /// <summary>
        /// 게임이 종료되었는지 확인
        /// </summary>
        /// <returns>어느 한 쪽의 HP가 0 이하면 true</returns>
        public bool IsGameOver()
        {
            return playerCurrentHP <= 0 || opponentCurrentHP <= 0;
        }

        /// <summary>
        /// 승리한 플레이어 반환 (게임이 종료된 경우에만)
        /// </summary>
        /// <returns>승리한 플레이어, 게임이 진행 중이면 null</returns>
        public CardZone.OwnerType? GetWinner()
        {
            if (!IsGameOver()) return null;

            if (playerCurrentHP <= 0) return CardZone.OwnerType.Opponent;
            if (opponentCurrentHP <= 0) return CardZone.OwnerType.Player;

            return null; // 이론적으로 도달하지 않음
        }

        /// <summary>
        /// 체력 비율 반환 (UI에서 HP 바 업데이트용)
        /// </summary>
        /// <param name="player">비율을 확인할 플레이어</param>
        /// <returns>0.0 ~ 1.0 비율</returns>
        public float GetHealthRatio(CardZone.OwnerType player)
        {
            int currentHP = GetCurrentHP(player);
            return (float)currentHP / maxHP;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 플레이어의 체력 직접 설정 (내부 사용)
        /// </summary>
        /// <param name="player">설정할 플레이어</param>
        /// <param name="hp">새로운 체력 값</param>
        private void SetHP(CardZone.OwnerType player, int hp)
        {
            hp = Mathf.Clamp(hp, 0, maxHP);

            switch (player)
            {
                case CardZone.OwnerType.Player:
                    playerCurrentHP = hp;
                    break;
                case CardZone.OwnerType.Opponent:
                    opponentCurrentHP = hp;
                    break;
            }
        }
        #endregion

        #region Debug & Testing
        /// <summary>
        /// 디버그용 체력 설정
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugSetHP(CardZone.OwnerType player, int hp)
        {
            int previousHP = GetCurrentHP(player);
            SetHP(player, hp);
            OnHealthChanged?.Invoke(player, previousHP, hp);

            Debug.Log($"[HealthManager] DEBUG: {player} 체력 강제 설정 ({previousHP} → {hp})");
        }

        /// <summary>
        /// 현재 상태 로그 출력
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintStatus()
        {
            Debug.Log($"[HealthManager] 현재 상태 - Player: {playerCurrentHP}/{maxHP}, Opponent: {opponentCurrentHP}/{maxHP}");
        }

        /// <summary>
        /// 디버그 로그 활성화/비활성화
        /// </summary>
        public void SetDebugMode(bool enable) => enableDebugLog = enable;
        #endregion
    }
}