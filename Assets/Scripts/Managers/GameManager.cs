using UnityEngine;
using System.IO;
using Utills;
using Manager;

public class GameManager : SingletonDontDestroy<GameManager>
{
    private string clientFolder;
    private string settingsFile = "ClientSettings.txt"; // JSON 저장 파일명

    private void Start()
    {
        // 클라이언트 폴더명 결정 (빌드된 클라이언트인지 확인)
        clientFolder = GetClientFolderName();
    }

    private void OnApplicationQuit()
    {
        // 현재 해상도를 저장
        SaveClientSettings();
    }

    /// <summary>
    /// 현재 실행 중인 클라이언트 폴더 이름 가져오기
    /// </summary>
    private string GetClientFolderName()
    {
        // 실행 파일이 위치한 폴더 가져오기
        string exeFolder = Path.GetDirectoryName(Application.dataPath);

        // 실행 파일 이름 가져오기 (예: Client1.exe -> Client1)
        string exeName = Path.GetFileNameWithoutExtension(exeFolder);

        // 최종적으로 Client1\Client1_Data 형식의 폴더 경로 반환
        return exeName;
    }

    /// <summary>
    /// 현재 해상도를 저장하여 ClientSettings.txt 업데이트
    /// </summary>
    private void SaveClientSettings()
    {
        // 현재 해상도 가져오기
        int width = Screen.width;
        int height = Screen.height;

        // 기존 설정 불러오기 (ClientID 유지)
        ClientSettings settings = JsonUtils.LoadFromFile<ClientSettings>(clientFolder, settingsFile);
        settings.ScreenWidth = width;
        settings.ScreenHeight = height;

        // 저장
        JsonUtils.SaveToFile(settings, clientFolder, settingsFile);
        Debug.Log($"Updated Resolution Saved: {width} x {height} in {clientFolder}");
    }
}
