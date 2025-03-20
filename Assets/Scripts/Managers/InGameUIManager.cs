using Photon.Pun;
using Photon.Pun.UtilityScripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    /// <summary>
    /// 인 게임에서 UI를 관리하는 매니저
    /// </summary>
    public class InGameUIManager : Utills.Singleton<InGameUIManager>
    {
        PunTurnManager tm;
        PhotonManager pm;
        public Button startButton;
        public Button endButton;
        public Button leaveButton;
        public TextMeshProUGUI turn;
        public Sprite enabledStartSprite;
        public Sprite enabledLeaveSprite;
        public Sprite enabledEndSprite;
        public Sprite disabledSprite;
        public bool isStart;

        void Start()
        {
            tm = FindAnyObjectByType<PunTurnManager>();
            pm = FindAnyObjectByType<PhotonManager>();
            UpdateButtons(1);

            startButton.onClick.AddListener(OnClickStart);
            endButton.onClick.AddListener(OnClickEnd);
            leaveButton.onClick.AddListener(OnClickLeave);
            SetButtonState(leaveButton, true, enabledLeaveSprite);
            SetButtonState(endButton, false, disabledSprite);
        }

        void Update()
        {
            turn.text = tm.Turn.ToString();
        }

        // `PhotonManager`에서 UI 이벤트 등록
        public void RegisterPhotonManager(PhotonManager photonManager)
        {
            photonManager.MyTurn += EnableEndTurnButton;
            photonManager.YourTurn += DisableEndTurnButton;
            photonManager.EnterPlayer += OnPlayerEnter;
            photonManager.LeavePlayer += OnPlayerLeave;
            photonManager.UpdatePlayerCount += UpdateButtons;
        }

        // 내 턴일 때 버튼 활성화
        private void EnableEndTurnButton()
        {
            SetButtonState(endButton, true, enabledEndSprite);
        }

        // 상대 턴일 때 버튼 비활성화
        private void DisableEndTurnButton()
        {
            SetButtonState(endButton, false, enabledEndSprite);
        }

        // 게임을 시작하는 버튼 클릭
        public void OnClickStart()
        {
            isStart = true;
            StartGame();
        }

        // 게임 시작 (턴 시작)
        private void StartGame()
        {
            startButton.gameObject.SetActive(false);
            if (PhotonNetwork.IsMasterClient)
            {
                tm.BeginTurn();
            }
        }

        // 턴 종료 버튼 클릭
        public void OnClickEnd()
        {
            tm.SendMove(null, true);
            Debug.Log("onClick End");
            tm.BeginTurn();
        }

        // 방 나가기 버튼 클릭
        public void OnClickLeave()
        {
            pm.OnLeaveRoom();
        }

        // 플레이어 입장 시 UI 업데이트
        private void OnPlayerEnter()
        {
            Debug.Log("플레이어 입장");
        }

        // 플레이어 퇴장 시 UI 업데이트
        private void OnPlayerLeave()
        {
            Debug.Log("플레이어 퇴장");
        }

        // 인원 수 업데이트 (Start 버튼 활성화)
        private void UpdateButtons(int playerCount)
        {
            if (playerCount == 2)
            {
                SetButtonState(startButton, true, enabledStartSprite, PhotonNetwork.IsMasterClient);
            }
            else
            {
                SetButtonState(startButton, false, enabledStartSprite, PhotonNetwork.IsMasterClient);
            }
        }
        public void ResetUI()
        {
            Debug.Log("UI 리셋");

            // 시작 버튼 다시 활성화
            SetButtonState(startButton, false, enabledStartSprite, PhotonNetwork.IsMasterClient);

            // 턴 종료 버튼 비활성화
            SetButtonState(endButton, false, enabledEndSprite);

            // 게임 시작 상태 초기화
            isStart = false;
        }

        // 버튼 상태 설정 (외부에서 호출)
        public void SetButtonState(Button button, bool isInteractable, Sprite enabledSprite, bool isActive = true)
        {
            button.gameObject.SetActive(isActive);
            button.interactable = isInteractable;
            button.GetComponent<Image>().sprite = isInteractable ? enabledSprite : disabledSprite;
        }
    }
}
