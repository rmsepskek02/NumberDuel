using Objects;
using UnityEngine;
using UnityEngine.UI;

namespace Utills
{
    /// <summary>
    /// UI 관련 유틸리티 함수 모음
    /// Button 사운드 자동 등록 등의 기능 제공
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// 현재 씬의 모든 Button에 ButtonSound 컴포넌트 자동 추가
        /// 이미 ButtonSound가 있는 버튼은 스킵
        /// </summary>
        public static void RegisterAllButtonSounds()
        {
            // Unity 2023+ 최신 API 사용
            Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int addedCount = 0;
            int skippedCount = 0;

            foreach (var button in buttons)
            {
                // 이미 ButtonSound가 있으면 스킵
                if (button.GetComponent<ButtonSound>() != null)
                {
                    skippedCount++;
                    continue;
                }

                // ButtonSound 컴포넌트 추가
                button.gameObject.AddComponent<ButtonSound>();
                addedCount++;
            }

            Debug.Log($"[UIHelper] ButtonSound 등록 완료 - 추가: {addedCount}개, 스킵: {skippedCount}개 (총 {buttons.Length}개)");
        }

        /// <summary>
        /// 특정 Button에 ButtonSound 컴포넌트 추가 (Extension Method)
        /// </summary>
        /// <param name="button">대상 버튼</param>
        /// <param name="soundType">재생할 사운드 타입 (기본값: UI_ButtonClick)</param>
        /// <param name="volumeScale">볼륨 배율 (기본값: 1.0)</param>
        /// <returns>추가된 ButtonSound 컴포넌트</returns>
        public static ButtonSound AddClickSound(this Button button, SoundType soundType = SoundType.UI_ButtonClick, float volumeScale = 1.0f)
        {
            // 이미 있으면 기존 컴포넌트 반환
            var buttonSound = button.GetComponent<ButtonSound>();
            if (buttonSound == null)
            {
                buttonSound = button.gameObject.AddComponent<ButtonSound>();
            }

            // 사운드 타입과 볼륨 설정
            buttonSound.SetSoundType(soundType);
            buttonSound.SetVolumeScale(volumeScale);

            return buttonSound;
        }

        /// <summary>
        /// 특정 GameObject 하위의 모든 Button에 ButtonSound 추가
        /// </summary>
        /// <param name="parent">부모 GameObject</param>
        /// <param name="includeInactive">비활성 오브젝트 포함 여부 (기본값: true)</param>
        public static void RegisterButtonSoundsInChildren(GameObject parent, bool includeInactive = true)
        {
            Button[] buttons = parent.GetComponentsInChildren<Button>(includeInactive);
            int addedCount = 0;

            foreach (var button in buttons)
            {
                if (button.GetComponent<ButtonSound>() == null)
                {
                    button.gameObject.AddComponent<ButtonSound>();
                    addedCount++;
                }
            }

            Debug.Log($"[UIHelper] {parent.name} 하위 ButtonSound 등록 완료 - 추가: {addedCount}개");
        }

        /// <summary>
        /// GameObject를 DontDestroyOnLoad로 설정
        /// 씬 전환 시에도 파괴되지 않음
        /// </summary>
        /// <param name="gameObject">대상 GameObject</param>
        public static void SetDontDestroyOnLoad(GameObject gameObject)
        {
            if (gameObject == null)
            {
                Debug.LogError("[UIHelper] GameObject가 null입니다!");
                return;
            }

            Object.DontDestroyOnLoad(gameObject);
            Debug.Log($"[UIHelper] {gameObject.name}을(를) DontDestroyOnLoad로 설정했습니다.");
        }

        /// <summary>
        /// GameObject를 DontDestroyOnLoad로 설정 (Extension Method)
        /// </summary>
        public static void MakeDontDestroy(this GameObject gameObject)
        {
            SetDontDestroyOnLoad(gameObject);
        }
    }
}
