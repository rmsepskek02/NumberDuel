using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MultiClientBuilder
{
    private static string buildPath = "Builds"; // 기본 빌드 경로

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

            // 빌드 실행
            BuildPipeline.BuildPlayer(scenes, clientExe, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log($"Client {i} 빌드 완료! ({clientExe})");

            // 클라이언트 설정 저장
            SaveClientSettings(clientPath, i);

            // 실행 시 창 모드 적용하는 설정 파일 생성
            CreateWindowedConfig(clientPath, i);
        }

        // 빌드된 클라이언트 실행
        RunBuiltClients(clientCount);
    }

    // 활성화된 씬 목록 가져오기
    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    // 무작위 30자 문자열 생성
    private static string GenerateRandomString(int length = 30)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Range(0, s.Length)]).ToArray());
    }

    // 클라이언트 설정 저장 또는 로드
    private static void SaveClientSettings(string clientPath, int clientNumber)
    {
        string settingsFile = "ClientSettings.txt";
        string clientFolder = $"Client{clientNumber}";

        // 기존 설정 파일이 있는 경우 -> 불러오기
        if (File.Exists(Path.Combine(clientPath, settingsFile)))
        {
            Debug.Log($"Client {clientNumber} 기존 설정 로드");
            ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>(clientFolder, settingsFile);
            //JsonUtils.SaveToFile(settings, clientFolder, settingsFile); // 재저장 (갱신)
        }
        else
        {
            // 초기 빌드인 경우 -> 새 설정 생성
            ClientSettings settings = new ClientSettings(GenerateRandomString(), 1280, 720);
            JsonUtils.SaveToFile(settings, clientFolder, settingsFile);
        }
    }

    // 창 모드 적용하는 설정 파일 생성
    private static void CreateWindowedConfig(string clientPath, int clientNumber)
    {
        string settingsFile = Path.Combine(clientPath, "ClientSettings.txt");
        ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>($"Client{clientNumber}", "ClientSettings.txt");

        string configPath = Path.Combine(clientPath, "windowed.txt");
        File.WriteAllText(configPath, $"-screen-width {settings.ScreenWidth} -screen-height {settings.ScreenHeight} -screen-fullscreen 0");
    }

    // 빌드된 클라이언트 실행 (창 모드 강제 적용)
    private static void RunBuiltClients(int clientCount)
    {
        for (int i = 1; i <= clientCount; i++)
        {
            string clientExe = Path.Combine(buildPath, $"Client{i}", $"Client{i}.exe");
            string configPath = Path.Combine(buildPath, $"Client{i}", "windowed.txt");

            if (File.Exists(clientExe))
            {
                // 창 모드 설정 적용하여 실행
                System.Diagnostics.Process.Start(clientExe, File.ReadAllText(configPath));
            }
            else
            {
                Debug.LogError($"Client {i} 실행 파일을 찾을 수 없습니다: {clientExe}");
            }
        }
    }
}
