using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.JUInputSystem;

namespace JUTPS.ActionScripts
{

    [AddComponentMenu("JU TPS/Third Person System/Additionals/Sidescroller Locomotion")]
    public class SidescrollerLocomotion : JUTPSActions.JUTPSAction
    {
        public bool BlockHorizontalLocomotion = true;
        public bool UseVerticalInputToCrouch = true;
        public bool BlockZPosition = true;

        private float startZPosition;
        private void Start()
        {
            //startZPosition = transform.position.z;
        }
        private void Update()
        {
            if (BlockHorizontalLocomotion)
            {
                TPSCharacter.BlockVerticalInput = true;
            }

            if (BlockZPosition)
            {
                Vector3 velocity = rb.linearVelocity; velocity.z = 0;
                rb.linearVelocity = velocity;

                transform.position = new Vector3(transform.position.x, transform.position.y, startZPosition);
            }

            if (UseVerticalInputToCrouch == false) return;
            //Crouch
            if (JUInput.GetAxis(JUInput.Axis.MoveVertical) < -0.2f && TPSCharacter.IsCrouched == false)
            {
                TPSCharacter.IsCrouched = true;
            }
            if (JUInput.GetAxis(JUInput.Axis.MoveVertical) > 0.2f && TPSCharacter.IsCrouched == true)
            {
                TPSCharacter.IsCrouched = false;
            }
        }
    }

}