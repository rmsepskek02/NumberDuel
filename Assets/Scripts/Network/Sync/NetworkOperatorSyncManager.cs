using Objects;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Manager.Network.Data;

namespace Manager.Network.Sync
{
    /// <summary>
    /// 연산자 및 조커 효과 동기화를 담당하는 매니저
    /// 상대방 화면에 연산/조커 효과를 동기화
    /// </summary>
    public class NetworkOperatorSyncManager
    {
        private readonly NetworkGameManager hub;
        private readonly NetworkCardSyncManager cardSync;

        /// <summary>
        /// NetworkOperatorSyncManager 생성자
        /// </summary>
        /// <param name="hub">NetworkGameManager 참조</param>
        /// <param name="cardSync">NetworkCardSyncManager 참조</param>
        public NetworkOperatorSyncManager(NetworkGameManager hub, NetworkCardSyncManager cardSync)
        {
            this.hub = hub;
            this.cardSync = cardSync;
        }

        #region Operator Synchronization
        /// <summary>
        /// 연산자 사용 결과 동기화
        /// </summary>
        public void SyncOperationResult(Card operatorCard, Card firstCard, Card secondCard, float result, OperatorType operatorType)
        {
            if (!hub.CanPerformNetworkAction()) return;

            // 카드 ID 추출
            var firstNetworkCard = firstCard?.GetComponent<NetworkCard>();
            var secondNetworkCard = secondCard?.GetComponent<NetworkCard>();

            string firstId = firstNetworkCard?.UniqueId ?? "";
            string secondId = secondNetworkCard?.UniqueId ?? "";

            if (string.IsNullOrEmpty(firstId) || string.IsNullOrEmpty(secondId))
                return;

            // 카드 값 추출
            float firstValue = firstCard.GetComponentInChildren<CardText>()?.RawValue ?? 0;
            float secondValue = secondCard.GetComponentInChildren<CardText>()?.RawValue ?? 0;

            // 나머지 계산 (Divide만)
            float remainder = 0;
            if (operatorType == OperatorType.Divide && secondValue != 0)
            {
                remainder = firstValue % secondValue;
            }

            var opData = new OperationData(operatorType, firstId, secondId, firstValue, secondValue, result, remainder);

            // secondCard가 삭제되는 경우 체크
            if (operatorType == OperatorType.Minus && result <= 0)
                opData.destroySecondCard = false; // Minus는 firstCard가 삭제됨
            else if (operatorType == OperatorType.Divide && result <= 0)
                opData.destroySecondCard = false; // Divide도 firstCard가 삭제됨

            string jsonData = JsonUtility.ToJson(opData);
            hub.photonView.RPC("RPC_SyncOperation", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 연산 결과 RPC 수신
        /// </summary>
        public void ApplyRemoteOperation(string jsonData)
        {
            try
            {
                // 원격 액션 시작 (카운터 증가)
                hub.StartRemoteAction();

                var opData = JsonUtility.FromJson<OperationData>(jsonData);
                hub.StartCoroutine(ApplyRemoteOperationInternal(opData));
            }
            catch (System.Exception)
            {
                // 오류 발생 시 카운터 감소
                hub.EndRemoteAction();
            }
        }

        /// <summary>
        /// 원격 연산 결과 적용
        /// </summary>
        private IEnumerator ApplyRemoteOperationInternal(OperationData data)
        {
            OperatorType opType = (OperatorType)data.operatorType;

            // 카드 찾기 (소유자 변환 적용)
            var firstCard = hub.FindCardByNetworkId(data.firstCardId);
            var secondCard = hub.FindCardByNetworkId(data.secondCardId);

            if (firstCard == null || secondCard == null)
            {
                // 카드를 찾지 못하면 카운터 감소 후 종료
                hub.EndRemoteAction();
                yield break;
            }

            // ★ 연산자 사용 사운드 이벤트 발생 (필드 카드에 적용되는 순간)
            GameEventManager.Instance?.TriggerOperatorUsed(opType);

            // ExpressionZone에 수식 표시
            var ezManager = ExpressionZoneManager.Instance;
            if (ezManager != null)
            {
                ezManager.InitForOperation();
                ezManager.SetFirstOperand(firstCard);

                // 연산자 표시 (임시 연산자 카드 생성 필요 없이 타입만 전달)
                ezManager.SetOperatorByType(opType);

                ezManager.SetSecondOperand(secondCard);
                ezManager.ShowResult(firstCard, secondCard, opType);
            }

            yield return new WaitForSeconds(1.5f);

            // 결과 적용
            switch (opType)
            {
                case OperatorType.Plus:
                case OperatorType.Multiply:
                    UpdateRemoteCardValue(firstCard, data.result);
                    break;

                case OperatorType.Minus:
                    if (data.result > 0)
                        UpdateRemoteCardValue(firstCard, data.result);
                    else
                    {
                        // ★ 카드 파괴 사운드 이벤트 발생
                        GameEventManager.Instance?.TriggerCardDestroyed();
                        DestroyRemoteCard(firstCard);
                    }
                    break;

                case OperatorType.Divide:
                    if (data.result > 0)
                        UpdateRemoteCardValue(firstCard, data.result);
                    else
                    {
                        // ★ 카드 파괴 사운드 이벤트 발생
                        GameEventManager.Instance?.TriggerCardDestroyed();
                        DestroyRemoteCard(firstCard);
                    }

                    // 나머지 카드 생성
                    if (data.remainder > 0)
                    {
                        yield return new WaitForSeconds(0.3f);
                        CreateRemoteRemainderCard(data.remainder, firstCard.CurrentOwnerType);
                    }
                    break;
            }

            hub.RemoveBackCardFromHand(CardZone.OwnerType.Opponent);

            // 모든 작업 완료 - 원격 액션 종료 (카운터 감소)
            hub.EndRemoteAction();
        }
        #endregion

        #region Joker Synchronization
        /// <summary>
        /// 조커 효과 결과 동기화
        /// </summary>
        public void SyncJokerResult(Card jokerCard, JokerEffectType effectType, List<Card> targetCards = null)
        {
            if (!hub.CanPerformNetworkAction()) return;

            string[] targetIds = null;
            float[] cardValues = null;

            // 대상 카드 정보 추출
            if (targetCards != null && targetCards.Count > 0)
            {
                targetIds = new string[targetCards.Count];
                cardValues = new float[targetCards.Count];

                for (int i = 0; i < targetCards.Count; i++)
                {
                    var networkCard = targetCards[i]?.GetComponent<NetworkCard>();
                    targetIds[i] = networkCard?.UniqueId ?? "";

                    var cardText = targetCards[i]?.GetComponentInChildren<CardText>();
                    cardValues[i] = cardText?.RawValue ?? 0;
                }
            }

            var jokerData = new JokerData(effectType, targetIds, cardValues);
            string jsonData = JsonUtility.ToJson(jokerData);
            hub.photonView.RPC("RPC_SyncJoker", RpcTarget.Others, jsonData);
        }

        /// <summary>
        /// 조커 효과 RPC 수신
        /// </summary>
        public void ApplyRemoteJoker(string jsonData)
        {
            try
            {
                // 원격 액션 시작 (카운터 증가)
                hub.StartRemoteAction();

                var jokerData = JsonUtility.FromJson<JokerData>(jsonData);
                hub.StartCoroutine(ApplyRemoteJokerInternal(jokerData));
            }
            catch (System.Exception)
            {
                // 오류 발생 시 카운터 감소
                hub.EndRemoteAction();
            }
        }

        /// <summary>
        /// 원격 조커 효과 적용
        /// </summary>
        private IEnumerator ApplyRemoteJokerInternal(JokerData data)
        {
            JokerEffectType effectType = (JokerEffectType)data.effectType;

            switch (effectType)
            {
                case JokerEffectType.Draw:
                    yield return ApplyRemoteDraw();
                    break;

                case JokerEffectType.Delete:
                    yield return ApplyRemoteDelete(data);
                    break;

                case JokerEffectType.Swap:
                    yield return ApplyRemoteSwap(data);
                    break;
            }

            // 모든 작업 완료 - 원격 액션 종료 (카운터 감소)
            hub.EndRemoteAction();
        }

        /// <summary>
        /// 원격 Draw 효과 (상대방이 2장 드로우)
        /// </summary>
        private IEnumerator ApplyRemoteDraw()
        {
            // ★ 조커 드로우 효과 사운드 이벤트 발생
            GameEventManager.Instance?.TriggerJokerEffect(JokerEffectType.Draw);

            // DrawCardsToHand 내부에서 이미 SyncCardDraw를 호출하여
            // 상대방 화면에 뒷면 카드 2장이 생성되었음

            // 하지만 조커 카드 자체도 상대방 화면에서는 뒷면 카드로 보이므로
            // 조커 사용 후 해당 뒷면 카드를 제거해야 함
            yield return new WaitForSeconds(0.1f);
            hub.RemoveBackCardFromHand(CardZone.OwnerType.Opponent);
        }

        /// <summary>
        /// 원격 Delete 효과
        /// </summary>
        private IEnumerator ApplyRemoteDelete(JokerData data)
        {
            if (data.targetCardIds == null || data.targetCardIds.Length == 0)
                yield break;

            string targetId = data.targetCardIds[0];
            var targetCard = hub.FindCardByNetworkId(targetId);

            if (targetCard == null)
                yield break;

            // ExpressionZone에 Delete 효과 표시
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.InitForJokerDelete();
                ExpressionZoneManager.Instance.SetJokerDeleteTarget(targetCard);
            }

            // 플레이어가 볼 수 있도록 2초 대기
            yield return new WaitForSeconds(2.0f);

            // ★ 조커 삭제 효과 사운드 이벤트 발생 (카드 삭제 전)
            GameEventManager.Instance?.TriggerJokerEffect(JokerEffectType.Delete);

            hub.RemoveBackCardFromHand(CardZone.OwnerType.Opponent);

            // ★ 카드 파괴 사운드 이벤트 발생
            GameEventManager.Instance?.TriggerCardDestroyed();
            DestroyRemoteCard(targetCard);

            // ExpressionZone은 다음 액션 시작 시 초기화 (결과 유지)
        }

        /// <summary>
        /// 원격 Swap 효과
        /// </summary>
        private IEnumerator ApplyRemoteSwap(JokerData data)
        {
            if (data.targetCardIds == null || data.targetCardIds.Length < 2)
                yield break;

            var firstCard = hub.FindCardByNetworkId(data.targetCardIds[0]);
            var secondCard = hub.FindCardByNetworkId(data.targetCardIds[1]);

            if (firstCard == null || secondCard == null)
                yield break;

            // ExpressionZone에 Swap 효과 표시 (Opponent가 스왑을 시작했으므로 Opponent 색상 사용)
            if (ExpressionZoneManager.Instance != null)
            {
                ExpressionZoneManager.Instance.InitForJokerSwap(CardZone.OwnerType.Opponent);
                ExpressionZoneManager.Instance.SetJokerSwapFirst(firstCard);
                ExpressionZoneManager.Instance.SetJokerSwapSecond(secondCard);
            }

            // 플레이어가 볼 수 있도록 2초 대기
            yield return new WaitForSeconds(2.0f);

            // ★ 조커 스왑 효과 사운드 이벤트 발생 (값 교환 전)
            GameEventManager.Instance?.TriggerJokerEffect(JokerEffectType.Swap);

            // 1. 시크릿 카드 공개 (스왑 후 OPEN 상태로 전환)
            if (firstCard.IsSecret)
            {
                // ★ 시크릿 공개 사운드 이벤트 발생
                GameEventManager.Instance?.TriggerSecretRevealed();
                firstCard.RevealSecret();
            }

            if (secondCard.IsSecret)
            {
                // ★ 시크릿 공개 사운드 이벤트 발생
                GameEventManager.Instance?.TriggerSecretRevealed();
                secondCard.RevealSecret();
            }

            // 2. 값 교환
            var firstCardText = firstCard.GetComponentInChildren<CardText>();
            var secondCardText = secondCard.GetComponentInChildren<CardText>();

            if (firstCardText != null && secondCardText != null)
            {
                float firstValue = firstCardText.RawValue;
                float secondValue = secondCardText.RawValue;

                firstCardText.SetRawValue(secondValue);
                secondCardText.SetRawValue(firstValue);
            }

            // 3. 공격 방지 플래그 설정 (스왑된 카드는 이번 턴 공격 불가)
            firstCard.SetWasModifiedThisTurn(true);
            secondCard.SetWasModifiedThisTurn(true);

            hub.RemoveBackCardFromHand(CardZone.OwnerType.Opponent);

            // ExpressionZone은 다음 액션 시작 시 초기화 (결과 유지)
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// 원격 카드 값 업데이트
        /// </summary>
        private void UpdateRemoteCardValue(Card card, float newValue)
        {
            var cardText = card.GetComponentInChildren<CardText>();
            if (cardText != null)
            {
                cardText.SetRawValue(newValue);
            }
        }

        /// <summary>
        /// 원격 카드 삭제
        /// </summary>
        private void DestroyRemoteCard(Card card)
        {
            var zone = card.GetComponentInParent<CardZone>();
            zone?.RemoveCard(card.transform);
            Object.Destroy(card.gameObject);
        }

        /// <summary>
        /// 원격 나머지 카드 생성
        /// </summary>
        private void CreateRemoteRemainderCard(float value, CardZone.OwnerType ownerType)
        {
            // 소유자 변환 (상대방 관점)
            CardZone.OwnerType displayOwner = ownerType == CardZone.OwnerType.Player
                ? CardZone.OwnerType.Opponent
                : CardZone.OwnerType.Player;

            var fieldZone = cardSync.FindZone(displayOwner, CardZone.ZoneType.Field);
            if (fieldZone == null || !fieldZone.CanAddCard())
                return;

            var template = displayOwner == CardZone.OwnerType.Player
                ? ResourcesManager.Instance.GetPlayerCardTemplate()
                : ResourcesManager.Instance.GetOpponentCardTemplate();

            if (template == null) return;

            var newCard = Object.Instantiate(template);
            int intValue = Mathf.FloorToInt(value);
            newCard.name = $"RemainderCard_{intValue}";
            newCard.SetActive(true);

            var cardComponent = newCard.GetComponent<Card>();
            cardComponent?.InitializeAsNumber(intValue);

            fieldZone.AddCard(newCard.transform);
        }
        #endregion
    }
}
