using UnityEngine;
using UnityEngine.Events;
using Utills;

namespace Manager
{
    /// <summary>
    /// 게임 전체에서 사용하는 이벤트를 관리하는 싱글톤 클래스
    /// 게임 로직과 사운드/VFX 등의 부가 효과를 분리하기 위한 중앙 이벤트 허브
    /// UnityEvent 기반으로 성능 최적화 및 Inspector 연결 지원
    /// </summary>
    public class GameEventManager : SingletonDontDestroy<GameEventManager>
    {
        protected override void OnDestroy()
        {
            // 모든 UnityEvent 리스너 정리
            OnCardDrawn?.RemoveAllListeners();
            OnCardPlaced?.RemoveAllListeners();
            OnCardAttack?.RemoveAllListeners();
            OnCardDestroyed?.RemoveAllListeners();
            OnOperatorUsed?.RemoveAllListeners();
            OnDamageApplied?.RemoveAllListeners();
            OnSecretRevealed?.RemoveAllListeners();
            OnJokerEffect?.RemoveAllListeners();
            OnTurnStarted?.RemoveAllListeners();
            OnMatchFound?.RemoveAllListeners();
            OnGameEnded?.RemoveAllListeners();

            // 베이스 클래스의 OnDestroy 호출 (싱글톤 인스턴스 정리)
            base.OnDestroy();
        }

        #region UnityEvents
        // ===== Card Events =====
        /// <summary>
        /// 카드 드로우 이벤트 (파라미터 없음)
        /// </summary>
        public UnityEvent OnCardDrawn = new UnityEvent();

        /// <summary>
        /// 카드 배치 이벤트 (Secret 여부)
        /// </summary>
        public UnityEvent<bool> OnCardPlaced = new UnityEvent<bool>();

        /// <summary>
        /// 카드 공격 이벤트
        /// </summary>
        public UnityEvent OnCardAttack = new UnityEvent();

        /// <summary>
        /// 카드 파괴 이벤트
        /// </summary>
        public UnityEvent OnCardDestroyed = new UnityEvent();

        // ===== Combat Events =====
        /// <summary>
        /// 연산자 사용 이벤트 (필드 카드에 적용되는 순간)
        /// </summary>
        public UnityEvent<Objects.OperatorType> OnOperatorUsed = new UnityEvent<Objects.OperatorType>();

        /// <summary>
        /// 데미지 적용 이벤트
        /// </summary>
        public UnityEvent OnDamageApplied = new UnityEvent();

        /// <summary>
        /// 시크릿 공개 이벤트
        /// </summary>
        public UnityEvent OnSecretRevealed = new UnityEvent();

        // ===== Joker Events =====
        /// <summary>
        /// 조커 효과 이벤트 (필드 카드에 적용되는 순간)
        /// </summary>
        public UnityEvent<Objects.JokerEffectType> OnJokerEffect = new UnityEvent<Objects.JokerEffectType>();

        // ===== UI Events =====
        /// <summary>
        /// 턴 시작 이벤트
        /// </summary>
        public UnityEvent OnTurnStarted = new UnityEvent();

        /// <summary>
        /// 매칭 성공 이벤트
        /// </summary>
        public UnityEvent OnMatchFound = new UnityEvent();

        // ===== Game Events =====
        /// <summary>
        /// 게임 종료 이벤트 (승리 여부)
        /// </summary>
        public UnityEvent<bool> OnGameEnded = new UnityEvent<bool>();
        #endregion

        #region Event Trigger Methods
        // ===== Card Events =====
        /// <summary>
        /// 카드 드로우 이벤트 발생
        /// InGameManager.DrawCardsToHand()에서 호출
        /// </summary>
        public void TriggerCardDrawn()
        {
            OnCardDrawn?.Invoke();
        }

        /// <summary>
        /// 카드 배치 이벤트 발생
        /// CardZone.AddCard() 또는 NetworkCardSyncManager에서 호출
        /// </summary>
        /// <param name="isSecret">Secret 모드로 배치되었는지 여부</param>
        public void TriggerCardPlaced(bool isSecret)
        {
            OnCardPlaced?.Invoke(isSecret);
        }

        /// <summary>
        /// 카드 공격 이벤트 발생
        /// FieldAttackManager.ExecuteAttack()에서 호출
        /// </summary>
        public void TriggerCardAttack()
        {
            OnCardAttack?.Invoke();
        }

        /// <summary>
        /// 카드 파괴 이벤트 발생
        /// Card.AnimateRemoval()에서 호출
        /// </summary>
        public void TriggerCardDestroyed()
        {
            OnCardDestroyed?.Invoke();
        }

        // ===== Combat Events =====
        /// <summary>
        /// 연산자 사용 이벤트 발생 (필드 카드에 적용되는 순간)
        /// OperatorManager.ProcessOperation()에서 호출
        /// </summary>
        /// <param name="operatorType">사용된 연산자 타입</param>
        public void TriggerOperatorUsed(Objects.OperatorType operatorType)
        {
            OnOperatorUsed?.Invoke(operatorType);
        }

        /// <summary>
        /// 데미지 적용 이벤트 발생
        /// HealthManager.ApplyDamage()에서 호출
        /// </summary>
        public void TriggerDamageApplied()
        {
            OnDamageApplied?.Invoke();
        }

        /// <summary>
        /// 시크릿 공개 이벤트 발생
        /// Card.SetSecretMode(false)에서 호출
        /// </summary>
        public void TriggerSecretRevealed()
        {
            OnSecretRevealed?.Invoke();
        }

        // ===== Joker Events =====
        /// <summary>
        /// 조커 효과 이벤트 발생 (필드 카드에 적용되는 순간)
        /// JokerModeSelector.ExecuteXXXEffect()에서 호출
        /// </summary>
        /// <param name="effectType">조커 효과 타입</param>
        public void TriggerJokerEffect(Objects.JokerEffectType effectType)
        {
            OnJokerEffect?.Invoke(effectType);
        }

        // ===== UI Events =====
        /// <summary>
        /// 턴 시작 이벤트 발생
        /// TurnManager에서 호출
        /// </summary>
        public void TriggerTurnStarted()
        {
            OnTurnStarted?.Invoke();
        }

        /// <summary>
        /// 매칭 성공 이벤트 발생
        /// PhotonManager 또는 LobbyManager에서 호출
        /// </summary>
        public void TriggerMatchFound()
        {
            OnMatchFound?.Invoke();
        }

        // ===== Game Events =====
        /// <summary>
        /// 게임 종료 이벤트 발생
        /// InGameManager.CheckGameEnd()에서 호출
        /// </summary>
        /// <param name="isVictory">승리 여부 (true: 승리, false: 패배)</param>
        public void TriggerGameEnded(bool isVictory)
        {
            OnGameEnded?.Invoke(isVictory);
        }
        #endregion
    }
}
