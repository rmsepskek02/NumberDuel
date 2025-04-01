using UnityEngine;
using Photon.Pun.UtilityScripts;
using System.Collections.Generic;
using Utills;

namespace Manager
{
    /// <summary>
    /// 인 게임에서 필요한 기능들을 관리하는 매니저
    /// </summary>
    public class InGameManager : Singleton<InGameManager>
    {
        #region Variables
        PunTurnManager turnManager;
        public GameObject myDeck;
        public GameObject yourDeck;
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
            GameObject myDeckChildgo = myDeck.transform.GetChild(0).gameObject;
            GameObject yourDeckChildgo = yourDeck.transform.GetChild(0).gameObject;
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
    }
}