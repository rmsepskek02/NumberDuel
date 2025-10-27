using System;
using System.IO;
using UnityEngine;

namespace Utills
{
    /// <summary>
    /// JSON 직렬화 및 파일 입출력을 담당하는 유틸리티 클래스
    /// Unity의 JsonUtility를 사용하여 데이터 직렬화/역직렬화 수행
    /// 클라이언트 폴더 단위로 설정 파일 저장/로드 지원
    /// </summary>
    public static class JsonUtils
    {
        #region Serialization
        /// <summary>
        /// 객체를 JSON 문자열로 변환 (제네릭 타입 사용)
        /// </summary>
        public static string SerializeToJson<T>(T data)
        {
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// JSON 문자열을 객체로 변환 (제네릭 타입 사용)
        /// </summary>
        public static T DeserializeFromJson<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
        #endregion

        #region File Operations
        /// <summary>
        /// JSON 데이터를 클라이언트별 폴더에 저장 (파일 이름 지정 가능)
        /// </summary>
        public static void SaveToFile<T>(T data, string clientFolder, string fileName)
        {
            string filePath = Path.Combine("Builds", clientFolder, fileName);
            string json = SerializeToJson(data);

            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 클라이언트별 폴더에서 JSON 데이터를 로드 (파일이 없으면 기본 인스턴스 반환)
        /// </summary>
        public static T LoadFromFile<T>(string clientFolder, string fileName) where T : new()
        {
            string filePath = Path.Combine("Builds", clientFolder, fileName);

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return DeserializeFromJson<T>(json);
            }

            return new T();
        }
        #endregion
    }
}
