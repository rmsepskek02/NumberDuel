using Photon.Realtime;
using System;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// Lobby에 사용되는 방 리스트 UI
    /// </summary>
    public class RoomItem : MonoBehaviour
    {
        #region Variables
        public TextMeshProUGUI roomInfo;
        public Action<string> OnClickAction;
        private string roomName;
        #endregion

        public void SetInfo(RoomInfo info)
        {
            if (info == null)
            {
                Debug.LogError("[RoomItem] RoomInfo가 null입니다.");
                return;
            }

            // 방 이름을 가져오되, CustomProperties를 먼저 확인 후 roomInfo.Name을 사용
            if (info.CustomProperties.ContainsKey("roomName"))
            {
                roomName = info.CustomProperties["roomName"].ToString();
            }
            else
            {
                roomName = info.Name;
            }

            // UI에 방 이름 설정
            name = roomName;
            roomInfo.text = roomName;
            Debug.Log($"[RoomItem] 방 정보 설정됨: {roomName}");
        }

        public void OnClickRoomList()
        {
            if (OnClickAction != null)
            {
                OnClickAction(roomName);
            }
        }
    }
}
