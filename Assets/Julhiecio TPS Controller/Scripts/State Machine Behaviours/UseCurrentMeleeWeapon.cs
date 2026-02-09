using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.Events;

namespace JUTPS.AnimatorStateMachineBehaviours
{
    public class UseCurrentMeleeWeapon : StateMachineBehaviour
    {
        [Range(0, 1)]
        public float StartUsing = 0.15f;
        [Range(0, 1)]
        public float StopUsing = 0.8f;

        private JUTPS.CharacterBrain.JUCharacterBrain Controller;
        [HideInInspector] public bool UsingMeleeWeapon;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            UsingMeleeWeapon = false;

            if (Controller == null)
            {
                Controller = animator.gameObject.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
            }

            if (Controller == null)
            {
                Debug.LogError("the use of the melee weapon was not possible, could not find a JU Controller");
            }
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller.MeleeWeaponInUseRightHand == null && Controller.MeleeWeaponInUseLeftHand == null) return;

            Controller.ResetDefaultLayersWeight(LegLayerException: Controller.FiringMode);
            Controller.LeftHandWeightIK = 0;
            Controller.RightHandWeightIK = 0;

            if (stateInfo.normalizedTime > StartUsing && stateInfo.normalizedTime < StopUsing && UsingMeleeWeapon == false)
            {
                if (Controller.MeleeWeaponInUseRightHand) { Controller.MeleeWeaponInUseRightHand.UseItem(); }
                if (Controller.MeleeWeaponInUseLeftHand) { Controller.MeleeWeaponInUseLeftHand.UseItem(); }

                UsingMeleeWeapon = true;
            }

            if (stateInfo.normalizedTime > StopUsing && UsingMeleeWeapon == true)
            {
                if (Controller.MeleeWeaponInUseRightHand) { Controller.MeleeWeaponInUseRightHand.StopUseItem(); }
                if (Controller.MeleeWeaponInUseLeftHand) { Controller.MeleeWeaponInUseLeftHand.StopUseItem(); }

                UsingMeleeWeapon = false;
            }
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller.MeleeWeaponInUseRightHand) { Controller.MeleeWeaponInUseRightHand.StopUseItem(); }
            if (Controller.MeleeWeaponInUseLeftHand) { Controller.MeleeWeaponInUseLeftHand.StopUseItem(); }
        }
    }
}