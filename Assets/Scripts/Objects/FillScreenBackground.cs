using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카메라의 해상도 변화에 따라 배경 오브젝트를 자동으로 리사이즈하여 화면에 꽉 차도록 맞추는 컴포넌트.
    /// 
    /// - 메시 평면 방향(XY 또는 XZ)을 설정하여 다양한 메시 타입에 대응 가능
    /// - 오프셋(screenOffset)을 이용해 화면 내 위치를 상대적으로 조정 가능
    /// - 화면 비율이 변경되거나 전체화면 전환 시 자동으로 재조정됨
    /// </summary>
    [ExecuteAlways] // 에디터에서 실행 중에도 리사이즈 동작
    [RequireComponent(typeof(MeshFilter))] // 메시 기반 오브젝트에만 사용 가능
    public class FillScreenBackground : MonoBehaviour
    {
        /// <summary>
        /// 크기 기준이 되는 카메라 (지정하지 않으면 MainCamera 사용)
        /// </summary>
        public Camera mainCamera;

        /// <summary>
        /// 메시의 평면 방향 (XY 또는 XZ)
        /// - Unity 기본 Plane은 XZ이므로 기본값은 XZ
        /// - Sprite나 2D 메시라면 XY로 설정
        /// </summary>
        public enum PlaneAxis { XY, XZ }
        [SerializeField] private PlaneAxis meshAxis = PlaneAxis.XZ;

        /// <summary>
        /// 카메라 화면에 대한 오브젝트의 상대적 위치 오프셋
        /// 예: (0, 0) = 중앙, (0.5, 0.5) = 오른쪽 위
        /// </summary>
        public Vector2 screenOffset = Vector2.zero;

        private Vector2 lastScreenSize = Vector2.zero; // 이전 프레임의 해상도 저장용

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            Resize(); // 초기 크기 조정
        }

        private void Update()
        {
            // 해상도 변경 감지 후 리사이즈
            Vector2 currentScreenSize = new Vector2(Screen.width, Screen.height);
            if (currentScreenSize != lastScreenSize)
            {
                Resize();
                lastScreenSize = currentScreenSize;
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Alt+Enter 등으로 전체화면 전환 시 재조정
            if (hasFocus)
            {
                Resize();
            }
        }

        /// <summary>
        /// 현재 카메라 크기에 맞춰 오브젝트 크기와 위치 재조정
        /// </summary>
        private void Resize()
        {
            // 카메라의 가시 범위 계산 (Orthographic 기준)
            float camHeight = mainCamera.orthographicSize * 2f;
            float camWidth = camHeight * mainCamera.aspect;

            // 메시 크기 가져오기 (로컬 좌표 기준)
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null) return;
            Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

            // 메시 평면 방향에 따른 폭/높이 설정
            float rawWidth = meshAxis == PlaneAxis.XZ ? meshSize.x : meshSize.x;
            float rawHeight = meshAxis == PlaneAxis.XZ ? meshSize.z : meshSize.y;

            // 카메라 크기에 맞게 스케일 계산
            float scaleX = camWidth / rawWidth;
            float scaleY = camHeight / rawHeight;

            // 오브젝트 스케일 적용 (Y 축은 일반적으로 1 유지)
            transform.localScale = new Vector3(
                scaleX,
                transform.localScale.y,
                scaleY
            );

            // 카메라 위치 기준으로 화면 비율에 맞춘 상대 위치 설정
            float posX = mainCamera.transform.position.x + camWidth * screenOffset.x;
            float posY = mainCamera.transform.position.y + camHeight * screenOffset.y;
            transform.position = new Vector3(posX, posY, transform.position.z);
        }
    }
}
