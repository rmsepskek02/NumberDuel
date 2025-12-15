using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace UI.Shared
{
    /// <summary>
    /// InputFieldPopup Static Wrapper
    /// 어디서든 간편하게 입력 팝업을 호출할 수 있는 정적 클래스
    /// </summary>
    public static class InputFieldPopup
    {
        private static InputFieldPopupUI instance;
        private static GameObject popupObject;
        private const string PREFAB_PATH = "Prefabs/UI/InputFieldPopup";

        /// <summary>
        /// 닉네임 입력 팝업 표시
        /// </summary>
        /// <param name="onConfirm">닉네임 확인 시 콜백</param>
        /// <param name="onCancel">취소 시 콜백 (선택)</param>
        public static void ShowNicknameInput(Action<string> onConfirm, Action onCancel = null)
        {
            if (!EnsureInstance())
                return;

            // 닉네임 검증 함수
            async Task<(bool valid, string message)> ValidateNickname(string nickname)
            {
                // 길이 검사
                if (nickname.Length < 1)
                    return (false, "닉네임은 최소 1자 이상이어야 합니다.");

                if (nickname.Length > 12)
                    return (false, "닉네임은 최대 12자까지 가능합니다.");

                // 특수문자 확인
                foreach (char c in nickname)
                {
                    bool isKorean = (c >= '가' && c <= '힣') || (c >= 'ㄱ' && c <= 'ㅎ') || (c >= 'ㅏ' && c <= 'ㅣ');
                    bool isEnglish = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
                    bool isNumber = (c >= '0' && c <= '9');
                    bool isSpace = c == ' ';

                    if (!isKorean && !isEnglish && !isNumber && !isSpace)
                        return (false, "닉네임은 한글, 영문, 숫자, 공백만 사용할 수 있습니다.");
                }

                // 중복 검사
                bool isAvailable = await Manager.DatabaseManager.Instance.IsNicknameAvailable(nickname);

                if (!isAvailable)
                    return (false, "이미 사용 중인 닉네임입니다.");

                return (true, "사용 가능한 닉네임입니다!");
            }

            instance.Show(
                "닉네임을 설정해주세요",
                "1-12자, 한글/영문/숫자",
                TMP_InputField.ContentType.Standard,
                onConfirm,
                onCancel,
                ValidateNickname
            );
        }

        /// <summary>
        /// 비밀번호 입력 팝업 표시
        /// </summary>
        /// <param name="title">팝업 제목</param>
        /// <param name="onConfirm">비밀번호 확인 시 콜백</param>
        /// <param name="onCancel">취소 시 콜백 (선택)</param>
        public static void ShowPasswordInput(string title, Action<string> onConfirm, Action onCancel = null)
        {
            if (!EnsureInstance())
                return;

            // 비밀번호 검증 함수 (간단한 검증만)
            async Task<(bool valid, string message)> ValidatePassword(string password)
            {
                await Task.Yield(); // async 경고 제거용

                if (password.Length < 6)
                    return (false, "비밀번호는 최소 6자 이상이어야 합니다.");

                return (true, string.Empty);
            }

            instance.Show(
                title,
                "비밀번호 입력",
                TMP_InputField.ContentType.Password,
                onConfirm,
                onCancel,
                ValidatePassword
            );
        }

        /// <summary>
        /// 커스텀 입력 팝업 표시 (범용)
        /// </summary>
        public static void ShowCustomInput(
            string title,
            string placeholder,
            TMP_InputField.ContentType contentType,
            Action<string> onConfirm,
            Action onCancel = null,
            Func<string, Task<(bool valid, string message)>> validator = null)
        {
            if (!EnsureInstance())
                return;

            instance.Show(title, placeholder, contentType, onConfirm, onCancel, validator);
        }

        /// <summary>
        /// 팝업 인스턴스 확인 및 로드
        /// </summary>
        private static bool EnsureInstance()
        {
            if (instance == null)
            {
                return LoadPopup();
            }
            return true;
        }

        /// <summary>
        /// Resources에서 Prefab 로드
        /// </summary>
        private static bool LoadPopup()
        {
            var prefab = Resources.Load<GameObject>(PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError($"[InputFieldPopup] Prefab을 찾을 수 없습니다! 경로: Resources/{PREFAB_PATH}");
                return false;
            }

            popupObject = UnityEngine.Object.Instantiate(prefab);
            popupObject.name = "InputFieldPopup";

            // DontDestroyOnLoad
            UnityEngine.Object.DontDestroyOnLoad(popupObject);

            instance = popupObject.GetComponent<InputFieldPopupUI>();

            if (instance == null)
            {
                Debug.LogError("[InputFieldPopup] Prefab에 InputFieldPopupUI 컴포넌트가 없습니다!");
                UnityEngine.Object.Destroy(popupObject);
                return false;
            }

            Debug.Log("[InputFieldPopup] 팝업이 성공적으로 로드되었습니다.");
            return true;
        }

        /// <summary>
        /// 팝업 인스턴스 제거
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
