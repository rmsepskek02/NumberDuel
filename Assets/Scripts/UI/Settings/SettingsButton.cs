using Objects;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
    /// <summary>
    /// 씬별 설정 버튼 (톱니바퀴 아이콘)
    /// 모든 씬의 좌측 상단에 배치
    /// </summary>
    public class SettingsButton : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Button")]
        [SerializeField] private Button button;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 버튼이 없으면 자동으로 찾기
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            // 버튼 이벤트 등록
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            // 버튼 이벤트 해제
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClicked);
            }
        }
        #endregion

        #region Button Handler
        /// <summary>
        /// 설정 버튼 클릭
        /// </summary>
        private void OnButtonClicked()
        {
            // SettingsManager를 통해 설정 패널 열기
            if (Manager.SettingsManager.Instance != null)
            {
                Manager.SettingsManager.Instance.ShowSettings();
            }
            else
            {
                Debug.LogWarning("[SettingsButton] SettingsManager가 없습니다!");
            }

            // 사운드 재생
            Manager.SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }
        #endregion
    }
}
