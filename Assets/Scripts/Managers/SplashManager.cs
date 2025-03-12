using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// Splash 화면을 관리하는 매니저
    /// </summary>
    public class SplahManager : MonoBehaviour
    {
        #region Variables
        #endregion
        // Start is called before the first frame update
        void Start()
        {
            SceneManager.LoadScene("JoinScene");
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
