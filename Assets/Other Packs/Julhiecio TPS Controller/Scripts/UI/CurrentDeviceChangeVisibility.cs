using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.JUInputSystem;

namespace JUTPS.CrossPlataform
{
    public class CurrentDeviceChangeVisibility : MonoBehaviour
    {
        public GameObject KeyboardObject, GamepadObject;

        void LateUpdate()
        {
            GamepadObject.SetActive(JUInputManager.IsUsingGamepad);
            KeyboardObject.SetActive(!JUInputManager.IsUsingGamepad);
        }
    }
}