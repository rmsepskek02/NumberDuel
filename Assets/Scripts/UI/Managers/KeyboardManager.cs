using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UMI;
using Utills;

namespace Manager
{
    /// <summary>
    /// 모바일 키보드 대응 관리자
    ///
    /// 주요 기능:
    /// - UMI (Unity Mobile Input) 플러그인을 사용하여 실시간 키보드 높이 감지
    /// - InputField 포커스 시 키보드에 가려지지 않도록 UI를 자동으로 위로 이동
    /// - Scene 전환 시 UI를 원위치로 복원하여 일관된 사용자 경험 제공
    ///
    /// 작동 원리:
    /// 1. Scene 로드 시 모든 InputField를 자동 탐지하여 이벤트 등록
    /// 2. InputField 선택 시 해당 InputField가 속한 Canvas의 직속 자식 오브젝트를 이동 타겟으로 설정
    /// 3. 키보드 높이 감지 시 InputField가 키보드에 가려지는지 계산
    /// 4. 가려진다면 Canvas 직속 자식을 위로 이동하여 InputField가 키보드 위에 위치하도록 조정
    /// 5. 키보드 숨김 시 UI를 원위치로 복원
    ///
    /// 주의사항:
    /// - MobileInputManager가 먼저 초기화되어야 함 (UMI.MobileInput.Init() 필요)
    /// - Canvas RenderMode는 ScreenSpaceCamera 모드를 권장
    /// - 비활성화된 InputField도 자동으로 등록되므로 런타임에 활성화되어도 정상 작동
    /// </summary>
    public class KeyboardManager : SingletonDontDestroy<KeyboardManager>
    {
        #region Serialized Fields

        [Header("키보드 대응 설정")]
        [Tooltip("InputField와 키보드 사이의 여유 공간 (픽셀 단위)\n값이 클수록 InputField가 키보드에서 멀어집니다")]
        [SerializeField] private float additionalOffset = 100f;

        [Tooltip("UI 이동 애니메이션 지속 시간 (초)")]
        [SerializeField] private float animationDuration = 0.3f;

        [Tooltip("UI 이동 애니메이션 Easing 타입")]
        [SerializeField] private Ease animationEase = Ease.OutCubic;

        #endregion

        #region Private Fields

        // 현재 상태 추적
        private TMP_InputField currentInputField;  // 현재 포커스된 InputField
        private Canvas targetCanvas;                // 이동 대상이 속한 Canvas
        private RectTransform targetRectTransform;  // 실제로 이동시킬 RectTransform (Canvas의 직속 자식)
        private Vector2 originalPosition;           // 이동 전 원래 위치 (anchoredPosition)
        private float lastKeyboardHeight;           // 마지막으로 감지된 키보드 높이 (픽셀)
        private bool isMoved;                       // UI 이동 여부

        // InputField 관리
        private List<TMP_InputField> registeredInputFields = new List<TMP_InputField>();

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // Scene 전환 이벤트 구독
            SceneManager.sceneLoaded += OnSceneLoaded;

#if UNITY_ANDROID || UNITY_IOS
            // UMI 키보드 이벤트 구독 (모바일 플랫폼만)
            MobileInput.OnKeyboardAction += OnKeyboardHeightChanged;
#endif
        }

        protected override void OnDestroy()
        {
            // Scene 전환 이벤트 구독 해제
            SceneManager.sceneLoaded -= OnSceneLoaded;

#if UNITY_ANDROID || UNITY_IOS
            // UMI 키보드 이벤트 구독 해제
            MobileInput.OnKeyboardAction -= OnKeyboardHeightChanged;
#endif

            // 등록된 모든 InputField 이벤트 해제
            UnregisterAllInputFields();

            // 부모 클래스 정리 (Singleton instance 초기화)
            base.OnDestroy();
        }

        private void Update()
        {
            // 현재 포커스된 InputField가 있는 경우
            if (currentInputField != null)
            {
                // InputField가 비활성화되거나 파괴된 경우 처리
                if (currentInputField == null || !currentInputField.gameObject.activeInHierarchy)
                {
                    OnInputFieldDeselected(currentInputField);
                    return;
                }

                // 에디터 환경에서는 isFocused 플래그로 포커스 해제 감지
                // (모바일에서는 UMI 이벤트로 처리)
#if !UNITY_ANDROID && !UNITY_IOS
                if (!currentInputField.isFocused)
                {
                    OnInputFieldDeselected(currentInputField);
                }
#endif
            }
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Scene 로드 완료 시 호출
        /// 이전 Scene의 UI 상태를 정리하고 새 Scene의 InputField들을 등록
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 이전 Scene에서 이동된 UI가 있다면 즉시 복원
            if (isMoved && targetCanvas != null)
            {
                RestoreCanvasImmediate();
            }

            // 상태 초기화
            currentInputField = null;
            targetCanvas = null;
            targetRectTransform = null;
            isMoved = false;
            lastKeyboardHeight = 0f;

            // 이전 Scene의 InputField 이벤트 해제
            UnregisterAllInputFields();

            // 새 Scene의 InputField 등록 (다음 프레임에 실행하여 모든 오브젝트 로드 보장)
            StartCoroutine(RegisterAllInputFieldsNextFrame());
        }

        /// <summary>
        /// 다음 프레임에 InputField 등록
        /// Scene의 모든 오브젝트가 완전히 로드된 후 실행되도록 보장
        /// </summary>
        private IEnumerator RegisterAllInputFieldsNextFrame()
        {
            yield return null;
            RegisterAllInputFields();
        }

        #endregion

        #region InputField Registration

        /// <summary>
        /// Scene의 모든 InputField를 찾아서 이벤트 등록
        /// 비활성화된 InputField도 포함하여 런타임에 활성화되어도 정상 작동
        /// </summary>
        private void RegisterAllInputFields()
        {
            // 활성/비활성 상관없이 모든 TMP_InputField 탐색
            TMP_InputField[] inputFields = FindObjectsByType<TMP_InputField>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (var inputField in inputFields)
            {
                RegisterInputField(inputField);
            }
        }

        /// <summary>
        /// 런타임에 생성된 InputField를 수동으로 등록
        /// 팝업이나 동적으로 생성되는 UI에서 사용
        /// </summary>
        /// <param name="inputField">등록할 InputField</param>
        public void RegisterInputFieldRuntime(TMP_InputField inputField)
        {
            RegisterInputField(inputField);
        }

        /// <summary>
        /// 개별 InputField에 Select/Deselect 이벤트 등록
        /// </summary>
        private void RegisterInputField(TMP_InputField inputField)
        {
            // null 체크 및 중복 등록 방지
            if (inputField == null || registeredInputFields.Contains(inputField))
                return;

            // InputField 선택/해제 이벤트 리스너 등록
            inputField.onSelect.AddListener((eventData) => OnInputFieldSelected(inputField));
            inputField.onDeselect.AddListener((eventData) => OnInputFieldDeselected(inputField));

            registeredInputFields.Add(inputField);
        }

        /// <summary>
        /// 등록된 모든 InputField의 이벤트 리스너 제거
        /// Scene 전환 시 메모리 누수 방지
        /// </summary>
        private void UnregisterAllInputFields()
        {
            foreach (var inputField in registeredInputFields)
            {
                if (inputField != null)
                {
                    inputField.onSelect.RemoveAllListeners();
                    inputField.onDeselect.RemoveAllListeners();
                }
            }

            registeredInputFields.Clear();
        }

        #endregion

        #region InputField Events

        /// <summary>
        /// InputField 선택 시 호출
        /// Canvas 직속 자식 오브젝트를 찾아서 이동 타겟으로 설정
        /// </summary>
        private void OnInputFieldSelected(TMP_InputField inputField)
        {
            if (inputField == null)
                return;

            currentInputField = inputField;

            // InputField가 속한 Canvas 탐색
            targetCanvas = inputField.GetComponentInParent<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogWarning($"[KeyboardManager] InputField '{inputField.name}'의 Canvas를 찾을 수 없습니다!");
                return;
            }

            // Canvas의 직속 자식 중 InputField의 부모 컨테이너 찾기
            // (Canvas 전체를 움직이면 너무 커서 효과가 없으므로 직속 자식만 이동)
            targetRectTransform = FindCanvasDirectChild(inputField.transform, targetCanvas.transform);
            if (targetRectTransform == null)
            {
                Debug.LogWarning($"[KeyboardManager] InputField '{inputField.name}'의 Canvas 직속 부모를 찾을 수 없습니다!");
                return;
            }

            // 핵심: isMoved가 false일 때만 원래 위치 저장
            // (이미 이동된 상태면 최초 원래 위치를 유지해야 함)
            if (!isMoved)
            {
                originalPosition = targetRectTransform.anchoredPosition;
            }

            // 키보드가 이미 떠있으면 즉시 위치 재계산
            // (다른 InputField에서 전환된 경우)
            if (lastKeyboardHeight > 0)
            {
                MoveCanvasUp();
            }
        }

        /// <summary>
        /// InputField에서 Canvas까지 계층을 역추적하여 Canvas의 직속 자식 찾기
        ///
        /// 예시:
        /// Canvas
        ///  └─ Background (← 이것을 반환)
        ///      └─ Panel
        ///          └─ InputField
        /// </summary>
        /// <param name="inputFieldTransform">InputField의 Transform</param>
        /// <param name="canvasTransform">Canvas의 Transform</param>
        /// <returns>Canvas의 직속 자식 RectTransform, 없으면 null</returns>
        private RectTransform FindCanvasDirectChild(Transform inputFieldTransform, Transform canvasTransform)
        {
            Transform current = inputFieldTransform;

            // InputField에서 부모 방향으로 탐색
            while (current != null && current.parent != null)
            {
                // 현재 오브젝트의 부모가 Canvas라면, 현재 오브젝트가 Canvas의 직속 자식
                if (current.parent == canvasTransform)
                {
                    return current.GetComponent<RectTransform>();
                }

                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// InputField 포커스 해제 시 호출
        /// 다른 InputField로 전환되는지 체크 후 UI 복원 결정
        /// </summary>
        private void OnInputFieldDeselected(TMP_InputField inputField)
        {
            // 현재 포커스된 InputField가 아니면 무시
            if (inputField != currentInputField)
                return;

            currentInputField = null;

            // 즉시 UI 복원하지 않고, 다음 프레임에 다른 InputField로 전환되는지 확인
            StartCoroutine(CheckIfShouldRestoreUI());
        }

        /// <summary>
        /// 다음 프레임에 UI 복원이 필요한지 체크
        /// - currentInputField가 null이면: 완전히 포커스 해제 → UI 복원
        /// - currentInputField가 있으면: 다른 InputField로 전환됨 → UI 유지
        /// </summary>
        private IEnumerator CheckIfShouldRestoreUI()
        {
            yield return null; // 1프레임 대기

            // 1프레임 후에도 currentInputField가 null이면
            // → 다른 InputField로 전환되지 않음 → UI 복원 필요
            if (currentInputField == null)
            {
                RestoreCanvas();
            }
            // currentInputField가 null이 아니면
            // → 다른 InputField로 전환됨 → OnInputFieldSelected가 이미 호출됨
            // → 그 InputField 기준으로 위치 재계산됨
        }

        #endregion

        #region UMI Keyboard Events

        /// <summary>
        /// UMI에서 키보드 높이 변화 이벤트 수신
        ///
        /// 키보드 표시:
        /// - 현재 포커스된 InputField가 있다면 UI 이동 시도
        ///
        /// 키보드 숨김:
        /// - 키보드 높이 초기화
        /// - 이동된 UI 복원은 InputField deselect 이벤트에서 자동 처리됨
        /// </summary>
        /// <param name="isShow">키보드 표시 여부</param>
        /// <param name="height">키보드 높이 (픽셀 단위)</param>
        private void OnKeyboardHeightChanged(bool isShow, int height)
        {
            if (isShow && height > 0)
            {
                // 키보드 표시됨
                lastKeyboardHeight = height;

                if (currentInputField != null)
                {
                    MoveCanvasUp();
                }
            }
            else
            {
                // 키보드 숨겨짐
                lastKeyboardHeight = 0; // 키보드 높이 초기화

                // ❌ 제거: OnInputFieldDeselected 호출하지 않음
                // InputField의 onDeselect 이벤트가 자연스럽게 발생하므로
                // 여기서 중복 호출하면 문제 발생
                // (InputField 간 전환 시 키보드가 잠깐 내려갔다 올라가는 경우)

                // UI 복원은 OnInputFieldDeselected의 CheckIfShouldRestoreUI에서 처리됨
            }
        }

        #endregion

        #region Canvas Movement

        /// <summary>
        /// Canvas의 직속 자식을 위로 이동하여 InputField가 키보드에 가려지지 않도록 함
        ///
        /// 이동 로직:
        /// 1. InputField 하단 스크린 좌표 계산
        /// 2. 키보드 상단 스크린 좌표와 비교
        /// 3. InputField가 키보드에 가려진다면 필요한 이동 거리 계산
        /// 4. DOTween을 사용하여 부드럽게 위로 이동
        /// </summary>
        private void MoveCanvasUp()
        {
            if (targetCanvas == null || currentInputField == null || targetRectTransform == null)
                return;

            // 필요한 이동 거리 계산 (현재 위치 기준)
            float requiredOffset = CalculateRequiredOffset();

            // InputField가 키보드에 가려지지 않으면 이동 불필요
            if (requiredOffset <= 0f)
            {
                return;
            }

            // 이전 애니메이션이 진행 중이면 중단
            targetRectTransform.DOKill();

            // 목표 위치 계산
            // isMoved=true이면 현재 위치에서 추가 이동, false이면 원래 위치에서 이동
            Vector2 targetPosition = originalPosition + new Vector2(0f, requiredOffset);

            // 이동 플래그 즉시 설정 (애니메이션 중 키보드가 내려가도 복원 가능하도록)
            isMoved = true;

            // DOTween 애니메이션으로 부드럽게 이동
            targetRectTransform
                .DOAnchorPos(targetPosition, animationDuration)
                .SetEase(animationEase)
                .SetUpdate(true);  // Time.timeScale 영향 받지 않음 (일시정지 중에도 작동)
        }

        /// <summary>
        /// Canvas의 직속 자식을 원위치로 복원 (애니메이션 적용)
        /// 키보드가 숨겨질 때 호출
        /// </summary>
        private void RestoreCanvas()
        {
            if (targetCanvas == null || targetRectTransform == null || !isMoved)
                return;

            // 이전 애니메이션이 진행 중이면 중단
            targetRectTransform.DOKill();

            // DOTween 애니메이션으로 부드럽게 원위치로 복귀
            targetRectTransform
                .DOAnchorPos(originalPosition, animationDuration)
                .SetEase(animationEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // 애니메이션 완료 후 상태 초기화
                    isMoved = false;
                    targetCanvas = null;
                    targetRectTransform = null;
                });
        }

        /// <summary>
        /// Canvas의 직속 자식을 원위치로 즉시 복원 (애니메이션 없음)
        /// Scene 전환 시 호출되어 빠르게 정리
        /// </summary>
        private void RestoreCanvasImmediate()
        {
            if (targetCanvas == null || targetRectTransform == null || !isMoved)
                return;

            // 진행 중인 애니메이션 중단
            targetRectTransform.DOKill();

            // 즉시 원위치로 복원
            targetRectTransform.anchoredPosition = originalPosition;

            // 상태 초기화
            isMoved = false;
            targetCanvas = null;
            targetRectTransform = null;
        }

        /// <summary>
        /// InputField를 키보드 위로 이동시키기 위해 필요한 거리 계산
        ///
        /// 계산 과정:
        /// 1. UI를 원래 위치로 임시 복원하여 InputField의 원래 스크린 좌표 계산
        /// 2. InputField와 키보드의 스크린 좌표 비교
        /// 3. InputField 하단이 키보드보다 아래에 있으면 가려진 것으로 판단
        /// 4. InputField를 키보드 상단 + additionalOffset 위치로 이동시키기 위한 거리 계산
        /// 5. 스크린 픽셀 단위를 Canvas 좌표 단위로 변환하여 반환
        /// </summary>
        /// <returns>Canvas 좌표 단위의 이동 거리 (가려지지 않으면 0 이하)</returns>
        private float CalculateRequiredOffset()
        {
            if (currentInputField == null || targetCanvas == null || targetRectTransform == null)
                return 0f;

            RectTransform inputRect = currentInputField.GetComponent<RectTransform>();

            // 현재 UI 위치 저장
            Vector2 currentUIPosition = targetRectTransform.anchoredPosition;

            // InputField의 원래 위치를 계산하기 위해 UI를 임시로 원위치로 이동
            targetRectTransform.anchoredPosition = originalPosition;

            // InputField의 월드 좌표 모서리 가져오기 (원래 위치 기준)
            Vector3[] inputCorners = new Vector3[4];
            inputRect.GetWorldCorners(inputCorners);

            // UI를 원래 위치로 복원
            targetRectTransform.anchoredPosition = currentUIPosition;

            // 월드 좌표를 스크린 픽셀 좌표로 변환
            // ScreenSpaceOverlay 모드면 카메라 null, ScreenSpaceCamera 모드면 worldCamera 사용
            Camera cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : targetCanvas.worldCamera;

            // InputField 하단(왼쪽 아래 모서리)의 스크린 Y 좌표 (원래 위치 기준)
            Vector2 inputBottomScreen = RectTransformUtility.WorldToScreenPoint(cam, inputCorners[0]);
            float inputBottomY = inputBottomScreen.y;

            // 키보드 상단의 스크린 Y 좌표
            float keyboardTopY = lastKeyboardHeight;

            // InputField가 키보드보다 위에 있으면 이동 불필요
            if (inputBottomY > keyboardTopY)
            {
                return 0f;
            }

            // 목표 위치: 키보드 상단 + 여유 공간(additionalOffset)
            float targetInputBottomY = keyboardTopY + additionalOffset;

            // 필요한 이동 거리 (스크린 픽셀 단위)
            float offsetInPixels = targetInputBottomY - inputBottomY;

            // 스크린 픽셀을 Canvas 좌표 단위로 변환
            // Canvas의 scaleFactor는 디바이스 해상도에 따라 달라짐
            float canvasScaleFactor = targetCanvas.scaleFactor;
            float offsetInCanvasUnits = offsetInPixels / canvasScaleFactor;

            return offsetInCanvasUnits;
        }

        #endregion
    }
}
