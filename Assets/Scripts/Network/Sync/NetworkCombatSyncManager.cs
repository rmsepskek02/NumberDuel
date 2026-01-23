using Objects;
using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using Manager.Network.Data;

namespace Manager.Network.Sync
{
    /// <summary>
    /// 전투 동기화를 담당하는 매니저
    /// 상대방 화면에 전투 액션 및 결과를 동기화
    /// </summary>
    public class NetworkCombatSyncManager
    {
        private readonly NetworkGameManager hub;

        /// <summary>
        /// NetworkCombatSyncManager 생성자
        /// </summary>
        /// <param name="hub">NetworkGameManager 참조</param>
        public NetworkCombatSyncManager(NetworkGameManager hub)
        {
            this.hub = hub;
        }

        #region Combat Synchronization
        /// <summary>
        /// 전투 액션 동기화 (공격, ExpressionZone, HP 모두 포함)
        /// </summary>
        public void SyncCombatAction(Card attacker, Card defender, float attackerValue,
                                     float defenderValue, int damageAmount)
        {
            if (attacker == null)
                return;

            if (!hub.CanPerformNetworkAction())
                return;

            // 공격자 정보
            var attackerNetworkCard = attacker.GetComponent<NetworkCard>();
            string attackerCardId = attackerNetworkCard != null ? attackerNetworkCard.UniqueId : "";
            bool attackerWasSecret = attacker.IsSecret;

            // 방어자 정보
            string defenderCardId = "";
            bool defenderWasSecret = false;
            if (defender != null)
            {
                var defenderNetworkCard = defender.GetComponent<NetworkCard>();
                defenderCardId = defenderNetworkCard != null ? defenderNetworkCard.UniqueId : "";
                defenderWasSecret = defender.IsSecret;
            }

            // 전투 결과 계산 (어느 카드가 파괴되는지 + 새로운 값)
            bool destroyAttacker = false;
            bool destroyDefender = false;
            float newAttackerValue = 0f;
            float newDefenderValue = 0f;

            if (defender != null) // 일반 공격
            {
                float result = attackerValue - defenderValue;
                if (result > 0) // 공격자 승리
                {
                    destroyDefender = true;
                    newAttackerValue = result; // 공격자의 새로운 값
                }
                else if (result < 0) // 방어자 승리
                {
                    destroyAttacker = true;
                    newDefenderValue = Mathf.Abs(result); // 방어자의 새로운 값
                }
                else // 무승부
                {
                    destroyAttacker = true;
                    destroyDefender = true;
                }
            }
            // 빈 필드 공격은 카드 파괴 없음

            var combatData = new CombatActionData(
                attackerCardId, defenderCardId,
                attackerValue, defenderValue,
                attackerWasSecret, defenderWasSecret,
                damageAmount,
                destroyAttacker, destroyDefender,
                newAttackerValue, newDefenderValue
            );

            string jsonData = JsonUtility.ToJson(combatData);
            hub.photonView.RPC("RPC_SyncCombatAction", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 전투 액션 RPC 수신 처리
        /// </summary>
        public void ApplyRemoteCombatAction(string jsonData)
        {
            try
            {
                // 원격 액션 시작 (카운터 증가)
                hub.StartRemoteAction();

                var combatData = JsonUtility.FromJson<CombatActionData>(jsonData);
                hub.StartCoroutine(ApplyRemoteCombatActionInternal(combatData));
            }
            catch (Exception)
            {
                // 오류 발생 시 카운터 감소
                hub.EndRemoteAction();
            }
        }

        /// <summary>
        /// 원격 전투 액션 적용
        /// </summary>
        private IEnumerator ApplyRemoteCombatActionInternal(CombatActionData combatData)
        {
            // 1. 공격자/방어자 카드 찾기
            Card attackerCard = hub.FindCardByNetworkId(combatData.attackerCardId);
            Card defenderCard = !string.IsNullOrEmpty(combatData.defenderCardId)
                ? hub.FindCardByNetworkId(combatData.defenderCardId)
                : null;

            if (attackerCard == null)
            {
                // 카드를 찾지 못하면 카운터 감소 후 종료
                hub.EndRemoteAction();
                yield break;
            }

            // 2. Secret 해제
            if (combatData.attackerWasSecret)
            {
                // ★ 시크릿 공개 사운드 이벤트 발생
                GameEventManager.Instance?.TriggerSecretRevealed();
                attackerCard.RevealSecret();
            }

            if (defenderCard != null && combatData.defenderWasSecret)
            {
                // ★ 시크릿 공개 사운드 이벤트 발생
                GameEventManager.Instance?.TriggerSecretRevealed();
                defenderCard.RevealSecret();
            }

            // ★ 카드 공격 사운드 이벤트 발생
            GameEventManager.Instance?.TriggerCardAttack();

            // 3. 전투 아이콘 표시
            if (combatData.showAttackerSwordIcon)
            {
                attackerCard.ShowAttackIcon();
            }

            if (combatData.showDefenderShieldIcon && defenderCard != null)
            {
                defenderCard.ShowDefenseIcon();
            }

            // 4. ExpressionZone 업데이트 (코루틴 대기)
            yield return hub.StartCoroutine(SyncExpressionZone(attackerCard, defenderCard, combatData));

            // 5. 모든 작업 완료 - 원격 액션 종료 (카운터 감소)
            hub.EndRemoteAction();
        }

        /// <summary>
        /// ExpressionZone 동기화 코루틴
        /// </summary>
        private IEnumerator SyncExpressionZone(Card attacker, Card defender, CombatActionData data)
        {
            var ezManager = ExpressionZoneManager.Instance;

            // 공격 초기화
            ezManager.InitForAttack();
            ezManager.SetAttackerCard(attacker);

            yield return new WaitForSeconds(0.5f);

            if (data.isEmptyFieldAttack)
            {
                // 빈 필드 공격
                ezManager.SetEmptyFieldDefender(CardZone.OwnerType.Player);
                yield return new WaitForSeconds(1.5f);
                ezManager.ShowEmptyFieldResult(data.attackerValue);
            }
            else
            {
                // 일반 공격
                if (defender != null)
                {
                    ezManager.SetDefenderCard(defender);
                }

                yield return new WaitForSeconds(1.5f);
                ezManager.ShowResult(attacker, defender);
            }

            yield return new WaitForSeconds(1f);

            // HP 감소 (상대방 관점에서는 내 HP가 감소)
            if (data.damageToOpponent > 0 && HealthManager.Instance != null)
            {
                HealthManager.Instance.ApplyDamage(data.damageToOpponent, CardZone.OwnerType.Player);
            }

            yield return new WaitForSeconds(0.5f);

            // 추가: 카드 값 업데이트 (제거 전에 실행)
            if (data.newAttackerValue > 0 && attacker != null && !data.destroyAttacker)
            {
                var attackerText = attacker.GetComponentInChildren<CardText>();
                if (attackerText != null)
                {
                    attackerText.SetRawValue(data.newAttackerValue);
                }
                attacker.SetSecret(false);  // 전투로 값이 변경되면 시크릿 해제
            }

            if (data.newDefenderValue > 0 && defender != null && !data.destroyDefender)
            {
                var defenderText = defender.GetComponentInChildren<CardText>();
                if (defenderText != null)
                {
                    defenderText.SetRawValue(data.newDefenderValue);
                }
                defender.SetSecret(false);  // 전투로 값이 변경되면 시크릿 해제
            }

            // 카드 제거 처리
            if (data.destroyAttacker && attacker != null)
            {
                // ★ 카드 파괴 사운드 이벤트 발생
                GameEventManager.Instance?.TriggerCardDestroyed();
                var zone = attacker.GetComponentInParent<CardZone>();
                zone?.RemoveCard(attacker.transform);
                UnityEngine.Object.Destroy(attacker.gameObject);
            }

            if (data.destroyDefender && defender != null)
            {
                // ★ 카드 파괴 사운드 이벤트 발생
                GameEventManager.Instance?.TriggerCardDestroyed();
                var zone = defender.GetComponentInParent<CardZone>();
                zone?.RemoveCard(defender.transform);
                UnityEngine.Object.Destroy(defender.gameObject);
            }

            yield return new WaitForSeconds(0.3f);

            // 아이콘 숨김
            if (attacker != null)
            {
                attacker.HideAllIcons();
            }

            if (defender != null)
            {
                defender.HideAllIcons();
            }
        }
        #endregion
    }
}
