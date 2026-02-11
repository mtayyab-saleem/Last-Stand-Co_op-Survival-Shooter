using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using JUTPS;

namespace JUTPSActions
{
    public class JUTPSAction : MonoBehaviour
    {
        protected JUCharacterController TPSCharacter;
        protected Animator anim;
        protected Rigidbody rb;
        protected Collider coll;
        protected Camera cam;
        public virtual void Awake()
        {
            TPSCharacter = GetComponent<JUCharacterController>();
            rb = GetComponent<Rigidbody>();
            anim = GetComponent<Animator>();
            coll = GetComponent<Collider>();
            GetCamera();
            Invoke(nameof(GetCamera), 0.001f);
        }
        private void GetCamera()
        {
            if (TPSCharacter != null)
            {
                cam = (TPSCharacter.MyPivotCamera != null) ? TPSCharacter.MyPivotCamera.mCamera : null;
            }
        }
    }
    public class JUTPSAnimatedAction : JUTPSAction
    {
        private int ActionCurrentLayerIndex = 5;
        protected float LayerWeight;

        public float ActionDuration;
        [HideInInspector] public float ActionCurrentTime;

        public float EnterTransitionSpeed;
        public float ExitTransitionSpeed;

        [SerializeField]protected StateOfAction ActionState;

        protected bool NoneAction = true, ActionStarted, IsActionPlaying, ActionEnded;

        public enum ActionPart { RightArm, BothArms, FullBody, Legs, Torso }
        protected enum StateOfAction { None, Started, Playing, Ended }

        public void StartAction()
        {
            ActionStarted = true;
            ActionCurrentTime = 0;
        }

        protected void Action()
        {
            //ANIMATION LAYER
            anim.SetLayerWeight(ActionCurrentLayerIndex, LayerWeight);

            if (!ActionStarted)
            {
                ActionState = StateOfAction.None;
                LayerWeight = Mathf.MoveTowards(LayerWeight, 0, ExitTransitionSpeed * Time.deltaTime);
                return;
            }

            //ACTION TIMER
            if (ActionCurrentTime < ActionDuration) ActionCurrentTime += Time.deltaTime;

            //LAYER WEIGHT 
            switch (ActionState) {
                case StateOfAction.Started:
                    LayerWeight = Mathf.MoveTowards(LayerWeight, 0, ExitTransitionSpeed * Time.deltaTime);
                    break;
                case StateOfAction.Playing:
                LayerWeight = Mathf.MoveTowards(LayerWeight, 1, EnterTransitionSpeed * Time.deltaTime);
                    break;
                case StateOfAction.Ended:
                    LayerWeight = Mathf.MoveTowards(LayerWeight, 0, ExitTransitionSpeed * Time.deltaTime);
                    break;
            }

            //STARTED STATE
            if (ActionCurrentTime > 0 && IsActionPlaying == false)
            {
                ActionState = StateOfAction.Started;
                IsActionPlaying = true;
                OnActionStarted();

                //Debug.Log("Started State");
            }
            //PLAYING STATE
            if (ActionCurrentTime > 0.001f && ActionCurrentTime < ActionDuration)
            {
                ActionState = StateOfAction.Playing;
                IsActionPlaying = true;
                NoneAction = false;
                OnActionIsPlaying();

                //Debug.Log("Playing State: " + ActionCurrentTime + " | Layer Weight: " + LayerWeight);
            }

            //ENDED STATE
            if (ActionCurrentTime > ActionDuration && ActionEnded == false)
            {
                ActionState = StateOfAction.Ended;
                ActionEnded = true;
                ActionCurrentTime = 0;
                //Debug.Log("Ended State");
                OnActionEnded();
            }

            //NONE STATE
            if (ActionCurrentTime == 0 && ActionEnded == true)
            {
                ActionState = StateOfAction.None;

                ActionStarted = false;
                IsActionPlaying = false;
                ActionEnded = false;
                NoneAction = true;
                ActionCurrentTime = 0;
                OnNoAction();

                //Debug.Log("None State");
            }
        }

        /// <summary>
        /// This is an empty void, here you must write the conditions to call the action.
        /// <para>○ Example: </para>
        /// <para>__________________________________________________</para>
        /// <para>                                                  </para>
        /// <para> public override void Actioncondition()           </para>
        /// <para> {                                                </para>
        /// <para>______//Call flying action for example            </para>
        /// <para>______if(character.IsGrounded) { StartAction(); }      </para>
        /// <para> }                                                </para>
        /// </summary>
        public virtual void ActionCondition()
        {

        }

        public virtual void OnActionStarted() { }
        public virtual void OnActionIsPlaying() { }
        public virtual void OnActionEnded() { }
        public virtual void OnNoAction() { }


        public virtual void Update()
        {
            ActionCondition();
            Action();
        }

        //Locomotion and animation
        protected void PlayAnimation(string AnimationStateName, int LayerID = -1, float normalizedTime = 0)
        {
            if (LayerID > -1)
            {
                anim.Play(AnimationStateName, LayerID, normalizedTime);
            }
            else
            {
                anim.Play(AnimationStateName, ActionCurrentLayerIndex, normalizedTime);
            }
        }
        protected void SwitchAnimationLayer(ActionPart BodyPartLayer)
        {
            ActionCurrentLayerIndex = ((int)BodyPartLayer) + 7;
        }
        protected void SwitchAnimationLayer(int LayerIndex)
        {
            ActionCurrentLayerIndex = LayerIndex;
        }

        protected int GetCurrentAnimationLayer()
        {
            return ActionCurrentLayerIndex;
        }
        
        protected void DisableCharacterMovement(float duration = 0)
        {
            if (TPSCharacter == null) return;

            TPSCharacter.DisableLocomotion(duration);
        }

        //Item Manegement
        protected int LasUsedItemID;
        protected void SetCurrentItemIndexToLastUsedItem()
        {
            if (TPSCharacter.HoldableItemInUseRightHand != null)
            {
                //LasUsedItemID = JUTPS.InventorySystem.JUInventory.GetGlobalItemSwitchID(TPSCharacter.HoldableItemInUseRightHand, TPSCharacter.Inventory);
                LasUsedItemID = TPSCharacter.HoldableItemInUseRightHand.ItemSwitchID;
            }
            else
            {
                LasUsedItemID = -1;
            }
        }
        protected void DisableItemOnHand()
        {
            //Get Current Item
            TPSCharacter.SwitchToItem(0);
        }
        protected void EnableLastUsedItem()
        {
            TPSCharacter.SwitchToItem(LasUsedItemID);
        }
    }
}

