using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.Events;
namespace JUTPS.AnimatorStateMachineBehaviours
{
    public class JUAnimationEvent : StateMachineBehaviour
    {
        public enum JUAnimDefaultEvents
        {
            None, ReloadRightHandWeapon, ReloadLeftHandWeapon,
            EmitBulletShell, DisableMovement, EnableMovement,
            DisableRotation, EnableRotation, DisableFireModeIK,
            EnableFireModeIK, StopRolling, StartRolling,
            ThrowItem
        };

        public JUAnimDefaultEvents DefaultEvent = JUAnimDefaultEvents.None;
        [Range(0, 1)]
        public float Duration;
        public string AnimationEventName = "Custom Animation Event";
        public float Delay = 0;
        //public bool DefaultAnimationEventFromJUController = false;

        private JUTPS.CharacterBrain.JUCharacterBrain Controller;
        [HideInInspector] public bool CalledAnimationEvent;

        // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            CalledAnimationEvent = false;
        }

        // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
        override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            //Debug.Log("Animation State Normalized Time: " + stateInfo.normalizedTime);
            if (stateInfo.normalizedTime >= Duration && CalledAnimationEvent == false)
            {
                if (DefaultEvent != JUAnimDefaultEvents.None)
                {
                    if (Controller == null)
                    {
                        Controller = animator.gameObject.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
                    }

                    if (Controller == null)
                    {
                        Debug.LogError("The [JU Animation Event] of [Animation State: " + stateInfo + "] is a default JUController Animation Event, but could not find a JU Controller");
                    }

                    //Controller.Invoke(AnimationEventName, Delay);
                    CallDefaultEvent(DefaultEvent, Controller);
                }
                else
                {
                    JUAnimationEventReceiver receiver = animator.gameObject.GetComponent<JUAnimationEventReceiver>();

                    if (receiver != null)
                    {
                        CallCustomEvent(AnimationEventName, receiver);
                    }
                    else
                    {
                        Debug.LogError("[Default Event : " + DefaultEvent.ToString() + "] " + "There is no JU Animation Event Receiver on GameObject '" + animator.gameObject.name + "', if you wanted to create a custom Animation Event, add the component 'JUAnimationEventReceiver'");
                    }
                }
                CalledAnimationEvent = true;
            }
        }
        public static void CallDefaultEvent(JUAnimDefaultEvents DefaultEvent, JUTPS.CharacterBrain.JUCharacterBrain TargetController)
        {
            switch (DefaultEvent)
            {
                case JUAnimDefaultEvents.None:
                    Debug.LogWarning("None Event To Call");
                    break;
                case JUAnimDefaultEvents.ReloadRightHandWeapon:
                    TargetController.reloadRightHandWeapon();
                    break;
                case JUAnimDefaultEvents.ReloadLeftHandWeapon:
                    TargetController.reloadLeftHandWeapon();
                    break;
                case JUAnimDefaultEvents.EmitBulletShell:
                    TargetController.emitBulletShell();
                    break;
                case JUAnimDefaultEvents.DisableMovement:
                    TargetController.disableMove();
                    break;
                case JUAnimDefaultEvents.EnableMovement:
                    TargetController.enableMove();
                    break;
                case JUAnimDefaultEvents.DisableRotation:
                    TargetController.disableRotation();
                    break;
                case JUAnimDefaultEvents.EnableRotation:
                    TargetController.enableRotation();
                    break;
                case JUAnimDefaultEvents.DisableFireModeIK:
                    TargetController.disableFireModeIK();
                    break;
                case JUAnimDefaultEvents.EnableFireModeIK:
                    TargetController.enableFireModeIK();
                    break;
                case JUAnimDefaultEvents.StopRolling:
                    TargetController.stopRolling();
                    break;
                case JUAnimDefaultEvents.StartRolling:
                    TargetController.startRolling();
                    break;
                case JUAnimDefaultEvents.ThrowItem:
                    TargetController._ThrowCurrentThrowableItem();
                    break;
            }
        }
        public static void CallCustomEvent(string EventName, JUAnimationEventReceiver Receiver)
        {
            Receiver.CallEvent(EventName);
        }
    }
}