using NUnit.Framework;
using Objects;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 게임의 Resource를 관리하는 매니저
    /// 기본 초기화는 즉시 실행, 네트워크 색상 동기화는 별도 처리
    /// </summary>
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        [SerializeField]
        private SerializableDictionary<string, GameObject> prefabCache = new SerializableDictionary<string, GameObject>();

        [SerializeField]
        private SerializableDictionary<string, Sprite> spriteCache = new SerializableDictionary<string, Sprite>();

        private Sprite playerSprite;
        private Sprite opponentSprite;
        private GameObject playerCardTemplate;
        private GameObject opponentCardTemplate;

        /// <summary>
        /// 색상 동기화 완료 여부 (NetworkGameManager에서 설정)
        /// </summary>
        private bool isColorSynchronized = false;

        /// <summary>
        /// 기본 초기화 완료 여부
        /// </summary>
        private bool isBasicInitialized = false;

        /// <summary>
        /// 외부에서 Player용 참조
        /// </summary>
        public GameObject GetPlayerCardTemplate() => playerCardTemplate;
        public Sprite GetPlayerSprite() => playerSprite;

        /// <summary>
        /// 외부에서 Opponent용 참조
        /// </summary>
        public GameObject GetOpponentCardTemplate() => opponentCardTemplate;
        public Sprite GetOpponentSprite() => opponentSprite;

        /// <summary>
        /// 색상 동기화 완료 여부 확인
        /// </summary>
        public bool IsColorSynchronized => isColorSynchronized;

        /// <summary>
        /// 기본 초기화 완료 여부 확인
        /// </summary>
        public bool IsBasicInitialized => isBasicInitialized;

        protected override void Awake()
        {
            base.Awake();

            // 기본 리소스 로딩 (즉시 실행)
            LoadAllPrefabs(Global.Card);
            LoadAllSprites(Global.Card);
            LoadAllSprites(Global.Joker);

            // 기본 카드 템플릿 생성 (색상은 나중에 설정)
            CreateBasicCardTemplates();

            isBasicInitialized = true;
            Debug.Log("[ResourcesManager] 기본 초기화 완료");
        }

        private void OnDestroy()
        {
            ClearCache();
        }

        #region Basic Initialization (즉시 실행)
        /// <summary>
        /// 기본 카드 템플릿 생성
        /// 색상은 기본값으로 설정하고, 나중에 네트워크 동기화로 변경
        /// </summary>
        private void CreateBasicCardTemplates()
        {
            // 기존 템플릿 정리
            if (playerCardTemplate != null)
                DestroyImmediate(playerCardTemplate);
            if (opponentCardTemplate != null)
                DestroyImmediate(opponentCardTemplate);

            // 프리팹 가져오기
            var playerPrefab = GetPrefab(Global.Card, "Player_Card");
            var opponentPrefab = GetPrefab(Global.Card, "Opponent_Card");

            if (playerPrefab == null || opponentPrefab == null)
            {
                Debug.LogError("[ResourcesManager] Player_Card 또는 Opponent_Card 프리팹을 찾을 수 없습니다.");
                return;
            }

            // 템플릿 인스턴스 생성
            playerCardTemplate = Instantiate(playerPrefab);
            opponentCardTemplate = Instantiate(opponentPrefab);

            // 이름 설정
            playerCardTemplate.name = "PlayerCardTemplate";
            opponentCardTemplate.name = "OpponentCardTemplate";

            // 기본 스프라이트 설정 (첫 번째 사용 가능한 스프라이트)
            SetDefaultSprites();

            // 템플릿은 비활성화 상태로 유지
            playerCardTemplate.SetActive(false);
            opponentCardTemplate.SetActive(false);
        }

        /// <summary>
        /// 기본 스프라이트 설정 (방장인 경우 이미 설정됨, 아닌 경우 방 속성에서 읽기)
        /// </summary>
        private void SetDefaultSprites()
        {
            List<Sprite> availableSprites = GetAvailableColorSprites();

            if (availableSprites.Count >= 2)
            {
                // 방장이 아닌 경우 방 속성에서 색상 읽기 시도
                if (!Photon.Pun.PhotonNetwork.IsMasterClient && Photon.Pun.PhotonNetwork.InRoom)
                {
                    var roomProperties = Photon.Pun.PhotonNetwork.CurrentRoom.CustomProperties;

                    if (roomProperties.TryGetValue("masterPlayerColor", out object masterColor) &&
                        roomProperties.TryGetValue("guestPlayerColor", out object guestColor))
                    {
                        string masterColorName = masterColor.ToString();
                        string guestColorName = guestColor.ToString();

                        // 비방장 관점에서 색상 반대 적용
                        SetPlayerColors(guestColorName, masterColorName);
                        Debug.Log($"[ResourcesManager] 비방장 - 방 속성 색상 적용: Player={guestColorName}, Opponent={masterColorName}");
                        return;
                    }
                    else
                    {
                        Debug.Log("[ResourcesManager] 비방장 - 방 속성에 색상 없음, 기본 색상 사용");
                    }
                }

                // 방장이거나 방 속성에 색상이 없는 경우 기본 색상 사용
                playerSprite = availableSprites[0];
                opponentSprite = availableSprites[1];

                ApplyVisual(playerCardTemplate, playerSprite);
                ApplyVisual(opponentCardTemplate, opponentSprite);

                string role = Photon.Pun.PhotonNetwork.IsMasterClient ? "방장" : "비방장";
                Debug.Log($"[ResourcesManager] {role} - 기본 색상 사용: Player={playerSprite.name}, Opponent={opponentSprite.name}");
            }
            else
            {
                Debug.LogError("[ResourcesManager] 사용 가능한 스프라이트가 부족합니다.");
            }
        }
        #endregion

        #region Network Color Synchronization (별도 실행)
        /// <summary>
        /// 네트워크 동기화된 플레이어 색상 설정
        /// NetworkGameManager에서 호출하여 모든 클라이언트가 동일한 색상 사용
        /// </summary>
        /// <param name="playerSpriteName">플레이어 카드 스프라이트 이름</param>
        /// <param name="opponentSpriteName">상대방 카드 스프라이트 이름</param>
        public void SetPlayerColors(string playerSpriteName, string opponentSpriteName)
        {
            if (!isBasicInitialized)
            {
                Debug.LogWarning("[ResourcesManager] 기본 초기화가 완료되지 않았습니다.");
                return;
            }

            Debug.Log($"[ResourcesManager] 색상 동기화 적용 시작: Player={playerSpriteName}, Opponent={opponentSpriteName}");

            // 스프라이트 이름으로 실제 스프라이트 찾기
            if (spriteCache.TryGetValue(playerSpriteName, out Sprite foundPlayerSprite))
            {
                playerSprite = foundPlayerSprite;
            }
            else
            {
                Debug.LogError($"[ResourcesManager] Player 스프라이트를 찾을 수 없습니다: {playerSpriteName}");
                return;
            }

            if (spriteCache.TryGetValue(opponentSpriteName, out Sprite foundOpponentSprite))
            {
                opponentSprite = foundOpponentSprite;
            }
            else
            {
                Debug.LogError($"[ResourcesManager] Opponent 스프라이트를 찾을 수 없습니다: {opponentSpriteName}");
                return;
            }

            // 기존 템플릿에 새로운 색상 적용
            ApplyVisual(playerCardTemplate, playerSprite);
            ApplyVisual(opponentCardTemplate, opponentSprite);

            // 이미 생성된 DeckStacker들도 색상 업데이트
            UpdateExistingDeckStackers();

            // 색상 동기화 완료 표시
            isColorSynchronized = true;

            Debug.Log("[ResourcesManager] 네트워크 색상 동기화 완료");
        }

        /// <summary>
        /// 방장용: 랜덤 색상 선택 후 이름 반환
        /// NetworkGameManager에서 호출하여 RPC로 전송할 데이터 생성
        /// </summary>
        /// <returns>선택된 스프라이트 이름들 (player, opponent)</returns>
        public (string playerSpriteName, string opponentSpriteName) SelectRandomColors()
        {
            List<Sprite> availableSprites = GetAvailableColorSprites();

            if (availableSprites.Count < 2)
            {
                Debug.LogError("[ResourcesManager] 사용할 수 있는 색상 스프라이트가 2개 이상 필요합니다.");
                return ("", "");
            }

            // 랜덤 선택 (중복 없이)
            int index1 = UnityEngine.Random.Range(0, availableSprites.Count);
            int index2;
            do
            {
                index2 = UnityEngine.Random.Range(0, availableSprites.Count);
            } while (index2 == index1);

            string playerSpriteName = availableSprites[index1].name;
            string opponentSpriteName = availableSprites[index2].name;

            Debug.Log($"[ResourcesManager] 랜덤 색상 선택: Player={playerSpriteName}, Opponent={opponentSpriteName}");

            return (playerSpriteName, opponentSpriteName);
        }

        /// <summary>
        /// 사용 가능한 색상 스프라이트 목록 반환
        /// </summary>
        /// <returns>필터링된 스프라이트 목록</returns>
        private List<Sprite> GetAvailableColorSprites()
        {
            List<Sprite> availableSprites = new List<Sprite>();

            foreach (var kvp in spriteCache)
            {
                string spriteName = kvp.Key.ToLower();

                // 뒷면 카드가 아니고 빈 카드인 스프라이트만 선택
                if (!spriteName.Contains("color_back") && spriteName.Contains("empty"))
                {
                    availableSprites.Add(kvp.Value);
                }
            }

            Debug.Log($"[ResourcesManager] 사용 가능한 색상 스프라이트: {availableSprites.Count}개");
            return availableSprites;
        }
        #endregion

        #region Existing Objects Update
        /// <summary>
        /// 이미 생성된 DeckStacker들의 색상 업데이트
        /// 색상 동기화 시 기존에 생성된 덱들도 함께 업데이트
        /// </summary>
        private void UpdateExistingDeckStackers()
        {
            // 씬에 있는 모든 DeckStacker 찾기
            DeckStacker[] deckStackers = FindObjectsByType<DeckStacker>(FindObjectsSortMode.None);

            foreach (var stacker in deckStackers)
            {
                // 플레이어 덱인지 상대 덱인지 확인
                bool isPlayerDeck = stacker.IsMyDeck;
                Sprite targetSprite = isPlayerDeck ? playerSprite : opponentSprite;

                // DeckStacker의 시각적 요소 업데이트
                UpdateDeckStackerVisual(stacker, targetSprite);
            }

            Debug.Log("[ResourcesManager] 기존 DeckStacker들 색상 업데이트 완료");
        }

        /// <summary>
        /// 개별 DeckStacker의 시각적 요소 업데이트
        /// </summary>
        /// <param name="stacker">업데이트할 DeckStacker</param>
        /// <param name="sprite">적용할 스프라이트</param>
        private void UpdateDeckStackerVisual(DeckStacker stacker, Sprite sprite)
        {
            if (stacker == null || sprite == null) return;

            // DeckStacker의 자식 오브젝트들에서 SpriteRenderer 찾기
            SpriteRenderer[] spriteRenderers = stacker.GetComponentsInChildren<SpriteRenderer>();

            foreach (var spriteRenderer in spriteRenderers)
            {
                // 카드 관련 SpriteRenderer만 업데이트 (배경 등은 제외)
                if (spriteRenderer.gameObject.name.Contains("Card") ||
                    spriteRenderer.gameObject.name.Contains("Deck"))
                {
                    spriteRenderer.sprite = sprite;

                    // Material Property Block을 사용한 텍스처 설정
                    var propertyBlock = new MaterialPropertyBlock();
                    spriteRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetTexture("_MainTex", sprite.texture);
                    spriteRenderer.SetPropertyBlock(propertyBlock);

                    Debug.Log($"[ResourcesManager] DeckStacker 색상 업데이트: {spriteRenderer.gameObject.name} -> {sprite.name}");
                }
            }
        }
        #endregion

        #region Visual Application
        /// <summary>
        /// 카드 오브젝트에 시각적 설정 적용
        /// 스프라이트 및 텍스트 색상 설정
        /// </summary>
        /// <param name="card">설정할 카드 오브젝트</param>
        /// <param name="sprite">적용할 스프라이트</param>
        private void ApplyVisual(GameObject card, Sprite sprite)
        {
            if (card == null || sprite == null)
            {
                Debug.LogWarning("[ResourcesManager] ApplyVisual: card 또는 sprite가 null입니다.");
                return;
            }

            // SpriteRenderer에 스프라이트 적용
            var spriteRenderer = card.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;

                // Material Property Block을 사용한 텍스처 설정
                var propertyBlock = new MaterialPropertyBlock();
                spriteRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetTexture("_MainTex", sprite.texture);
                spriteRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                Debug.LogWarning($"[ResourcesManager] SpriteRenderer를 찾을 수 없습니다: {card.name}");
            }

            // 텍스트 색상 설정 (스프라이트 이름 기반)
            var textMeshPro = card.GetComponentInChildren<TMPro.TextMeshPro>();
            if (textMeshPro != null)
            {
                Color textColor = MatchColorFromSprite(sprite.name);
                textMeshPro.color = textColor;
            }
            else
            {
                Debug.LogWarning($"[ResourcesManager] TextMeshPro를 찾을 수 없습니다: {card.name}");
            }
        }

        /// <summary>
        /// 스프라이트 이름에 따라 적절한 텍스트 색상 결정
        /// </summary>
        /// <param name="spriteName">스프라이트 이름</param>
        /// <returns>매칭된 색상</returns>
        public Color MatchColorFromSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return Color.white;

            string lowerName = spriteName.ToLower();

            if (lowerName.Contains("red")) return Global.Red;
            if (lowerName.Contains("green")) return Global.Green;
            if (lowerName.Contains("yellow")) return Global.Yellow;
            if (lowerName.Contains("purple")) return Global.Purple;

            // 기본값: 흰색
            return Color.white;
        }
        #endregion

        #region Cache Management
        /// <summary>
        /// 모든 캐시 제거 (메모리 최적화)
        /// </summary>
        public void ClearCache()
        {
            prefabCache.Clear();
            spriteCache.Clear();

            // 템플릿 제거
            if (playerCardTemplate != null)
            {
                if (Application.isPlaying)
                    Destroy(playerCardTemplate);
                else
                    DestroyImmediate(playerCardTemplate);
                playerCardTemplate = null;
            }

            if (opponentCardTemplate != null)
            {
                if (Application.isPlaying)
                    Destroy(opponentCardTemplate);
                else
                    DestroyImmediate(opponentCardTemplate);
                opponentCardTemplate = null;
            }

            isColorSynchronized = false;
            isBasicInitialized = false;
        }
        #endregion

        #region Resource Loading
        /// <summary>
        /// 특정 폴더 내 모든 프리팹을 한 번에 로드
        /// </summary>
        /// <param name="path">로드할 폴더 경로</param>
        private void LoadAllPrefabs(string path)
        {
            try
            {
                GameObject[] prefabs = Resources.LoadAll<GameObject>($"Prefabs/{path}");

                foreach (GameObject prefab in prefabs)
                {
                    if (prefab != null)
                        prefabCache[prefab.name] = prefab;
                }

                if (prefabs.Length == 0)
                {
                    Debug.LogError($"[ResourcesManager] 프리팹을 찾을 수 없습니다: Prefabs/{path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ResourcesManager] 프리팹 로드 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 폴더 내 모든 Sprite를 한 번에 로드
        /// </summary>
        /// <param name="path">로드할 폴더 경로</param>
        private void LoadAllSprites(string path)
        {
            try
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>($"Image/{path}");

                foreach (Sprite sprite in sprites)
                {
                    if (sprite != null)
                        spriteCache[sprite.name] = sprite;
                }

                if (sprites.Length == 0)
                {
                    Debug.LogError($"[ResourcesManager] 스프라이트를 찾을 수 없습니다: Image/{path}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ResourcesManager] 스프라이트 로드 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Prefab을 로드하고 캐싱 (Resources 폴더 내에서 로드)
        /// </summary>
        /// <param name="path">폴더 경로</param>
        /// <param name="prefabName">프리팹 이름</param>
        /// <returns>로드된 프리팹 또는 null</returns>
        public GameObject GetPrefab(string path, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                Debug.LogError("[ResourcesManager] GetPrefab: prefabName이 비어있습니다.");
                return null;
            }

            if (!prefabCache.ContainsKey(prefabName))
            {
                try
                {
                    GameObject loadedPrefab = Resources.Load<GameObject>($"Prefabs/{path}/{prefabName}");
                    if (loadedPrefab != null)
                    {
                        prefabCache[prefabName] = loadedPrefab;
                    }
                    else
                    {
                        Debug.LogError($"[ResourcesManager] Prefab '{prefabName}'을 경로 'Prefabs/{path}/'에서 찾을 수 없습니다.");
                        return null;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ResourcesManager] 프리팹 로드 중 오류: {ex.Message}");
                    return null;
                }
            }
            return prefabCache[prefabName];
        }

        /// <summary>
        /// Sprite를 로드하고 캐싱 (Resources 폴더 내에서 로드)
        /// </summary>
        /// <param name="path">폴더 경로</param>
        /// <param name="spriteName">스프라이트 이름</param>
        /// <returns>로드된 스프라이트 또는 null</returns>
        public Sprite GetSprite(string path, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                Debug.LogError("[ResourcesManager] GetSprite: spriteName이 비어있습니다.");
                return null;
            }

            if (!spriteCache.ContainsKey(spriteName))
            {
                try
                {
                    Sprite loadedSprite = Resources.Load<Sprite>($"Image/{path}/{spriteName}");
                    if (loadedSprite != null)
                    {
                        spriteCache[spriteName] = loadedSprite;
                    }
                    else
                    {
                        Debug.LogError($"[ResourcesManager] Sprite '{spriteName}'을 경로 'Image/{path}/'에서 찾을 수 없습니다.");
                        return null;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ResourcesManager] 스프라이트 로드 중 오류: {ex.Message}");
                    return null;
                }
            }
            return spriteCache[spriteName];
        }

        /// <summary>
        /// Prefab을 인스턴스화하여 생성
        /// </summary>
        /// <param name="prefabName">프리팹 이름</param>
        /// <param name="position">생성 위치</param>
        /// <param name="rotation">생성 회전</param>
        /// <param name="parent">부모 Transform</param>
        /// <returns>생성된 인스턴스 또는 null</returns>
        public GameObject InstantiatePrefab(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefabCache.TryGetValue(prefabName, out GameObject prefab))
            {
                try
                {
                    GameObject instance = Instantiate(prefab, position, rotation, parent);
                    return instance;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ResourcesManager] 프리팹 인스턴스 생성 중 오류: {ex.Message}");
                    return null;
                }
            }

            Debug.LogError($"[ResourcesManager] '{prefabName}' 프리팹을 캐시에서 찾을 수 없습니다.");
            return null;
        }
        #endregion

        #region Debug & Validation
        /// <summary>
        /// 현재 상태 디버그 출력
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugPrintStatus()
        {
            Debug.Log($"[ResourcesManager] 상태: " +
                     $"기본초기화={isBasicInitialized}, " +
                     $"색상동기화={isColorSynchronized}, " +
                     $"Player스프라이트={playerSprite?.name}, " +
                     $"Opponent스프라이트={opponentSprite?.name}, " +
                     $"Player템플릿={playerCardTemplate?.name}, " +
                     $"Opponent템플릿={opponentCardTemplate?.name}, " +
                     $"프리팹캐시={prefabCache.Count}개, " +
                     $"스프라이트캐시={spriteCache.Count}개");
        }
        #endregion
    }
}