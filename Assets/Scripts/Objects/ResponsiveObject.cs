using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 오브젝트의 크기와 위치를 화면 해상도에 따라 자동으로 조정하는 컴포넌트
    /// - 초기 해상도를 기준으로 스케일을 비율로 조정
    /// - 위치는 카메라 내 상대적인 비율로 유지됨
    /// - 드래그 등 수동 이동이 감지되면 자동 위치 조정은 중단됨
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class ResponsiveObject : MonoBehaviour
    {
        public Camera mainCamera; // 오브젝트 위치 및 해상도 기준이 되는 카메라

        private Vector2 initialScreenSize;     // 최초 실행 시의 해상도
        private Vector3 originalScale;         // 최초 오브젝트 스케일
        private Vector3 originalPosition;      // 최초 오브젝트 위치
        private Vector2 positionRatio;         // 카메라 기준 위치 비율

        private Vector3 lastPosition;          // 이전 프레임의 위치 (이동 감지용)
        private bool isManuallyMoved = false;  // 사용자가 직접 이동했는지 여부

        private void Awake()
        {
            // 카메라가 지정되지 않았으면 메인 카메라 사용
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            // 초기 설정: 한 번만 수행
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
                originalPosition = transform.position;
                initialScreenSize = new Vector2(Screen.width, Screen.height);

                // 카메라 화면 기준 위치 비율 저장 (0~1 범위)
                float camHeight = mainCamera.orthographicSize * 2f;
                float camWidth = camHeight * mainCamera.aspect;
                positionRatio = new Vector2(
                    (originalPosition.x - mainCamera.transform.position.x) / camWidth,
                    (originalPosition.y - mainCamera.transform.position.y) / camHeight
                );
            }

            lastPosition = transform.position;
            Resize(); // 시작 시 한 번 실행
        }

        private void Update()
        {
            // 사용자가 직접 오브젝트를 이동했는지 확인
            if (!isManuallyMoved && transform.position != lastPosition)
            {
                isManuallyMoved = true;
            }

            Resize(); // 매 프레임 해상도에 맞춰 크기/위치 조정
            lastPosition = transform.position;
        }

        /// <summary>
        /// 오브젝트의 스케일과 위치를 화면 해상도에 맞춰 재조정
        /// </summary>
        private void Resize()
        {
            if (originalScale == Vector3.zero) return; // 초기화 안 된 경우 처리 안 함

            // 화면 면적 변화 비율 계산
            float currentArea = Screen.width * Screen.height;
            float initialArea = initialScreenSize.x * initialScreenSize.y;
            float scaleFactor = Mathf.Sqrt(currentArea / initialArea); // 균형 있는 스케일링을 위해 루트 사용

            // 크기 비례 적용 (비율 유지)
            transform.localScale = originalScale * scaleFactor;

            // 위치 자동 보정 (사용자가 수동 이동한 경우 제외)
            if (!isManuallyMoved)
            {
                float camHeight = mainCamera.orthographicSize * 2f;
                float camWidth = camHeight * mainCamera.aspect;

                float posX = mainCamera.transform.position.x + camWidth * positionRatio.x;
                float posY = mainCamera.transform.position.y + camHeight * positionRatio.y;

                transform.position = new Vector3(posX, posY, originalPosition.z);
            }
        }
    }
}
