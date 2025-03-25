using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 배경 메시의 원본 비율을 유지하면서 카메라 화면에 맞게 스케일 조정하는 컴포넌트
    /// - 여백이 생기는 것을 허용하되 왜곡 없이 전체 화면 대응 가능
    /// </summary>
    //[ExecuteAlways]
    public class KeepAspectBackground : ScreenBackgroundBase
    {
        [Tooltip("카메라 기준 비율 (1,1 = 전체 화면 기준)")]
        public Vector2 screenRatioSize = new Vector2(1f, 1f);

        protected override void Resize()
        {

            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Vector2 camSize = GetCameraSize();
            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

            float rawWidth = meshSize.x;
            float rawHeight = meshAxis == PlaneAxis.XZ ? meshSize.z : meshSize.y;

            float targetWidth = camSize.x * screenRatioSize.x;
            float targetHeight = camSize.y * screenRatioSize.y;

            float scaleX = targetWidth / rawWidth;
            float scaleY = targetHeight / rawHeight;

            float finalScale = Mathf.Min(scaleX, scaleY);

            transform.localScale = new Vector3(
                finalScale,
                transform.localScale.y,
                finalScale
            );
            ApplyPosition(camSize);
        }
    }
}
