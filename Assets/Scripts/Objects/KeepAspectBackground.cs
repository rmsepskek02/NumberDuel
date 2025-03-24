using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 배경 오브젝트의 가로세로 비율을 유지하면서 해상도에 맞게 크기를 조정하는 컴포넌트
    /// - 화면 여백이 생기는 것을 허용하되, 원본 비율은 유지
    /// - 카메라 뷰 기준 비율로 크기 설정
    /// - 실행 중 해상도 변경에도 반응함
    /// </summary>
    [ExecuteAlways] // 에디터에서도 실행됨
    [RequireComponent(typeof(MeshFilter))]
    public class KeepAspectBackground : MonoBehaviour
    {
        public Camera mainCamera;

        /// <summary>
        /// 카메라 크기 기준 비율 (1,1 이면 전체 화면)
        /// </summary>
        public Vector2 screenRatioSize = new Vector2(1f, 1f);

        /// <summary>
        /// 화면 기준 위치 오프셋 (중앙 기준 -0.5 ~ 0.5)
        /// </summary>
        public Vector2 screenOffset = Vector2.zero;

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Resize();
        }

        private void Update()
        {
            // 화면 해상도가 변경되었을 경우에만 크기 재조정
            if (Screen.width != Screen.currentResolution.width || Screen.height != Screen.currentResolution.height)
            {
                Resize();
            }
        }

        /// <summary>
        /// 카메라 비율에 맞게 오브젝트 크기와 위치를 조정
        /// </summary>
        private void Resize()
        {
            float camHeight = mainCamera.orthographicSize * 2f;      // 카메라 세로 높이
            float camWidth = camHeight * mainCamera.aspect;          // 카메라 가로 폭

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null) return;

            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;    // 메시의 실제 원본 크기

            // 목표 크기 계산 (카메라 크기 비율 기반)
            float targetWidth = camWidth * screenRatioSize.x;
            float targetHeight = camHeight * screenRatioSize.y;

            // 실제 스케일링 값 계산 (오브젝트 크기를 목표 크기에 맞추기)
            float scaleX = targetWidth / meshSize.x;
            float scaleZ = targetHeight / meshSize.z;

            // 원본 비율 유지 → 더 작은 쪽 기준으로 스케일 통일 (여백 허용)
            float finalScale = Mathf.Min(scaleX, scaleZ);

            transform.localScale = new Vector3(
                finalScale,
                transform.localScale.y, // Y는 고정 (2D 배경이라면 일반적으로 1)
                finalScale
            );

            // 오프셋에 따라 위치 재조정
            float posX = mainCamera.transform.position.x + camWidth * screenOffset.x;
            float posY = mainCamera.transform.position.y + camHeight * screenOffset.y;

            transform.position = new Vector3(posX, posY, transform.position.z);
        }
    }
}
