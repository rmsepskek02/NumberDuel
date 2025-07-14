using Objects;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 결과 중심의 네트워크 동기화 시스템
    /// 각 매니저는 로컬에서 실행하고 결과만 전달
    /// </summary>
    public class NetworkGameManager : MonoBehaviourPun
    {
        #region Singleton
        private static NetworkGameManager instance;
        public static NetworkGameManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindAnyObjectByType<NetworkGameManager>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        #endregion

        #region Result Data Structures
        /// <summary>
        /// 게임 액션 결과 데이터
        /// </summary>
        [System.Serializable]
        public class GameActionResult
        {
            public string actionType;
            public List<CardStateChange> cardChanges;
            public List<DamageInfo> damages;
            public List<string> removedCards;

            public GameActionResult(string type)
            {
                actionType = type;
                cardChanges = new List<CardStateChange>();
                damages = new List<DamageInfo>();
                removedCards = new List<string>();
            }
        }

        [System.Serializable]
        public class CardStateChange
        {
            public string cardId;
            public string newValue;
            public string newZone; // 이동한 경우
            public bool wasModified;

            public CardStateChange(string id, string value, bool modified = false, string zone = "")
            {
                cardId = id;
                newValue = value;
                wasModified = modified;
                newZone = zone;
            }
        }

        [System.Serializable]
        public class DamageInfo
        {
            public int damage;
            public int targetPlayer; // 0=Player, 1=Opponent

            public DamageInfo(int dmg, CardZone.OwnerType target)
            {
                damage = dmg;
                targetPlayer = (int)target;
            }
        }
        #endregion

        #region Public Interface for Managers
        /// <summary>
        /// 연산자 사용 결과 동기화
        /// </summary>
        public void SyncOperationResult(Card operatorCard, Card firstCard, Card secondCard, float result, OperatorType operatorType)
        {
            var actionResult = new GameActionResult("OPERATION");

            // 연산자 카드 제거
            actionResult.removedCards.Add(GetCardId(operatorCard));

            // 결과에 따른 카드 변경
            switch (operatorType)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    actionResult.cardChanges.Add(new CardStateChange(
                        GetCardId(firstCard),
                        result.ToString(),
                        true
                    ));
                    break;

                case OperatorType.Minus:
                    if (result > 0)
                        actionResult.cardChanges.Add(new CardStateChange(GetCardId(firstCard), result.ToString(), true));
                    else
                        actionResult.removedCards.Add(GetCardId(firstCard));
                    break;

                case OperatorType.Divide:
                    if (result > 0)
                        actionResult.cardChanges.Add(new CardStateChange(GetCardId(firstCard), result.ToString(), true));
                    else
                        actionResult.removedCards.Add(GetCardId(firstCard));
                    // 나머지 처리는 별도 로직 필요
                    break;
            }

            SyncResult(actionResult);
        }

        /// <summary>
        /// 공격 결과 동기화
        /// </summary>
        public void SyncAttackResult(Card attacker, Card defender, float attackValue, float defenseValue)
        {
            var actionResult = new GameActionResult("ATTACK");
            float result = attackValue - defenseValue;

            if (defender == null) // 빈 필드 공격
            {
                int damage = DamageCalculator.CalculateEmptyFieldDamage(attackValue);
                actionResult.damages.Add(new DamageInfo(damage, CardZone.OwnerType.Opponent));
                actionResult.cardChanges.Add(new CardStateChange(GetCardId(attacker), "", true)); // 수정됨 표시
            }
            else // 일반 공격
            {
                if (result > 0) // 공격자 승리
                {
                    int damage = DamageCalculator.CalculateAttackDamage(attackValue, defenseValue);
                    actionResult.damages.Add(new DamageInfo(damage, CardZone.OwnerType.Opponent));
                    actionResult.cardChanges.Add(new CardStateChange(GetCardId(attacker), result.ToString(), true));
                    actionResult.removedCards.Add(GetCardId(defender));
                }
                else if (result < 0) // 수비자 승리
                {
                    actionResult.cardChanges.Add(new CardStateChange(GetCardId(defender), Mathf.Abs(result).ToString()));
                    actionResult.removedCards.Add(GetCardId(attacker));
                }
                else // 무승부
                {
                    actionResult.removedCards.Add(GetCardId(attacker));
                    actionResult.removedCards.Add(GetCardId(defender));
                }
            }

            SyncResult(actionResult);
        }

        /// <summary>
        /// 조커 효과 결과 동기화
        /// </summary>
        public void SyncJokerResult(Card jokerCard, JokerEffectType effectType, List<Card> targetCards = null)
        {
            var actionResult = new GameActionResult("JOKER");

            // 조커 카드는 항상 제거
            actionResult.removedCards.Add(GetCardId(jokerCard));

            switch (effectType)
            {
                case JokerEffectType.Draw:
                    // Draw는 각자 로컬에서 처리 (덱이 다르므로)
                    break;

                case JokerEffectType.Delete:
                    if (targetCards != null && targetCards.Count > 0)
                        actionResult.removedCards.Add(GetCardId(targetCards[0]));
                    break;

                case JokerEffectType.Swap:
                    if (targetCards != null && targetCards.Count >= 2)
                    {
                        var card1Text = targetCards[0].GetComponentInChildren<CardText>();
                        var card2Text = targetCards[1].GetComponentInChildren<CardText>();

                        actionResult.cardChanges.Add(new CardStateChange(GetCardId(targetCards[0]), card2Text.RawValue.ToString()));
                        actionResult.cardChanges.Add(new CardStateChange(GetCardId(targetCards[1]), card1Text.RawValue.ToString()));
                    }
                    break;
            }

            SyncResult(actionResult);
        }

        /// <summary>
        /// 카드 배치 결과 동기화
        /// </summary>
        public void SyncCardPlacement(Card card, CardZone fromZone, CardZone toZone)
        {
            var actionResult = new GameActionResult("PLACEMENT");

            actionResult.cardChanges.Add(new CardStateChange(
                GetCardId(card),
                "",
                false,
                GetZoneReference(toZone)
            ));

            SyncResult(actionResult);
        }
        #endregion

        #region Core Sync Method
        /// <summary>
        /// 결과 데이터를 RPC로 전송
        /// </summary>
        private void SyncResult(GameActionResult result)
        {
            if (!CanPerformNetworkAction()) return;

            string jsonData = JsonUtility.ToJson(result);
            photonView.RPC("RPC_ApplyResult", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 결과 데이터를 수신하여 게임 상태에 직접 적용
        /// </summary>
        [PunRPC]
        private void RPC_ApplyResult(string jsonData)
        {
            try
            {
                var result = JsonUtility.FromJson<GameActionResult>(jsonData);
                ApplyGameResult(result);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkGameManager] 결과 적용 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 게임 결과를 실제 게임 상태에 적용
        /// </summary>
        private void ApplyGameResult(GameActionResult result)
        {
            // 1. 카드 상태 변경 적용
            foreach (var change in result.cardChanges)
            {
                ApplyCardStateChange(change);
            }

            // 2. 카드 제거 적용
            foreach (var cardId in result.removedCards)
            {
                RemoveCard(cardId);
            }

            // 3. 데미지 적용
            foreach (var damage in result.damages)
            {
                ApplyDamage(damage);
            }

            // 4. 특별 처리 (액션 타입별)
            switch (result.actionType)
            {
                case "JOKER":
                    HandleJokerSpecialCases(result);
                    break;
            }

            Debug.Log($"[NetworkGameManager] {result.actionType} 결과 적용 완료");
        }
        #endregion

        #region Result Application Methods
        /// <summary>
        /// 카드 상태 변경 적용
        /// </summary>
        private void ApplyCardStateChange(CardStateChange change)
        {
            Card card = FindCardById(change.cardId);
            if (card == null) return;

            // 값 변경
            if (!string.IsNullOrEmpty(change.newValue))
            {
                var cardText = card.GetComponentInChildren<CardText>();
                if (cardText != null)
                {
                    cardText.SetRawValue(float.Parse(change.newValue));
                }
            }

            // 수정됨 표시
            if (change.wasModified)
            {
                card.SetWasModifiedThisTurn(true);
            }

            // Zone 이동
            if (!string.IsNullOrEmpty(change.newZone))
            {
                CardZone targetZone = FindZoneByReference(change.newZone);
                if (targetZone != null)
                {
                    targetZone.AddCard(card.transform);
                }
            }
        }

        /// <summary>
        /// 카드 제거
        /// </summary>
        private void RemoveCard(string cardId)
        {
            Card card = FindCardById(cardId);
            if (card == null) return;

            CardZone zone = card.GetComponentInParent<CardZone>();

            StartCoroutine(card.AnimateRemoval(() => {
                zone?.RemoveCard(card.transform);
                Destroy(card.gameObject);
            }));
        }

        /// <summary>
        /// 데미지 적용
        /// </summary>
        private void ApplyDamage(DamageInfo damageInfo)
        {
            CardZone.OwnerType target = (CardZone.OwnerType)damageInfo.targetPlayer;

            if (HealthManager.Instance != null)
            {
                HealthManager.Instance.ApplyDamage(damageInfo.damage, target);
            }
        }

        /// <summary>
        /// 조커 특별 처리 (Draw 등)
        /// </summary>
        private void HandleJokerSpecialCases(GameActionResult result)
        {
            // Draw 효과는 각자 로컬에서 처리해야 함 (덱이 다르므로)
            // 여기서는 Draw 신호만 받아서 로컬 드로우 실행
            if (result.actionType == "JOKER" && result.cardChanges.Count == 0 && result.removedCards.Count == 1)
            {
                // Draw 조커로 추정
                InGameManager.Instance.DrawCardsToHand(2, TurnManager.Instance.LocalPlayerRole);
            }
        }
        #endregion

        #region Utility Methods
        private bool CanPerformNetworkAction()
        {
            return TurnManager.Instance != null && TurnManager.Instance.CanPerformAction();
        }

        private string GetCardId(Card card)
        {
            var networkCard = card.GetComponent<NetworkCard>();
            return networkCard?.UniqueId ?? "";
        }

        private string GetZoneReference(CardZone zone)
        {
            return $"{zone.Owner}_{zone.Zone}";
        }

        private Card FindCardById(string cardId)
        {
            var networkCards = FindObjectsByType<NetworkCard>(FindObjectsSortMode.None);
            foreach (var networkCard in networkCards)
            {
                if (networkCard.UniqueId == cardId)
                {
                    return networkCard.GetComponent<Card>();
                }
            }
            return null;
        }

        private CardZone FindZoneByReference(string zoneRef)
        {
            string[] parts = zoneRef.Split('_');
            if (parts.Length != 2) return null;

            CardZone.OwnerType owner = System.Enum.Parse<CardZone.OwnerType>(parts[0]);
            CardZone.ZoneType zone = System.Enum.Parse<CardZone.ZoneType>(parts[1]);

            if (CardZone.AllZonesRoot == null) return null;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            return System.Linq.Enumerable.FirstOrDefault(zones, z => z.Owner == owner && z.Zone == zone);
        }
        #endregion
    }
}