using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.Events;
namespace JUTPS.AnimatorStateMachineBehaviours
{
    public class ForceNOFootPlacement : StateMachineBehaviour
    {
        private JUFootPlacement FootPlacer;
        public bool EnableOnEndTransition = true;
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (FootPlacer == null)
            {
                FootPlacer = animator.gameObject.GetComponent<JUFootPlacement>();
            }

            if (FootPlacer == null)
            {
                Debug.Log("State Machine Behaviour trying to access JU Foot Placement but could not find it, if you dont want to use JU Foot Placement you can ignore this message or remove this State Machine Behaviour");
                return;
            }
        }
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (FootPlacer == null) return;

            FootPlacer.enabled = false;
            
        }
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (FootPlacer == null) return;

            if (EnableOnEndTransition)
            {
                FootPlacer.enabled = true;
            }
        }
    }
}
