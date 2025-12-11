using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Scene Management/Quit Game")]
    public class QuitApplication : MonoBehaviour
    {
        public bool QuitOnAwake;
        void Awake()
        {
            if (QuitOnAwake) Application.Quit();
        }
        public void _QuitApp()
        {
            Application.Quit();
        }
    }
}
