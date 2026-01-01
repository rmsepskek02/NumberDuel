using System;
using UnityEngine;

namespace UI.Shared
{
    /// <summary>
    /// 범용 확인 팝업 관리 (Static)
    /// 게임 전체에서 ConfirmationPopup.Show() 호출로 사용 가능
    ///
    /// 사용 예시:
    /// ConfirmationPopup.Show("정말로 삭제하시겠습니까?", () => DeleteItem());
    /// </summary>
    public static class ConfirmationPopup
    {
        private static ConfirmationPopupUI instance;
        private static GameObject popupObject;
        private const string PREFAB_PATH = "Prefabs/UI/ConfirmationPopup";

        /// <summary>
        /// 확인 팝업 표시 (통합 메서드)
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <param name="onConfirm">확인 버튼 클릭 시 실행할 액션 (null 가능)</param>
        /// <param name="onCancel">취소 버튼 클릭 시 실행할 액션 (null = 취소 버튼 숨김)</param>
        /// <param name="confirmText">확인 버튼 텍스트 (기본값: "확인")</param>
        /// <param name="cancelText">취소 버튼 텍스트 (기본값: "취소")</param>
        public static void Show(
            string message,
            Action onConfirm = null,
            Action onCancel = null,
            string confirmText = "확인",
            string cancelText = "취소")
        {
            // 최초 1회만 로드
            if (instance == null)
            {
                if (!LoadPopup())
                {
                    Debug.LogError("[ConfirmationPopup] 팝업 로드 실패!");
                    return;
                }
            }

            // 팝업 표시
            instance.Show(message, onConfirm, onCancel, confirmText, cancelText);
        }

        /// <summary>
        /// 확인 팝업 숨기기
        /// </summary>
        public static void Hide()
        {
            instance?.Hide();
        }

        /// <summary>
        /// Resources에서 Prefab 로드 및 인스턴스 생성 (최초 1회만)
        /// </summary>
        private static bool LoadPopup()
        {
            // Resources/UI/ConfirmationPopup.prefab 로드
            var prefab = Resources.Load<GameObject>(PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError($"[ConfirmationPopup] Prefab을 찾을 수 없습니다! 경로: Resources/{PREFAB_PATH}");
                return false;
            }

            // 인스턴스 생성
            popupObject = UnityEngine.Object.Instantiate(prefab);
            popupObject.name = "ConfirmationPopup"; // (Clone) 제거

            // DontDestroyOnLoad 설정 (씬 전환되어도 유지)
            UnityEngine.Object.DontDestroyOnLoad(popupObject);

            // ConfirmationPopupUI 컴포넌트 가져오기
            instance = popupObject.GetComponent<ConfirmationPopupUI>();

            if (instance == null)
            {
                Debug.LogError("[ConfirmationPopup] Prefab에 ConfirmationPopupUI 컴포넌트가 없습니다!");
                UnityEngine.Object.Destroy(popupObject);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 팝업 인스턴스 제거 (필요 시)
        /// </summary>
        public static void Destroy()
        {
            if (popupObject != null)
            {
                UnityEngine.Object.Destroy(popupObject);
                popupObject = null;
                instance = null;
            }
        }
    }
}
