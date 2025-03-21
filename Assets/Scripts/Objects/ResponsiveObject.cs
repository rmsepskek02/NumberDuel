using UnityEngine;

public class ResponsiveObject : MonoBehaviour
{
    public Camera mainCamera;
    public Vector2 scaleFactor = new Vector2(1f, 1f); // 크기 조정 비율
    public Vector2 positionOffset = new Vector2(0f, 0f); // 위치 조정 오프셋
    public bool maintainAspectRatio = true; // 가로세로 비율 유지 여부

    private Vector2 lastScreenSize;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        ResizeObject();
    }

    void Update()
    {
        // 해상도가 변경될 경우 크기 재조정
        if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            ResizeObject();
            lastScreenSize = new Vector2(Screen.width, Screen.height);
        }
    }

    /// <summary>
    /// 오브젝트를 해상도에 맞게 크기와 위치 조정
    /// </summary>
    void ResizeObject()
    {
        if (mainCamera == null) return;

        float cameraHeight = mainCamera.orthographicSize * 0.2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;

        // 오브젝트 크기 조정
        float newWidth = cameraWidth * scaleFactor.x;
        float newHeight = maintainAspectRatio ? newWidth / transform.localScale.x * transform.localScale.y : cameraHeight * scaleFactor.y;

        transform.localScale = new Vector3(newWidth, transform.localScale.y, newHeight);

        // 오브젝트 위치 조정 (카메라를 기준으로 상대적인 위치 적용)
        float posX = mainCamera.transform.position.x + (cameraWidth * positionOffset.x);
        float posY = mainCamera.transform.position.y + (cameraHeight * positionOffset.y);

        transform.position = new Vector3(posX, posY, transform.position.z);
    }
}
