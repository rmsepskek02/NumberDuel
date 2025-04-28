using UnityEngine;
using DG.Tweening;

namespace Objects
{
    /// <summary>
    /// 카드의 시각적 동작(모션)을 제어하는 컴포넌트.
    /// - Hover 시 카드가 확대되고 위로 들리는 연출 제공
    /// - 드래그 후 원래 자리로 부드럽게 복귀
    /// - 클릭 시 회전 연출 (정면/원래 방향)
    /// ObjectMouseEvent의 입력 이벤트에 반응함.
    /// </summary>
    public class CardMotion : MonoBehaviour
    {
        [Header("Hover Settings")]
        [Tooltip("Hover 시 카드가 커지는 배율")]
        [SerializeField] private float hoverScale = 1.3f;

        [Tooltip("Hover 시 카드가 Y축으로 올라가는 높이")]
        [SerializeField] private float hoverYOffset = 0.3f;

        [Header("Animation Settings")]
        [Tooltip("위치, 크기 애니메이션 시간")]
        [SerializeField] private float moveDuration = 0.3f;

        [Tooltip("회전 애니메이션 시간")]
        [SerializeField] private float rotateDuration = 0.25f;

        [Tooltip("이동/스케일 애니메이션 이징")]
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        [Tooltip("회전 애니메이션 이징")]
        [SerializeField] private Ease rotateEase = Ease.OutCubic;

        private Transform rootTransform; // 카드의 부모 오브젝트 (회전 및 위치 이동용)
        private ObjectMouseEvent objectMouseEvent; // 입력 이벤트를 수신하는 컴포넌트

        // 초기 상태 저장용 변수들
        private Vector3 originalLocalPosition;
        private Vector3 originalLocalScale;
        private float originalY;

        private Vector3 originalRootPosition;
        private Quaternion originalRootRotation;

        // DOTween 트윈 캐시
        private Tween moveTween;
        private Tween scaleTween;
        private Tween rootMoveTween;
        private Tween rotateTween;

        private bool isReturning; // 현재 복귀 애니메이션 중인지 여부
        private bool isLockedExternally = false;

        private void Awake()
        {
            objectMouseEvent = GetComponent<ObjectMouseEvent>();
            rootTransform = transform.parent;
        }

        private void OnEnable()
        {
            // 입력 이벤트 리스너 등록
            objectMouseEvent?.RegisterListeners(
                HandleHoverEnter,
                HandleHoverExit,
                null, // 클릭 시작은 사용하지 않음
                null, // 클릭 끝도 사용하지 않음
                HandleDragBegin,
                HandleDragEnd,
                HandleToggleChanged
            );
        }

        private void OnDisable()
        {
            // 이벤트 등록 해제 및 트윈 종료
            objectMouseEvent?.UnregisterListeners(
                HandleHoverEnter,
                HandleHoverExit,
                null,
                null,
                HandleDragBegin,
                HandleDragEnd,
                HandleToggleChanged
            );

            CancelAllTweens();
        }

        private void Start()
        {
            // 카드 초기 상태 저장
            originalLocalPosition = transform.localPosition;
            originalLocalScale = transform.localScale;
            originalY = originalLocalPosition.y;

            if (rootTransform != null)
            {
                originalRootPosition = rootTransform.localPosition;
                originalRootRotation = rootTransform.localRotation;
            }
        }

        /// <summary>
        /// 마우스가 카드 위에 올라왔을 때 호출됨
        /// 복귀 중이라도 실행되어야 하므로 isReturning 체크 안 함
        /// </summary>
        private void HandleHoverEnter()
        {
            // 카드가 맨 앞으로 오도록 렌더 순서 조정
            transform.SetSiblingIndex(transform.parent.childCount - 1);
            AnimateHoverEnter();
        }

        /// <summary>
        /// 마우스가 카드에서 벗어났을 때 호출됨
        /// 복귀 중엔 애니메이션 방해하지 않기 위해 무시
        /// </summary>
        private void HandleHoverExit()
        {
            AnimateHoverExit();
            AnimateRotation(originalRootRotation);
            objectMouseEvent?.ForceResetToggle();
        }

        /// <summary>
        /// 카드 토글 클릭 시 회전 방향 전환
        /// </summary>
        private void HandleToggleChanged(bool toFront)
        {
            if (isReturning) return;

            // 앞면 보기 or 원래 각도 복원
            AnimateRotation(toFront ? Quaternion.Euler(0f, 0f, 0f) : originalRootRotation);
        }

        /// <summary>
        /// 드래그 시작 시 기존 애니메이션 모두 중단
        /// </summary>
        private void HandleDragBegin()
        {
            CancelMoveAndScaleTweens();
        }

        /// <summary>
        /// 드래그가 끝나면 카드가 원래 위치로 돌아감
        /// </summary>
        private void HandleDragEnd()
        {
            AnimateReturnToOriginal();
        }

        /// <summary>
        /// Hover 진입 시 카드 확대 및 Y축 상승 연출
        /// 복귀 중이라도 기존 트윈을 중단하지 않고 위에 덧씌움
        /// </summary>
        private void AnimateHoverEnter()
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalLocalScale * hoverScale, moveDuration).SetEase(moveEase);

            if (!isReturning && rootTransform != null)
            {
                rootMoveTween?.Kill();
                rootMoveTween = rootTransform.DOLocalMoveY(originalRootPosition.y + hoverYOffset, moveDuration).SetEase(moveEase);
            }
        }

        /// <summary>
        /// Hover 해제 시 카드 크기와 위치 원복
        /// </summary>
        private void AnimateHoverExit()
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(originalLocalScale, moveDuration).SetEase(moveEase);

            if (!isReturning && rootTransform != null)
            {
                rootMoveTween?.Kill();
                rootMoveTween = rootTransform.DOLocalMoveY(originalRootPosition.y, moveDuration).SetEase(moveEase);
            }
        }

        /// <summary>
        /// 카드 회전 연출
        /// 정면 or 원래 각도로 부드럽게 회전
        /// </summary>
        private void AnimateRotation(Quaternion target)
        {
            if (rootTransform == null) return;

            rotateTween?.Kill();
            rotateTween = rootTransform.DOLocalRotateQuaternion(target, rotateDuration).SetEase(rotateEase);
        }

        /// <summary>
        /// 드래그가 끝났을 때 카드가 원래 위치/크기/회전으로 복귀
        /// 도중에는 HoverExit을 무시하여 애니메이션 충돌 방지
        /// 복귀 완료 후 마우스가 올라가 있다면 다시 Hover 효과 적용
        /// </summary>
        private void AnimateReturnToOriginal()
        {
            if (isLockedExternally) return;

            isReturning = true;
            objectMouseEvent?.SetInteractionBlocked(true);

            // 기존 트윈 제거
            moveTween?.Kill();
            rootMoveTween?.Kill();
            rotateTween?.Kill();

            // 위치만 복귀
            moveTween = transform.DOLocalMove(originalLocalPosition, moveDuration).SetEase(moveEase);

            if (rootTransform != null)
            {
                rootMoveTween = rootTransform.DOLocalMove(originalRootPosition, moveDuration).SetEase(moveEase);
                AnimateRotation(originalRootRotation);
            }

            // 복귀 완료 처리
            DOVirtual.DelayedCall(moveDuration, () =>
            {
                isReturning = false;
                objectMouseEvent?.SetInteractionBlocked(false);

                if (objectMouseEvent != null && objectMouseEvent.IsHovered)
                {
                    AnimateHoverEnter(); // 복귀 끝난 뒤 Hover 적용
                }
            });
        }

        /// <summary>
        /// 위치 및 스케일 관련 트윈만 종료
        /// </summary>
        private void CancelMoveAndScaleTweens()
        {
            moveTween?.Kill();
            rootMoveTween?.Kill();
            scaleTween?.Kill();
        }

        /// <summary>
        /// 모든 트윈 종료 (회전 포함)
        /// </summary>
        private void CancelAllTweens()
        {
            CancelMoveAndScaleTweens();
            rotateTween?.Kill();
        }

        /// <summary>
        /// 강제로 Transform 원래 값 복구
        /// </summary>
        private void ForceResetTransform()
        {
            transform.localScale = originalLocalScale;
            transform.localPosition = originalLocalPosition;

            if (rootTransform != null)
            {
                rootTransform.localPosition = originalRootPosition;
                rootTransform.localRotation = originalRootRotation;
            }
        }

        /// <summary>
        /// 현재 Transform 상태를 원래 상태로 다시 저장
        /// (부채꼴 재배치된 뒤 호출)
        /// </summary>
        private void RefreshOriginalState()
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

        private void RestartReturnMotion()
        {
            if (isReturning)
            {
                CancelAllTweens();

                moveTween = transform.DOLocalMove(originalLocalPosition, moveDuration).SetEase(moveEase);

                if (rootTransform != null)
                {
                    rootMoveTween = rootTransform.DOLocalMove(originalRootPosition, moveDuration).SetEase(moveEase);
                    AnimateRotation(originalRootRotation);
                }
            }
        }

        private void LockMotion()
        {
            isLockedExternally = true;
            CancelAllTweens(); // 강제 Kill도 같이 해줌
        }

        // 외부 사용 함수
        public void ResetReturnMotion()
        {
            RefreshOriginalState();
            RestartReturnMotion();
        }
        public void LockAndReset()
        {
            LockMotion();
            ForceResetTransform();
        }
    }
}
