using UnityEngine;
using UnityEditor;
using Data;
using Objects;

namespace NumberDuel.Editor
{
    /// <summary>
    /// SystemMessages.asset 일괄 업데이트 에디터 스크립트
    /// Unity Editor 메뉴: Tools > Update System Messages
    /// </summary>
    public static class UpdateSystemMessages
    {
        [MenuItem("Tools/Update System Messages")]
        public static void UpdateMessages()
        {
            // Resources/Data/SystemMessages.asset 로드
            string assetPath = "Assets/Resources/Data/SystemMessages.asset";
            SystemMessageData messageData = AssetDatabase.LoadAssetAtPath<SystemMessageData>(assetPath);

            if (messageData == null)
            {
                Debug.LogError($"[UpdateSystemMessages] SystemMessages.asset을 찾을 수 없습니다: {assetPath}");
                return;
            }

            Debug.Log("[UpdateSystemMessages] SystemMessages.asset 업데이트 시작...");

            // ========================================
            // 1. 사용하지 않는 메시지 삭제
            // ========================================
            string[] keysToDelete = new string[]
            {
                "Registering",
                "RegisterSuccess",
                "LoggingOut",
                "CreatingProfile",
                "EmailNotVerified",
                "EmailVerificationSent"
            };

            foreach (string key in keysToDelete)
            {
                var message = messageData.messages.Find(m => m.messageKey == key);
                if (message != null)
                {
                    messageData.messages.Remove(message);
                    Debug.Log($"[UpdateSystemMessages] 삭제됨: {key}");
                }
            }

            // ========================================
            // 2. 새로운 메시지 추가
            // ========================================

            // ProfileNotFound
            if (!messageData.HasMessage("ProfileNotFound"))
            {
                messageData.messages.Add(new SystemMessageData.MessageInfo
                {
                    messageKey = "ProfileNotFound",
                    messageText = "프로필을 찾을 수 없습니다.",
                    messageType = Objects.MessageType.Error,
                    displayDuration = 3f,
                    textColor = new Color(1f, 0.3f, 0.3f)
                });
                Debug.Log("[UpdateSystemMessages] 추가됨: ProfileNotFound");
            }

            // ProfileCreateFailed
            if (!messageData.HasMessage("ProfileCreateFailed"))
            {
                messageData.messages.Add(new SystemMessageData.MessageInfo
                {
                    messageKey = "ProfileCreateFailed",
                    messageText = "프로필 생성에 실패했습니다.",
                    messageType = Objects.MessageType.Error,
                    displayDuration = 3f,
                    textColor = new Color(1f, 0.3f, 0.3f)
                });
                Debug.Log("[UpdateSystemMessages] 추가됨: ProfileCreateFailed");
            }

            // ========================================
            // 3. 기존 메시지 업데이트 (마침표/물음표 추가)
            // ========================================
            UpdateMessageText(messageData, "InputEmailPassword", "이메일과 비밀번호를 입력해주세요.");
            UpdateMessageText(messageData, "LoginFailed", "로그인에 실패했습니다.");
            UpdateMessageText(messageData, "ServerConnecting", "서버에 연결 중...");
            UpdateMessageText(messageData, "ServerConnected", "서버에 연결되었습니다.");
            UpdateMessageText(messageData, "ServerConnectionFailed", "서버 연결에 실패했습니다.");
            UpdateMessageText(messageData, "RoomCreating", "방 생성 중...");
            UpdateMessageText(messageData, "RoomCreated", "방이 생성되었습니다.");
            UpdateMessageText(messageData, "RoomCreateFailed", "방 생성에 실패했습니다.");
            UpdateMessageText(messageData, "RoomJoining", "방 입장 중...");
            UpdateMessageText(messageData, "RoomJoined", "방에 입장했습니다.");
            UpdateMessageText(messageData, "RoomJoinFailed", "방 입장에 실패했습니다.");
            UpdateMessageText(messageData, "OpponentDisconnected", "상대가 연결이 끊어졌습니다.");
            UpdateMessageText(messageData, "DuplicateLogin", "다른 기기에서 로그인되어 자동 로그아웃되었습니다.");
            UpdateMessageText(messageData, "EmailVerificationComplete", "이메일 인증이 완료되었습니다!");
            UpdateMessageText(messageData, "EmailVerificationReSent", "인증 이메일을 다시 발송했습니다.");

            // LogoutComplete를 Success 타입으로 변경
            var logoutMessage = messageData.messages.Find(m => m.messageKey == "LogoutComplete");
            if (logoutMessage != null)
            {
                logoutMessage.messageText = "로그아웃되었습니다.";
                logoutMessage.messageType = Objects.MessageType.Success;
                logoutMessage.textColor = new Color(0.3f, 1f, 0.3f);
                Debug.Log("[UpdateSystemMessages] 업데이트됨: LogoutComplete (타입 변경: Info → Success)");
            }

            // ========================================
            // 4. 변경 사항 저장
            // ========================================
            EditorUtility.SetDirty(messageData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[UpdateSystemMessages] ✅ SystemMessages.asset 업데이트 완료!");
            Debug.Log($"[UpdateSystemMessages] 총 메시지 수: {messageData.messages.Count}");
        }

        /// <summary>
        /// 메시지 텍스트 업데이트 헬퍼
        /// </summary>
        private static void UpdateMessageText(SystemMessageData data, string key, string newText)
        {
            var message = data.messages.Find(m => m.messageKey == key);
            if (message != null)
            {
                string oldText = message.messageText;
                message.messageText = newText;
                Debug.Log($"[UpdateSystemMessages] 업데이트됨: {key}\n  이전: \"{oldText}\"\n  이후: \"{newText}\"");
            }
            else
            {
                Debug.LogWarning($"[UpdateSystemMessages] '{key}' 메시지를 찾을 수 없습니다.");
            }
        }
    }
}
