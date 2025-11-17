using System;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using Manager;

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
        private RoomInfo cachedRoomInfo;
        #endregion

        #region Public Methods
        public void SetInfo(RoomInfo info)
        {
            if (info == null)
            {
                return;
            }

            // RoomInfo 캐싱
            cachedRoomInfo = info;

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
            // 방 클릭 시 기본 동작: 방 이름을 InputField에 채우기
            if (OnClickAction != null)
            {
                OnClickAction(roomName);
            }

            // 추가 정보 표시 (비밀번호 여부, 인원 등)
            if (cachedRoomInfo != null)
            {
                bool hasPassword = cachedRoomInfo.CustomProperties.ContainsKey("roomPassword")
                    && !string.IsNullOrEmpty(cachedRoomInfo.CustomProperties["roomPassword"]?.ToString());

                if (hasPassword)
                {
                    // 비밀번호가 필요한 방임을 알림
                    SystemMessageManager.Instance?.ShowMessage("RoomLocked");
                }
            }
        }
        #endregion
    }
}
