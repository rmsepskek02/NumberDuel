using System;
using UnityEngine;

namespace UI.Shared
{
    /// <summary>
    /// 비밀번호 찾기 팝업 관리 (Static)
    /// 게임 전체에서 FindPasswordPopup.Show() 호출로 사용 가능
    ///
    /// 사용 예시:
    /// FindPasswordPopup.Show();
    /// </summary>
    public static class FindPasswordPopup
    {
        private static FindPasswordPopupUI instance;
        private static GameObject popupObject;
        private const string PREFAB_PATH = "Prefabs/UI/FindPasswordPopup";

        /// <summary>
        /// 비밀번호 찾기 팝업 표시
        /// </summary>
        public static void Show()
        {
            // 최초 1회만 로드
            if (instance == null)
            {
                if (!LoadPopup())
                {
                    Debug.LogError("[FindPasswordPopup] 팝업 로드 실패!");
                    return;
                }
            }

            // 팝업 표시
            instance.Show();
        }

        /// <summary>
        /// 비밀번호 찾기 팝업 숨기기
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
            // Resources/Prefabs/UI/FindPasswordPopup.prefab 로드
            var prefab = Resources.Load<GameObject>(PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError($"[FindPasswordPopup] Prefab을 찾을 수 없습니다! 경로: Resources/{PREFAB_PATH}");
                return false;
            }

            // 인스턴스 생성
            popupObject = UnityEngine.Object.Instantiate(prefab);
            popupObject.name = "FindPasswordPopup"; // (Clone) 제거

            // DontDestroyOnLoad 설정 (씬 전환되어도 유지)
            UnityEngine.Object.DontDestroyOnLoad(popupObject);

            // FindPasswordPopupUI 컴포넌트 가져오기
            instance = popupObject.GetComponent<FindPasswordPopupUI>();

            if (instance == null)
            {
                Debug.LogError("[FindPasswordPopup] Prefab에 FindPasswordPopupUI 컴포넌트가 없습니다!");
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
