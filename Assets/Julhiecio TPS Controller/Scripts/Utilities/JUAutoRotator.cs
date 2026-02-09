using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPSEditor.JUHeader;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/Auto Rotator")]
    public class JUAutoRotator : MonoBehaviour
    {
        [JUHeader("Auto Rotate")]
        public float Speed = 1;
        public Vector3 RotateAxis;
        public Space Space;
        void Update()
        {
            transform.Rotate(RotateAxis * Speed * Time.deltaTime, Space);
        }
    }
}
