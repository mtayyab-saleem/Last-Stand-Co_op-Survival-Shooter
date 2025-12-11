using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/Auto Destroy")]

    public class AutoDestroy : MonoBehaviour
    {
        public float SecondsToDestroy;
        public bool DestroyOnStart = true;
        void Start()
        {
            if (DestroyOnStart) TimedDestroyObject();
        }
        public void TimedDestroyObject()
        {
            Destroy(this.gameObject, SecondsToDestroy);
        }
    }
}