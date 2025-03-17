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
        public bool isStart;

        void Start()
        {
            tm = FindAnyObjectByType<PunTurnManager>();
            pm = FindAnyObjectByType<PhotonManager>();
            UpdateStartButton(1);

            startButton.onClick.AddListener(OnClickStart);
            endButton.onClick.AddListener(OnClickEnd);
            leaveButton.onClick.AddListener(OnClickLeave);
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
            photonManager.UpdatePlayerCount += UpdateStartButton;
        }

        // 내 턴일 때 버튼 활성화
        private void EnableEndTurnButton()
        {
            endButton.interactable = true;
        }

        // 상대 턴일 때 버튼 비활성화
        private void DisableEndTurnButton()
        {
            endButton.interactable = false;
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
        private void UpdateStartButton(int playerCount)
        {
            if (playerCount == 2)
            {
                startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
                startButton.interactable = true;
            }
            else
            {
                startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
                startButton.interactable = false;
            }
        }

        public void ResetUI()
        {
            Debug.Log("UI 리셋");

            // 시작 버튼 다시 활성화
            startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            startButton.interactable = false;

            // 턴 종료 버튼 비활성화
            endButton.interactable = false;

            // 게임 시작 상태 초기화
            isStart = false;
        }
    }
}
