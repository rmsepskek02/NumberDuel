using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MultiClientBuilder
{
    private static string buildPath = "Builds"; // �⺻ ���� ���

    [MenuItem("Tools/Build Clients/x2")]
    public static void BuildClientsX2() => BuildClients(2);

    [MenuItem("Tools/Build Clients/x3")]
    public static void BuildClientsX3() => BuildClients(3);

    [MenuItem("Tools/Build Clients/x4")]
    public static void BuildClientsX4() => BuildClients(4);

    [MenuItem("Tools/Build Clients/x5")]
    public static void BuildClientsX5() => BuildClients(5);

    private static void BuildClients(int clientCount)
    {
        string[] scenes = GetEnabledScenes();

        for (int i = 1; i <= clientCount; i++)
        {
            string clientPath = Path.Combine(buildPath, $"Client{i}");
            string clientExe = Path.Combine(clientPath, $"Client{i}.exe");

            // ���� ����
            BuildPipeline.BuildPlayer(scenes, clientExe, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log($"Client {i} ���� �Ϸ�! ({clientExe})");

            // Ŭ���̾�Ʈ ���� ����
            SaveClientSettings(clientPath, i);

            // ���� �� â ��� �����ϴ� ���� ���� ����
            CreateWindowedConfig(clientPath, i);
        }

        // ����� Ŭ���̾�Ʈ ����
        RunBuiltClients(clientCount);
    }

    // Ȱ��ȭ�� �� ��� ��������
    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    // ������ 30�� ���ڿ� ����
    private static string GenerateRandomString(int length = 30)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Range(0, s.Length)]).ToArray());
    }

    // Ŭ���̾�Ʈ ���� ���� �Ǵ� �ε�
    private static void SaveClientSettings(string clientPath, int clientNumber)
    {
        string settingsFile = "ClientSettings.txt";
        string clientFolder = $"Client{clientNumber}";

        // ���� ���� ������ �ִ� ��� -> �ҷ�����
        if (File.Exists(Path.Combine(clientPath, settingsFile)))
        {
            Debug.Log($"Client {clientNumber} ���� ���� �ε�");
            ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>(clientFolder, settingsFile);
            //JsonUtils.SaveToFile(settings, clientFolder, settingsFile); // ������ (����)
        }
        else
        {
            // �ʱ� ������ ��� -> �� ���� ����
            ClientSettings settings = new ClientSettings(GenerateRandomString(), 1280, 720);
            JsonUtils.SaveToFile(settings, clientFolder, settingsFile);
        }
    }

    // â ��� �����ϴ� ���� ���� ����
    private static void CreateWindowedConfig(string clientPath, int clientNumber)
    {
        string settingsFile = Path.Combine(clientPath, "ClientSettings.txt");
        ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>($"Client{clientNumber}", "ClientSettings.txt");

        string configPath = Path.Combine(clientPath, "windowed.txt");
        File.WriteAllText(configPath, $"-screen-width {settings.ScreenWidth} -screen-height {settings.ScreenHeight} -screen-fullscreen 0");
    }

    // ����� Ŭ���̾�Ʈ ���� (â ��� ���� ����)
    private static void RunBuiltClients(int clientCount)
    {
        for (int i = 1; i <= clientCount; i++)
        {
            string clientExe = Path.Combine(buildPath, $"Client{i}", $"Client{i}.exe");
            string configPath = Path.Combine(buildPath, $"Client{i}", "windowed.txt");

            if (File.Exists(clientExe))
            {
                // â ��� ���� �����Ͽ� ����
                System.Diagnostics.Process.Start(clientExe, File.ReadAllText(configPath));
            }
            else
            {
                Debug.LogError($"Client {i} ���� ������ ã�� �� �����ϴ�: {clientExe}");
            }
        }
    }
}
