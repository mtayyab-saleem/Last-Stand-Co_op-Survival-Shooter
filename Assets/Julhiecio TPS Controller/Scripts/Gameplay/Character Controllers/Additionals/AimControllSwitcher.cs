using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.ActionScripts;
using JUTPS.JUInputSystem;

namespace JUTPS.CrossPlataform
{

    public class AimControllSwitcher : MonoBehaviour
    {
        public AimOnMousePosition MouseLooker;
        public AimOnRightJoystickDirection JoystickLooker;

        void Update()
        {
            if (JUInputManager.IsUsingGamepad == false && JUGameManager.IsMobileControls == false)
            {
                MouseLooker.enabled = true;
                JoystickLooker.enabled = false;
            }
            else
            {
                MouseLooker.enabled = false;
                JoystickLooker.enabled = true;
            }
        }
    }

}
