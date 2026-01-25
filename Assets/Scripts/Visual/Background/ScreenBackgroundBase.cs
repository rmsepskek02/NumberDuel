using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 배경 오브젝트의 화면 크기 및 위치 조정 공통 기능을 제공하는 베이스 클래스
    /// - 메시 기반 오브젝트에 사용
    /// - 실행 중 해상도 또는 포커스 상태 변화에 반응
    /// - 자식 클래스는 Resize 구현을 통해 스케일링 전략을 제공해야 함
    /// </summary>
    
    [RequireComponent(typeof(MeshFilter))]
    public abstract class ScreenBackgroundBase : MonoBehaviour
    {
        public enum PlaneAxis { XY, XZ }

        [Tooltip("크기 기준이 되는 카메라 (지정하지 않으면 MainCamera 사용)")]
        public Camera mainCamera;

        [Tooltip("화면 기준 위치 오프셋 (0 = 중앙, 0.5 = 오른쪽/위쪽)")]
        public Vector2 screenOffset = Vector2.zero;

        [Tooltip("메시의 평면 방향 (XY 또는 XZ)")]
        public PlaneAxis meshAxis = PlaneAxis.XZ;

        protected MeshFilter meshFilter;
        protected Vector2 lastScreenSize;

        protected virtual void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            meshFilter = GetComponent<MeshFilter>();
            Resize();
        }

        protected virtual void Update()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            Resize(); // 항상 Resize 호출
        }

        protected virtual void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Resize();
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            Resize();
        }
#endif

        /// <summary>
        /// 현재 카메라의 화면 크기(가로, 세로)를 구함
        /// </summary>
        protected Vector2 GetCameraSize()
        {
            float camHeight = mainCamera.orthographicSize * 2f;
            return new Vector2(camHeight * mainCamera.aspect, camHeight);
        }

        /// <summary>
        /// 카메라 위치와 오프셋 기준으로 오브젝트 위치를 설정
        /// </summary>
        protected void ApplyPosition(Vector2 camSize)
        {
            float posX = mainCamera.transform.position.x + camSize.x * screenOffset.x;
            float posY = mainCamera.transform.position.y + camSize.y * screenOffset.y;
            transform.position = new Vector3(posX, posY, transform.position.z);
        }

        /// <summary>
        /// 자식 클래스에서 반드시 구현해야 하는 크기 조정 메서드
        /// </summary>
        protected abstract void Resize();
    }
}
