using UnityEngine;

/// <summary>
/// 특정 해상도 비율을 기준으로 게임 화면을 유지하기 위한 카메라 조절 스크립트.
/// 세로가 부족할 경우에만 orthographicSize를 증가시켜 게임 비율을 유지.
/// 가로가 줄어들 때는 화면 크기를 줄이지 않음 (오브젝트가 잘릴 수 있음).
/// </summary>
[RequireComponent(typeof(Camera))]
public class FixedAspectCamera : MonoBehaviour
{
    public float baseOrthoSize = 5f; // 기준 카메라 orthographicSize
    public float targetAspect = 20f / 10f; // 기준 해상도 비율 (ex: 16:9 → 1.777)

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;

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
