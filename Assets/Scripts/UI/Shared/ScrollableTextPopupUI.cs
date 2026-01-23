using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Shared
{
    /// <summary>
    /// 스크롤 가능한 텍스트 팝업 UI
    /// 개인정보처리방침, 이용약관, 공지사항 등 긴 텍스트 표시용
    /// </summary>
    public class ScrollableTextPopupUI : PopupBase<ScrollableTextPopupUI>
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TextMeshProUGUI contentText;
        [SerializeField] private Button closeButton;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            closeButton?.onClick.AddListener(OnCloseClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            closeButton?.onClick.RemoveListener(OnCloseClicked);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// 스크롤 가능한 텍스트 팝업 표시
        /// </summary>
        /// <param name="title">팝업 제목</param>
        /// <param name="content">본문 내용</param>
        public static void Show(string title, string content)
        {
            if (instance == null && !LoadPopup())
                return;

            instance.ShowInternal(title, content);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 팝업 표시 (Instance)
        /// </summary>
        private void ShowInternal(string title, string content)
        {
            // 텍스트 설정
            if (titleText != null)
                titleText.text = title;

            if (contentText != null)
                contentText.text = content;

            // 활성화
            gameObject.SetActive(true);

            // UIStackManager에 등록
            RegisterToUIStack();

            // 스크롤 위치 초기화 (프레임 대기 후)
            StartCoroutine(ResetScrollPositionDelayed());

            // 애니메이션
            PlayShowAnimation();

            // 사운드 재생
            PlayClickSound();
        }

        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void OnCloseClicked()
        {
            HideInternal();
        }

        /// <summary>
        /// 프레임 대기 후 스크롤 위치 초기화
        /// </summary>
        private IEnumerator ResetScrollPositionDelayed()
        {
            yield return null; // 1프레임 대기 (레이아웃 재계산 완료)

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        #endregion

        #region ICloseable Override

        /// <summary>
        /// ICloseable 구현: 팝업 닫기
        /// </summary>
        public override void Close()
        {
            OnCloseClicked();
        }

        #endregion
    }
}
