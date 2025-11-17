using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Objects;
using Utills;

namespace Objects
{
    /// <summary>
    /// 시스템 메시지 UI 표시 및 애니메이션 처리
    /// 화면 하단 중앙에 배치
    /// DOTween을 사용한 페이드 인/아웃 및 슬라이드 애니메이션
    /// SingletonDontDestroy로 씬 전환 시에도 유지됨
    /// </summary>
    public class SystemMessageUI : SingletonDontDestroy<SystemMessageUI>
    {
        #region Fields and Properties
        [Header("UI 컴포넌트")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private RectTransform messagePanel;

        [Header("애니메이션 설정")]
        [Tooltip("페이드 인 시간 (초)")]
        [SerializeField] private float fadeInDuration = 0.3f;

        [Tooltip("페이드 아웃 시간 (초)")]
        [SerializeField] private float fadeOutDuration = 0.3f;

        [Tooltip("슬라이드 애니메이션 사용 여부")]
        [SerializeField] private bool useSlideAnimation = true;

        [Tooltip("슬라이드 거리 (Y축)")]
        [SerializeField] private float slideDistance = 30f;

        [Tooltip("애니메이션 이징")]
        [SerializeField] private Ease fadeEase = Ease.OutCubic;

        // 원본 위치 저장
        private Vector2 originalPosition;
        private bool isInitialized = false;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake(); // SingletonDontDestroy의 Awake 호출

            // 부모 Canvas도 DontDestroyOnLoad 처리
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.transform.parent == null)
            {
                // 최상위 Canvas인 경우에만 DontDestroyOnLoad 적용
                DontDestroyOnLoad(rootCanvas.gameObject);
            }

            Initialize();
        }

        private void OnDestroy()
        {
            // DOTween 정리
            if (canvasGroup != null)
                canvasGroup.DOKill();
            if (messagePanel != null)
                messagePanel.DOKill();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 초기화
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            // 컴포넌트 자동 할당
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (messageText == null)
                messageText = GetComponentInChildren<TextMeshProUGUI>();
            if (messagePanel == null)
                messagePanel = GetComponent<RectTransform>();

            // 컴포넌트 검증
            if (canvasGroup == null)
            {
                Debug.LogError("[SystemMessageUI] CanvasGroup이 없습니다! GameObject에 추가해주세요.");
            }
            if (messageText == null)
            {
                Debug.LogError("[SystemMessageUI] TextMeshProUGUI가 없습니다! 자식 오브젝트에 추가해주세요.");
            }

            // 원본 위치 저장
            if (messagePanel != null)
            {
                originalPosition = messagePanel.anchoredPosition;
            }

            // 초기 상태 설정 (비활성)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            isInitialized = true;
        }
        #endregion

        #region Public Interface
        /// <summary>
        /// 메시지 표시 (기본 동작: 페이드 인/아웃 포함)
        /// </summary>
        /// <param name="text">표시할 메시지</param>
        /// <param name="type">메시지 타입</param>
        /// <param name="duration">표시 시간 (초)</param>
        /// <param name="color">텍스트 색상</param>
        public void ShowMessage(string text, MessageType type, float duration, Color color)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // 이전 애니메이션 정지
            StopAllAnimations();

            // 메시지 설정
            if (messageText != null)
            {
                messageText.text = text;
                messageText.color = color;
            }

            // 애니메이션 시작
            StartCoroutine(ShowMessageSequence(duration));
        }

        /// <summary>
        /// 메시지 즉시 교체 (페이드 인 없이 텍스트만 변경)
        /// Info 타입 메시지에 사용 - 진행 상태를 실시간으로 업데이트
        /// </summary>
        /// <param name="text">표시할 메시지</param>
        /// <param name="color">텍스트 색상</param>
        public void ReplaceMessageInstantly(string text, Color color)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            // 현재 메시지가 표시 중이면 텍스트만 교체
            if (messageText != null)
            {
                messageText.text = text;
                messageText.color = color;
            }

            // 이미 표시 중이 아니면 새로 표시
            if (canvasGroup != null && canvasGroup.alpha < 0.5f)
            {
                // 코루틴만 정리하고 DOTween은 유지
                StopAllCoroutines();

                // 페이드 인만 실행 (표시 유지 없음)
                StartCoroutine(FadeIn());
            }
        }

        /// <summary>
        /// 현재 메시지가 표시 중인지 확인
        /// </summary>
        public bool IsShowingMessage()
        {
            return canvasGroup != null && canvasGroup.alpha > 0.5f;
        }

        /// <summary>
        /// 메시지 즉시 숨김
        /// </summary>
        public void HideMessageInstantly()
        {
            StopAllAnimations();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (messagePanel != null)
            {
                messagePanel.anchoredPosition = originalPosition;
            }
        }

        /// <summary>
        /// 페이드 아웃 시간 반환
        /// SystemMessageManager에서 다음 메시지 타이밍 계산에 사용
        /// </summary>
        public float GetFadeOutDuration()
        {
            return fadeOutDuration;
        }
        #endregion

        #region Animation
        /// <summary>
        /// 메시지 표시 시퀀스
        /// </summary>
        private System.Collections.IEnumerator ShowMessageSequence(float displayDuration)
        {
            // 1단계: 페이드 인 + 슬라이드 인
            yield return StartCoroutine(FadeIn());

            // 2단계: 표시 유지
            yield return new WaitForSeconds(displayDuration);

            // 3단계: 페이드 아웃 + 슬라이드 아웃
            yield return StartCoroutine(FadeOut());
        }

        /// <summary>
        /// 페이드 인 애니메이션
        /// </summary>
        private System.Collections.IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            // 초기 상태 설정
            canvasGroup.alpha = 0f;

            if (useSlideAnimation && messagePanel != null)
            {
                // 시작 위치: 아래에서 위로
                Vector2 startPosition = originalPosition + Vector2.down * slideDistance;
                messagePanel.anchoredPosition = startPosition;

                // 동시에 페이드 인 + 슬라이드
                canvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase);
                messagePanel.DOAnchorPos(originalPosition, fadeInDuration).SetEase(fadeEase);
            }
            else
            {
                // 페이드 인만
                canvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase);
            }

            yield return new WaitForSeconds(fadeInDuration);
        }

        /// <summary>
        /// 페이드 아웃 애니메이션
        /// </summary>
        private System.Collections.IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            if (useSlideAnimation && messagePanel != null)
            {
                // 슬라이드 아웃 (위로 사라짐)
                Vector2 endPosition = originalPosition + Vector2.up * slideDistance;

                // 동시에 페이드 아웃 + 슬라이드
                canvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase);
                messagePanel.DOAnchorPos(endPosition, fadeOutDuration).SetEase(fadeEase);
            }
            else
            {
                // 페이드 아웃만
                canvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeEase);
            }

            yield return new WaitForSeconds(fadeOutDuration);

            // 원래 위치로 복귀 (다음 메시지를 위해)
            if (messagePanel != null)
            {
                messagePanel.anchoredPosition = originalPosition;
            }
        }

        /// <summary>
        /// 모든 애니메이션 정지
        /// </summary>
        private void StopAllAnimations()
        {
            StopAllCoroutines();

            if (canvasGroup != null)
                canvasGroup.DOKill();
            if (messagePanel != null)
                messagePanel.DOKill();
        }
        #endregion

        #region Editor Helpers
#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 컴포넌트 자동 할당
        /// </summary>
        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            messageText = GetComponentInChildren<TextMeshProUGUI>();
            messagePanel = GetComponent<RectTransform>();
        }
#endif
        #endregion
    }
}
