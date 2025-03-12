using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Manager
{
    /// <summary>
    /// Login 화면을 관리하는 매니저
    /// </summary>
    public class JoinManager : MonoBehaviourPunCallbacks
    {
        #region Variables
        public Button joinButton;
        public TMP_InputField inputId;
        public TMP_InputField inputPassword;
        #endregion
        // Start is called before the first frame update
        void Start()
        {
            // 서버 접속
            // AppId, 지역, 서버에 요청
            // 요청하고 받아야 함
            PhotonNetwork.ConnectUsingSettings();
        }

        // Update is called once per frame
        void Update()
        {

        }

        // 마스터 서버 접속 성공시 호출 (Lobby에 진입할 수 없는 상태)
        public override void OnConnected()
        {
            base.OnConnected();
            //Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);
        }

        // 마스터 서버 접속 성공시 호출 (Lobby에 진입할 수 있는 상태 일 때 호출가능)
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            //Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);


        }

        // 로비 진입 성공시 호출
        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();
            Debug.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

            PhotonNetwork.NickName = inputId.text;
            //Debug.Log("TEXT = " + inputId.text);
            // 로비로 이동
            PhotonNetwork.LoadLevel("LobbyScene");
        }

        public void OnClickButton()
        {
            // 로비 진입 요청
            PhotonNetwork.JoinLobby();
        }
    }
}
