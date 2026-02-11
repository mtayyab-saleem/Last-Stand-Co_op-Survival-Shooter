using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Events;

using JUTPS.AI;
using JUTPS.CharacterBrain;

namespace JUTPS.AI
{
    [System.Serializable]
    public class FieldView
    {
        [Range(0, 500)] public float Radious;
        [Range(0,360)]public float Angle;

        public FieldView(float radious, float angle)
        {
            Radious = radious;
            Angle = angle;
        }
        public Collider[] CheckViewCollider(Vector3 position, Vector3 forward, LayerMask targetMask, GameObject viewerToIgnore = null)
        {
            List<Collider> colliders = Physics.OverlapSphere(position, Radious, targetMask).ToList();

            foreach (Collider col in colliders.ToArray())
            {
                Transform target = col.transform;
                
                Vector3 targetposition = target.position; targetposition.y = position.y;

                Vector3 directionToTarget = (targetposition - position).normalized;

                if (Vector3.Angle(forward, directionToTarget) > Angle / 2 || col.gameObject == viewerToIgnore)
                {
                    colliders.Remove(col);
                }
            }

            return colliders.ToArray();
        }
        public bool IsVisibleToThisFieldOfView(Transform LookedTarget, Vector3 ViewPosition, Vector3 ViewForward, LayerMask LayerMask, float threshold = 0.6f, string[] TagsToConsiderVisible = default(string[]))
        {
            if (LookedTarget == null) return false;

            bool CanSeeTarget = true;
            Vector3 directionToTarget = (LookedTarget.position - ViewPosition).normalized;

            //CAN NOT see the target
            if (Vector3.Angle(ViewForward, directionToTarget) > Angle / 2)
            {
                CanSeeTarget = false;
            }
            else
            {

                float normalDistance = Vector3.Distance(ViewPosition, LookedTarget.position);
                Vector3 lineCastEndPosition = ViewPosition + directionToTarget * normalDistance;

                RaycastHit hit;
                Physics.Linecast(ViewPosition, lineCastEndPosition, out hit, LayerMask);
                if (hit.collider != null)
                {
                    //float distanceToEndPoint = Vector3.Distance(hit.point, lineCastEndPosition);

                    //CAN see the target
                    if (JUCharacterArtificialInteligenceBrain.TagMatches(hit.collider.tag, TagsToConsiderVisible) == false)
                    {
                        //Debug.Log(distanceToEndPoint);
                        Debug.DrawLine(hit.point, ViewPosition, Color.cyan);

                        CanSeeTarget = false;
                    }
                    else
                    {
                        CanSeeTarget = true;
                    }
                }

                /*
                float normalDistance = Vector3.Distance(ViewPosition, LookedTarget.position);
                Vector3 lineCastEndPosition = ViewPosition + directionToTarget * normalDistance;

                RaycastHit hit;
                Physics.Linecast(ViewPosition, lineCastEndPosition, out hit);
                if (hit.collider != null)
                {
                    float distanceToEndPoint = Vector3.Distance(hit.point, lineCastEndPosition);

                    //CAN see the target
                    if (distanceToEndPoint > threshold && JUCharacterArtificialInteligenceBrain.TagMatches(hit.transform.tag, TagsToConsiderVisible) == false)
                    {
                        //Debug.Log(distanceToEndPoint);
                        Debug.DrawLine(hit.point, ViewPosition);

                        CanSeeTarget = false;
                    }
                    else
                    {
                        CanSeeTarget = true;
                    }
                }*/
            }
            return CanSeeTarget;
        }
        public static void DrawFieldOfView(Vector3 position, Vector3 forward, FieldView view)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(position, Vector3.up, view.Radious);

            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.DrawWireArc(position, Vector3.up, forward, view.Angle/2, view.Radious - 0.1f);
            UnityEditor.Handles.DrawWireArc(position, Vector3.up, forward, -view.Angle/2, view.Radious - 0.1f);

            UnityEditor.Handles.color = new Color(1, 0, 0, 0.1f);
            UnityEditor.Handles.DrawSolidArc(position, Vector3.up, forward, view.Angle/2, view.Radious - 0.2f);
            UnityEditor.Handles.DrawSolidArc(position, Vector3.up, forward, -view.Angle/2, view.Radious - 0.2f);
#endif
        }
    }

    public class JUCharacterArtificialInteligenceBrain : MonoBehaviour
    {
        [HideInInspector]public JUCharacterBrain character;

        //Used to update following state
        private Vector3 oldPosition;
        
        [HideInInspector] public int CurrentWayPointToFollow;
        [HideInInspector] public Vector3[] PathToDestination;

        [Header("Destination Settings")]
        public Vector3 Destination;
        public WaypointPath WaypointPath;
        private float RecalculatePathRefreshRate = 1;

        [Header("Character Path Locomotion Settings")]
        public float DistanceToFinishOnePoint = 1f;
        public bool CheckNearestPointOnPath;
        public WaypointPath.OnEndPathAction OnEndPath = WaypointPath.OnEndPathAction.Stop;

        [Header("Events")]
        public UnityEvent OnStartPath;
        public UnityEvent OnFollowing;
        public UnityEvent OnEnded;
        private bool Started, Following, Ended;

        protected FollowState followState = FollowState.None;
        public enum FollowState { None, FollowingPath, FollowingWaypointPath }
        protected virtual void Awake()
        {
            //Get ju character
            character = GetComponent<JUCharacterBrain>();
            if (character != null)
            {
                //Is Artificial Intelligence
                character.IsArtificialIntelligence = true;

                //Disable Default
                if (character is JUCharacterController)
                {
                    (character as JUCharacterController).UseDefaultControllerInput = false;
                }
            }

            //Calculate Path
            RecalculatePath();
            InvokeRepeating("RecalculatePath", RecalculatePathRefreshRate, RecalculatePathRefreshRate);
        }

        public void GoToPosition(Vector3 position, float StoppingDistance = 3, bool running = false)
        {
            if (character.IsArtificialIntelligence == false || character.IsGrounded == false) return;

            followState = FollowState.FollowingPath;

            FollowPath(ref PathToDestination, character, StoppingDistance, ref CurrentWayPointToFollow, running, OnEndPath, CheckNearestPointOnPath);
            SetDestination(position);
            CheckEndEvents();
        }
        public virtual void IdleState()
        {
            character._Move(0, 0, false);
        }
        public void FollowCurrentWaypoint(bool running)
        {
            if (character.IsArtificialIntelligence == false || character.IsGrounded == false) return;

            if (WaypointPath == null) return;

            followState = FollowState.FollowingWaypointPath;
            PathToDestination = WaypointPath.WaypointPathPositions;

            FollowPath(ref PathToDestination, character, DistanceToFinishOnePoint, ref CurrentWayPointToFollow, running, OnEndPath, CheckNearestPointOnPath);
            CheckEndEvents();
        }
        protected void CheckEndEvents()
        {
            // >>> Call State Events
            if (GetCurrentFollowingState() == WaypointUtilities.FollowingState.Started && Started == false)
            {
                OnStartPath.Invoke();
                Started = true;
                Ended = false;
            }
            if (GetCurrentFollowingState() == WaypointUtilities.FollowingState.Following)
            {
                OnFollowing.Invoke();
            }
            Following = (GetCurrentFollowingState() == WaypointUtilities.FollowingState.Following);

            if (GetCurrentFollowingState() == WaypointUtilities.FollowingState.Ended && Ended == false)
            {
                OnEnded.Invoke();
                Following = false;
                Started = false;
                Ended = true;
                followState = FollowState.None;
            }
        }

        public void RecalculatePath()
        {
            if (followState == FollowState.FollowingWaypointPath) { return; }

            PathToDestination = JUPathFinder.CalculatePath(transform.position, Destination);
            WaypointUtilities.DividePath(ref PathToDestination, 2);

            CurrentWayPointToFollow = 0;
        }
        public void SetPathCalculationRefreshRate(float seconds = 1f)
        {
            RecalculatePathRefreshRate = seconds;
            if(IsInvoking("RecalculatePath") == true)
            {
                CancelInvoke("RecalculatePath");
                InvokeRepeating("RecalculatePath", RecalculatePathRefreshRate, RecalculatePathRefreshRate);
            }
        }
        public void SetDestination(Vector3 destination)
        {
            Destination = destination;
        }

        Bounds pathBounds;
        public float GetDistanceFromCurrentWaypoint()
        {
            if (WaypointPath == null) return 0;

            if (WaypointPath.WaypointPathPositions.Length > 0)
            {
                if(pathBounds == null){ pathBounds = new Bounds(WaypointPath.WaypointPathPositions[0], Vector3.zero); }

                foreach (Vector3 waypointCorner in WaypointPath.WaypointPathPositions)
                {
                    pathBounds.Encapsulate(waypointCorner);
                }
                return Vector3.Distance(pathBounds.center, transform.position);

            }
            else
            {
                return 0;
            }
        }
        
        public float GetDistanceFromNextWaypoint()
        {
            return Vector3.Distance(WaypointPath.WaypointPathPositions[CurrentWayPointToFollow], transform.position);
        }
        public static void FollowPath(ref Vector3[] path, JUCharacterBrain juCharacter, float stoppingDistance, ref int currentPathCornerId, bool running = false, WaypointPath.OnEndPathAction onPathEnd = WaypointPath.OnEndPathAction.Stop, bool CheckClosestPoint = false)
        {
            if (juCharacter.IsArtificialIntelligence == false || juCharacter.IsGrounded == false || path.Length == 0) { return; }

            //Reset target waypoint
            if (path.Length - 1 < currentPathCornerId)
            {
                currentPathCornerId = 0;
            }

            // >>> Create parameters to control vehicle
            //Get direction of the target waypoint
            Vector3 TargetDirection = (path[currentPathCornerId] - juCharacter.transform.position);

            //Get direction of the closest waypoint
            Vector3 ClosestPointDirection = (WaypointUtilities.GetClosestPoint(juCharacter.transform.position, path, 1) - juCharacter.transform.position).normalized;
            //Get distance between the vehicle and target waypoint
            float DistanceToNextWaypoint = Vector3.Distance(juCharacter.transform.position, path[currentPathCornerId]);

            // >>> Create Rewrited Inputs
            float HorizontalInput = TargetDirection.x;
            float VerticalInput = TargetDirection.z;
            bool RunningInput = running;

            //Set the next waypoint to follow
            if (DistanceToNextWaypoint < stoppingDistance && currentPathCornerId < path.Length - 1)
            {
                currentPathCornerId++;
            }

            //Check Closest Point of The Path
            if (CheckClosestPoint)
            {
                Vector3 closestWaypoint = WaypointUtilities.GetClosestPoint(juCharacter.transform.position, path);
                int closestCornerID = System.Array.IndexOf(path, closestWaypoint);
                if (Vector3.Distance(juCharacter.transform.position, closestWaypoint) < DistanceToNextWaypoint && closestCornerID > currentPathCornerId && closestCornerID != path.Length - 1)
                {
                    //Debug.Log("Detected Closest Point on the path");
                    currentPathCornerId = closestCornerID;
                }
            }

            // >>> On vehicle reaches at the end of the path
            if (currentPathCornerId == path.Length-1 && DistanceToNextWaypoint < stoppingDistance)
            {
                switch (onPathEnd)
                {
                    case WaypointPath.OnEndPathAction.Stop:
                        VerticalInput = 0;
                        HorizontalInput = 0;
                        RunningInput = false;
                        break;
                    case WaypointPath.OnEndPathAction.ReversePath:
                        System.Array.Reverse(path);
                        currentPathCornerId = 0;
                        break;
                    case WaypointPath.OnEndPathAction.RestartPath:
                        currentPathCornerId = 0;
                        break;
                }
            }

            // >>> Set inputs
            juCharacter._Move(HorizontalInput, VerticalInput, RunningInput);
        }
        public WaypointUtilities.FollowingState GetCurrentFollowingState()
        {
            return WaypointUtilities.GetPathFollowingState(transform, ref oldPosition, PathToDestination, CurrentWayPointToFollow, DistanceToFinishOnePoint);
        }


        public static bool IsAttackable(GameObject target, string[] TargetTags)
        {
            if (target == null) return false;
            
            bool attack = false;
            if (target.TryGetComponent(out JUHealth health))
            {
                if (health.IsDead == false)
                {
                    attack = true;
                }
                else
                {
                    attack = false;
                }
            }
            return attack;
        }
        public static Transform SelectTargetOnList(Collider[] targets, string[] allowedTags)
        {
            Transform target = null;
            List<Transform> aliveTargets = new List<Transform>();

            foreach (Collider col in targets)
            {
                if (TagMatches(col.tag, allowedTags))
                {
                    if (col.TryGetComponent(out JUHealth health))
                    {
                        if (health.Health > 0 && health.IsDead == false)
                        {
                            aliveTargets.Add(health.transform);
                        }
                        else
                        {
                            aliveTargets.Remove(health.transform);
                        }
                    }
                    else
                    {
                        target = col.transform;
                    }
                }
            }
            if(aliveTargets.Count > 0)
            {
                target = aliveTargets[0];
            }
            return target;
        }
        public static bool TagMatches(string targetTag, string[] allowedTags)
        {
            if (targetTag == null || allowedTags == null) return false;

            bool match = false;
            foreach (string tag in allowedTags)
            {
                if (targetTag == tag) match = true;
            }
            return match;

        }

#if UNITY_EDITOR
        private Color randomTargetIndicatorLineColor;
        protected virtual void OnDrawGizmos()
        {
            if (Application.isPlaying == false)
            {
                Gizmos.DrawWireSphere(Destination, 1);
            }
            else
            {
                Gizmos.DrawWireSphere(Destination, 1);

                if (PathToDestination.Length - 1 < CurrentWayPointToFollow) return;

                if (randomTargetIndicatorLineColor == Color.clear)
                {
                    randomTargetIndicatorLineColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
                }

                Gizmos.color = randomTargetIndicatorLineColor;
                Gizmos.DrawLine(transform.position, PathToDestination[CurrentWayPointToFollow]);

                //Target Indicator
                var NewGUIStyle = JUTPSEditor.CustomEditorStyles.Toolbar();
                NewGUIStyle.normal.textColor = randomTargetIndicatorLineColor;
                UnityEditor.Handles.Label(PathToDestination[CurrentWayPointToFollow] + Vector3.up * 1, "Target", NewGUIStyle);
            }
        }
#endif
    }
}