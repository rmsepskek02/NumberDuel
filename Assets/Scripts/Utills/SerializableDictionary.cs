using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utills
{
    /// <summary>
    /// Unity에서 Dictionary를 직렬화하기 위한 SerializableDictionary
    /// Inspector에서 편집 가능하도록 List 기반 직렬화 구현
    /// ISerializationCallbackReceiver를 통해 직렬화/역직렬화 처리
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        #region Fields and Properties
        [SerializeField] private List<TKey> keys = new List<TKey>();
        [SerializeField] private List<TValue> values = new List<TValue>();
        #endregion

        #region Serialization
        /// <summary>
        /// 직렬화 전에 Dictionary를 List로 변환
        /// </summary>
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        /// <summary>
        /// 역직렬화 후에 List를 Dictionary로 변환
        /// </summary>
        public void OnAfterDeserialize()
        {
            this.Clear();
            if (keys.Count != values.Count)
            {
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                this[keys[i]] = values[i];
            }
        }
        #endregion
    }
}
