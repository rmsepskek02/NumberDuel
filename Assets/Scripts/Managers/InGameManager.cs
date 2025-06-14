using UnityEngine;
using Photon.Pun.UtilityScripts;
using System.Collections.Generic;
using Utills;
using Objects;

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
    }
}