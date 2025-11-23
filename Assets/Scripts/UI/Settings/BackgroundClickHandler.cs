using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Settings
{
    /// <summary>
    /// 설정 UI 배경 클릭 핸들러
    /// - 배경(SettingsPanel) 자체를 클릭했을 때만 설정 창 닫기
    /// - 자식 UI 요소(MainPanel 등)를 클릭하면 무시
    /// </summary>
    public class BackgroundClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            // 실제로 클릭된 오브젝트가 정확히 이 오브젝트(SettingsPanel)인지 확인
            // MainPanel이나 다른 자식 UI를 클릭하면 eventData.pointerPress는 다른 오브젝트를 가리킴
            if (eventData.pointerPress == gameObject)
            {
                // 배경만 클릭했을 때 설정 창 닫기
                Manager.SettingsManager.Instance?.HideSettings();
            }
        }
    }
}
