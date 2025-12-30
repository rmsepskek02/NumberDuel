using System;
using UnityEngine;

namespace UI.Shared
{
    /// <summary>
    /// 범용 비밀번호 연동 팝업 관리 (Static)
    /// 게임 전체에서 LinkPasswordPopupManager.Show() 호출로 사용 가능
    /// </summary>
    public static class LinkPasswordPopupManager
    {
        private static LinkPasswordPopupUI instance;
        private static GameObject popupObject;
        private const string PREFAB_PATH = "Prefabs/UI/LinkPasswordPopup";

        /// <summary>
        /// 팝업 미리 로드 (최초 지연 방지)
        /// 설정 화면 진입 시 호출 권장
        /// </summary>
        public static void PreloadPopup()
        {
            if (instance == null)
            {
                LoadPopup();
            }
        }

        /// <summary>
        /// 비밀번호 연동 팝업 표시
        /// </summary>
        /// <param name="googleEmail">Google 계정 이메일</param>
        /// <param name="onComplete">완료 콜백</param>
        public static void Show(string googleEmail, Action<bool> onComplete)
        {
            // 최초 1회만 로드 (Preload 안 한 경우 대비)
            if (instance == null)
            {
                if (!LoadPopup())
                {
                    Debug.LogError("[LinkPasswordPopupManager] 팝업 로드 실패!");
                    return;
                }
            }

            // 팝업 표시
            instance.Show(googleEmail, onComplete);
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
            // Resources/Prefabs/UI/LinkPasswordPopup.prefab 로드
            var prefab = Resources.Load<GameObject>(PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError($"[LinkPasswordPopupManager] Prefab을 찾을 수 없습니다! 경로: Resources/{PREFAB_PATH}");
                return false;
            }

            // 인스턴스 생성
            popupObject = UnityEngine.Object.Instantiate(prefab);
            popupObject.name = "LinkPasswordPopup"; // (Clone) 제거

            // DontDestroyOnLoad 설정 (씬 전환되어도 유지)
            UnityEngine.Object.DontDestroyOnLoad(popupObject);

            // LinkPasswordPopupUI 컴포넌트 가져오기
            instance = popupObject.GetComponent<LinkPasswordPopupUI>();

            if (instance == null)
            {
                Debug.LogError("[LinkPasswordPopupManager] Prefab에 LinkPasswordPopupUI 컴포넌트가 없습니다!");
                UnityEngine.Object.Destroy(popupObject);
                return false;
            }

            Debug.Log("[LinkPasswordPopupManager] 팝업이 성공적으로 로드되었습니다.");
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
