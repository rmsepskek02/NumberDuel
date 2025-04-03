using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 개별 카드 오브젝트에 대한 클릭 이벤트 처리
    /// - ClickableObjectBase 상속을 통해 입력 처리
    /// - 클릭되면 카드 이름을 텍스트로 출력
    /// </summary>
    public class Card : ClickableObjectBase
    {
        private ObjectMouseEvent objectMouseEvent;
        public TextMeshPro testText;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
        }

        protected override bool CanTriggerClick()
        {
            // 드래그가 아닌 클릭 요청일 때만 클릭 처리 허용
            return objectMouseEvent != null && objectMouseEvent.ClickRequested;
        }

        protected override void OnClick()
        {
            Debug.Log($"Card '{gameObject.name}' was clicked!");

            if (testText != null)
            {
                testText.text = gameObject.name;
            }

            // 클릭 처리 후 클릭 상태 초기화
            objectMouseEvent.ResetClickFlag();
        }
    }
}
