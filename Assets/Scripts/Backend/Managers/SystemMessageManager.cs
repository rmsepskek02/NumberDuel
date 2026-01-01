using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utills;
using Objects;
using Data;

namespace Manager
{
    /// <summary>
    /// 시스템 메시지를 관리하는 싱글톤 매니저
    /// DontDestroyOnLoad로 씬 전환 시에도 유지됨
    /// 메시지 큐 시스템으로 여러 메시지를 순차적으로 표시
    /// </summary>
    public class SystemMessageManager : SingletonDontDestroy<SystemMessageManager>
    {
        #region Fields and Properties
        [Header("메시지 데이터")]
        [SerializeField] private SystemMessageData messageData;

        [Header("설정")]
        [SerializeField] private bool preventDuplicateMessages = true;
        [Tooltip("메시지 간 최소 간격 (초)")]
        [SerializeField] private float minMessageInterval = 0.3f;
        [Tooltip("Info 메시지를 즉시 교체할지 여부 (진행 상태 실시간 업데이트)")]
        [SerializeField] private bool instantReplaceInfoMessages = true;

        // 메시지 큐 (Warning, Error, Success만 큐에 쌓임)
        private Queue<MessageRequest> messageQueue = new Queue<MessageRequest>();
        private bool isShowingMessage = false;
        private string lastMessageKey = "";
        private float lastMessageTime = 0f;

        // 현재 표시 중인 Info 메시지 추적
        private MessageRequest currentInfoMessage = null;
        private Coroutine infoMessageHideCoroutine = null;

        // UI 참조
        private SystemMessageUI messageUI;

        /// <summary>
        /// 메시지 표시 요청 정보
        /// </summary>
        private class MessageRequest
        {
            public string text;
            public MessageType type;
            public float duration;
            public Color color;
        }
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            // Resources에서 메시지 데이터 로드
            if (messageData == null)
            {
                messageData = Resources.Load<SystemMessageData>("Data/SystemMessages");
                if (messageData == null)
                {
                    Debug.LogError("[SystemMessageManager] SystemMessages.asset을 Resources/Data/에서 찾을 수 없습니다!");
                }
            }
        }

        private void Start()
        {
            // UI 찾기 (지연 초기화)
            FindMessageUI();
        }
        #endregion

        #region UI Management
        /// <summary>
        /// SystemMessageUI 찾기
        /// SingletonDontDestroy로 유지되는 UI를 직접 참조
        /// SplashScene에서 생성된 UI가 전역적으로 사용됨
        /// </summary>
        private void FindMessageUI()
        {
            if (messageUI == null)
            {
                // SingletonDontDestroy 인스턴스 직접 참조
                messageUI = SystemMessageUI.Instance;

                if (messageUI == null)
                {
                    Debug.LogWarning("[SystemMessageManager] SystemMessageUI를 찾을 수 없습니다. SplashScene에서 생성되었는지 확인하세요.");
                }
            }
        }

        /// <summary>
        /// UI가 준비되었는지 확인
        /// SingletonDontDestroy로 유지되므로 한 번 찾으면 계속 사용 가능
        /// </summary>
        private bool IsUIReady()
        {
            if (messageUI == null)
            {
                FindMessageUI();
            }

            return messageUI != null;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 프리셋 메시지 표시 (메시지 키로 찾기)
        /// </summary>
        /// <param name="messageKey">메시지 키 (예: "PlayerLeft", "CannotAttack")</param>
        public void ShowMessage(string messageKey)
        {
            if (messageData == null)
            {
                Debug.LogError("[SystemMessageManager] MessageData가 null입니다!");
                return;
            }

            var messageInfo = messageData.GetMessage(messageKey);
            if (messageInfo == null)
            {
                Debug.LogWarning($"[SystemMessageManager] '{messageKey}' 메시지를 찾을 수 없습니다.");
                return;
            }

            // 중복 메시지 방지
            if (preventDuplicateMessages && IsDuplicateMessage(messageKey))
            {
                return;
            }

            EnqueueMessage(messageInfo.messageText, messageInfo.messageType, messageInfo.displayDuration, messageInfo.textColor);
            lastMessageKey = messageKey;
        }

        /// <summary>
        /// 커스텀 메시지 표시
        /// </summary>
        /// <param name="customText">표시할 메시지 텍스트</param>
        /// <param name="type">메시지 타입 (기본값: Info)</param>
        /// <param name="duration">표시 시간 (초, 기본값: 3초)</param>
        public void ShowMessage(string customText, MessageType type = MessageType.Info, float duration = 3f)
        {
            if (string.IsNullOrEmpty(customText))
            {
                Debug.LogWarning("[SystemMessageManager] 빈 메시지는 표시할 수 없습니다.");
                return;
            }

            Color color = GetDefaultColorForType(type);
            EnqueueMessage(customText, type, duration, color);
        }

        /// <summary>
        /// 파라미터가 있는 프리셋 메시지 표시
        /// </summary>
        /// <param name="messageKey">메시지 키</param>
        /// <param name="args">string.Format에 사용할 파라미터</param>
        /// <example>
        /// ShowMessageWithParams("PlayerJoined", "Player123");
        /// → "플레이어 {0}이(가) 입장했습니다." → "플레이어 Player123이(가) 입장했습니다."
        /// </example>
        public void ShowMessageWithParams(string messageKey, params object[] args)
        {
            if (messageData == null)
            {
                Debug.LogError("[SystemMessageManager] MessageData가 null입니다!");
                return;
            }

            var messageInfo = messageData.GetMessage(messageKey);
            if (messageInfo == null)
            {
                Debug.LogWarning($"[SystemMessageManager] '{messageKey}' 메시지를 찾을 수 없습니다.");
                return;
            }

            try
            {
                string formattedText = string.Format(messageInfo.messageText, args);
                EnqueueMessage(formattedText, messageInfo.messageType, messageInfo.displayDuration, messageInfo.textColor);
            }
            catch (System.FormatException e)
            {
                Debug.LogError($"[SystemMessageManager] 메시지 포맷 오류: {e.Message}");
            }
        }

        /// <summary>
        /// 모든 대기 중인 메시지 삭제
        /// </summary>
        public void ClearMessageQueue()
        {
            messageQueue.Clear();
            currentInfoMessage = null;

            // Info 메시지 타이머 취소
            if (infoMessageHideCoroutine != null)
            {
                StopCoroutine(infoMessageHideCoroutine);
                infoMessageHideCoroutine = null;
            }

            if (IsUIReady())
            {
                messageUI.HideMessageInstantly();
            }
        }
        #endregion

        #region Message Queue Management
        /// <summary>
        /// 메시지를 큐에 추가하고 표시 시작
        /// Info 타입: 즉시 교체 (실시간 상태 업데이트)
        /// Warning/Error/Success: 큐에 쌓아서 순차 표시
        /// </summary>
        private void EnqueueMessage(string text, MessageType type, float duration, Color color)
        {
            var request = new MessageRequest
            {
                text = text,
                type = type,
                duration = duration,
                color = color
            };

            // Info 타입은 즉시 교체 방식으로 처리
            if (type == MessageType.Info && instantReplaceInfoMessages)
            {
                HandleInfoMessage(request);
                return;
            }

            // Warning, Error, Success는 큐에 추가
            // 큐에 메시지가 있으면 마지막 메시지와 비교
            if (messageQueue.Count > 0)
            {
                // 큐의 마지막 메시지 확인 (Peek는 Queue에서 지원하지 않으므로 배열로 변환)
                var queueArray = messageQueue.ToArray();
                var lastMessage = queueArray[queueArray.Length - 1];

                // 마지막 메시지와 동일하면 추가하지 않음
                if (lastMessage.text == request.text)
                {
                    return;
                }
            }

            messageQueue.Enqueue(request);

            // 현재 표시 중이 아니면 바로 시작
            if (!isShowingMessage)
            {
                StartCoroutine(ProcessMessageQueue());
            }
        }

        /// <summary>
        /// Info 메시지 즉시 처리 (진행 상태 실시간 업데이트)
        /// ✅ 3초 후 자동으로 사라짐
        /// </summary>
        private void HandleInfoMessage(MessageRequest request)
        {
            if (!IsUIReady())
            {
                Debug.LogWarning("[SystemMessageManager] UI가 준비되지 않아 Info 메시지를 표시할 수 없습니다.");
                return;
            }

            // 현재 Info 메시지가 있으면 즉시 교체
            currentInfoMessage = request;

            // 중요 메시지(Error, Success) 표시 중이면 Info 메시지는 대기
            if (isShowingMessage)
            {
                return;
            }

            // 이전 Info 메시지 타이머 취소
            if (infoMessageHideCoroutine != null)
            {
                StopCoroutine(infoMessageHideCoroutine);
                infoMessageHideCoroutine = null;
            }

            // UI에 즉시 교체
            messageUI.ReplaceMessageInstantly(request.text, request.color);

            // ✅ 3초 후 자동으로 사라지도록 타이머 시작
            infoMessageHideCoroutine = StartCoroutine(HideInfoMessageAfterDuration(request.duration));
        }

        /// <summary>
        /// Info 메시지 자동 숨김 코루틴
        /// </summary>
        private IEnumerator HideInfoMessageAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration);

            // 중요 메시지가 표시 중이 아니고, 현재 Info 메시지가 남아있으면 숨김
            if (!isShowingMessage && currentInfoMessage != null)
            {
                messageUI.HideMessageInstantly();
                currentInfoMessage = null;
            }

            infoMessageHideCoroutine = null;
        }

        /// <summary>
        /// 메시지 큐 처리 코루틴
        /// 순차적으로 메시지를 표시 (Warning, Error, Success만)
        /// </summary>
        private IEnumerator ProcessMessageQueue()
        {
            isShowingMessage = true;

            // Info 메시지가 표시 중이면 일단 숨김 (중요 메시지 우선)
            if (currentInfoMessage != null)
            {
                messageUI.HideMessageInstantly();
                currentInfoMessage = null;
            }

            while (messageQueue.Count > 0)
            {
                // UI 준비 대기
                if (!IsUIReady())
                {
                    Debug.LogWarning("[SystemMessageManager] UI가 준비되지 않았습니다. 다음 프레임에 재시도...");
                    yield return null;
                    continue;
                }

                // 메시지 간 최소 간격 보장
                float timeSinceLastMessage = Time.time - lastMessageTime;
                if (timeSinceLastMessage < minMessageInterval)
                {
                    yield return new WaitForSeconds(minMessageInterval - timeSinceLastMessage);
                }

                // 큐에서 메시지 가져오기
                MessageRequest request = messageQueue.Dequeue();

                // UI에 메시지 표시
                messageUI.ShowMessage(request.text, request.type, request.duration, request.color);

                // 메시지 표시 시간 + 페이드 아웃 시간 대기
                float totalWaitTime = request.duration + messageUI.GetFadeOutDuration();
                yield return new WaitForSeconds(totalWaitTime);

                lastMessageTime = Time.time;
            }

            isShowingMessage = false;

            // 큐가 비었고 대기 중인 Info 메시지가 있으면 다시 표시
            if (currentInfoMessage != null)
            {
                messageUI.ReplaceMessageInstantly(currentInfoMessage.text, currentInfoMessage.color);
            }
        }

        /// <summary>
        /// 중복 메시지 체크
        /// 같은 메시지가 짧은 시간 내에 반복되는 것 방지
        /// </summary>
        private bool IsDuplicateMessage(string messageKey)
        {
            if (lastMessageKey != messageKey)
            {
                return false;
            }

            float timeSinceLastMessage = Time.time - lastMessageTime;
            return timeSinceLastMessage < 1f; // 1초 이내 중복 메시지 방지
        }

        /// <summary>
        /// 메시지 타입별 기본 색상 반환
        /// </summary>
        private Color GetDefaultColorForType(MessageType type)
        {
            return type switch
            {
                MessageType.Info => Color.white,
                MessageType.Warning => new Color(1f, 0.92f, 0.016f), // 노란색
                MessageType.Error => new Color(1f, 0.3f, 0.3f), // 빨간색
                MessageType.Success => new Color(0.3f, 1f, 0.3f), // 초록색
                _ => Color.white
            };
        }
        #endregion
    }
}
