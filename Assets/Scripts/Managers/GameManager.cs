using UnityEngine;
using System.IO;
using Utills;

public class GameManager : SingletonDontDestroy<GameManager>
{
    private string clientFolder;
    private string settingsFile = "ClientSettings.txt"; // JSON ���� ���ϸ�

    private void Start()
    {
        // Ŭ���̾�Ʈ ������ ���� (����� Ŭ���̾�Ʈ���� Ȯ��)
        clientFolder = GetClientFolderName();
    }

    private void OnApplicationQuit()
    {
        // ���� �ػ󵵸� ����
        SaveClientSettings();
    }

    /// <summary>
    /// ���� ���� ���� Ŭ���̾�Ʈ ���� �̸� ��������
    /// </summary>
    private string GetClientFolderName()
    {
        // ���� ���� �̸��� ������� Ŭ���̾�Ʈ ���� ����
        string exeName = Path.GetFileNameWithoutExtension(Application.dataPath);
        return exeName.Replace(" ", "").Trim(); // ���� ���� �� ����
    }

    /// <summary>
    /// ���� �ػ󵵸� �����Ͽ� ClientSettings.txt ������Ʈ
    /// </summary>
    private void SaveClientSettings()
    {
        // ���� �ػ� ��������
        int width = Screen.width;
        int height = Screen.height;

        // ���� ���� �ҷ����� (ClientID ����)
        ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>(clientFolder, settingsFile);
        settings.ScreenWidth = width;
        settings.ScreenHeight = height;

        // ����
        JsonUtils.SaveToFile(settings, clientFolder, settingsFile);
        Debug.Log($"Updated Resolution Saved: {width} x {height}");
    }
}
