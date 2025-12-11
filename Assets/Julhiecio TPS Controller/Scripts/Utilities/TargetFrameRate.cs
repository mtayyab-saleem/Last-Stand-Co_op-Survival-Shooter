using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.Utilities
{
    public class TargetFrameRate : MonoBehaviour
    {
        public int TargetFPS = -1;
        void Start()
        {
            Application.targetFrameRate = TargetFPS;
        }
    }
}