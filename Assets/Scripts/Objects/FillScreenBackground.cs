using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class FillScreenBackground : MonoBehaviour
{
    public Camera mainCamera;
    public Vector2 screenOffset = Vector2.zero;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Resize();
    }

    private void Update()
    {
        if (Screen.width != Screen.currentResolution.width || Screen.height != Screen.currentResolution.height)
        {
            Resize();
        }
    }

    private void Resize()
    {
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null) return;

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;

        float targetWidth = camWidth;
        float targetHeight = camHeight;

        float scaleX = targetWidth / meshSize.x;
        float scaleZ = targetHeight / meshSize.z;

        // 비율 무시, 강제 화면 채우기
        transform.localScale = new Vector3(
            scaleX,
            transform.localScale.y,
            scaleZ
        );

        float posX = mainCamera.transform.position.x + camWidth * screenOffset.x;
        float posY = mainCamera.transform.position.y + camHeight * screenOffset.y;
        transform.position = new Vector3(posX, posY, transform.position.z);
    }
}
