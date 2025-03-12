using Photon.Realtime;
using System;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// Lobby�� ���Ǵ� �� ����Ʈ UI
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
                Debug.LogError("[RoomItem] RoomInfo�� null�Դϴ�.");
                return;
            }

            // �� �̸��� ��������, CustomProperties�� ���� Ȯ�� �� roomInfo.Name�� ���
            if (info.CustomProperties.ContainsKey("roomName"))
            {
                roomName = info.CustomProperties["roomName"].ToString();
            }
            else
            {
                roomName = info.Name;
            }

            // UI�� �� �̸� ����
            name = roomName;
            roomInfo.text = roomName;
            Debug.Log($"[RoomItem] �� ���� ������: {roomName}");
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
