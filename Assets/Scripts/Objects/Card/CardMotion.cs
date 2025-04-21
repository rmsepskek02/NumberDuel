using UnityEngine;
using DG.Tweening;

namespace Objects
{
    public class CardMotion : MonoBehaviour
    {
        [Header("Hover Settings")]
        [SerializeField] private float hoverScale = 1.3f;
        [SerializeField] private float hoverYOffset = 0.3f;

        [Header("Animation Settings")]
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private float rotateDuration = 0.25f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;
        [SerializeField] private Ease rotateEase = Ease.OutCubic;

        private Transform rootTransform;
        private ObjectMouseEvent objectMouseEvent;

        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private float originalY;

        private Vector3 originalRootPosition;
        private Quaternion originalRootRotation;

        private Tween moveTween;
        private Tween scaleTween;
        private Tween rootMoveTween;
        private Tween rotateTween;

        private bool initialized = false;
        private bool isReturning = false;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            rootTransform = transform.parent;
        }

        private void OnEnable()
        {
            objectMouseEvent?.RegisterListeners(
                HandleHoverEnter,
                HandleHoverExit,
                HandleClickPressed,
                HandleClickReleased,
                HandleDragBegin,
                HandleDragEnd,
                HandleToggleChanged
            );
        }

        private void OnDisable()
        {
            objectMouseEvent?.UnregisterListeners(
                HandleHoverEnter,
                HandleHoverExit,
                HandleClickPressed,
                HandleClickReleased,
                HandleDragBegin,
                HandleDragEnd,
                HandleToggleChanged
            );

            CancelAllTweens();
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                SetInitialState();
                initialized = true;
            }
        }

        public void SetInitialState()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalScale = transform.localScale;
            originalY = originalLocalPosition.y;

            if (rootTransform != null)
            {
                originalRootPosition = rootTransform.localPosition;
                originalRootRotation = rootTransform.localRotation;
            }
        }

        private void HandleHoverEnter()
        {
            if (isReturning) return;

            transform.SetSiblingIndex(transform.parent.childCount - 1);
            AnimateHoverEnter();
        }

        private void HandleHoverExit()
        {
            if (isReturning) return;

            AnimateHoverExit();
            AnimateRotation(originalRootRotation);
            objectMouseEvent?.ForceResetToggle();
        }

        private void HandleClickPressed() { }
        private void HandleClickReleased() { }

        private void HandleToggleChanged(bool toFront)
        {
            if (isReturning) return;
            AnimateRotation(toFront ? Quaternion.Euler(0f, 0f, 0f) : originalRootRotation);
        }

        private void HandleDragBegin()
        {
            CancelAllTweens();
        }

        private void HandleDragEnd()
        {
            AnimateReturnToOriginal();
        }

        private void AnimateHoverEnter()
        {
            CancelMoveAndScaleTweens();

            scaleTween = transform.DOScale(originalLocalScale * hoverScale, moveDuration).SetEase(moveEase);
            moveTween = transform.DOLocalMoveY(originalY + hoverYOffset, moveDuration).SetEase(moveEase);

            if (rootTransform != null)
            {
                rootMoveTween = rootTransform.DOLocalMoveY(originalRootPosition.y + hoverYOffset, moveDuration).SetEase(moveEase);
            }
        }

        private void AnimateHoverExit()
        {
            CancelMoveAndScaleTweens();

            scaleTween = transform.DOScale(originalLocalScale, moveDuration).SetEase(moveEase);
            moveTween = transform.DOLocalMoveY(originalY, moveDuration).SetEase(moveEase);

            if (rootTransform != null)
            {
                rootMoveTween = rootTransform.DOLocalMoveY(originalRootPosition.y, moveDuration).SetEase(moveEase);
            }
        }

        private void AnimateRotation(Quaternion target)
        {
            if (rootTransform == null) return;

            rotateTween?.Kill();
            rotateTween = rootTransform.DOLocalRotateQuaternion(target, rotateDuration).SetEase(rotateEase);
        }

        private void AnimateReturnToOriginal()
        {
            isReturning = true;
            objectMouseEvent?.SetInteractionBlocked(true);

            CancelAllTweens();

            moveTween = transform.DOLocalMove(originalLocalPosition, moveDuration).SetEase(moveEase);
            scaleTween = transform.DOScale(originalLocalScale, moveDuration).SetEase(moveEase);

            if (rootTransform != null)
            {
                rootMoveTween = rootTransform.DOLocalMove(originalRootPosition, moveDuration).SetEase(moveEase);
                AnimateRotation(originalRootRotation);
            }

            DOVirtual.DelayedCall(moveDuration, () =>
            {
                isReturning = false;
                objectMouseEvent?.SetInteractionBlocked(false);

                // Hover 복원 트리거
                if (IsPointerOverCard())
                {
                    objectMouseEvent?.ForceHoverEnter();
                    HandleHoverEnter();
                }
            });
        }

        private bool IsPointerOverCard()
        {
            if (Camera.main == null) return false;

            Vector2 screenPos = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            return Physics.Raycast(ray, out RaycastHit hit) &&
                   (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform));
        }

        private void CancelMoveAndScaleTweens()
        {
            moveTween?.Kill();
            rootMoveTween?.Kill();
            scaleTween?.Kill();
        }

        private void CancelAllTweens()
        {
            CancelMoveAndScaleTweens();
            rotateTween?.Kill();
        }
    }
}
