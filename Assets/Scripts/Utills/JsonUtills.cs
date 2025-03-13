using System;
using System.IO;
using UnityEngine;

public static class JsonUtils
{
    /// <summary>
    /// ��ü�� JSON ���ڿ��� ��ȯ (���׸� Ÿ�� ����)
    /// </summary>
    public static string SerializeToJson<T>(T data)
    {
        return JsonUtility.ToJson(data, true); // true�� ���� ���� JSON ����
    }

    /// <summary>
    /// JSON ���ڿ��� ��ü�� ��ȯ (���׸� Ÿ�� ����)
    /// </summary>
    public static T DeserializeFromJson<T>(string json)
    {
        return JsonUtility.FromJson<T>(json);
    }

    /// <summary>
    /// JSON �����͸� Ŭ���̾�Ʈ�� ������ ���� (���� �̸� ���� ����)
    /// </summary>
    public static void SaveToFile<T>(T data, string clientFolder, string fileName)
    {
        string filePath = Path.Combine("Builds", clientFolder, fileName);
        string json = SerializeToJson(data);

        // ���丮�� ������ ����
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, json);
        Debug.Log($"Data saved to: {filePath}");
    }

    /// <summary>
    /// Ŭ���̾�Ʈ�� �������� JSON �����͸� �ε� (������ ������ �⺻ �ν��Ͻ� ��ȯ)
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
        return new T(); // �⺻ �����ڷ� �ʱ�ȭ
    }
}
