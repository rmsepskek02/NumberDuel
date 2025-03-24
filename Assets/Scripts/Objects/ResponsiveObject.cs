using UnityEngine;

//[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
public class ResponsiveObject : MonoBehaviour
{
    public Camera mainCamera;
    public bool maintainAspectRatio = true;

    private Vector2 initialScreenSize;
    private Vector3 originalScale;
    private Vector3 originalPosition;

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
        }

        lastPosition = transform.position;
        Resize();
    }

    private void Update()
    {
        // 수동 이동 감지 (드래그 등)
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

        float initialArea = initialScreenSize.x * initialScreenSize.y;
        float currentArea = Screen.width * Screen.height;
        float scaleFactor = Mathf.Sqrt(currentArea / initialArea);
        float finalScale = maintainAspectRatio ? scaleFactor : 1f;

        transform.localScale = new Vector3(
            originalScale.x * finalScale,
            originalScale.y,
            originalScale.z * finalScale
        );

        if (!isManuallyMoved)
        {
            transform.position = new Vector3(
                originalPosition.x * finalScale,
                originalPosition.y * finalScale,
                originalPosition.z
            );
        }
    }
}
