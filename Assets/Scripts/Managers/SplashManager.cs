using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Manager
{
    /// <summary>
    /// Splash ȭ���� �����ϴ� �Ŵ���
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
