using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드 오브젝트의 클릭 이벤트를 처리하는 컴포넌트.
    /// DragObject와 함께 사용되어, 드래그와 클릭을 정확히 구분하여 처리함.
    /// </summary>
    public class Card : ClickableObjectBase
    {
        private DragObject dragObject;
        public TextMeshPro testText;

        private void Awake()
        {
            dragObject = GetComponent<DragObject>();
        }

        protected override bool CanTriggerClick() => dragObject != null && !dragObject.WasDragged;

        protected override void OnClick()
        {
            Debug.Log($"Card '{gameObject.name}' was clicked!");
            testText.text = gameObject.name;
        }
    }
}
