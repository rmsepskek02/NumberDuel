using Manager;
using TMPro;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 덱 컴포넌트로 클릭 또는 터치 입력을 처리하는 컴포넌트
    /// ResponsiveObject와 결합되어야 하며, DragObject는 제외 필요 주의
    /// - 클릭 또는 터치로 덱 컴포넌트 클릭이 발생했는지 감지하여 OnClick 호출
    /// - 모바일과 PC 모두 대응
    /// </summary>
    //public class Deck : ClickableObjectBase
    //{
    //    public TextMeshPro testText;
    //
    //    protected override bool CanTriggerClick() => true;
    //
    //    protected override void OnClick()
    //    {
    //        Debug.Log($"Deck '{gameObject.name}' was clicked!");
    //        testText.text = gameObject.name;
    //    }
    //}
}
