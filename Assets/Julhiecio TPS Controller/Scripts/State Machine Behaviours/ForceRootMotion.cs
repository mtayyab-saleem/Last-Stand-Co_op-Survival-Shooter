using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.Events;
namespace JUTPS.AnimatorStateMachineBehaviours
{
    public class ForceRootMotion : StateMachineBehaviour
    {
        private JUTPS.CharacterBrain.JUCharacterBrain Controller;

        public bool ForceRootMotionRotation = false;

        public bool DisableOnEndTransition = true;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller == null)
            {
                Controller = animator.gameObject.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
            }
            if (Controller == null)
            {
                Debug.LogError("the use of the root motion was not possible, could not find a JU Controller");
                return;
            }

            base.OnStateEnter(animator, stateInfo, layerIndex);
        }
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller == null)
            {
                Controller = animator.gameObject.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
            }

            if (Controller == null)
            {
                Debug.LogError("the use of the root motion was not possible, could not find a JU Controller");
                return;
            }
            if (Vector3.Dot(animator.transform.up, Vector3.up) < 0.8 && Vector3.Dot(animator.transform.up, Vector3.up) > -0.8f)
            {
                Controller.RootMotion = false;
                Controller.RootMotionRotation = false;
                return;
            }
            Controller.RootMotion = true;
            Controller.RootMotionRotation = ForceRootMotionRotation ? true : false;
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller == null)
            {
                Debug.LogError("the use of the root motion was not possible, could not find a JU Controller");
                return;
            }
            if (DisableOnEndTransition)
            {
                Controller.RootMotion = false;
                Controller.RootMotionRotation = false;
            }
        }

    }
}