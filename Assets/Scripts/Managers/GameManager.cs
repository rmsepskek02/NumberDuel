using UnityEngine;
using System.IO;
using Utills;
using Manager;

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
        // ���� ������ ��ġ�� ���� ��������
        string exeFolder = Path.GetDirectoryName(Application.dataPath);

        // ���� ���� �̸� �������� (��: Client1.exe -> Client1)
        string exeName = Path.GetFileNameWithoutExtension(exeFolder);

        // ���������� Client1\Client1_Data ������ ���� ��� ��ȯ
        return exeName;
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
        Debug.Log($"Updated Resolution Saved: {width} x {height} in {clientFolder}");
    }
}
