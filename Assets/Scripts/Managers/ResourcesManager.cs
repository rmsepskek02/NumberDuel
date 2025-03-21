using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Utills;

namespace Manager
{
    public class ResourcesManager : Singleton<ResourcesManager>
    {
        [SerializeField] 
        private SerializableDictionary<string, GameObject> prefabCache = new SerializableDictionary<string, GameObject>();
        [SerializeField] 
        private SerializableDictionary<string, Material> materialCache = new SerializableDictionary<string, Material>();

        protected override void Awake()
        {
            base.Awake();
            LoadAllPrefabs(Global.Card);
            LoadAllMaterial(Global.Card);
        }
        private void OnDestroy()
        {
            ClearCache();
        }

        /// <summary>
        /// 모든 캐시 제거 (메모리 최적화)
        /// </summary>
        public void ClearCache()
        {
            prefabCache.Clear();
            materialCache.Clear();
            Debug.Log("[ResourcesManager] 캐시 초기화 완료.");
        }

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

        /// <summary>
        /// 특정 폴더 내 모든 Material을 한 번에 로드
        /// </summary>
        private void LoadAllMaterial(string path)
        {
            Material[] materials = Resources.LoadAll<Material>($"Materials/{path}");

            foreach (Material material in materials)
            {
                materialCache[material.name] = material;
            }

            Debug.Log($"[ResourcesManager] {materials.Length}개의 프리팹이 로드됨.");
        }

        /// <summary>
        /// Material을 로드하고 캐싱 (Resources 폴더 내에서 로드)
        /// </summary>
        public Material GetMaterial(string path,string materialName)
        {
            if (!materialCache.ContainsKey(materialName))
            {
                Material loadedMaterial = Resources.Load<Material>($"Materials/{path}/{materialName}");
                if (loadedMaterial != null)
                {
                    materialCache[materialName] = loadedMaterial;
                }
                else
                {
                    Debug.LogError($"[ResourcesManager] Material '{materialName}'을 찾을 수 없습니다.");
                    return null;
                }
            }
            return materialCache[materialName];
        }

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

            if (!materialCache.TryGetValue(materialName, out Material material))
            {
                Debug.LogError($"[ResourcesManager] '{materialName}' Material을 찾을 수 없습니다.");
                return;
            }

            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
                Debug.Log($"[ResourcesManager] '{targetObject.name}' 오브젝트의 Material을 '{materialName}'로 변경함.");
            }
            else
            {
                Debug.LogError($"[ResourcesManager] '{targetObject.name}' 오브젝트에 Renderer가 없습니다.");
            }
        }
    }
}
