using System.Collections.Generic;
using UnityEngine;
using Objects;

namespace Data
{
    /// <summary>
    /// 시스템 메시지 데이터를 관리하는 ScriptableObject
    /// 에디터에서 메시지를 쉽게 추가/수정 가능
    /// Resources/Data/SystemMessages.asset에 저장
    /// </summary>
    [CreateAssetMenu(fileName = "SystemMessages", menuName = "NumberDuel/System Messages")]
    public class SystemMessageData : ScriptableObject
    {
        #region Inner Classes
        /// <summary>
        /// 개별 메시지 정보
        /// </summary>
        [System.Serializable]
        public class MessageInfo
        {
            [Header("메시지 식별")]
            [Tooltip("코드에서 참조할 고유 키 (예: PlayerLeft, CannotAttack)")]
            public string messageKey;

            [Header("메시지 내용")]
            [TextArea(2, 4)]
            [Tooltip("플레이어에게 표시될 메시지 텍스트")]
            public string messageText;

            [Header("메시지 설정")]
            [Tooltip("메시지 타입 (Info/Warning/Error/Success)")]
            public MessageType messageType = MessageType.Info;

            [Tooltip("메시지가 화면에 표시될 시간 (초)")]
            [Range(1f, 10f)]
            public float displayDuration = 3f;

            [Tooltip("메시지 텍스트 색상")]
            public Color textColor = Color.white;
        }
        #endregion

        #region Fields
        [Header("시스템 메시지 목록")]
        [Tooltip("게임에서 사용할 모든 시스템 메시지")]
        public List<MessageInfo> messages = new List<MessageInfo>();
        #endregion

        #region Public Methods
        /// <summary>
        /// 메시지 키로 메시지 정보 찾기
        /// </summary>
        /// <param name="key">찾을 메시지 키</param>
        /// <returns>찾은 메시지 정보, 없으면 null</returns>
        public MessageInfo GetMessage(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var message = messages.Find(m => m.messageKey == key);

            return message;
        }

        /// <summary>
        /// 메시지가 존재하는지 확인
        /// </summary>
        /// <param name="key">확인할 메시지 키</param>
        /// <returns>존재하면 true</returns>
        public bool HasMessage(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return messages.Exists(m => m.messageKey == key);
        }
        #endregion

        #region Validation (Editor Only)
#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 데이터 검증
        /// 중복 키, 빈 값 체크
        /// </summary>
        private void OnValidate()
        {
            // 중복 키 체크
            HashSet<string> keys = new HashSet<string>();
            foreach (var message in messages)
            {
                if (string.IsNullOrEmpty(message.messageKey))
                {
                    continue;
                }

                if (!keys.Add(message.messageKey))
                {
                    Debug.LogError($"[SystemMessageData] 중복된 메시지 키: '{message.messageKey}'");
                }
            }
        }
#endif
        #endregion
    }
}
