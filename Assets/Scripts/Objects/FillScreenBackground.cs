using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카메라 해상도에 맞춰 배경 오브젝트를 강제로 화면에 꽉 채우는 컴포넌트
    /// - 원본 비율을 유지하지 않고 화면을 꽉 채움
    /// - 화면이 늘어나거나 줄어들 때 자동으로 리사이즈됨
    /// - ExecuteAlways 속성으로 에디터에서도 즉시 반영 가능
    /// </summary>
    [ExecuteAlways] // 에디터 모드에서도 항상 실행
    [RequireComponent(typeof(MeshFilter))]
    public class FillScreenBackground : MonoBehaviour
    {
        public Camera mainCamera;

        /// <summary>
        /// 화면 기준 위치 오프셋 (중앙 기준, -0.5 ~ 0.5)
        /// </summary>
        public Vector2 screenOffset = Vector2.zero;

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Resize();
        }

        private void Update()
        {
            // 해상도가 바뀐 경우 자동으로 리사이즈
            if (Screen.width != Screen.currentResolution.width || Screen.height != Screen.currentResolution.height)
            {
                Resize();
            }
        }

        /// <summary>
        /// 카메라 크기에 맞춰 배경 오브젝트 크기 및 위치 재조정
        /// </summary>
        private void Resize()
        {
            // 카메라 크기 계산 (Orthographic 기준)
            float camHeight = mainCamera.orthographicSize * 2f;
            float camWidth = camHeight * mainCamera.aspect;

            // 메시 크기 확인
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null) return;

            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

            // 목표 크기를 현재 화면 크기에 강제 맞춤 (비율 무시)
            float targetWidth = camWidth;
            float targetHeight = camHeight;

            float scaleX = targetWidth / meshSize.x;
            float scaleZ = targetHeight / meshSize.z;

            transform.localScale = new Vector3(
                scaleX,
                transform.localScale.y, // 일반적으로 1 유지
                scaleZ
            );

            // 오프셋 적용하여 위치 조정 (화면 중앙 기준)
            float posX = mainCamera.transform.position.x + camWidth * screenOffset.x;
            float posY = mainCamera.transform.position.y + camHeight * screenOffset.y;
            transform.position = new Vector3(posX, posY, transform.position.z);
        }
    }
}
