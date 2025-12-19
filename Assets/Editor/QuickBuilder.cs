using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Utills;

public class QuickBuilder
{
    private static string buildPath = "Builds"; // 기본 빌드 경로

    // 메뉴 추가: x1 메뉴
    [MenuItem("Tools/Build Clients/x1")]
    public static void BuildClientsX1() => BuildClients(1);

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

        // 빌드 전 모든 클라이언트의 기존 로그 파일 삭제
        CleanupOldLogs(clientCount);

        for (int i = 1; i <= clientCount; i++)
        {
            string clientPath = Path.Combine(buildPath, $"Client{i}");
            string clientExe = Path.Combine(clientPath, $"Client{i}.exe");

            // 빌드 실행
            BuildPipeline.BuildPlayer(scenes, clientExe, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log($"Client {i} 빌드 완료! ({clientExe})");

            // 클라이언트 설정 저장
            SaveClientSettings(clientPath, i);

            // 실행 시 창 모드 및 로그 파일 경로 설정 파일 생성
            CreateWindowedConfig(clientPath, i);
        }

        Debug.Log("=== 모든 클라이언트 빌드 완료! 새로운 로그 파일이 생성됩니다. ===");

        // 빌드된 클라이언트 실행
        RunBuiltClients(clientCount);
    }

    // 기존 로그 파일 삭제
    private static void CleanupOldLogs(int clientCount)
    {
        Debug.Log("=== 기존 로그 파일 삭제 중... ===");

        for (int i = 1; i <= clientCount; i++)
        {
            string clientPath = Path.Combine(buildPath, $"Client{i}");
            string logFile = Path.Combine(clientPath, "Player.log");

            if (File.Exists(logFile))
            {
                try
                {
                    File.Delete(logFile);
                    Debug.Log($"Client {i}: 기존 로그 삭제 완료");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Client {i}: 로그 삭제 실패 - {e.Message}");
                }
            }
            else
            {
                Debug.Log($"Client {i}: 기존 로그 없음 (처음 빌드)");
            }
        }

        Debug.Log("=== 로그 파일 삭제 완료! ===");
    }

    // 활성화된 씬 목록 가져오기
    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }

    // 랜덤한 30자 문자열 생성
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
        }
        else
        {
            // 초기 빌드인 경우 -> 새 설정 생성
            ClientSettings settings = new ClientSettings(GenerateRandomString(), 1280, 720);
            JsonUtils.SaveToFile(settings, clientFolder, settingsFile);
        }
    }

    // 창 모드 및 로그 파일 경로 설정 파일 생성
    private static void CreateWindowedConfig(string clientPath, int clientNumber)
    {
        string settingsFile = Path.Combine(clientPath, "ClientSettings.txt");
        ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>($"Client{clientNumber}", "ClientSettings.txt");

        // 로그 파일 경로를 절대 경로로 설정
        string fullClientPath = Path.GetFullPath(clientPath);
        string logFilePath = Path.Combine(fullClientPath, "Player.log");

        string configPath = Path.Combine(clientPath, "windowed.txt");

        // 창 모드 + 로그 파일 경로 설정
        string arguments = $"-screen-width {settings.ScreenWidth} -screen-height {settings.ScreenHeight} -screen-fullscreen 0 -logFile \"{logFilePath}\"";

        File.WriteAllText(configPath, arguments);

        Debug.Log($"Client {clientNumber} 로그 경로: {logFilePath}");
    }

    // 빌드된 클라이언트 실행 (창 모드 설정 적용)
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
                Debug.Log($"Client {i} 실행: {clientExe}");
            }
            else
            {
                Debug.LogError($"Client {i} 빌드 파일을 찾을 수 없습니다: {clientExe}");
            }
        }
    }

    [MenuItem("Tools/Build Android (Development)")]
    public static void BuildAndroidDevelopment()
    {
        string[] scenes = GetEnabledScenes();
        string path = "Builds/Android/NumberDuel_Dev.apk";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.Development |          // Development 빌드
                      BuildOptions.AutoRunPlayer |        // 자동 실행
                      BuildOptions.AllowDebugging |       // 디버깅 허용
                      BuildOptions.ConnectWithProfiler    // Profiler 자동 연결
        };

        BuildPipeline.BuildPlayer(options);
        Debug.Log($"🔧 Development 빌드 완료: {path}");
        Debug.Log("📱 Android Logcat을 열어 실시간 로그를 확인하세요!");
        Debug.Log("   Window > Analysis > Android Logcat");
    }

    [MenuItem("Tools/Build Android (Release)")]
    public static void BuildAndroidRelease()
    {
        string[] scenes = GetEnabledScenes();
        string path = "Builds/Android/NumberDuel.apk";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.Android,
            options = BuildOptions.None // 최적화 빌드
        };

        BuildPipeline.BuildPlayer(options);
        Debug.Log($"🚀 Release 빌드 완료: {path}");
    }
}