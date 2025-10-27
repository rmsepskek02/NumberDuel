using System;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// Lobby에 표시되는 방 리스트 UI
    /// </summary>
    public class RoomItem : MonoBehaviour
    {
        #region Fields and Properties
        public TextMeshProUGUI roomInfo;
        public Action<string> OnClickAction;
        private string roomName;
        #endregion

        #region Public Methods
        public void SetInfo(RoomInfo info)
        {
            if (info == null)
            {
                return;
            }

            // 방 이름은 우선순위로, CustomProperties에 있으면 확인 후 roomInfo.Name을 사용
            if (info.CustomProperties.ContainsKey("roomName"))
            {
                roomName = info.CustomProperties["roomName"].ToString();
            }
            else
            {
                roomName = info.Name;
            }

            // UI에 방 이름 표시
            name = roomName;
            roomInfo.text = roomName;
        }

        public void OnClickRoomList()
        {
            if (OnClickAction != null)
            {
                OnClickAction(roomName);
            }
        }
        #endregion
    }
}
