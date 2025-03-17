using UnityEngine;

public class InGameBackground : MonoBehaviour
{
    public Camera mainCamera;  // 메인 카메라 (Orthographic 사용)
    public float baseWidth = 1920f;  // 기준 해상도 너비
    public float baseHeight = 1080f; // 기준 해상도 높이
    public float scaleFactor = 1.0f; // 크기 조절 배율 (조정 가능)

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        ResizePlane();
    }

    void Update()
    {
        ResizePlane();
    }

    void ResizePlane()
    {
        float currentWidth = Screen.width;
        float currentHeight = Screen.height;

        // 해상도 비율 계산 (기본 해상도 대비)
        float widthRatio = currentWidth / baseWidth;
        float heightRatio = currentHeight / baseHeight;
        float matchRatio = Mathf.Lerp(widthRatio, heightRatio, 0.5f); // Canvas Scaler처럼 적용

        // Plane의 크기 조정 (Scale 변경)
        transform.localScale = new Vector3(matchRatio * scaleFactor, 1, matchRatio * scaleFactor);

        // Plane을 카메라에 맞게 위치 조정 (정확한 중앙 정렬)
        float cameraHeight = mainCamera.orthographicSize * 2;
        float cameraWidth = cameraHeight * (Screen.width / (float)Screen.height);
        transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0);
    }
}
