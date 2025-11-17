using UnityEngine;

namespace Utills
{
    /// <summary>
    /// 씬 전환 시에도 유지되는 싱글톤 패턴 베이스 클래스
    /// DontDestroyOnLoad를 사용하여 전역적으로 유일한 인스턴스 보장
    /// 멀티 스레드 환경에서도 안전한 인스턴스 생성
    /// </summary>
    /// <typeparam name="T">싱글톤으로 사용할 MonoBehaviour 타입</typeparam>
    public class SingletonDontDestroy<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Fields and Properties
        private static T instance;
        private static readonly object lockObj = new object();

        public static T Instance
        {
            get
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = FindAnyObjectByType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObject = new GameObject(typeof(T).Name);
                            instance = singletonObject.AddComponent<T>();
                            DontDestroyOnLoad(singletonObject);
                        }
                    }
                    return instance;
                }
            }
        }
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        #endregion
    }
}
