using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Manager
{
    /// <summary>
    /// Login 화면을 관리하는 매니저
    /// Photon 네트워크 연결 및 닉네임 설정 담당
    /// </summary>
    public class JoinManager : MonoBehaviourPunCallbacks
    {
        #region Fields and Properties
        public Button joinButton;
        public TMP_InputField inputId;
        public TMP_InputField inputPassword;
        #endregion

        #region Unity Lifecycle
        void Start()
        {
            // 한국 리전 설정
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "kr";

            // 서버 연결 (AppId, 버전, 서버에 요청)
            PhotonNetwork.ConnectUsingSettings();

            inputId.text = GameManager.Instance.clinetSettings.ClientID;
        }
        #endregion

        #region Photon Callbacks
        /// <summary>
        /// 서버 연결 완료 시점에 호출 (Lobby에 진입한 후 가능 상황)
        /// </summary>
        public override void OnConnected()
        {
            base.OnConnected();
        }

        /// <summary>
        /// 서버와 마스터 연결 성공 시점에 호출 (Lobby에 진입할 수 있는 상황 후 첫 호출가능)
        /// </summary>
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
        }

        /// <summary>
        /// 로비 진입 성공 시점에 호출
        /// </summary>
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();

            PhotonNetwork.NickName = inputId.text;

            // 로비로 이동
            PhotonNetwork.LoadLevel("LobbyScene");
        }
        #endregion

        #region Button Events
        /// <summary>
        /// 로비 진입 버튼 클릭
        /// </summary>
        public void OnClickButton()
        {
            // 로비 진입 요청
            PhotonNetwork.JoinLobby();
        }
        #endregion
    }
}
