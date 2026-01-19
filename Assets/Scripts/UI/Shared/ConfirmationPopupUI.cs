using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace UI.Shared
{
    /// <summary>
    /// 재사용 가능한 확인 팝업
    /// 동적으로 메시지와 액션 설정 가능
    /// </summary>
    public class ConfirmationPopupUI : PopupBase<ConfirmationPopupUI>
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI cancelButtonText;
        [SerializeField] private TextMeshProUGUI confirmButtonText;

        #endregion

        #region Private Fields

        private Action onConfirmed;
        private Action onCanceled;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // 버튼 이벤트 등록
            cancelButton?.onClick.AddListener(OnCancelClicked);
            confirmButton?.onClick.AddListener(OnConfirmClicked);
        }

        private void Update()
        {
            // 팝업이 활성화되어 있고 애니메이션 중이 아닐 때만 키 입력 처리
            if (!gameObject.activeSelf || isAnimating)
                return;

            // Enter 키 처리
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                // 취소 버튼이 활성화되어 있으면 Enter 무시 (명시적 클릭 유도)
                if (cancelButton != null && cancelButton.gameObject.activeSelf)
                {
                    return;
                }

                // 취소 버튼이 없으면 확인 버튼 클릭
                OnConfirmClicked();

                // Enter 키 입력을 소비했다고 표시
                UIStackManager.Instance?.ConsumeEnterKey();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 버튼 이벤트 해제
            cancelButton?.onClick.RemoveListener(OnCancelClicked);
            confirmButton?.onClick.RemoveListener(OnConfirmClicked);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        public static void Show(string message, Action onConfirm)
        {
            Show(message, onConfirm, null);
        }

        /// <summary>
        /// 확인 팝업 표시 (취소 콜백 포함)
        /// </summary>
        public static void Show(string message, Action onConfirm, Action onCancel)
        {
            Show(message, onConfirm, onCancel, "확인", "취소");
        }

        /// <summary>
        /// 확인 팝업 표시 (버튼 텍스트 커스터마이징 포함)
        /// </summary>
        public static void Show(string message, Action onConfirm, Action onCancel, string confirmText, string cancelText = "취소")
        {
            if (instance == null && !LoadPopup())
                return;

            instance.ShowInternal(message, onConfirm, onCancel, confirmText, cancelText);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 팝업 표시 (Instance)
        /// </summary>
        private void ShowInternal(string message, Action onConfirm, Action onCancel, string confirmText, string cancelText)
        {
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

            // UIStackManager에 등록
            RegisterToUIStack();

            // 애니메이션
            PlayShowAnimation();

            // 사운드 재생
            PlayClickSound();
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelClicked()
        {
            // 콜백 저장 (HideInternal에서 초기화될 수 있으므로)
            var callback = onCanceled;

            // 먼저 숨기기 (UIStack에서 제거)
            HideInternal();

            // 콜백 실행
            callback?.Invoke();
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private void OnConfirmClicked()
        {
            // 콜백 저장 (HideInternal에서 초기화될 수 있으므로)
            var callback = onConfirmed;

            // 먼저 숨기기 (UIStack에서 제거)
            // 콜백에서 다른 팝업을 열 때 스택 순서가 올바르게 유지됨
            HideInternal();

            // 콜백 실행
            callback?.Invoke();
        }

        #endregion

        #region ICloseable Override

        /// <summary>
        /// ICloseable 구현: 팝업 닫기 (취소 버튼 클릭과 동일)
        /// </summary>
        public override void Close()
        {
            OnCancelClicked();
        }

        #endregion
    }
}
