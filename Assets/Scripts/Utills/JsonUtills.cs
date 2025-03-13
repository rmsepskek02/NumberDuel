using System;
using System.IO;
using UnityEngine;

public static class JsonUtils
{
    /// <summary>
    /// 객체를 JSON 문자열로 변환 (제네릭 타입 지원)
    /// </summary>
    public static string SerializeToJson<T>(T data)
    {
        return JsonUtility.ToJson(data, true); // true는 보기 좋은 JSON 포맷
    }

    /// <summary>
    /// JSON 문자열을 객체로 변환 (제네릭 타입 지원)
    /// </summary>
    public static T DeserializeFromJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// JSON 데이터를 클라이언트별 폴더에 저장 (파일 이름 지정 가능)
    /// </summary>
    public static void SaveToFile<T>(T data, string clientFolder, string fileName)
    {
        string filePath = Path.Combine("Builds", clientFolder, fileName);
        string json = SerializeToJson(data);

        // 디렉토리가 없으면 생성
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, json);
        Debug.Log($"Data saved to: {filePath}");
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

        Debug.LogWarning($"File not found: {filePath}, returning default instance.");
        return new T(); // 기본 생성자로 초기화
    }
}
