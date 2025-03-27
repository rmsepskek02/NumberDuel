using Objects;
using UnityEngine;
using UnityEngine.InputSystem;

public class HoverCardMotion : MonoBehaviour
{
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool initialized = false;
    private bool isHovered;

    [SerializeField] private float returnSpeed = 10f;

    private DragObject dragObject;

    public void SetInitialState()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        initialized = true;

        dragObject = GetComponent<DragObject>();
    }

    private void Update()
    {
        if (!initialized || dragObject == null)
            return;

        // 드래그 중일 땐 위치 유지
        if (dragObject.IsDragging)
            return;

        // 드래그가 끝난 후 → 부드럽게 원래 위치로 회복
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * returnSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, originalLocalRotation, Time.deltaTime * returnSpeed);

        // 마우스 위치를 Input System 방식으로 가져오기
        Vector2 inputPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(inputPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"[HoverCardMotion] Ray Hit: {hit.collider.name}");
        }
    }
    private void OnMouseEnter()
    {
        if (!initialized || dragObject == null || dragObject.IsDragging) return;

        isHovered = true;

        Debug.Log($"[Hover ON] {gameObject.name}");
    }

    private void OnMouseExit()
    {
        if (!initialized || dragObject == null) return;

        isHovered = false;

        Debug.Log($"[Hover OFF] {gameObject.name}");
    }

}
