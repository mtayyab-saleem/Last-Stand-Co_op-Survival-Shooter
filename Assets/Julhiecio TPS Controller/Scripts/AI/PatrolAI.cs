using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace JUTPS.AI
{
    [AddComponentMenu("JU TPS/AI/Patrol AI")]
    public class PatrolAI : JUCharacterArtificialInteligenceBrain
    {
        private CharacterBrain.JUCharacterBrain targetJuCharacter;

        private Transform currentTarget;

        private float distanceFromDestination;
        private Vector3 fieldViewPosition;

        private Vector3 smoothedTargetPosition;
        private Vector3 closestWalkablePosition;
        private Vector3 lastVisiblePosition;

        [Header("Follow Settings")]
        public string[] TargetTags = new string[] { "Player" };
        public FieldView FieldOfView = new FieldView(10, 60);
        public LayerMask SensorLayerMask;
        public float StartRunningAtDistance = 5;
        public float StopDistance = 3;

        [Header("Attack Settings")]
        public float AttackAtDistance = 15;
        public float AimUpOffset = 1;
        public float LookTargetSpeed = 5;
        public float AttackDuration = 1.5f;
        public float MinTimeToAttack = 1, MaxTimeToAttack = 2;

        private float currentTimeToAttack;
        private float currentMaxTimeToAttack;
        private float currentAttackDuration;
        private bool isAttacking;

        private bool isCurrentTargetAttackable;
        private bool currentTargetIsVisible;
        private bool isRunning;
        private float currentTimeToDisableFireMode;


        [Space(10)]
        public UnityEvent _OnFollowWaypoint;
        private bool FollowingWaypoint = false;
        public UnityEvent _OnFollowAIPath;
        private bool FollowingAIPath = false;
        public UnityEvent _OnSeeTarget;
        private bool SawATarget = false;
        public UnityEvent _OnStopSeeingTarget;
        private bool StoppedSeeingTarget;


        private void Start()
        {
            //Timed Tartget Check
            InvokeRepeating(nameof(CheckTargets), 0.5f, 0.5f);
        }

        protected virtual void Update()
        {
            if (character.IsDead) { this.enabled = false; return; }
            
            //Get Targets Every Frame
            //CheckTargets();

            //Default events check
            CheckEndEvents();

            //Running check
            isRunning = (distanceFromDestination > StartRunningAtDistance);

            // >> Follow Target
            if (currentTarget != null)
            {
                Debug.DrawLine(fieldViewPosition, smoothedTargetPosition, Color.green);
                HuntTheTargetState();

                //Attack
                if (distanceFromDestination < AttackAtDistance && currentTargetIsVisible && isCurrentTargetAttackable)
                {
                    EnterAttackModeState();
                }
                else
                {
                    ExitAttackModeState();
                }
            }
            // >>> Follow waypoint path
            else
            {
                Debug.DrawLine(fieldViewPosition, smoothedTargetPosition, Color.red);

                if (WaypointPath != null)
                {
                    //Patrol Mode
                    if (character.FiringMode)
                    {
                        if (distanceFromDestination > StopDistance)
                        {
                            FollowAIPathState(closestWalkablePosition, isRunning);
                        }
                        else
                        {
                            if (distanceFromDestination < StopDistance / 2)
                            {
                                FollowAIPathState(transform.position - transform.forward * 5, true);
                            }
                            else
                            {
                                IdleState();
                            }
                        }

                        //Disable Firing Mode in 2 seconds
                        currentTimeToDisableFireMode += Time.deltaTime;
                        if (currentTimeToDisableFireMode > 2 && character.FiringMode)
                        {
                            ExitAttackModeState();
                        }

                        character.LookAtPosition = smoothedTargetPosition + Vector3.up * AimUpOffset;
                    }
                    else
                    {
                        if (Vector3.Distance(transform.position, WaypointPath.GetWaypointCenter()) < 15)
                        {
                            FollowWaypointPathState(isRunning);
                        }
                        else
                        {
                            FollowAIPathState(WaypointPath.WaypointPathPositions[0], isRunning);
                        }
                    }
                }
                else
                {
                    if (Destination == Vector3.zero)
                    {
                        IdleState();
                    }
                    else
                    {
                        if (distanceFromDestination < StopDistance / 2 || PathToDestination.Length == 0)
                        {
                            IdleState();
                        }
                        else
                        {
                            FollowAIPathState(closestWalkablePosition, isRunning);
                        }
                    }
                }
            }


            //Update target visibility
            UpdateCurrentTarget();
        }

        public void CheckTargets()
        {
            //Check targets
            Vector3 fieldViewPosition = transform.position + transform.up * (AimUpOffset + 0.2f);
            Collider[] targets = FieldOfView.CheckViewCollider(fieldViewPosition, transform.forward, SensorLayerMask, viewerToIgnore: this.gameObject);

            
            //Target Selection
            if (targets.Length > 0)
            {
                currentTarget = SelectTargetOnList(targets, TargetTags);

                if (currentTarget == null)
                {
                    targetJuCharacter = null;
                }
                else
                {
                    if (targetJuCharacter != null)
                    {
                        isCurrentTargetAttackable = !targetJuCharacter.IsDead;
                        if (currentTarget != targetJuCharacter.transform)
                        {
                            if (currentTarget.TryGetComponent(out JUTPS.CharacterBrain.JUCharacterBrain character)) targetJuCharacter = character;
                        }
                    }
                    else
                    {
                        isCurrentTargetAttackable = false;
                        if (currentTarget.TryGetComponent(out JUTPS.CharacterBrain.JUCharacterBrain character)) targetJuCharacter = character;
                    }
                }
            }
            




            //Update visibility
            currentTargetIsVisible = (targetJuCharacter == null) ? FieldOfView.IsVisibleToThisFieldOfView(currentTarget, fieldViewPosition, transform.forward, SensorLayerMask, TagsToConsiderVisible: TargetTags) : FieldOfView.IsVisibleToThisFieldOfView(targetJuCharacter.HumanoidSpine, fieldViewPosition, transform.forward, SensorLayerMask, TagsToConsiderVisible: TargetTags);
        }

        public void UpdateCurrentTarget()
        {
            //Update events
            if (currentTarget != null && SawATarget == false)
            {
                _OnSeeTarget.Invoke();
                SawATarget = true;
                StoppedSeeingTarget = false;
            }
            if (currentTarget == null && StoppedSeeingTarget == false && SawATarget == true)
            {
                _OnStopSeeingTarget.Invoke();
                StoppedSeeingTarget = true;
                SawATarget = false;
            }

            //Get Distance
            //distanceFromDestination = (currentTarget != null) ? Vector3.Distance(transform.position, currentTarget.position) : Vector3.Distance(transform.position, PathToDestination[CurrentWayPointToFollow]);
            //distanceFromDestination = (currentTarget != null) ? Vector3.Distance(transform.position, currentTarget.position) : Vector3.Distance(transform.position, Destination);

            if (CurrentWayPointToFollow > PathToDestination.Length - 1)
            {
                distanceFromDestination = (currentTarget != null) ? Vector3.Distance(transform.position, currentTarget.position) : Vector3.Distance(transform.position, Destination);
            }
            else
            {
                distanceFromDestination = (currentTarget != null) ? Vector3.Distance(transform.position, currentTarget.position) : Vector3.Distance(transform.position, PathToDestination[CurrentWayPointToFollow]);
            }
            //Smooth target position
            smoothedTargetPosition = (currentTarget != null && targetJuCharacter == null) ? Vector3.Lerp(smoothedTargetPosition, currentTarget.position, LookTargetSpeed * Time.deltaTime) : smoothedTargetPosition;

            //Update Visibility
            if (currentTarget == null) currentTargetIsVisible = false;

            //Target position in ju character are humanoid spine 
            if (targetJuCharacter != null) smoothedTargetPosition = Vector3.Lerp(smoothedTargetPosition, targetJuCharacter.HumanoidSpine.position - targetJuCharacter.transform.up * AimUpOffset, LookTargetSpeed * Time.deltaTime);

            //Get nearby position, this line avoid bugs with Navmesh Obstacles
            closestWalkablePosition = JUPathFinder.GetClosestWalkablePoint(currentTarget != null ? currentTarget.position : closestWalkablePosition);

            //Update last visible position
            if (currentTargetIsVisible) lastVisiblePosition = closestWalkablePosition;

            //Stop Following
            if (currentTarget != null)
            {
                if (distanceFromDestination > 2 * FieldOfView.Radious)
                {
                    currentTarget = null;
                }
            }
        }


        public void HuntTheTargetState()
        {
            //On player hide
            if(StoppedSeeingTarget && currentTargetIsVisible == false)
            {
                FollowAIPathState(lastVisiblePosition, isRunning);
                return;
            }
            //Hunt player
            if (currentTargetIsVisible && SawATarget)
            {
                FollowAIPathState(closestWalkablePosition, isRunning);
            }
            else
            {
                //Return to patrol state
                FollowWaypointPathState(isRunning);
            }
        }
        public void FollowAIPathState(Vector3 Position, bool Run)
        {
            GoToPosition(Position, DistanceToFinishOnePoint, Run);
            JUPathFinder.VisualizePath(PathToDestination);
            //Change End Event
            OnEndPath = WaypointPath.OnEndPathAction.Stop;

            //Update Events
            if (FollowingAIPath == false)
            {
                _OnFollowAIPath.Invoke();
                FollowingAIPath = true;
            }
            FollowingWaypoint = false;
        }
        public void FollowWaypointPathState(bool Run)
        {
            if (WaypointPath == null) { return; }

            FollowCurrentWaypoint(Run);
            //Change End Event
            OnEndPath = WaypointPath.OnEndPathAction.ReversePath;

            //Update Events
            if (FollowingWaypoint == false)
            {
                _OnFollowWaypoint.Invoke();
                FollowingWaypoint = true;
            }
            FollowingAIPath = false;
        }


        public void EnterAttackModeState()
        {
            currentTimeToDisableFireMode = 0;
            if (isCurrentTargetAttackable == false)
            {
                isAttacking = false;
                currentMaxTimeToAttack = 0;
                currentAttackDuration = 0;
            }

            character.LookAtPosition = smoothedTargetPosition + Vector3.up * AimUpOffset;
            character.FiringMode = true;
            character.FiringModeIK = true;

            //Attacking Timer
            if (isAttacking == false)
            {
                //Get random time to init attack
                if (currentMaxTimeToAttack == 0) { currentMaxTimeToAttack = Random.Range(MinTimeToAttack, MaxTimeToAttack); }

                //Timer count
                currentTimeToAttack += Time.deltaTime;

                //Init Attack
                if (currentTimeToAttack >= currentMaxTimeToAttack) { currentTimeToAttack = 0; isAttacking = true; }

                character.DefaultUseOfAllItems(false, false);
            }

            //Attacking
            if (isAttacking && currentAttackDuration < AttackDuration)
            {
                //Do attack
                if (Vector3.Dot(smoothedTargetPosition - transform.position, currentTarget.position - transform.position) > 0.8f)
                {
                    character.DefaultUseOfAllItems(true, true, false, false, false, true);
                }
                //Attack duration timer count
                currentAttackDuration += Time.deltaTime;

                //End attacking ant init next attack timer
                if (currentAttackDuration >= AttackDuration)
                {
                    isAttacking = false;
                    currentMaxTimeToAttack = 0;
                    currentAttackDuration = 0;
                }
            }

        }
        public void ExitAttackModeState()
        {
            character.FiringMode = false;
            character.FiringModeIK = false;
            //currentTimeToDisableFireMode = 0;

            isAttacking = false;
            currentMaxTimeToAttack = 0;
            currentAttackDuration = 0;
        }

#if UNITY_EDITOR

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            FieldView.DrawFieldOfView(transform.position + transform.up * AimUpOffset, transform.forward, FieldOfView);
        }
#endif
    }
}