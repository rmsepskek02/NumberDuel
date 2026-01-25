using UnityEngine;

/// <summary>
/// 특정 해상도 비율(targetAspect)을 기준으로 카메라의 orthographicSize를 조절하는 스크립트.
/// - 기준 비율보다 세로가 더 긴 화면 (좁은 비율)에서는 카메라 크기를 확장하여 게임 영역을 유지.
/// - 가로가 더 좁아지더라도 orthographicSize는 고정되므로 오브젝트가 잘릴 수 있음.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FixedAspectCamera : MonoBehaviour
{
    public float baseOrthoSize = 5f;             // 기준 해상도 비율일 때의 orthographicSize
    public float targetAspect = 20f / 10f;       // 기준 해상도 비율 (가로 / 세로)

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        // 현재 비율이 기준보다 세로가 긴 경우: 카메라 확대
        if (currentAspect < targetAspect)
        {
            float scaleFactor = targetAspect / currentAspect;
            cam.orthographicSize = baseOrthoSize * scaleFactor;
        }
        else
        {
            // 기준 비율보다 가로가 좁거나 같으면 고정된 orthographicSize 유지
            cam.orthographicSize = baseOrthoSize;
        }
    }
}
