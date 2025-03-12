using Objects;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// Lobby�� �����ϴ� �Ŵ���
    /// </summary>
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        #region Variables
        public TextMeshProUGUI width;
        public TextMeshProUGUI height;
        public TMP_InputField roomNameInputField;
        public TMP_InputField roomPasswordInputField;
        public Transform roomListContent;
        public GameObject roomItemFactory;

        Dictionary<string, RoomInfo> roomCache = new Dictionary<string, RoomInfo>();
        string roomNameText;
        string roomPasswordText;
        #endregion

        void Start()
        {
            OnConnectedToMaster();
        }

        void Update()
        {
            roomNameText = roomNameInputField.text;
            roomPasswordText = roomPasswordInputField.text;

            // ȭ�� ũ�� ǥ��
            width.text = $"{Screen.width}";
            height.text = $"{Screen.height}";
        }

        // �� ����
        public void CreateRoom(string roomName, string roomPassword)
        {
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = 2,
                IsVisible = true,
                IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
                {
                    { "roomName", roomName },
                    { "roomPassword", roomPassword }
                },
                CustomRoomPropertiesForLobby = new[] { "roomName", "roomPassword" }
            };

            PhotonNetwork.CreateRoom(roomName, roomOptions);
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            Debug.Log($"�� ���� �Ϸ�: {PhotonNetwork.CurrentRoom.Name}, IsVisible: {PhotonNetwork.CurrentRoom.IsVisible}");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            base.OnCreateRoomFailed(returnCode, message);
            Debug.LogError($"�� ���� ����: {message}");
        }

        // �� ����
        public void JoinRoom(string roomName, string password)
        {
            PhotonNetwork.JoinRoom(roomName);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            Debug.Log($"�� ���� ����: {PhotonNetwork.CurrentRoom.Name}");
            PhotonNetwork.LoadLevel("GameScene");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);
            Debug.LogError($"�� ���� ����: {message}");
        }

        // �� ��� ������Ʈ
        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            base.OnRoomListUpdate(roomList);
            Debug.Log($"�� ��� ������Ʈ ({roomList.Count}��)");

            roomCache.Clear(); // ���� ��� �ʱ�ȭ

            foreach (RoomInfo room in roomList)
            {
                // ���� ���� �� ��� ��Ͽ��� ����
                if (room.PlayerCount >= room.MaxPlayers)
                {
                    Debug.Log($"�� {room.Name}�� ���� ���� ��Ͽ��� ���ܵ�.");
                    continue;
                }

                roomCache[room.Name] = room;
            }

            UpdateRoomListUI();
        }


        void UpdateRoomCache(List<RoomInfo> roomList)
        {
            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList)
                {
                    roomCache.Remove(room.Name);
                }
                else
                {
                    roomCache[room.Name] = room;
                }
            }
        }

        void UpdateRoomListUI()
        {
            // ���� �� ��� UI �ʱ�ȭ
            foreach (Transform child in roomListContent)
            {
                Destroy(child.gameObject);
            }

            // �� ��� UI ������Ʈ
            foreach (RoomInfo roomInfo in roomCache.Values)
            {
                Debug.Log($"�� ���� �ε�: {roomInfo.Name}");

                GameObject roomItem = Instantiate(roomItemFactory, roomListContent);
                RoomItem itemComponent = roomItem.GetComponent<RoomItem>();

                if (itemComponent != null)
                {
                    itemComponent.SetInfo(roomInfo);
                }
                else
                {
                    Debug.LogError("RoomItem ������Ʈ�� ã�� �� �����ϴ�!");
                }
                // ���� ������ �� ó���� ����
                itemComponent.OnClickAction = (string roomName) =>
                {
                    roomNameInputField.text = roomName;
                };
            }
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.JoinLobby();
            Debug.Log("������ ���� �����, �κ� ���� �õ�");
        }

        public void OnClickCreate()
        {
            CreateRoom(roomNameText, roomPasswordText);
        }

        public void OnClickJoin()
        {
            JoinRoom(roomNameText, roomPasswordText);
        }

        public void OnClickRefresh()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                Debug.Log("�κ� �ٽ� �����Ͽ� �� ��� ���ΰ�ħ");
            }
            else
            {
                Debug.Log("�̹� �κ� ����. ���� �� ��� ����");
                UpdateRoomListUI();
            }
        }

        public void OnClickQuit()
        {
#if UNITY_EDITOR
            Debug.Log("CLICK QUIT");
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            Application.Quit();
        }
    }
}
