using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 배경 메시를 화면에 꽉 차도록 스케일 조정하는 컴포넌트
    /// - XY 또는 XZ 평면 방향 설정 가능
    /// - 화면 비율 변화 및 전체화면 전환 등에도 자동 대응
    /// </summary>
    [ExecuteAlways]
    public class FillScreenBackground : ScreenBackgroundBase
    {
        protected override void Resize()
        {
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Vector2 camSize = GetCameraSize();
            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

            // 메시 평면 방향에 따라 높이 축 분기
            float rawWidth = meshSize.x;
            float rawHeight = meshAxis == PlaneAxis.XZ ? meshSize.z : meshSize.y;

            float scaleX = camSize.x / rawWidth;
            float scaleY = camSize.y / rawHeight;

            transform.localScale = new Vector3(
                scaleX,
                transform.localScale.y,
                scaleY
            );

            ApplyPosition(camSize);
        }
    }
}