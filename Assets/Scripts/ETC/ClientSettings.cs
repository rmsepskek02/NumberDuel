using System;
using System.IO;
using UnityEngine;

[Serializable]
public class ClientSettings
{
    public string ClientID;
    public int ScreenWidth;
    public int ScreenHeight;

    // �⺻���� ����� ������
    public ClientSettings()
    {
        ClientID = "DefaultClient";
        ScreenWidth = 1280;
        ScreenHeight = 720;
    }

    // ����� ���� ���� �ִ� ������
    public ClientSettings(string clientID, int screenWidth, int screenHeight)
    {
        ClientID = clientID;
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
    }
}
