using Manager;
using System.IO;
using UnityEngine;
using Utills;

namespace Manager
{
    /// <summary>
    /// 클라이언트별 설정을 관리하는 매니저
    /// 각 클라이언트의 화면 해상도 정보를 저장 및 복원
    /// </summary>
    public class GameManager : SingletonDontDestroy<GameManager>
    {
        #region Fields and Properties
        private string clientFolder;
        private string settingsFile = "ClientSettings.txt";
        public ClientSettings clinetSettings;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            clientFolder = GetClientFolderName();
            LoadClientSettings();
        }

        private void OnApplicationQuit()
        {
            SaveClientSettings();
        }
        #endregion

        #region Client Settings Management
        /// <summary>
        /// ClientId 로드
        /// </summary>
        private void LoadClientSettings()
        {
            clinetSettings = JsonUtils.LoadFromFile<ClientSettings>(clientFolder, settingsFile);
        }

        /// <summary>
        /// 현재 실행 중인 클라이언트 폴더 이름 가져오기
        /// </summary>
        private string GetClientFolderName()
        {
            string exeFolder = Path.GetDirectoryName(Application.dataPath);
            string exeName = Path.GetFileNameWithoutExtension(exeFolder);
            return exeName;
        }

        /// <summary>
        /// 현재 해상도를 저장하여 ClientSettings.txt 업데이트
        /// </summary>
        private void SaveClientSettings()
        {
            int width = Screen.width;
            int height = Screen.height;

            clinetSettings.ScreenWidth = width;
            clinetSettings.ScreenHeight = height;

            JsonUtils.SaveToFile(clinetSettings, clientFolder, settingsFile);
        }
        #endregion
    }
}
