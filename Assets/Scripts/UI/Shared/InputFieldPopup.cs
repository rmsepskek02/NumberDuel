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

        #region Public Methods
        /// <summary>
        /// 닉네임 입력 팝업 표시
        /// </summary>
        public static void ShowNicknameInput(Action<string> onConfirm, Action onCancel = null)
        {
            if (!EnsureInstance())
                return;

            async Task<(bool valid, string message)> ValidateNickname(string nickname)
            {
                bool isAvailable = await Manager.DatabaseManager.Instance.IsNicknameAvailable(nickname);

                if (!isAvailable)
                    return (false, "이미 사용 중인 닉네임입니다.");

                return (true, "사용 가능한 닉네임입니다!");
            }

            instance.Show(
                "닉네임을 설정해주세요",
                "닉네임을 설정해주세요",
                TMP_InputField.ContentType.Standard,
                onConfirm,
                onCancel,
                ValidateNickname
            );
        }

        /// <summary>
        /// 비밀번호 입력 팝업 표시
        /// </summary>
        public static void ShowPasswordInput(string title, Action<string> onConfirm, Action onCancel = null)
        {
            if (!EnsureInstance())
                return;

            async Task<(bool valid, string message)> ValidatePassword(string password)
            {
                await Task.Yield();

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
        /// 팝업 숨기기
        /// </summary>
        public static void Hide()
        {
            if (instance != null)
                instance.Hide();
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
        #endregion

        #region Private Methods
        private static bool EnsureInstance()
        {
            if (instance == null)
                return LoadPopup();

            return true;
        }

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
            UnityEngine.Object.DontDestroyOnLoad(popupObject);

            instance = popupObject.GetComponent<InputFieldPopupUI>();

            if (instance == null)
            {
                Debug.LogError("[InputFieldPopup] Prefab에 InputFieldPopupUI 컴포넌트가 없습니다!");
                UnityEngine.Object.Destroy(popupObject);
                return false;
            }

            return true;
        }
        #endregion
    }
}
