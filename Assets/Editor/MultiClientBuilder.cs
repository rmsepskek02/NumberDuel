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

            // ���� �� â ��� �����ϴ� ���� ���� ����
            CreateWindowedConfig(clientPath);
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

    // â ��� �����ϴ� ���� ���� ����
    private static void CreateWindowedConfig(string clientPath)
    {
        string configPath = Path.Combine(clientPath, "windowed.txt");
        File.WriteAllText(configPath, $"-screen-width {1280} -screen-height {720} -screen-fullscreen 0");
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
