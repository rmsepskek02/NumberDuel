using System;
using DG.Tweening;
using Manager;
using Objects;
using UnityEngine;

namespace UI.Shared
{
    /// <summary>
    /// 팝업 UI 기본 클래스
    /// 공통 기능: Static 인스턴스 관리, 애니메이션, UIStack, 사운드
    /// </summary>
    /// <typeparam name="T">자식 클래스 타입</typeparam>
    public abstract class PopupBase<T> : MonoBehaviour, ICloseable where T : PopupBase<T>
    {
        #region Static Instance Management

        protected static T instance;
        protected static GameObject popupObject;

        /// <summary>
        /// Prefab 경로 (자동 생성: "Prefabs/UI/{클래스명에서 UI 제거}")
        /// </summary>
        protected static string PrefabPath => $"Prefabs/UI/{typeof(T).Name.Replace("UI", "")}";

        /// <summary>
        /// Prefab 로드 및 인스턴스 생성
        /// </summary>
        protected static bool LoadPopup()
        {
            if (instance != null)
                return true;

            var prefab = Resources.Load<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[{typeof(T).Name}] Prefab not found at: {PrefabPath}");
                return false;
            }

            popupObject = Instantiate(prefab);
            popupObject.name = typeof(T).Name.Replace("UI", "");
            DontDestroyOnLoad(popupObject);

            instance = popupObject.GetComponent<T>();
            if (instance == null)
            {
                Debug.LogError($"[{typeof(T).Name}] Component not found on prefab");
                Destroy(popupObject);
                popupObject = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 팝업 Preload (최초 호출 시 딜레이 방지)
        /// </summary>
        public static void Preload()
        {
            if (instance == null)
                LoadPopup();
        }

        /// <summary>
        /// 팝업 인스턴스 및 GameObject 삭제
        /// </summary>
        public static void Destroy()
        {
            if (popupObject != null)
            {
                UnityEngine.Object.Destroy(popupObject);
                popupObject = null;
                instance = null;
            }
        }

        /// <summary>
        /// 팝업 숨기기 (Static)
        /// </summary>
        public static void Hide()
        {
            instance?.HideInternal();
        }

        #endregion

        #region Serialized Fields

        [Header("Animation Settings")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected RectTransform popupRect;
        [SerializeField] protected float showDuration = 0.2f;
        [SerializeField] protected float hideDuration = 0.15f;

        #endregion

        #region Protected Fields

        protected bool isAnimating = false;
        protected Tween showTween;
        protected Tween hideTween;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // 초기 상태: 숨김
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            // Tween 정리
            showTween?.Kill();
            hideTween?.Kill();
        }

        #endregion

        #region ICloseable Implementation

        /// <summary>
        /// ICloseable 구현: 팝업 닫기
        /// UIStackManager의 ESC 키 처리에서 호출됨
        /// </summary>
        public virtual void Close()
        {
            HideInternal();
        }

        /// <summary>
        /// ICloseable 구현: 닫을 수 있는 상태인지 확인
        /// </summary>
        public virtual bool CanClose()
        {
            return !isAnimating;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 팝업 숨기기 (Instance)
        /// </summary>
        protected virtual void HideInternal()
        {
            // 이전 Tween 정리
            showTween?.Kill();

            // UIStackManager에서 제거
            UnregisterFromUIStack();

            // 애니메이션
            PlayHideAnimation();

            // 사운드 재생
            PlayClickSound();
        }

        /// <summary>
        /// UIStackManager에 등록
        /// </summary>
        protected void RegisterToUIStack()
        {
            UIStackManager.Instance?.Push(this);
        }

        /// <summary>
        /// UIStackManager에서 제거
        /// </summary>
        protected void UnregisterFromUIStack()
        {
            UIStackManager.Instance?.Pop();
        }

        /// <summary>
        /// 클릭 사운드 재생
        /// </summary>
        protected void PlayClickSound()
        {
            SoundManager.Instance?.PlaySFX(SoundType.UI_ButtonClick);
        }

        /// <summary>
        /// KeyboardManager에 InputField 등록 (모바일 키보드 대응)
        /// </summary>
        protected void RegisterInputFieldToKeyboard(TMPro.TMP_InputField inputField)
        {
            if (inputField == null)
                return;

#if UNITY_ANDROID || UNITY_IOS
            var keyboardManager = KeyboardManager.Instance;
            if (keyboardManager != null)
            {
                keyboardManager.RegisterInputFieldRuntime(inputField);
            }
#endif
        }

        #endregion

        #region Animations

        /// <summary>
        /// 표시 애니메이션 (Scale 0.9 → 1.0 + Fade In)
        /// </summary>
        protected void PlayShowAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                return;
            }

            // 이전 Tween 정리
            hideTween?.Kill();

            // 애니메이션 시작
            isAnimating = true;

            // 초기 상태
            canvasGroup.alpha = 0f;
            popupRect.localScale = Vector3.one * 0.9f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence showSequence = DOTween.Sequence();

            // Fade In
            showSequence.Append(canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutQuad));

            // Scale Up
            showSequence.Join(popupRect.DOScale(1f, showDuration).SetEase(Ease.OutBack));

            // 완료 시 상호작용 활성화
            showSequence.OnComplete(() =>
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                isAnimating = false;
            });

            showTween = showSequence;
        }

        /// <summary>
        /// 숨김 애니메이션 (Scale 1.0 → 0.9 + Fade Out)
        /// </summary>
        protected void PlayHideAnimation()
        {
            if (canvasGroup == null || popupRect == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // 애니메이션 시작
            isAnimating = true;

            // 상호작용 비활성화
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Tween 시퀀스
            Sequence hideSequence = DOTween.Sequence();

            // Fade Out
            hideSequence.Append(canvasGroup.DOFade(0f, hideDuration).SetEase(Ease.InQuad));

            // Scale Down
            hideSequence.Join(popupRect.DOScale(0.9f, hideDuration).SetEase(Ease.InBack));

            // 완료 시 비활성화
            hideSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                isAnimating = false;
            });

            hideTween = hideSequence;
        }

        #endregion
    }
}
