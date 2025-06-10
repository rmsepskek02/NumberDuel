using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 게임의 Resource를 관리하는 매니저
    /// </summary>
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        [SerializeField]
        private SerializableDictionary<string, GameObject> prefabCache = new SerializableDictionary<string, GameObject>();

        [SerializeField]
        private SerializableDictionary<string, Sprite> spriteCache = new SerializableDictionary<string, Sprite>();

        //[SerializeField] 
        //private SerializableDictionary<string, Material> materialCache = new SerializableDictionary<string, Material>();

        private Sprite playerSprite;
        private Sprite opponentSprite;
        private GameObject playerCardTemplate;
        private GameObject opponentCardTemplate;

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

        protected override void Awake()
        {
            base.Awake();
            LoadAllPrefabs(Global.Card);   // Resources/Prefabs/Card 하위 프리팹들 자동 로드
            LoadAllSprites(Global.Card);        // Resources/Image/Card 하위 스프라이트 자동 로드
            LoadAllSprites(Global.Joker);
            PrepareCardTemplates();
        }

        private void OnDestroy()
        {
            ClearCache();
        }

        #region Card Template 준비

        /// <summary>
        /// 게임 시작 시 한 번만 호출되어, Player/Opponent용 Prefab 무작위로 설정
        /// </summary>
        private void PrepareCardTemplates()
        {
            SelectPlayerAndOpponentSprites();

            var playerPrefab = GetPrefab(Global.Card, "Player_Card");
            var opponentPrefab = GetPrefab(Global.Card, "Opponent_Card");

            playerCardTemplate = Instantiate(playerPrefab);
            opponentCardTemplate = Instantiate(opponentPrefab);

            playerCardTemplate.name = "PlayerCardTemplate";
            opponentCardTemplate.name = "OpponentCardTemplate";

            ApplyVisual(playerCardTemplate, playerSprite);
            ApplyVisual(opponentCardTemplate, opponentSprite);

            playerCardTemplate.SetActive(false);
            opponentCardTemplate.SetActive(false);
        }

        private void ApplyVisual(GameObject card, Sprite sprite)
        {
            var sr = card.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;

                var block = new MaterialPropertyBlock();
                sr.GetPropertyBlock(block);
                block.SetTexture("_MainTex", sprite.texture);
                sr.SetPropertyBlock(block);
            }

            // 텍스트 색상 변경 (Sprite 색상 추정 or 정의된 색상 사용)
            var text = card.GetComponentInChildren<TMPro.TextMeshPro>();
            if (text != null)
            {
                // Sprite 이름 기준으로 색 결정 (예: "color_red_empty" → Global.Red)
                Color matchedColor = MatchColorFromSprite(sprite.name);
                text.color = matchedColor;
            }
        }

        /// <summary>
        /// Sprite 이름에 따라 색상을 결정
        /// </summary>
        public Color MatchColorFromSprite(string spriteName)
        {
            string lowerName = spriteName.ToLower();

            if (lowerName.Contains("red")) return Global.Red;
            if (lowerName.Contains("green")) return Global.Green;
            if (lowerName.Contains("yellow")) return Global.Yellow;
            if (lowerName.Contains("purple")) return Global.Purple;

            return Color.white; // 기본값
        }

        private void SelectPlayerAndOpponentSprites()
        {
            List<Sprite> list = new List<Sprite>();
            foreach (var kvp in spriteCache)
            {
                if (!kvp.Key.ToLower().Contains("color_back")
                    && kvp.Key.ToLower().Contains("empty")) // 이름 기준 필터링
                    list.Add(kvp.Value);
            }
            var sprites = list;

            if (sprites.Count < 2)
            {
                Debug.LogError("[ResourcesManager] 사용할 수 있는 Sprite가 2개 이상 필요합니다.");
                return;
            }

            int index1 = UnityEngine.Random.Range(0, sprites.Count);
            int index2;
            do { index2 = UnityEngine.Random.Range(0, sprites.Count); } while (index2 == index1);

            playerSprite = sprites[index1];
            opponentSprite = sprites[index2];

            Debug.Log($"[ResourcesManager] PlayerSprite: {playerSprite.name}, OpponentSprite: {opponentSprite.name}");
        }

        #endregion

        /// <summary>
        /// 모든 캐시 제거 (메모리 최적화)
        /// </summary>
        public void ClearCache()
        {
            prefabCache.Clear();
            spriteCache.Clear();
            //materialCache.Clear();
            Debug.Log("[ResourcesManager] 캐시 초기화 완료.");
        }

        #region Prefab 관련

        /// <summary>
        /// 특정 폴더 내 모든 프리팹을 한 번에 로드
        /// </summary>
        private void LoadAllPrefabs(string path)
        {
            GameObject[] prefabs = Resources.LoadAll<GameObject>($"Prefabs/{path}");

            foreach (GameObject prefab in prefabs)
            {
                prefabCache[prefab.name] = prefab;
            }

            Debug.Log($"[ResourcesManager] {prefabs.Length}개의 프리팹이 로드됨.");
        }

        /// <summary>
        /// Prefab을 로드하고 캐싱 (Resources 폴더 내에서 로드)
        /// </summary>
        public GameObject GetPrefab(string path, string prefabName)
        {
            if (!prefabCache.ContainsKey(prefabName))
            {
                GameObject loadedPrefab = Resources.Load<GameObject>($"Prefabs/{path}/{prefabName}");
                if (loadedPrefab != null)
                {
                    prefabCache[prefabName] = loadedPrefab;
                }
                else
                {
                    Debug.LogError($"[ResourcesManager] Prefab '{prefabName}'을 찾을 수 없습니다.");
                    return null;
                }
            }
            return prefabCache[prefabName];
        }

        /// <summary>
        /// Prefab을 인스턴스화하여 생성 (딕셔너리에서 탐색 후 가져오기)
        /// </summary>
        public GameObject InstantiatePrefab(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefabCache.TryGetValue(prefabName, out GameObject prefab))
            {
                return Instantiate(prefab, position, rotation, parent);
            }

            Debug.LogError($"[ResourcesManager] '{prefabName}' 프리팹을 찾을 수 없습니다.");
            return null;
        }

        #endregion

        #region Sprite 관련

        /// <summary>
        /// 특정 폴더 내 모든 Sprite를 한 번에 로드
        /// </summary>
        private void LoadAllSprites(string path)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>($"Image/{path}");

            foreach (Sprite sprite in sprites)
            {
                spriteCache[sprite.name] = sprite;
            }

            Debug.Log($"[ResourcesManager] {sprites.Length}개의 스프라이트가 로드됨.");
        }

        /// <summary>
        /// Sprite를 로드하고 캐싱 (Resources 폴더 내에서 로드)
        /// </summary>
        public Sprite GetSprite(string path, string spriteName)
        {
            if (!spriteCache.ContainsKey(spriteName))
            {
                Sprite loadedSprite = Resources.Load<Sprite>($"Image/{path}/{spriteName}");
                if (loadedSprite != null)
                {
                    spriteCache[spriteName] = loadedSprite;
                }
                else
                {
                    Debug.LogError($"[ResourcesManager] Sprite '{spriteName}'을 찾을 수 없습니다.");
                    return null;
                }
            }
            return spriteCache[spriteName];
        }
      
        #endregion

        #region Material 관련 (미사용 중, 필요 시 확장 가능)

        /// <summary>
        /// 특정 폴더 내 모든 Material을 한 번에 로드
        /// </summary>
        private void LoadAllMaterial(string path)
        {
            Material[] materials = Resources.LoadAll<Material>($"Materials/{path}");

            foreach (Material material in materials)
            {
                //materialCache[material.name] = material;
            }

            Debug.Log($"[ResourcesManager] {materials.Length}개의 머티리얼이 로드됨.");
        }

        /// <summary>
        /// Material을 로드하고 캐싱 (Resources 폴더 내에서 로드)
        /// </summary>
        //public Material GetMaterial(string path,string materialName)
        //{
        //    if (!materialCache.ContainsKey(materialName))
        //    {
        //        Material loadedMaterial = Resources.Load<Material>($"Materials/{path}/{materialName}");
        //        if (loadedMaterial != null)
        //        {
        //            materialCache[materialName] = loadedMaterial;
        //        }
        //        else
        //        {
        //            Debug.LogError($"[ResourcesManager] Material '{materialName}'을 찾을 수 없습니다.");
        //            return null;
        //        }
        //    }
        //    return materialCache[materialName];
        //}

        /// <summary>
        /// 특정 오브젝트의 Material을 변경
        /// </summary>
        /// <param name="targetObject">Material을 변경할 대상 오브젝트</param>
        /// <param name="materialName">적용할 Material 이름</param>
        public void ApplyMaterialToObject(GameObject targetObject, string materialName)
        {
            if (targetObject == null)
            {
                Debug.LogError("[ResourcesManager] 대상 오브젝트가 null입니다.");
                return;
            }

            //if (!materialCache.TryGetValue(materialName, out Material material))
            //{
            //    Debug.LogError($"[ResourcesManager] '{materialName}' Material을 찾을 수 없습니다.");
            //    return;
            //}

            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                //renderer.material = material;
                Debug.Log($"[ResourcesManager] '{targetObject.name}' 오브젝트의 Material을 '{materialName}'로 변경함.");
            }
            else
            {
                Debug.LogError($"[ResourcesManager] '{targetObject.name}' 오브젝트에 Renderer가 없습니다.");
            }
        }

        #endregion
    }
}