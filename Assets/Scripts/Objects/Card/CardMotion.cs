using DG.Tweening;
using Manager;
using UnityEngine;

namespace Objects
{
    /// <summary>
    /// 카드의 시각적 모션(애니메이션)을 담당하는 컴포넌트.
    /// - Hover 시 카드가 확대되고 위로 올라가는 효과
    /// - 드래그 시 원래 자리로 되돌아가는 효과
    /// - 클릭 시 회전 효과 (앞면/뒷면 전환)
    /// ObjectMouseEvent의 입력 이벤트를 받는다.
    /// </summary>
    public class CardMotion : MonoBehaviour
    {
        #region Fields and Properties
        [Header("Hover Settings")]
        [Tooltip("Hover 시 카드가 커지는 배율")]
        [SerializeField] private float hoverScale = 1.3f;

        [Tooltip("Hover 시 카드가 Y방향으로 올라가는 거리")]
        [SerializeField] private float hoverYOffset = 0.3f;

        [Header("Animation Settings")]
        [Tooltip("위치, 크기 애니메이션 시간")]
        [SerializeField] private float moveDuration = 0.3f;

        [Tooltip("회전 애니메이션 시간")]
        [SerializeField] private float rotateDuration = 0.25f;

        [Tooltip("이동/스케일 애니메이션 곡선")]
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        [Tooltip("회전 애니메이션 곡선")]
        [SerializeField] private Ease rotateEase = Ease.OutCubic;

        private Transform rootTransform; // 카드의 부모 트랜스폼 (회전 및 위치 이동용)
        private ObjectMouseEvent objectMouseEvent; // 입력 이벤트를 감지하는 컴포넌트

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

        private bool isReturning; // 복귀 모션 애니메이션 진행중 여부
        private bool isLockedExternally = false;
        #endregion

        #region Unity Lifecycle
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
                null, // 클릭 프레스 리스너는 사용 안함
                null, // 클릭 릴리즈 리스너는 사용 안함
                HandleDragBegin,
                HandleDragEnd,
                HandleToggleChanged
            );
        }

        private void OnDisable()
        {
            // 이벤트 해제 처리 및 트윈 정지
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
        #endregion

        #region Event Handlers
        /// <summary>
        /// 마우스가 카드 위로 올라왔을 때 호출됨
        /// 복귀 모션이라도 중단되어야 하므로 isReturning 체크 안 함
        /// </summary>
        private void HandleHoverEnter()
        {
            // 카드가 제일 앞쪽에 렌더링 되도록 순서 조정
            transform.SetSiblingIndex(transform.parent.childCount - 1);
            AnimateHoverEnter();
        }

        /// <summary>
        /// 마우스가 카드에서 벗어날 때 호출됨
        /// 복귀 중이어도 애니메이션 중단하고 초기 상태 복귀
        /// </summary>
        private void HandleHoverExit()
        {
            AnimateHoverExit();
            AnimateRotation(originalRootRotation);
            objectMouseEvent?.ForceResetToggle();
        }

        /// <summary>
        /// 카드 위에서 클릭 시 회전 상태 전환
        /// </summary>
        private void HandleToggleChanged(bool toFront)
        {
            if (isReturning) return;

            // 앞면 보기 or 뒷면 보기 회전
            AnimateRotation(toFront ? Quaternion.Euler(0f, 0f, 0f) : originalRootRotation);
        }

        /// <summary>
        /// 드래그 시작 시 현재 애니메이션 모두 중단
        /// </summary>
        private void HandleDragBegin()
        {
            CancelMoveAndScaleTweens();
        }

        /// <summary>
        /// 드래그가 끝나면 카드가 원래 위치로 복귀
        /// </summary>
        private void HandleDragEnd()
        {
            AnimateReturnToOriginal();
        }
        #endregion

        #region Animation Methods
        /// <summary>
        /// Hover 진입 시 카드 확대 및 Y축 위로 이동
        /// 복귀 모션이라도 이전 트윈만 중단시키고 새로 시작함
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
        /// Hover 종료 시 카드 크기와 위치 복귀
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
        /// 카드 회전 효과
        /// 앞면 or 뒷면 카드를 되돌리기 위한 회전
        /// </summary>
        private void AnimateRotation(Quaternion target)
        {
            if (rootTransform == null) return;

            rotateTween?.Kill();
            rotateTween = rootTransform.DOLocalRotateQuaternion(target, rotateDuration).SetEase(rotateEase);
        }

        /// <summary>
        /// 드래그가 끝난 뒤 카드가 원래 위치/크기/회전상태로 복귀 (프로세스 상태 예외 처리)
        /// 프로세스 중이면 외부에서 잠금되더라도 강제로 복귀함
        /// </summary>
        private void AnimateReturnToOriginal()
        {
            // DELETE 프로세스 등 특정 상황에서는 무조건 복귀하도록 강제
            bool shouldForceReturn = InGameManager.Instance != null &&
                                   InGameManager.Instance.CurrentProcess != GameProcessState.Idle;

            if (isLockedExternally && !shouldForceReturn)
                return;

            isReturning = true;
            objectMouseEvent?.SetInteractionBlocked(true);

            // 진행 트윈 정지
            moveTween?.Kill();
            rootMoveTween?.Kill();
            rotateTween?.Kill();

            // 위치와 복귀
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
                    AnimateHoverEnter(); // 복귀 직후 Hover 상태
                }
            });
        }

        /// <summary>
        /// 위치 및 스케일 진행 트윈만 정지
        /// </summary>
        private void CancelMoveAndScaleTweens()
        {
            moveTween?.Kill();
            rootMoveTween?.Kill();
            scaleTween?.Kill();
        }

        /// <summary>
        /// 모든 트윈 정지 (회전 포함)
        /// </summary>
        private void CancelAllTweens()
        {
            CancelMoveAndScaleTweens();
            rotateTween?.Kill();
        }
        #endregion

        #region State Management
        /// <summary>
        /// 강제로 Transform 상태 즉시 초기화
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
        /// 현재 Transform 상태를 저장 상태로 다시 갱신
        /// (재배치된 경우 이후 호출)
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
            CancelAllTweens(); // 진행 Kill후 즉시 정지
        }
        #endregion

        #region Public Methods
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
        #endregion
    }
}
