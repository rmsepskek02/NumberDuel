using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    /// <summary>
    /// Lobby에 사용되는 방리스트
    /// </summary>
    public class RoomItem : MonoBehaviour
    {
        public TextMeshProUGUI roomInfo;
        public Action<string> OnClickAction;
        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetInfo(string roomName)
        {
            name = roomName;
            roomInfo.text = roomName;
        }
        public void SetInfo(RoomInfo info)
        {
            SetInfo((string)info.CustomProperties["roomName"]);
        }
        public void OnClickRoomList()
        {
            if (OnClickAction != null)
            {
                OnClickAction(name);
            }
        }
    }
}
