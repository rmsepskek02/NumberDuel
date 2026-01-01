using UnityEngine;
using UnityEngine.InputSystem;
using Objects;
using Manager;

namespace Testing
{
    /// <summary>
    /// New Input System을 사용한 덱 시스템 테스트 스크립트
    /// UI 없이 빠른 바로 테스트 가능
    /// </summary>
    public class SimpleDeckTest : MonoBehaviour
    {
        [Header("Card Zones")]
        [SerializeField] private CardZone playerHandZone;
        [SerializeField] private CardZone opponentHandZone;

        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;

        // Input Actions
        private InputAction initDeckAction;
        private InputAction drawPlayerAction;
        private InputAction drawOpponentAction;
        private InputAction drawMultipleAction;
        private InputAction resetDeckAction;
        private InputAction printStatusAction;
        private InputAction autoTestAction;

        private bool isInitialized = false;

        #region Unity Lifecycle
        private void Awake()
        {
            SetupInputActions();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Start()
        {
        }
        #endregion

        #region Input Setup
        /// <summary>
        /// Input Actions 설정
        /// </summary>
        private void SetupInputActions()
        {
            // InputActionAsset이 없으면 자동로 액션 생성
            if (inputActions == null)
            {
                CreateInputActionsInCode();
            }
            else
            {
                GetInputActionsFromAsset();
            }

            // 이벤트 바인딩
            ConnectInputEvents();
        }

        /// <summary>
        /// 자동로 생성 Input Actions 생성 (InputActionAsset 없을 때)
        /// </summary>
        private void CreateInputActionsInCode()
        {
            initDeckAction = new InputAction("InitDeck", binding: "<Keyboard>/i");
            drawPlayerAction = new InputAction("DrawPlayer", binding: "<Keyboard>/p");
            drawOpponentAction = new InputAction("DrawOpponent", binding: "<Keyboard>/o");
            drawMultipleAction = new InputAction("DrawMultiple", binding: "<Keyboard>/m");
            resetDeckAction = new InputAction("ResetDeck", binding: "<Keyboard>/r");
            printStatusAction = new InputAction("PrintStatus", binding: "<Keyboard>/s");
            autoTestAction = new InputAction("AutoTest", binding: "<Keyboard>/t");
        }

        /// <summary>
        /// InputActionAsset에서 Actions 가져오기
        /// </summary>
        private void GetInputActionsFromAsset()
        {
            initDeckAction = inputActions.FindAction("InitDeck");
            drawPlayerAction = inputActions.FindAction("DrawPlayer");
            drawOpponentAction = inputActions.FindAction("DrawOpponent");
            drawMultipleAction = inputActions.FindAction("DrawMultiple");
            resetDeckAction = inputActions.FindAction("ResetDeck");
            printStatusAction = inputActions.FindAction("PrintStatus");
            autoTestAction = inputActions.FindAction("AutoTest");
        }

        /// <summary>
        /// Input 이벤트 바인딩
        /// </summary>
        private void ConnectInputEvents()
        {
            if (initDeckAction != null)
                initDeckAction.performed += OnInitDeck;

            if (drawPlayerAction != null)
                drawPlayerAction.performed += OnDrawPlayer;

            if (drawOpponentAction != null)
                drawOpponentAction.performed += OnDrawOpponent;

            if (drawMultipleAction != null)
                drawMultipleAction.performed += OnDrawMultiple;

            if (resetDeckAction != null)
                resetDeckAction.performed += OnResetDeck;

            if (printStatusAction != null)
                printStatusAction.performed += OnPrintStatus;

            if (autoTestAction != null)
                autoTestAction.performed += OnAutoTest;
        }

        /// <summary>
        /// Input Actions 활성화
        /// </summary>
        private void EnableInputActions()
        {
            initDeckAction?.Enable();
            drawPlayerAction?.Enable();
            drawOpponentAction?.Enable();
            drawMultipleAction?.Enable();
            resetDeckAction?.Enable();
            printStatusAction?.Enable();
            autoTestAction?.Enable();
        }

        /// <summary>
        /// Input Actions 비활성화
        /// </summary>
        private void DisableInputActions()
        {
            initDeckAction?.Disable();
            drawPlayerAction?.Disable();
            drawOpponentAction?.Disable();
            drawMultipleAction?.Disable();
            resetDeckAction?.Disable();
            printStatusAction?.Disable();
            autoTestAction?.Disable();
        }
        #endregion

        #region Input Event Handlers
        private void OnInitDeck(InputAction.CallbackContext context)
        {
            InitializeDeck();
        }

        private void OnDrawPlayer(InputAction.CallbackContext context)
        {
            if (isInitialized)
                DrawPlayerCard();
        }

        private void OnDrawOpponent(InputAction.CallbackContext context)
        {
            if (isInitialized)
                DrawOpponentCard();
        }

        private void OnDrawMultiple(InputAction.CallbackContext context)
        {
            if (isInitialized)
                DrawMultipleCards(5);
        }

        private void OnResetDeck(InputAction.CallbackContext context)
        {
            ResetDeck();
        }

        private void OnPrintStatus(InputAction.CallbackContext context)
        {
            PrintDeckStatus();
        }

        private void OnAutoTest(InputAction.CallbackContext context)
        {
            RunAutoTest();
        }
        #endregion

        #region Deck Test Methods
        /// <summary>
        /// 덱 초기화
        /// </summary>
        private void InitializeDeck()
        {
            if (DeckManager.Instance == null)
            {
                Debug.LogError("DeckManager.Instance가 null입니다!");
                return;
            }

            try
            {
                DeckManager.Instance.InitializeDecks();
                isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"덱 초기화 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 카드 드로우
        /// </summary>
        private void DrawPlayerCard()
        {
            try
            {
                if (InGameManager.Instance != null)
                {
                    // InGameManager를 통한 드로우 (권장)
                    InGameManager.Instance.DrawCardsToHand(1, CardZone.OwnerType.Player);
                }
                else
                {
                    // 직접 DeckManager 사용
                    var cardData = DeckManager.Instance.DrawPlayerCard();
                    if (cardData.HasValue)
                    {
                        // 카드 오브젝트 생성
                        var cardObj = DeckManager.Instance.CreateCardObject(
                            cardData.Value,
                            CardZone.OwnerType.Player,
                            playerHandZone
                        );
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"드로우 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 상대 카드 드로우
        /// </summary>
        private void DrawOpponentCard()
        {
            try
            {
                if (InGameManager.Instance != null)
                {
                    InGameManager.Instance.DrawCardsToHand(1, CardZone.OwnerType.Opponent);
                }
                else
                {
                    var cardData = DeckManager.Instance.DrawOpponentCard();
                    if (cardData.HasValue)
                    {
                        var cardObj = DeckManager.Instance.CreateCardObject(
                            cardData.Value,
                            CardZone.OwnerType.Opponent,
                            opponentHandZone
                        );
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"상대 드로우 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 여러 장 드로우
        /// </summary>
        private void DrawMultipleCards(int count)
        {
            try
            {
                if (InGameManager.Instance != null)
                {
                    InGameManager.Instance.DrawCardsToHand(count, CardZone.OwnerType.Player);
                }
                else
                {
                    var cards = DeckManager.Instance.DrawPlayerCards(count);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"다중 드로우 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 덱 리셋
        /// </summary>
        private void ResetDeck()
        {
            try
            {
                if (DeckManager.Instance != null)
                {
                    DeckManager.Instance.ResetDecks();
                    isInitialized = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"덱 리셋 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 덱 상태 출력 (디버그 콘솔 출력)
        /// </summary>
        private void PrintDeckStatus()
        {
            if (DeckManager.Instance != null && isInitialized)
            {
                // Status information removed for production
            }
        }

        // 손패 제한 테스트를 위한 메서드 추가
        [ContextMenu("손패 제한 테스트")]
        public void TestHandLimit()
        {
            if (!isInitialized)
            {
                return;
            }

            // 플레이어 손패에 12장 드로우 시도 (2장은 파괴되어야 함)
            for (int i = 0; i < 12; i++)
            {
                DrawPlayerCard();
            }

            PrintDeckStatus();
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// 카드 정보 문자열 반환
        /// </summary>
        private string GetCardDescription(CardData cardData)
        {
            return cardData.cardType switch
            {
                CardType.Number => $"숫자({cardData.numberValue})",
                CardType.Operator => $"연산자({cardData.operatorType})",
                CardType.Joker => "조커",
                _ => "알 수 없음"
            };
        }

        /// <summary>
        /// 자동 테스트 (T키로 실행)
        /// </summary>
        [ContextMenu("자동 테스트 실행")]
        public void RunAutoTest()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(AutoTestRoutine());
            }
        }

        private System.Collections.IEnumerator AutoTestRoutine()
        {
            // 1. 덱 초기화
            InitializeDeck();
            yield return new WaitForSeconds(1f);

            // 2. 플레이어 카드 3장 드로우
            for (int i = 0; i < 3; i++)
            {
                DrawPlayerCard();
                yield return new WaitForSeconds(0.5f);
            }

            // 3. 상대 카드 3장 드로우
            for (int i = 0; i < 3; i++)
            {
                DrawOpponentCard();
                yield return new WaitForSeconds(0.5f);
            }

            // 4. 다중 드로우 테스트
            DrawMultipleCards(3);
            yield return new WaitForSeconds(1f);

            // 5. 상태 출력
            PrintDeckStatus();
        }

        /// <summary>
        /// 덱 소진 상황 테스트 (심화편)
        /// </summary>
        [ContextMenu("덱 소진 상황 테스트")]
        public void TestDeckDepletion()
        {
            if (!isInitialized)
            {
                return;
            }

            int drawCount = 0;
            while (!DeckManager.Instance.IsPlayerDeckEmpty && drawCount < 35) // 안전장치
            {
                DrawPlayerCard();
                drawCount++;

                if (drawCount % 5 == 0) // 5장마다 상태 출력
                {
                    PrintDeckStatus();
                }
            }
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            DisableInputActions();

            // 이벤트 바인딩 해제
            if (initDeckAction != null)
                initDeckAction.performed -= OnInitDeck;
            if (drawPlayerAction != null)
                drawPlayerAction.performed -= OnDrawPlayer;
            if (drawOpponentAction != null)
                drawOpponentAction.performed -= OnDrawOpponent;
            if (drawMultipleAction != null)
                drawMultipleAction.performed -= OnDrawMultiple;
            if (resetDeckAction != null)
                resetDeckAction.performed -= OnResetDeck;
            if (printStatusAction != null)
                printStatusAction.performed -= OnPrintStatus;
            if (autoTestAction != null)
                autoTestAction.performed -= OnAutoTest;
        }
        #endregion
    }
}
