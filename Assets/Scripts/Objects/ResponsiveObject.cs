using UnityEngine;

namespace Objects
{
    /// <summary>
    /// FixedAspectCamera와 연동하여, 카메라의 orthographicSize 변화에 따라
    /// 오브젝트의 스케일과 위치를 자동으로 조정하는 컴포넌트
    /// - 가로 비율이 줄어들 경우엔 스케일과 위치 유지
    /// - 세로 비율이 줄어들 경우엔 축소 및 위치 재조정
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class ResponsiveObject : MonoBehaviour
    {
        public Camera mainCamera;
        private float baseOrthoSize;

        private Vector3 originalScale;
        private Vector3 originalPosition;
        private Vector2 positionRatio;

        private Vector3 lastPosition;
        private bool isManuallyMoved = false;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            baseOrthoSize = GetBaseOrthoSize();
        }

        private void OnEnable()
        {
            if (originalScale == Vector3.zero)
            {
                originalScale = transform.localScale;
                originalPosition = transform.position;

                float camHeight = baseOrthoSize * 2f;
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
                isManuallyMoved = true;

            Resize();
            lastPosition = transform.position;
        }

        private void Resize()
        {
            float currentOrthoSize = mainCamera.orthographicSize;

            float scaleFactor = Mathf.Min(currentOrthoSize / baseOrthoSize, 1f);
            transform.localScale = originalScale * scaleFactor;

            if (!isManuallyMoved)
            {
                float camHeight = currentOrthoSize * 2f;
                float camWidth = camHeight * mainCamera.aspect;

                float posX = mainCamera.transform.position.x + camWidth * positionRatio.x;
                float posY = mainCamera.transform.position.y + camHeight * positionRatio.y;

                transform.position = new Vector3(posX, posY, originalPosition.z);
            }
        }


        private float GetBaseOrthoSize()
        {
            FixedAspectCamera aspectController = mainCamera.GetComponent<FixedAspectCamera>();
            if (aspectController != null)
            {
                return aspectController.baseOrthoSize;
            }

            Debug.LogWarning("FixedAspectCamera 컴포넌트를 찾을 수 없습니다. 현재 orthographicSize를 기준으로 사용합니다.");
            return mainCamera.orthographicSize;
        }
    }
}


//using UnityEngine;

//namespace Objects
//{
//    [RequireComponent(typeof(MeshFilter))]
//    public class ResponsiveObject : MonoBehaviour
//    {
//        public Camera mainCamera;

//        private float baseOrthoSize;
//        private Vector3 originalScale;
//        private Vector3 originalPosition;
//        private Vector2 safeRatio; // Safe Area 내에서의 비율

//        private Vector3 lastPosition;
//        private bool isManuallyMoved = false;

//        private void Awake()
//        {
//            if (mainCamera == null)
//                mainCamera = Camera.main;

//            baseOrthoSize = GetBaseOrthoSize();
//        }

//        private void OnEnable()
//        {
//            if (originalScale == Vector3.zero)
//            {
//                originalScale = transform.localScale;
//                originalPosition = transform.position;

//                // 기준 카메라 사이즈로 SafeArea 포지션 비율 계산
//                float camHeight = baseOrthoSize * 2f;
//                float camWidth = camHeight * mainCamera.aspect;

//                Rect safeArea = Screen.safeArea;
//                Vector2 anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
//                Vector2 anchorMax = new Vector2((safeArea.x + safeArea.width) / Screen.width,
//                                                (safeArea.y + safeArea.height) / Screen.height);

//                float safeW = anchorMax.x - anchorMin.x;
//                float safeH = anchorMax.y - anchorMin.y;

//                float xRatio = ((originalPosition.x - mainCamera.transform.position.x) / camWidth - anchorMin.x + 0.5f) / safeW;
//                float yRatio = ((originalPosition.y - mainCamera.transform.position.y) / camHeight - anchorMin.y + 0.5f) / safeH;

//                safeRatio = new Vector2(xRatio, yRatio);
//            }

//            lastPosition = transform.position;
//            Resize();
//        }

//        private void Update()
//        {
//            if (!isManuallyMoved && transform.position != lastPosition)
//                isManuallyMoved = true;

//            Resize();
//            lastPosition = transform.position;
//        }

//        private void Resize()
//        {
//            float currentOrthoSize = mainCamera.orthographicSize;
//            float scaleFactor = Mathf.Min(currentOrthoSize / baseOrthoSize, 1f);
//            transform.localScale = originalScale * scaleFactor;

//            if (!isManuallyMoved)
//            {
//                float camHeight = currentOrthoSize * 2f;
//                float camWidth = camHeight * mainCamera.aspect;

//                Rect safeArea = Screen.safeArea;
//                Vector2 anchorMin = new Vector2(safeArea.x / Screen.width, safeArea.y / Screen.height);
//                Vector2 anchorMax = new Vector2((safeArea.x + safeArea.width) / Screen.width,
//                                                (safeArea.y + safeArea.height) / Screen.height);

//                float safeW = anchorMax.x - anchorMin.x;
//                float safeH = anchorMax.y - anchorMin.y;

//                float posX = mainCamera.transform.position.x + camWidth * (anchorMin.x + safeW * safeRatio.x - 0.5f);
//                float posY = mainCamera.transform.position.y + camHeight * (anchorMin.y + safeH * safeRatio.y - 0.5f);

//                transform.position = new Vector3(posX, posY, originalPosition.z);
//            }
//        }

//        private float GetBaseOrthoSize()
//        {
//            FixedAspectCamera camAspect = mainCamera.GetComponent<FixedAspectCamera>();
//            return camAspect != null ? camAspect.baseOrthoSize : mainCamera.orthographicSize;
//        }
//    }
//}
