using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utills;

namespace UI
{
    /// <summary>
    /// UI 스택 관리자
    /// - ESC 키를 통해 가장 최근에 열린 UI를 닫습니다 (LIFO)
    /// - 모든 ICloseable UI 요소를 스택으로 관리
    /// </summary>
    public class UIStackManager : SingletonDontDestroy<UIStackManager>
    {
        #region Fields and Properties
        private Stack<ICloseable> uiStack = new Stack<ICloseable>();

        /// <summary>
        /// 현재 스택에 UI가 있는지 확인
        /// </summary>
        public bool HasOpenUI => uiStack.Count > 0;

        /// <summary>
        /// 현재 스택의 UI 개수
        /// </summary>
        public int UICount => uiStack.Count;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            // ESC 키 감지 (New Input System)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscapeKey();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// UI를 스택에 추가
        /// </summary>
        /// <param name="ui">추가할 UI</param>
        public void Push(ICloseable ui)
        {
            if (ui == null)
                return;

            // 중복 Push 방지 (이미 스택에 있으면 무시)
            if (uiStack.Contains(ui))
                return;

            uiStack.Push(ui);
        }

        /// <summary>
        /// UI를 스택에서 제거
        /// </summary>
        public void Pop()
        {
            if (uiStack.Count > 0)
            {
                uiStack.Pop();
            }
        }

        /// <summary>
        /// 특정 UI를 스택에서 제거 (중간에 있어도 제거 가능)
        /// </summary>
        /// <param name="ui">제거할 UI</param>
        public void Remove(ICloseable ui)
        {
            if (ui == null || uiStack.Count == 0)
                return;

            // 스택을 임시 리스트로 변환하여 제거
            List<ICloseable> tempList = new List<ICloseable>(uiStack);
            if (tempList.Remove(ui))
            {
                uiStack.Clear();
                tempList.Reverse(); // 리스트를 역순으로
                foreach (var item in tempList)
                {
                    uiStack.Push(item);
                }
            }
        }

        /// <summary>
        /// 스택 초기화 (씬 전환 시 사용)
        /// </summary>
        public void Clear()
        {
            uiStack.Clear();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// ESC 키 입력 처리
        /// - 스택이 비어있으면: 설정 UI 열기
        /// - 스택에 UI가 있으면: 맨 위 UI 닫기
        /// </summary>
        private void HandleEscapeKey()
        {
            if (uiStack.Count == 0)
            {
                // 스택이 비어있으면 → 설정 UI 열기
                Manager.SettingsManager.Instance?.ShowSettings();
                return;
            }

            // 스택의 맨 위 UI 가져오기
            ICloseable topUI = uiStack.Peek();

            // 닫을 수 있는지 확인
            if (topUI.CanClose())
            {
                topUI.Close(); // Close 내부에서 Pop() 호출됨
            }
        }
        #endregion
    }
}
