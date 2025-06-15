using UnityEngine;
using Photon.Pun.UtilityScripts;
using System.Collections.Generic;
using Utills;
using Objects;
using System.Linq;

namespace Manager
{
    /// <summary>
    /// 인 게임에서 필요한 기능들을 관리하는 매니저
    /// </summary>
    public class InGameManager : Singleton<InGameManager>
    {
        #region Variables
        PunTurnManager turnManager;
        //public GameObject myDeck;
        //public GameObject yourDeck;
        public List<int> playerList = new List<int>();
        public List<string> playerADeck;
        public List<string> playerBDeck;
        public List<string> playerAHand;
        public List<string> playerBHand;
        public GameObject myHandCardList;
        public GameObject myFieldCardList;
        public GameObject yourHandCardList;
        public GameObject yourFieldCardList;
        public bool isStart;
        public GameObject uiInGame;
        public GameObject choice;
        public GameObject joker;
        public int clickedMyCardIdx;
        public string clickedMyCardNumber;
        public int clickedYourCardIdx;
        public string clickedYourCardNumber;
        public string firstCardNumber = "";
        public Transform firstCard;
        public string secondCardNumber = "";
        public bool isCopy;
        public bool isDelete;
        public bool isPlus;
        public bool isMinus;
        public bool isMultiple;
        public bool isDivision;



        // =========================
        private readonly List<Card> fieldCards = new List<Card>();

        #endregion

        #region Process Management

        /// <summary>
        /// 현재 진행 중인 프로세스
        /// </summary>
        private GameProcessState currentProcess = GameProcessState.Idle;

        /// <summary>
        /// 현재 프로세스 상태 (읽기 전용)
        /// </summary>
        public GameProcessState CurrentProcess => currentProcess;

        /// <summary>
        /// 프로세스가 진행 중인지 여부
        /// </summary>
        public bool IsProcessing => currentProcess != GameProcessState.Idle;

        /// <summary>
        /// 프로세스 시작
        /// </summary>
        public bool StartProcess(GameProcessState process)
        {
            if (IsProcessing)
            {
                Debug.LogWarning($"[InGameManager] 이미 {currentProcess} 프로세스가 진행 중입니다. {process} 시작 실패.");
                return false;
            }

            currentProcess = process;
            Debug.Log($"[InGameManager] {process} 프로세스 시작");

            // UI 업데이트나 이벤트 발생 필요시 여기에 추가

            return true;
        }

        /// <summary>
        /// 프로세스 종료
        /// </summary>
        public void EndProcess()
        {
            Debug.Log($"[InGameManager] {currentProcess} 프로세스 종료");
            currentProcess = GameProcessState.Idle;

            // UI 업데이트나 이벤트 발생 필요시 여기에 추가
        }

        /// <summary>
        /// 특정 프로세스가 진행 중인지 확인
        /// </summary>
        public bool IsProcessActive(GameProcessState process)
        {
            return currentProcess == process;
        }

        #endregion

        private void Start()
        {
            turnManager = FindAnyObjectByType<PunTurnManager>();
            SetCardColor();
            //choice.SetActive(false);
            //joker.SetActive(false);
            //InitRoom();
        }

        // TODO :: 방장이 빨강 손님이 파랑
        private void SetCardColor()
        {
        }
        //public string RemoveCardForDeck(int playerNumber)
        //{
        //    string drawCard = "";
        //    if (playerNumber == playerList[0])
        //    {
        //        if (playerAHand.Count >= 8) return drawCard;
        //        int randomIndex = Random.Range(0, playerADeck.Count); // 0부터 playerBDeck.Count-1까지의 랜덤 인덱스
        //        drawCard = playerADeck[randomIndex]; // 해당 인덱스의 카드 선택
        //        playerAHand.Add(drawCard);
        //        playerADeck.RemoveAt(randomIndex); // 리스트에서 카드 제거
        //        return drawCard;
        //    }
        //    else if (playerNumber == playerList[1])
        //    {
        //        if (playerBHand.Count >= 8) return drawCard;
        //        int randomIndex = Random.Range(0, playerBDeck.Count); // 0부터 playerBDeck.Count-1까지의 랜덤 인덱스
        //        drawCard = playerBDeck[randomIndex]; // 해당 인덱스의 카드 선택
        //        playerBHand.Add(drawCard);
        //        playerBDeck.RemoveAt(randomIndex); // 리스트에서 카드 제거
        //        return drawCard;
        //    }
        //    else
        //    {
        //        return drawCard;
        //    }
        //}
        // 게임을 시작하는 버튼의 클릭 이벤트
        public void OnClickStart()
        {
            isStart = true;
            Debug.Log("isStart = " + isStart);
            //StartTurn();
        }
        public void OnClickEnd()
        {
            turnManager.SendMove(null, true);
            //ResetYourFieldCardColor();
            //ResetMyFieldCardColor();
            Debug.Log("onClick End");
            turnManager.BeginTurn();
        }
        // 카드 선택 초기화
        //public void InitClickedCard()
        //{
        //    clickedMyCardIdx = 0;
        //    clickedMyCardNumber = "";
        //    clickedYourCardIdx = 0;
        //    clickedYourCardNumber = "";
        //}
        // 내 필드카드 색상 초기화
        //public void ResetMyFieldCardColor()
        //{
        //    foreach (Transform child in myFieldCardList.transform)
        //    {
        //        Color childColor = child.GetComponent<Image>().color;
        //        Color openColor = Global.Colors.ChangeColor(Global.Colors.OpenColor);
        //        Color secretColor = Global.Colors.ChangeColor(Global.Colors.SecretColor);
        //        if (childColor != openColor && childColor != secretColor)
        //        {
        //            child.GetComponent<Image>().color = child.GetComponent<CardController>().originColor;
        //            child.GetComponent<CardController>().isAttack = false;
        //        }
        //    }
        //}
        // 상대 필드카드 색상 초기화
        //public void ResetYourFieldCardColor()
        //{
        //    foreach (Transform child in yourFieldCardList.transform)
        //    {
        //        if (child.GetChild(0).gameObject.activeSelf)
        //            child.GetComponent<Image>().color = Global.Colors.ChangeColor(Global.Colors.WhiteColor);
        //        else
        //            child.GetComponent<Image>().color = Global.Colors.ChangeColor(Global.Colors.SecretColor);
        //    }
        //}
        // 방 초기화
        //void InitRoom()
        //{
        //    // TODO 게임 포톤초기화
        //    isStart = false;
        //    dc.enableDraw = false;
        //    endButton.interactable = false;
        //    playerADeck = new List<string> {
        //    "1", "2", "3", "4", "5",
        //    "1", "2", "3", "4", "5", 
        //    //"1", "2", "3", "4", "5", 
        //    //"1", "2", "3", "4", "5", 
        //    "+", "+", "-", "-", "X", "X", "%", "%", 
        //    //"Joker", "Joker", 
        //};
        //    playerBDeck = new List<string> {
        //    "1", "2", "3", "4", "5", "" +
        //    "1", "2", "3", "4", "5", 
        //    //"1", "2", "3", "4", "5", 
        //    //"1", "2", "3", "4", "5",
        //    "+", "+", "-", "-", "X", "X", "%", "%",
        //    //"Joker", "Joker", 
        //};
        //    DestroyChild(myHandCardList);
        //    DestroyChild(myFieldCardList);
        //    DestroyChild(yourHandCardList);
        //    DestroyChild(yourFieldCardList);
        //}
        //void DestroyChild(GameObject go)
        //{
        //    foreach (Transform child in go.transform)
        //    {
        //        Destroy(child.gameObject);
        //    }
        //}

        /// <summary>
        /// 필드에 들어간 카드를 등록합니다.
        /// </summary>
        public void RegisterFieldCard(Card card)
        {
            if (!fieldCards.Contains(card))
                fieldCards.Add(card);
        }

        /// <summary>
        /// 필드에서 제거된 카드를 해제합니다.
        /// </summary>
        public void UnregisterFieldCard(Card card)
        {
            if (fieldCards.Contains(card))
                fieldCards.Remove(card);
        }

        /// <summary>
        /// 현재 필드에 존재하는 모든 카드를 반환합니다.
        /// - 복사본 반환
        /// </summary>
        public List<Card> GetAllFieldCards()
        {
            return new List<Card>(fieldCards);
        }

        // InGameManager.cs에 추가할 메서드들

        /// <summary>
        /// 게임 시작 시 덱 초기화 및 초기 핸드 드로우
        /// </summary>
        public void StartGame()
        {
            // 덱 초기화
            DeckManager.Instance.InitializeDecks();

            // 초기 핸드 드로우 (각자 5장)
            DrawCardsToHand(5, CardZone.OwnerType.Player);
            DrawCardsToHand(5, CardZone.OwnerType.Opponent);

            Debug.Log("[InGameManager] 게임 시작 - 덱 초기화 및 초기 핸드 드로우 완료");
        }

        /// <summary>
        /// 지정된 플레이어의 손패로 카드 드로우 (손패 제한 적용)
        /// </summary>
        /// <param name="count">드로우할 카드 수</param>
        /// <param name="owner">카드 소유자</param>
        public void DrawCardsToHand(int count, CardZone.OwnerType owner)
        {
            // 손패 Zone 찾기
            CardZone handZone = FindHandZone(owner);
            if (handZone == null)
            {
                Debug.LogError($"[InGameManager] {owner} 손패 Zone을 찾을 수 없습니다.");
                return;
            }

            int drawnCount = 0;
            int destroyedCount = 0;

            for (int i = 0; i < count; i++)
            {
                // 현재 손패 개수 확인
                int currentHandCount = GetHandCardCount(handZone);

                // 덱에서 카드 데이터 드로우
                var cardData = owner == CardZone.OwnerType.Player
                    ? DeckManager.Instance.DrawPlayerCard()
                    : DeckManager.Instance.DrawOpponentCard();

                if (!cardData.HasValue)
                {
                    Debug.LogWarning($"[InGameManager] {owner} 덱이 비어있어 더 이상 드로우할 수 없습니다.");
                    break;
                }

                // 손패 제한 체크 (10장)
                if (currentHandCount >= 10)
                {
                    // 카드 데이터는 이미 덱에서 제거되었으므로 그냥 파괴
                    destroyedCount++;
                    Debug.Log($"[InGameManager] {owner} 손패가 가득참 (10장). 드로우한 카드 파괴: {GetCardDescription(cardData.Value)}");
                    continue;
                }

                // 실제 카드 오브젝트 생성 및 손패에 배치
                GameObject cardObject = DeckManager.Instance.CreateCardObject(cardData.Value, owner, handZone);
                if (cardObject != null)
                {
                    drawnCount++;
                }
            }

            // 결과 로그
            if (drawnCount > 0)
                Debug.Log($"[InGameManager] {owner} {drawnCount}장 드로우 완료");

            if (destroyedCount > 0)
                Debug.Log($"[InGameManager] {owner} {destroyedCount}장 손패 제한으로 파괴됨");
        }

        /// <summary>
        /// 손패의 현재 카드 개수 반환
        /// </summary>
        private int GetHandCardCount(CardZone handZone)
        {
            if (handZone == null) return 0;

            // Transform의 자식 개수로 카드 수 확인
            return handZone.transform.childCount;
        }

        /// <summary>
        /// 카드 정보 문자열 생성
        /// </summary>
        private string GetCardDescription(Manager.CardData cardData)
        {
            return cardData.cardType switch
            {
                CardType.Number => $"숫자({cardData.numberValue})",
                CardType.Operator => $"연산자({cardData.operatorType})",
                CardType.Joker => "조커",
                _ => "알 수 없음"
            };
        }

        /// <summary>
        /// 손패 Zone 찾기
        /// </summary>
        private CardZone FindHandZone(CardZone.OwnerType owner)
        {
            if (CardZone.AllZonesRoot == null) return null;

            var zones = CardZone.AllZonesRoot.GetComponentsInChildren<CardZone>();
            return zones.FirstOrDefault(z =>
                z.Owner == owner &&
                z.Zone == CardZone.ZoneType.Hand);
        }

        /// <summary>
        /// 턴 시작 시 카드 1장 드로우
        /// </summary>
        public void StartTurn(CardZone.OwnerType currentPlayer)
        {
            DrawCardsToHand(1, currentPlayer);
            Debug.Log($"[InGameManager] {currentPlayer} 턴 시작 - 카드 1장 드로우");
        }
    }
}