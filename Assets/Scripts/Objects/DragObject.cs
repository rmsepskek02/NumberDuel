using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DragObject : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 offset;
    private float zDistance;

    private bool isDragging = false;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        isDragging = true;

        zDistance = Vector3.Distance(transform.position, mainCamera.transform.position);
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        offset = transform.position - mouseWorldPos;
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            transform.position = mouseWorldPos + offset;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenMousePos = Input.mousePosition;
        screenMousePos.z = zDistance;
        return mainCamera.ScreenToWorldPoint(screenMousePos);
    }
}
