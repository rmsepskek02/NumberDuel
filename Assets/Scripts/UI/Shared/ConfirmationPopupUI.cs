using System;
using DG.Tweening;
using Objects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// 재사용 가능한 확인 팝업
    /// 동적으로 메시지와 액션 설정 가능
    /// </summary>
    public class ConfirmationPopupUI : MonoBehaviour
    {
        #region Fields and Properties
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI cancelButtonText;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform popupRect;

        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.2f;
        [SerializeField] private float hideDuration = 0.15f;

        private Action onConfirmed;
        private Action onCanceled;
        private Tween showTween;
        private Tween hideTween;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 버튼 이벤트 등록
            cancelButton?.onClick.AddListener(OnCancelClicked);
            confirmButton?.onClick.AddListener(OnConfirmClicked);

            // 초기 상태: 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // Tween 정리
            showTween?.Kill();
            hideTween?.Kill();

            // 버튼 이벤트 해제
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
            confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <param name="onConfirm">확인 버튼 클릭 시 실행할 액션</param>
        public void Show(string message, Action onConfirm)
        {
            Show(message, onConfirm, null);
        }

        /// <summary>
        /// 확인 팝업 표시 (취소 콜백 포함)
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <param name="onConfirm">확인 버튼 클릭 시 실행할 액션</param>
        /// <param name="onCancel">취소 버튼 클릭 시 실행할 액션</param>
        public void Show(string message, Action onConfirm, Action onCancel)
        {
            Show(message, onConfirm, onCancel, "확인", "취소");
        }

        /// <summary>
        /// 확인 팝업 표시 (버튼 텍스트 커스터마이징 포함)
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <param name="onConfirm">확인 버튼 클릭 시 실행할 액션</param>
        /// <param name="onCancel">취소 버튼 클릭 시 실행할 액션</param>
        /// <param name="confirmText">확인 버튼 텍스트 (기본값: "확인")</param>
        /// <param name="cancelText">취소 버튼 텍스트 (기본값: "취소")</param>
        public void Show(string message, Action onConfirm, Action onCancel, string confirmText, string cancelText)
        {
            // 이전 Tween 정리
            hideTween?.Kill();

            // 메시지 설정
            if (messageText != null)
            {
                messageText.text = message;
            }

            // 버튼 텍스트 설정
            if (confirmButtonText != null)
            {
                confirmButtonText.text = confirmText;
            }

            if (cancelButtonText != null)
            {
                cancelButtonText.text = cancelText;
            }

            // 취소 버튼 표시/숨김 처리 (onCancel이 null이면 버튼 숨김)
            if (cancelButton != null)
            {
                bool showCancelButton = onCancel != null;
                cancelButton.gameObject.SetActive(showCancelButton);
            }

            // 액션 저장
            onConfirmed = onConfirm;
            onCanceled = onCancel;

            // 활성화
            gameObject.SetActive(true);

            // 애니메이션
            PlayShowAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// 확인 팝업 숨기기
        /// </summary>
        public void Hide()
        {
            // 이전 Tween 정리
            showTween?.Kill();

            // 애니메이션
            PlayHideAnimation();

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);

            // SettingsManager에 알림
            //Manager.SettingsManager.Instance?.OnConfirmationClosed();
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelClicked()
        {
            // 취소 액션 실행
            onCanceled?.Invoke();

            Hide();
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private void OnConfirmClicked()
        {
            // 액션 실행
            onConfirmed?.Invoke();

            // 팝업 닫기
            Hide();
        }
        #endregion

        #region Animations
        /// <summary>
        /// 표시 애니메이션 (Scale 0.9 → 1.0 + Fade In)
        /// </summary>
        private void PlayShowAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                // CanvasGroup이 없으면 즉시 표시
                return;
            }

            // 초기 상태
            canvasGroup.alpha = 0f;
            popupRect.localScale = Vector3.one * 0.9f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence showSequence = DOTween.Sequence();

            // Fade In
            showSequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));

            // Scale Up
            showSequence.Join(popupRect.DOScale(1f, showDuration).SetEase(Ease.OutBack));

            // 완료 시 상호작용 활성화
            showSequence.OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });

            showTween = showSequence;
        }

        /// <summary>
        /// 숨김 애니메이션 (Scale 1.0 → 0.9 + Fade Out)
        /// </summary>
        private void PlayHideAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                // CanvasGroup이 없으면 즉시 비활성화
                gameObject.SetActive(false);
                return;
            }

            // 상호작용 비활성화
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence hideSequence = DOTween.Sequence();

            // Fade Out
            hideSequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

            // Scale Down
            hideSequence.Join(popupRect.DOScale(0.9f, hideDuration).SetEase(Ease.InBack));

            // 완료 시 비활성화
            hideSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });

            hideTween = hideSequence;
        }
        #endregion
    }
}
