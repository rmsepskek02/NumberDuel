using UnityEngine;

//[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FixedAspectCamera : MonoBehaviour
{
    public float baseOrthoSize = 5f; // 기준 해상도 (예: 16:9일 때의 orthoSize)
    public float targetAspect = 20f / 10f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        // 화면이 좁아져 세로가 부족한 경우
        if (currentAspect < targetAspect)
        {
            float scaleFactor = targetAspect / currentAspect;
            cam.orthographicSize = baseOrthoSize * scaleFactor;
        }
        else
        {
            cam.orthographicSize = baseOrthoSize;
        }
    }
}
