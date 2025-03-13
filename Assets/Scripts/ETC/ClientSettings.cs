using System;
using System.IO;
using UnityEngine;

[Serializable]
public class ClientSettings
{
    public string ClientID;
    public int ScreenWidth;
    public int ScreenHeight;

    // 기본값이 적용된 생성자
    public ClientSettings()
    {
        ClientID = "DefaultClient";
        ScreenWidth = 1280;
        ScreenHeight = 720;
    }

    // 사용자 정의 값이 있는 생성자
    public ClientSettings(string clientID, int screenWidth, int screenHeight)
    {
        ClientID = clientID;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
    }
}
