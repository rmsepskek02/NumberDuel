using UnityEngine;

public class InGameBackground : MonoBehaviour
{
    public Camera mainCamera;  // 메인 카메라 (Orthographic 사용)

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
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize * 0.2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // Plane의 크기를 현재 해상도에 딱 맞게 조정
        transform.localScale = new Vector3(cameraWidth, 1, cameraHeight);

        // Plane의 위치를 카메라 중심에 맞춤
        transform.position = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0);
    }
}
