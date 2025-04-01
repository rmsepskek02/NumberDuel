using TMPro;
using UnityEngine;

namespace Objects
{
    public class Card : ClickableObjectBase
    {
        private DragObject dragObject;
        public TextMeshPro testText;

        private void Awake()
        {
            dragObject = GetComponent<DragObject>();
        }

        protected override bool CanTriggerClick()
        {
            return dragObject != null && dragObject.ClickRequested;
        }

        protected override void OnClick()
        {
            Debug.Log($"Card '{gameObject.name}' was clicked!");

            if (testText != null)
            {
                testText.text = gameObject.name;
            }

            dragObject.ResetClickFlag(); // 클릭 처리 완료 후 초기화
        }
    }
}
