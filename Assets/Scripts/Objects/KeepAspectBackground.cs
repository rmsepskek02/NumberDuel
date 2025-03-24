using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class KeepAspectBackground : MonoBehaviour
{
    public Camera mainCamera;
    public Vector2 screenRatioSize = new Vector2(1f, 1f); // 카메라 화면 대비 크기 비율
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

        float targetWidth = camWidth * screenRatioSize.x;
        float targetHeight = camHeight * screenRatioSize.y;

        float scaleX = targetWidth / meshSize.x;
        float scaleZ = targetHeight / meshSize.z;

        float finalScale = Mathf.Min(scaleX, scaleZ); // 여백 허용, 비율 유지

        transform.localScale = new Vector3(
            finalScale,
            transform.localScale.y,
            finalScale
        );

        float posX = mainCamera.transform.position.x + camWidth * screenOffset.x;
        float posY = mainCamera.transform.position.y + camHeight * screenOffset.y;
        transform.position = new Vector3(posX, posY, transform.position.z);
    }
}
