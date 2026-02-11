using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.Events;
namespace JUTPS.AnimatorStateMachineBehaviours
{
    public class ForceNOFiringMode : StateMachineBehaviour
    {
        private JUTPS.CharacterBrain.JUCharacterBrain Controller;

        public bool BlockFireMode = false;
        public bool BlockFireModeIK = true;
        public bool EnableOnEndTransition = false;
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller == null)
            {
                Controller = animator.gameObject.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
            }

            if (Controller == null)
            {
                Debug.LogError("could not find a JU Controller");
                return;
            }

            if (BlockFireMode) Controller.FiringMode = false;

            if (BlockFireModeIK) Controller.FiringModeIK = false;
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (Controller == null)
            {
                Debug.LogError("could not find a JU Controller");
                return;
            }
            if (EnableOnEndTransition)
            {
                if (BlockFireMode) Controller.FiringMode = true;

                if (BlockFireModeIK) Controller.FiringModeIK = true;
            }
        }
    }
}
