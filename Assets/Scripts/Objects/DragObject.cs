using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 오브젝트를 마우스 또는 터치 입력으로 드래그할 수 있도록 하는 컴포넌트.
/// PC/모바일 모두 지원하며, 일정 거리 이상 움직이면 드래그로 간주함.
/// </summary>
public class DragObject : MonoBehaviour
{
    private Camera mainCamera;         // 입력을 월드 위치로 변환하기 위한 카메라
    private Vector3 offset;            // 클릭 지점과 오브젝트 중심 사이 거리
    private float zDistance;           // 카메라와 오브젝트 사이 z축 거리
    private bool isDragging;           // 현재 드래그 중인지 여부

    private Vector2 dragStartPos;      // 드래그 시작 시 마우스/터치 위치
    private bool wasDragged = false;   // 드래그가 수행되었는지 여부
    private float dragThreshold = 10f; // 드래그로 인식할 최소 이동 거리 (픽셀)

    public bool WasDragged => wasDragged; // 외부에서 참조할 수 있도록 제공 (클릭 판별에 사용)

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Vector2 inputPos = Vector2.zero;
        bool pressed = false;
        bool released = false;
        bool isPressed = false;

        // 입력 구분 (에디터/PC vs 모바일)
#if UNITY_EDITOR || UNITY_STANDALONE
        pressed = Mouse.current.leftButton.wasPressedThisFrame;
        released = Mouse.current.leftButton.wasReleasedThisFrame;
        isPressed = Mouse.current.leftButton.isPressed;
        inputPos = Mouse.current.position.ReadValue();
#else
        pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        isPressed = Touchscreen.current.primaryTouch.press.isPressed;
        inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
#endif

        Ray ray = mainCamera.ScreenPointToRay(inputPos);

        // 드래그 시작
        if (pressed)
        {
            wasDragged = false;
            dragStartPos = inputPos;

            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                zDistance = Vector3.Distance(mainCamera.transform.position, transform.position);
                offset = transform.position - mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
                isDragging = true;
            }
        }

        // 드래그 중 이동 처리
        if (isDragging && isPressed)
        {
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(inputPos.x, inputPos.y, zDistance));
            transform.position = worldPos + offset;

            if (!wasDragged && Vector2.Distance(inputPos, dragStartPos) > dragThreshold)
            {
                wasDragged = true; // 일정 거리 이상 이동했을 경우 드래그로 간주
            }
        }

        // 드래그 종료
        if (released)
        {
            isDragging = false;
        }
    }
}
