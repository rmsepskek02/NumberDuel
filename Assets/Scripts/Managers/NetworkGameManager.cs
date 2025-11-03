using Objects;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manager.Network.Data;
using Manager.Network.Sync;

namespace Manager
{
    /// <summary>
    /// 통합 RPC 시스템을 제공하는 네트워크 매니저
    /// 모든 네트워크 동기화를 중앙 집중식으로 관리
    /// NetworkCard 기반의 강화된 검증 시스템 사용
    /// Secret 카드 해제 동기화 포함
    /// </summary>
    public class NetworkGameManager : MonoBehaviourPun
    {
        #region Singleton Pattern
        private static NetworkGameManager instance;

        /// <summary>
        /// NetworkGameManager 싱글톤 인스턴스
        /// </summary>
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

            // Sync 매니저 초기화
            colorSync = new NetworkColorSyncManager(this);
            cardSync = new NetworkCardSyncManager(this);
            operatorSync = new NetworkOperatorSyncManager(this, cardSync);
            combatSync = new NetworkCombatSyncManager(this);
            iconSync = new NetworkIconSyncManager(this);
        }
        #endregion

        #region Fields and Properties
        /// <summary>
        /// 등록된 NetworkCard 딕셔너리 (고유 ID 기반 관리)
        /// </summary>
        private Dictionary<string, NetworkCard> registeredCards = new Dictionary<string, NetworkCard>();

        /// <summary>
        /// 카드 색상 동기화 매니저
        /// </summary>
        private NetworkColorSyncManager colorSync;

        /// <summary>
        /// 카드 드로우/배치 동기화 매니저
        /// </summary>
        private NetworkCardSyncManager cardSync;

        /// <summary>
        /// 연산자/조커 동기화 매니저
        /// </summary>
        private NetworkOperatorSyncManager operatorSync;

        /// <summary>
        /// 전투 동기화 매니저
        /// </summary>
        private NetworkCombatSyncManager combatSync;

        /// <summary>
        /// 아이콘 동기화 매니저
        /// </summary>
        private NetworkIconSyncManager iconSync;
        #endregion

        #region Card Color Synchronization System
        /// <summary>
        /// 카드 색상을 모든 클라이언트에 동기화
        /// 방장이 색상을 선택하고 다른 플레이어들에게 전송
        /// </summary>
        public void SyncCardColors()
        {
            colorSync?.SyncCardColors();
        }

        /// <summary>
        /// 저장된 색상으로 동기화
        /// </summary>
        /// <param name="senderColor">보내는 사람(방에 남아있던 사람)의 색상</param>
        /// <param name="receiverColor">받는 사람(새로 들어온 사람)의 색상</param>
        public void SyncStoredColors(string senderColor, string receiverColor)
        {
            colorSync?.SyncStoredColors(senderColor, receiverColor);
        }

        /// <summary>
        /// 카드 색상 동기화 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_SyncCardColors(string jsonData)
        {
            colorSync?.ApplyRemoteColorSync(jsonData);
        }
        #endregion

        #region NetworkCard Registry System
        /// <summary>
        /// NetworkCard를 등록하여 중앙에서 관리
        /// </summary>
        public void RegisterNetworkCard(NetworkCard card)
        {
            if (card == null)
                return;

            string uniqueId = card.UniqueId;

            if (string.IsNullOrEmpty(uniqueId))
                return;

            if (registeredCards.ContainsKey(uniqueId))
            {
                if (registeredCards[uniqueId] == card)
                    return;

                registeredCards[uniqueId] = card;
            }
            else
            {
                registeredCards.Add(uniqueId, card);
            }
        }

        /// <summary>
        /// NetworkCard 등록 해제
        /// </summary>
        public void UnregisterNetworkCard(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return;

            if (registeredCards.ContainsKey(uniqueId))
            {
                registeredCards.Remove(uniqueId);
            }
        }

        /// <summary>
        /// ID로 등록된 NetworkCard 찾기
        /// </summary>
        public NetworkCard FindNetworkCard(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return null;

            if (registeredCards.TryGetValue(uniqueId, out NetworkCard card))
            {
                if (card != null)
                    return card;
                else
                {
                    registeredCards.Remove(uniqueId);
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 모든 카드 등록 해제 (씬 전환 시 등)
        /// </summary>
        public void ClearAllRegisteredCards()
        {
            registeredCards.Clear();
        }
        #endregion

        #region Network Utility Methods
        /// <summary>
        /// 네트워크 액션 수행 가능 여부 확인 (초기 드로우 허용)
        /// 게임 상태 및 네트워크 연결 상태를 종합적으로 검증
        /// </summary>
        /// <returns>네트워크 액션 수행 가능 여부</returns>
        public bool CanPerformNetworkAction()
        {
            // 기본 네트워크 연결 확인
            if (!PhotonNetwork.InRoom)
                return false;

            // TurnManager 상태 확인
            if (TurnManager.Instance == null)
                return false;

            // 수정: 초기 드로우는 게임 시작 전에도 허용 (IsGameStarted 체크 제거)
            // 플레이어가 2명이면 드로우 동기화 허용
            return PhotonNetwork.CurrentRoom.PlayerCount >= 2;
        }

        /// <summary>
        /// 손패에서 아무 카드나 1장 제거 (개수 맞추기용)
        /// NetworkCardSyncManager로 위임
        /// </summary>
        /// <param name="owner">카드 소유자</param>
        public void RemoveBackCardFromHand(CardZone.OwnerType owner)
        {
            cardSync?.RemoveBackCardFromHand(owner);
        }
        #endregion

        #region Card Draw Synchronization System
        /// <summary>
        /// 카드 드로우를 다른 플레이어에게 동기화
        /// 상대방 화면에서 뒷면 카드 드로우 표시 및 덱 업데이트
        /// </summary>
        /// <param name="owner">카드를 드로우한 플레이어</param>
        /// <param name="count">드로우한 카드 수</param>
        public void SyncCardDraw(CardZone.OwnerType owner, int count)
        {
            cardSync?.SyncCardDraw(owner, count);
        }

        /// <summary>
        /// 카드 드로우 동기화 RPC 수신 처리
        /// 원격 플레이어의 드로우 행위를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardDrawData</param>
        [PunRPC]
        private void RPC_SyncCardDraw(string jsonData)
        {
            cardSync?.ApplyRemoteCardDraw(jsonData);
        }
        #endregion

        #region Card Placement Synchronization System
        /// <summary>
        /// 카드 배치를 다른 플레이어에게 동기화
        /// 상대방 화면에 실제 카드 배치 및 손패에서 뒷면 카드 제거
        /// </summary>
        /// <param name="cardData">배치할 카드 데이터</param>
        /// <param name="owner">카드 소유자</param>
        /// <param name="zoneType">배치될 Zone 타입</param>
        /// <param name="isSecret">Secret 모드 여부</param>
        /// <param name="uniqueId">카드의 고유 ID (NetworkCard에서 전달받음)</param>
        public void SyncCardPlacement(CardData cardData, CardZone.OwnerType owner,
                                     CardZone.ZoneType zoneType, bool isSecret, string uniqueId)
        {
            cardSync?.SyncCardPlacement(cardData, owner, zoneType, isSecret, uniqueId);
        }

        /// <summary>
        /// 카드 배치 동기화 RPC 수신 처리
        /// 원격 플레이어의 카드 배치를 로컬 화면에 반영
        /// </summary>
        /// <param name="jsonData">직렬화된 CardPlacementData</param>
        [PunRPC]
        private void RPC_SyncCardPlacement(string jsonData)
        {
            cardSync?.ApplyRemoteCardPlacement(jsonData);
        }
        #endregion

        #region Combat Synchronization System
        /// <summary>
        /// 전투 액션 동기화 (공격, ExpressionZone, HP 모두 포함)
        /// </summary>
        public void SyncCombatAction(Card attacker, Card defender, float attackerValue,
                                     float defenderValue, int damageAmount)
        {
            combatSync?.SyncCombatAction(attacker, defender, attackerValue, defenderValue, damageAmount);
        }

        /// <summary>
        /// 전투 액션 RPC 수신 처리
        /// </summary>
        [PunRPC]
        private void RPC_SyncCombatAction(string jsonData)
        {
            combatSync?.ApplyRemoteCombatAction(jsonData);
        }

        /// <summary>
        /// NetworkCard ID로 Card 컴포넌트 찾기
        /// Combat 및 Operator/Joker Sync 매니저에서 사용
        /// </summary>
        public Card FindCardByNetworkId(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return null;

            NetworkCard[] networkCards = FindObjectsByType<NetworkCard>(FindObjectsSortMode.None);
            foreach (var networkCard in networkCards)
            {
                if (networkCard.UniqueId == cardId)
                {
                    return networkCard.GetComponent<Card>();
                }
            }

            return null;
        }
        #endregion
        
        #region Network Utility Methods
        /// <summary>
        /// NetworkCard용 고유 ID 생성
        /// NetworkCard 클래스의 ID 생성 방식과 일치하는 8자리 알파뉴메릭 ID
        /// </summary>
        /// <returns>8자리 알파뉴메릭 고유 ID</returns>
        private string GenerateNetworkCardId()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpper();
        }
        #endregion

        #region Operator & Joker Synchronization
        /// <summary>
        /// 연산자 사용 결과 동기화
        /// </summary>
        public void SyncOperationResult(Card operatorCard, Card firstCard, Card secondCard, float result, OperatorType operatorType)
        {
            operatorSync?.SyncOperationResult(operatorCard, firstCard, secondCard, result, operatorType);
        }

        /// <summary>
        /// 연산 결과 RPC 수신
        /// </summary>
        [PunRPC]
        private void RPC_SyncOperation(string jsonData)
        {
            operatorSync?.ApplyRemoteOperation(jsonData);
        }

        /// <summary>
        /// 조커 효과 결과 동기화
        /// </summary>
        public void SyncJokerResult(Card jokerCard, JokerEffectType effectType, List<Card> targetCards = null)
        {
            operatorSync?.SyncJokerResult(jokerCard, effectType, targetCards);
        }

        /// <summary>
        /// 조커 효과 RPC 수신
        /// </summary>
        [PunRPC]
        private void RPC_SyncJoker(string jsonData)
        {
            operatorSync?.ApplyRemoteJoker(jsonData);
        }
        #endregion

        #region Card Icon Synchronization
        /// <summary>
        /// 카드 선택 아이콘 표시를 네트워크로 동기화
        /// </summary>
        /// <param name="card">아이콘을 표시할 카드</param>
        /// <param name="iconType">아이콘 타입 (CardIconType enum)</param>
        public void SyncCardIcon(Card card, CardIconType iconType)
        {
            iconSync?.SyncCardIcon(card, iconType);
        }

        /// <summary>
        /// 카드 선택 아이콘 숨김을 네트워크로 동기화
        /// </summary>
        /// <param name="card">아이콘을 숨길 카드</param>
        public void HideCardIcon(Card card)
        {
            iconSync?.HideCardIcon(card);
        }

        /// <summary>
        /// 카드 아이콘 표시 동기화 RPC
        /// </summary>
        [PunRPC]
        private void RPC_SyncCardIcon(string cardId, string iconType, string ownerType)
        {
            iconSync?.ApplyRemoteCardIcon(cardId, iconType, ownerType);
        }

        /// <summary>
        /// 카드 아이콘 숨김 동기화 RPC
        /// </summary>
        [PunRPC]
        private void RPC_HideCardIcon(string cardId, string ownerType)
        {
            iconSync?.ApplyRemoteHideIcon(cardId, ownerType);
        }
        #endregion
    }
}