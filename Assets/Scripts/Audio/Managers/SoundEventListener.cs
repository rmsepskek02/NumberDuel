using Objects;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 게임 이벤트를 구독하여 사운드 재생
    /// SoundManager의 자식으로 배치하여 함께 DontDestroyOnLoad 처리
    /// UnityEvent 기반으로 성능 최적화
    /// </summary>
    public class SoundEventListener : MonoBehaviour
    {
        #region Unity Lifecycle
        void Start()
        {
            SubscribeEvents();
        }

        void OnDestroy()
        {
            UnsubscribeEvents();
        }
        #endregion

        #region Event Subscription
        /// <summary>
        /// 모든 게임 이벤트 구독 (UnityEvent 방식)
        /// </summary>
        private void SubscribeEvents()
        {
            if (GameEventManager.Instance != null)
            {
                // Card Events
                GameEventManager.Instance.OnCardDrawn.AddListener(PlayDrawSound);
                GameEventManager.Instance.OnCardPlaced.AddListener(PlayPlaceSound);
                GameEventManager.Instance.OnCardAttack.AddListener(PlayAttackSound);
                GameEventManager.Instance.OnCardDestroyed.AddListener(PlayDestroySound);

                // Combat Events
                GameEventManager.Instance.OnOperatorUsed.AddListener(PlayOperatorSound);
                GameEventManager.Instance.OnDamageApplied.AddListener(PlayDamageSound);
                GameEventManager.Instance.OnSecretRevealed.AddListener(PlaySecretRevealSound);

                // Joker Events
                GameEventManager.Instance.OnJokerEffect.AddListener(PlayJokerEffectSound);

                // UI Events
                GameEventManager.Instance.OnTurnStarted.AddListener(PlayTurnStartSound);
                GameEventManager.Instance.OnMatchFound.AddListener(PlayMatchFoundSound);

                // Game Events
                GameEventManager.Instance.OnGameEnded.AddListener(PlayGameEndSound);
            }
        }

        /// <summary>
        /// 모든 게임 이벤트 구독 해제 (메모리 누수 방지)
        /// FindAnyObjectByType 사용으로 OnDestroy 중 GameObject 재생성 방지
        /// </summary>
        private void UnsubscribeEvents()
        {
            // FindAnyObjectByType를 사용하여 GameObject 재생성 방지
            // Instance getter는 OnDestroy 중에 새로운 GameObject를 생성할 수 있음
            var eventManager = FindAnyObjectByType<GameEventManager>();

            if (eventManager != null)
            {
                // Card Events
                eventManager.OnCardDrawn.RemoveListener(PlayDrawSound);
                eventManager.OnCardPlaced.RemoveListener(PlayPlaceSound);
                eventManager.OnCardAttack.RemoveListener(PlayAttackSound);
                eventManager.OnCardDestroyed.RemoveListener(PlayDestroySound);

                // Combat Events
                eventManager.OnOperatorUsed.RemoveListener(PlayOperatorSound);
                eventManager.OnDamageApplied.RemoveListener(PlayDamageSound);
                eventManager.OnSecretRevealed.RemoveListener(PlaySecretRevealSound);

                // Joker Events
                eventManager.OnJokerEffect.RemoveListener(PlayJokerEffectSound);

                // UI Events
                eventManager.OnTurnStarted.RemoveListener(PlayTurnStartSound);
                eventManager.OnMatchFound.RemoveListener(PlayMatchFoundSound);

                // Game Events
                eventManager.OnGameEnded.RemoveListener(PlayGameEndSound);
            }
        }
        #endregion

        #region Sound Playback Handlers
        // ===== Card Sounds =====
        /// <summary>
        /// 카드 드로우 사운드 재생
        /// </summary>
        private void PlayDrawSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.Card_Draw);
            }
        }

        /// <summary>
        /// 카드 배치 사운드 재생 (Secret 여부에 따라 다른 사운드)
        /// </summary>
        /// <param name="isSecret">Secret 모드 여부</param>
        private void PlayPlaceSound(bool isSecret)
        {
            if (SoundManager.Instance != null)
            {
                var soundType = isSecret ? SoundType.Card_PlaceSecret : SoundType.Card_PlaceNormal;
                SoundManager.Instance.PlaySFX(soundType);
            }
        }

        /// <summary>
        /// 카드 공격 사운드 재생
        /// </summary>
        private void PlayAttackSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.Card_Attack);
            }
        }

        /// <summary>
        /// 카드 파괴 사운드 재생
        /// </summary>
        private void PlayDestroySound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.Card_Destroy);
            }
        }

        // ===== Combat Sounds =====
        /// <summary>
        /// 연산자 사용 사운드 재생 (연산자 타입에 따라 다른 사운드)
        /// </summary>
        /// <param name="operatorType">연산자 타입</param>
        private void PlayOperatorSound(OperatorType operatorType)
        {
            if (SoundManager.Instance != null)
            {
                var soundType = operatorType switch
                {
                    OperatorType.Plus => SoundType.Combat_Plus,
                    OperatorType.Minus => SoundType.Combat_Minus,
                    OperatorType.Multiply => SoundType.Combat_Multiply,
                    OperatorType.Divide => SoundType.Combat_Divide,
                    _ => SoundType.Combat_Plus
                };
                SoundManager.Instance.PlaySFX(soundType);
            }
        }

        /// <summary>
        /// 데미지 적용 사운드 재생
        /// </summary>
        private void PlayDamageSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.Combat_Damage);
            }
        }

        /// <summary>
        /// 시크릿 공개 사운드 재생
        /// </summary>
        private void PlaySecretRevealSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.Combat_SecretReveal);
            }
        }

        // ===== Joker Sounds =====
        /// <summary>
        /// 조커 효과 사운드 재생 (조커 효과 타입에 따라 다른 사운드)
        /// </summary>
        /// <param name="effectType">조커 효과 타입</param>
        private void PlayJokerEffectSound(JokerEffectType effectType)
        {
            if (SoundManager.Instance != null)
            {
                var soundType = effectType switch
                {
                    JokerEffectType.Draw => SoundType.Joker_Draw,
                    JokerEffectType.Delete => SoundType.Joker_Delete,
                    JokerEffectType.Swap => SoundType.Joker_Swap,
                    _ => SoundType.Joker_Draw
                };
                SoundManager.Instance.PlaySFX(soundType);
            }
        }

        // ===== UI Sounds =====
        /// <summary>
        /// 턴 시작 사운드 재생
        /// </summary>
        private void PlayTurnStartSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.UI_TurnStart);
            }
        }

        /// <summary>
        /// 매칭 성공 사운드 재생
        /// </summary>
        private void PlayMatchFoundSound()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(SoundType.UI_MatchFound);
            }
        }

        // ===== Game Event Sounds =====
        /// <summary>
        /// 게임 종료 사운드 재생 (승리/패배에 따라 다른 사운드)
        /// </summary>
        /// <param name="isVictory">승리 여부</param>
        private void PlayGameEndSound(bool isVictory)
        {
            if (SoundManager.Instance != null)
            {
                var soundType = isVictory ? SoundType.Game_Victory : SoundType.Game_Defeat;
                SoundManager.Instance.PlaySFX(soundType);
            }
        }
        #endregion
    }
}
