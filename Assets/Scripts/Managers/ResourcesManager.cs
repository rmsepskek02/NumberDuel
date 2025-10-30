using Objects;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 게임의 Resource를 관리하는 매니저
    /// 기본 초기화와 색상 관리, 네트워크 색상 동기화를 분리 처리
    /// </summary>
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        #region Fields and Properties
        [SerializeField]
        private SerializableDictionary<string, GameObject> prefabCache = new SerializableDictionary<string, GameObject>();

        [SerializeField]
        private SerializableDictionary<string, Sprite> spriteCache = new SerializableDictionary<string, Sprite>();

        private Sprite playerSprite;
        private Sprite opponentSprite;
        private GameObject playerCardTemplate;
        private GameObject opponentCardTemplate;

        /// <summary>
        /// 색상 동기화 완료 여부 (NetworkGameManager에서 사용)
        /// </summary>
        private bool isColorSynchronized = false;

        /// <summary>
        /// 기본 초기화 완료 여부
        /// </summary>
        private bool isBasicInitialized = false;

        /// <summary>
        /// 외부에서 Player템 접근
        /// </summary>
        public GameObject GetPlayerCardTemplate() => playerCardTemplate;
        public Sprite GetPlayerSprite() => playerSprite;

        /// <summary>
        /// 외부에서 Opponent템 접근
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
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            // 기본 리소스 로드 (색상 무관)
            LoadAllPrefabs(Global.Card);
            LoadAllSprites(Global.Card);
            LoadAllSprites(Global.Joker);

            // 아이콘 스프라이트 로드 (Icons 폴더)
            LoadAllSprites("Icons");

            // 기본 카드 템플릿 생성 (색상은 나중에 적용)
            CreateBasicCardTemplates();

            isBasicInitialized = true;
        }

        private void OnDestroy()
        {
            ClearCache();
        }
        #endregion

        #region Basic Initialization (색상 무관)
        /// <summary>
        /// 기본 카드 템플릿 생성
        /// 색상은 기본값으로 설정하고, 나중에 네트워크 동기화로 변경
        /// </summary>
        private void CreateBasicCardTemplates()
        {
            // 기존 템플릿 제거
            if (playerCardTemplate != null)
                DestroyImmediate(playerCardTemplate);
            if (opponentCardTemplate != null)
                DestroyImmediate(opponentCardTemplate);

            // 프리팹 가져오기
            var playerPrefab = GetPrefab(Global.Card, "Player_Card");
            var opponentPrefab = GetPrefab(Global.Card, "Opponent_Card");

            if (playerPrefab == null || opponentPrefab == null)
            {
                return;
            }

            // 템플릿 인스턴스 생성
            playerCardTemplate = Instantiate(playerPrefab);
            opponentCardTemplate = Instantiate(opponentPrefab);

            // 이름 설정
            playerCardTemplate.name = "PlayerCardTemplate";
            opponentCardTemplate.name = "OpponentCardTemplate";

            // 기본 스프라이트 설정 (첫 번째 색상 스프라이트)
            SetDefaultSprites();

            // 템플릿을 비활성화 상태로 유지
            playerCardTemplate.SetActive(false);
            opponentCardTemplate.SetActive(false);
        }

        /// <summary>
        /// 기본 스프라이트 설정 (마스터 색상 이미 있다면, 아닌 경우 첫 번째에서 읽기)
        /// </summary>
        private void SetDefaultSprites()
        {
            List<Sprite> availableSprites = GetAvailableColorSprites();

            if (availableSprites.Count >= 2)
            {
                // 마스터가 아닌 경우 첫 번째에서 룸 속성값 읽기 시도
                if (!Photon.Pun.PhotonNetwork.IsMasterClient && Photon.Pun.PhotonNetwork.InRoom)
                {
                    var roomProperties = Photon.Pun.PhotonNetwork.CurrentRoom.CustomProperties;

                    if (roomProperties.TryGetValue("masterPlayerColor", out object masterColor) &&
                        roomProperties.TryGetValue("guestPlayerColor", out object guestColor))
                    {
                        string masterColorName = masterColor.ToString();
                        string guestColorName = guestColor.ToString();

                        // 게스트 플레이어는 반대 색상
                        SetPlayerColors(guestColorName, masterColorName);
                        return;
                    }
                }

                // 마스터이거나 룸 속성이 없는 경우 기본 색상 설정
                playerSprite = availableSprites[0];
                opponentSprite = availableSprites[1];

                ApplyVisual(playerCardTemplate, playerSprite);
                ApplyVisual(opponentCardTemplate, opponentSprite);
            }
        }
        #endregion

        #region Network Color Synchronization (색상 관리)
        /// <summary>
        /// 네트워크 동기화로 플레이어 색상 설정
        /// NetworkGameManager에서 호출하여 모든 클라이언트가 동일한 색상 설정
        /// </summary>
        /// <param name="playerSpriteName">플레이어 카드 스프라이트 이름</param>
        /// <param name="opponentSpriteName">상대 카드 스프라이트 이름</param>
        public void SetPlayerColors(string playerSpriteName, string opponentSpriteName)
        {
            if (!isBasicInitialized)
            {
                return;
            }

            // 스프라이트 이름으로 실제 스프라이트 찾기
            if (spriteCache.TryGetValue(playerSpriteName, out Sprite foundPlayerSprite))
            {
                playerSprite = foundPlayerSprite;
            }
            else
            {
                return;
            }

            if (spriteCache.TryGetValue(opponentSpriteName, out Sprite foundOpponentSprite))
            {
                opponentSprite = foundOpponentSprite;
            }
            else
            {
                return;
            }

            // 기존 템플릿에 새로운 색상 적용
            ApplyVisual(playerCardTemplate, playerSprite);
            ApplyVisual(opponentCardTemplate, opponentSprite);

            // 이미 생성된 DeckStacker들도 색상 업데이트
            UpdateExistingDeckStackers();

            // 색상 동기화 완료 표시
            isColorSynchronized = true;
        }

        /// <summary>
        /// 마스터: 랜덤 색상 선택 후 이름 반환
        /// NetworkGameManager에서 호출하여 RPC로 브로드캐스트 색상 전달
        /// </summary>
        /// <returns>선택된 스프라이트 이름들 (player, opponent)</returns>
        public (string playerSpriteName, string opponentSpriteName) SelectRandomColors()
        {
            List<Sprite> availableSprites = GetAvailableColorSprites();

            if (availableSprites.Count < 2)
            {
                return ("", "");
            }

            // 랜덤 선택 (중복 방지)
            int index1 = UnityEngine.Random.Range(0, availableSprites.Count);
            int index2;
            do
            {
                index2 = UnityEngine.Random.Range(0, availableSprites.Count);
            } while (index2 == index1);

            string playerSpriteName = availableSprites[index1].name;
            string opponentSpriteName = availableSprites[index2].name;

            return (playerSpriteName, opponentSpriteName);
        }

        /// <summary>
        /// 모든 사용할 색상 스프라이트 목록 반환
        /// </summary>
        /// <returns>필터링된 스프라이트 목록</returns>
        private List<Sprite> GetAvailableColorSprites()
        {
            List<Sprite> availableSprites = new List<Sprite>();

            foreach (var kvp in spriteCache)
            {
                string spriteName = kvp.Key.ToLower();

                // 뒷면 카드가 아니고 빈 카드인 스프라이트만 포함
                if (!spriteName.Contains("color_back") && spriteName.Contains("empty"))
                {
                    availableSprites.Add(kvp.Value);
                }
            }

            return availableSprites;
        }
        #endregion

        #region Existing Objects Update
        /// <summary>
        /// 이미 생성된 DeckStacker들의 색상 업데이트
        /// 색상 동기화 후 게임에 생성된 덱들도 함께 업데이트
        /// </summary>
        private void UpdateExistingDeckStackers()
        {
            // 현재 있는 모든 DeckStacker 찾기
            DeckStacker[] deckStackers = FindObjectsByType<DeckStacker>(FindObjectsSortMode.None);

            foreach (var stacker in deckStackers)
            {
                // 플레이어 덱인지 상대 덱인지 확인
                bool isPlayerDeck = stacker.IsMyDeck;
                Sprite targetSprite = isPlayerDeck ? playerSprite : opponentSprite;

                // DeckStacker의 시각적 색상 업데이트
                UpdateDeckStackerVisual(stacker, targetSprite);
            }
        }

        /// <summary>
        /// 단일 DeckStacker의 시각적 색상 업데이트
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
                // 카드 관련 SpriteRenderer만 업데이트 (아이콘 제외)
                if (spriteRenderer.gameObject.name.Contains("Card") ||
                    spriteRenderer.gameObject.name.Contains("Deck"))
                {
                    spriteRenderer.sprite = sprite;

                    // Material Property Block을 사용해 텍스처 변경
                    var propertyBlock = new MaterialPropertyBlock();
                    spriteRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetTexture("_MainTex", sprite.texture);
                    spriteRenderer.SetPropertyBlock(propertyBlock);
                }
            }
        }
        #endregion

        #region Visual Application
        /// <summary>
        /// 카드 오브젝트에 시각적 정보 적용
        /// 스프라이트 및 텍스트 색상 변경
        /// </summary>
        /// <param name="card">대상 카드 오브젝트</param>
        /// <param name="sprite">적용할 스프라이트</param>
        private void ApplyVisual(GameObject card, Sprite sprite)
        {
            if (card == null || sprite == null)
            {
                return;
            }

            // SpriteRenderer에 스프라이트 적용
            var spriteRenderer = card.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;

                // Material Property Block을 사용해 텍스처 변경
                var propertyBlock = new MaterialPropertyBlock();
                spriteRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetTexture("_MainTex", sprite.texture);
                spriteRenderer.SetPropertyBlock(propertyBlock);
            }

            // 텍스트 색상 매칭 (스프라이트 이름 기반)
            var textMeshPro = card.GetComponentInChildren<TMPro.TextMeshPro>();
            if (textMeshPro != null)
            {
                Color textColor = MatchColorFromSprite(sprite.name);
                textMeshPro.color = textColor;
            }
        }

        /// <summary>
        /// 스프라이트 이름에 따라 대응하는 텍스트 색상 반환
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
        /// 모든 캐시 정리 (메모리 최적화)
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
        /// 특정 경로 내 모든 프리팹을 한 번에 로드
        /// </summary>
        /// <param name="path">로드할 경로 이름</param>
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourcesManager] 프리팹 로드 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 경로 내 모든 Sprite를 한 번에 로드
        /// </summary>
        /// <param name="path">로드할 경로 이름</param>
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
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourcesManager] 스프라이트 로드 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// Prefab을 로드하고 캐시 (Resources 경로 하위부터 로드)
        /// </summary>
        /// <param name="path">경로 이름</param>
        /// <param name="prefabName">프리팹 이름</param>
        /// <returns>로드된 프리팹 또는 null</returns>
        public GameObject GetPrefab(string path, string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
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
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ResourcesManager] 프리팹 로드 중 오류: {ex.Message}");
                    return null;
                }
            }
            return prefabCache[prefabName];
        }

        /// <summary>
        /// Sprite를 로드하고 캐시 (Resources 경로 하위부터 로드)
        /// </summary>
        /// <param name="path">경로 이름</param>
        /// <param name="spriteName">스프라이트 이름</param>
        /// <returns>로드된 스프라이트 또는 null</returns>
        public Sprite GetSprite(string path, string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
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
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ResourcesManager] 스프라이트 로드 중 오류: {ex.Message}");
                    return null;
                }
            }
            return spriteCache[spriteName];
        }

        /// <summary>
        /// Prefab을 인스턴스화하여 반환
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
                catch (Exception ex)
                {
                    Debug.LogError($"[ResourcesManager] 프리팹 인스턴스 생성 중 오류: {ex.Message}");
                    return null;
                }
            }

            return null;
        }
        #endregion
    }
}
