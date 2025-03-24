using UnityEngine;

namespace Objects
{
    [RequireComponent(typeof(MeshFilter))]
    public class ResponsiveObject : MonoBehaviour
    {
        public Camera mainCamera;

        [Tooltip("해상도 변화에 따른 스케일 최소값과 최대값")]
        public float minScaleFactor = 0.5f;
        public float maxScaleFactor = 1.5f;

        private Vector2 initialScreenSize;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Vector2 positionRatio;

        private Vector3 lastPosition;
        private bool isManuallyMoved = false;

        private void Awake()
        {
            if (mainCamera == null) mainCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
                originalPosition = transform.position;
                initialScreenSize = new Vector2(Screen.width, Screen.height);

                float camHeight = mainCamera.orthographicSize * 2f;
                float camWidth = camHeight * mainCamera.aspect;
                positionRatio = new Vector2(
                    (originalPosition.x - mainCamera.transform.position.x) / camWidth,
                    (originalPosition.y - mainCamera.transform.position.y) / camHeight
                );
            }

            lastPosition = transform.position;
            Resize();
        }

        private void Update()
        {
            if (!isManuallyMoved && transform.position != lastPosition)
            {
                isManuallyMoved = true;
            }

            Resize();
            lastPosition = transform.position;
        }

        private void Resize()
        {
            if (originalScale == Vector3.zero) return;

            float currentArea = Screen.width * Screen.height;
            float initialArea = initialScreenSize.x * initialScreenSize.y;
            float scaleFactor = Mathf.Sqrt(currentArea / initialArea);

            // 클램프 적용으로 과도한 크기 제한
            scaleFactor = Mathf.Clamp(scaleFactor, minScaleFactor, maxScaleFactor);

            transform.localScale = originalScale * scaleFactor;

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
