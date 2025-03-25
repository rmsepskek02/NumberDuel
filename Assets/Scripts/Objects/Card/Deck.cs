using Manager;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 덱 오브젝트의 클릭 또는 터치 입력을 처리하는 컴포넌트
    /// ResponsiveObject가 포함되어야 하며, DragObject는 없어도 동작 가능
    /// - 클릭 또는 터치가 덱 오브젝트 위에서 발생했는지 감지하여 OnClick 호출
    /// - 모바일과 PC 모두 대응함
    /// </summary>
    public class Deck : ClickableObjectBase
    {
        public TextMeshPro testText;

        protected override bool CanTriggerClick() => true;

        protected override void OnClick()
        {
            Debug.Log($"Deck '{gameObject.name}' was clicked!");
            testText.text = gameObject.name;
        }
    }
}