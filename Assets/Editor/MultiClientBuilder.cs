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

            // 실행 시 창 모드 적용하는 설정 파일 생성
            CreateWindowedConfig(clientPath);
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

    // 창 모드 적용하는 설정 파일 생성
    private static void CreateWindowedConfig(string clientPath)
    {
        string configPath = Path.Combine(clientPath, "windowed.txt");
        File.WriteAllText(configPath, $"-screen-width {1280} -screen-height {720} -screen-fullscreen 0");
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
