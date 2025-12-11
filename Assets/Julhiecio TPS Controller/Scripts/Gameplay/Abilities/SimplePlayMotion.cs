using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.InputEvents;
using JUTPSEditor.JUHeader;

namespace JUTPS.ActionScripts
{
    [AddComponentMenu("JU TPS/Third Person System/Actions/Simple Play Motion")]
    public class SimplePlayMotion : JUTPSActions.JUTPSAnimatedAction
    {
        [JUHeader("Animation Parameter")]
        public ActionPart TargetLayer = ActionPart.FullBody;
        public string AnimatorStateName = "";
        [Range(0, 1)]
        public float StartMotionAt = 0;
        public InputEvent InputToCallAction;
        [JUHeader("Options")]
        public bool ForceFireMode;
        public bool ForceNoFireMode;
        public bool BlockCharacterLocomotion;
        public bool StartActionEvenStateIsPlaying;
        private void Start()
        {
            SwitchAnimationLayer(TargetLayer);
            InputToCallAction.SetupListeners();
            InputToCallAction.OnInputPerformed.AddListener(TryStartAction);
        }
        public void TryStartAction()
        {
            if (IsActionPlaying && StartActionEvenStateIsPlaying == false) return;
            StartAction();
            PlayAnimation(AnimatorStateName, GetCurrentAnimationLayer(), StartMotionAt);
        }
        public override void OnActionStarted()
        {
            if (BlockCharacterLocomotion)
            {
                TPSCharacter.disableMove();
            }
        }
        public override void OnActionEnded()
        {
            if (BlockCharacterLocomotion)
            {
                TPSCharacter.enableMove();
            }
        }

        void LateUpdate()
        {
            if (IsActionPlaying == false) return;
            if (ForceFireMode) TPSCharacter.FiringMode = true;
            if (ForceNoFireMode) { TPSCharacter.FiringMode = false; }
        }
    }

}