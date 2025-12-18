using System;
using UnityEngine;

namespace UI.Shared
{
    /// <summary>
    /// 범용 SNS 연동 팝업 관리 (Static)
    /// 게임 전체에서 LinkSocialPopupManager.Show() 호출로 사용 가능
    /// </summary>
    public static class LinkSocialPopupManager
    {
        private static LinkSocialPopupUI instance;
        private static GameObject popupObject;
        private const string PREFAB_PATH = "Prefabs/UI/LinkSocialPopup";

        /// <summary>
        /// SNS 연동 팝업 표시
        /// </summary>
        /// <param name="email">현재 계정 이메일</param>
        /// <param name="onComplete">완료 콜백</param>
        public static void Show(string email, Action<bool> onComplete)
        {
            // 최초 1회만 로드
            if (instance == null)
            {
                if (!LoadPopup())
                {
                    Debug.LogError("[LinkSocialPopupManager] 팝업 로드 실패!");
                    return;
                }
            }

            // 팝업 표시
            instance.Show(email, onComplete);
        }

        /// <summary>
        /// 팝업 숨기기
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
            // Resources/Prefabs/UI/LinkSocialPopup.prefab 로드
            var prefab = Resources.Load<GameObject>(PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError($"[LinkSocialPopupManager] Prefab을 찾을 수 없습니다! 경로: Resources/{PREFAB_PATH}");
                return false;
            }

            // 인스턴스 생성
            popupObject = UnityEngine.Object.Instantiate(prefab);
            popupObject.name = "LinkSocialPopup"; // (Clone) 제거

            // DontDestroyOnLoad 설정 (씬 전환되어도 유지)
            UnityEngine.Object.DontDestroyOnLoad(popupObject);

            // LinkSocialPopupUI 컴포넌트 가져오기
            instance = popupObject.GetComponent<LinkSocialPopupUI>();

            if (instance == null)
            {
                Debug.LogError("[LinkSocialPopupManager] Prefab에 LinkSocialPopupUI 컴포넌트가 없습니다!");
                UnityEngine.Object.Destroy(popupObject);
                return false;
            }

            Debug.Log("[LinkSocialPopupManager] 팝업이 성공적으로 로드되었습니다.");
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
