using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JUTPS.VehicleSystem;

using JUTPSEditor.JUHeader;


namespace JUTPS.AI
{
    [AddComponentMenu("JU TPS/AI/Vehicle AI")]
    public class VehicleAI : MonoBehaviour
    {
        private Vehicle vehicle;

        //Used to update following state
        private Vector3 oldPosition;

        [HideInInspector] public int CurrentWayPointToFollow;
        [HideInInspector] public Vector3[] PathToDestination;

        [Header("Destination Settings")]
        public bool EnablePathfinding;
        public float RecalculatePathRefreshRate = 1;
        public Vector3 Destination;
        [JUReadOnly("EnablePathfinding", true, false)] public WaypointPath WaypointPath;


        [Header("Vehicle Path Locomotion Settings")]
        public float DistanceToContinuePath = 2;
        public float VehicleDesacelerationIntensity = 1;
        public Vehicle.VehicleRaycastCheck FrontCheck;
        public bool CheckNearestPointOnPath;
        public WaypointPath.OnEndPathAction OnEndPath = WaypointPath.OnEndPathAction.Stop;

        [Header("Events")]
        public UnityEvent OnStartPath;
        public UnityEvent OnFollowing;
        public UnityEvent OnEnded;
        private bool Started, Following, Ended;
        void Start()
        {
            //Get vehicle component
            vehicle = GetComponent<Vehicle>();

            if (EnablePathfinding || WaypointPath == null)
            {
                RecalculatePath();
            }
            else
            {
                PathToDestination = WaypointPath.WaypointPathPositions;
            }
            InvokeRepeating("RecalculatePath", RecalculatePathRefreshRate, RecalculatePathRefreshRate);
        }
        public void SetVehicleDestination(Vector3 destination, bool recalculatePath = true)
        {
            Destination = destination;
            RecalculatePath();
        }
        public void RecalculatePath()
        {
            if (EnablePathfinding == false) return;
            PathToDestination = JUPathFinder.CalculatePath(transform.position, Destination);
            WaypointUtilities.DividePath(ref PathToDestination, 5);

            CurrentWayPointToFollow = 0;
        }
        private void Update()
        {
            if (!vehicle.IsOn || !vehicle.IsGrounded)
                return;

            FrontCheck.Check(vehicle.transform, transform.forward);
            FollowPath(ref PathToDestination, vehicle, DistanceToContinuePath, VehicleDesacelerationIntensity, ref CurrentWayPointToFollow, OnEndPath, FrontCheck.IsCollided, CheckNearestPointOnPath);

            if (EnablePathfinding)
            {
                JUPathFinder.VisualizePath(PathToDestination);
            }

            //Following State
            WaypointUtilities.FollowingState state = WaypointUtilities.GetPathFollowingState(transform, ref oldPosition, PathToDestination, CurrentWayPointToFollow, DistanceToContinuePath);

            //Call State Events
            if (state == WaypointUtilities.FollowingState.Started && Started == false)
            {
                OnStartPath.Invoke();
                Started = true;
                Ended = false;
            }
            if (state == WaypointUtilities.FollowingState.Following)
            {
                OnFollowing.Invoke();
            }
            Following = (state == WaypointUtilities.FollowingState.Following);

            if (state == WaypointUtilities.FollowingState.Ended && Ended == false)
            {
                OnEnded.Invoke();
                Following = false;
                Started = false;
                Ended = true;
            }
        }


        public static void FollowPath(ref Vector3[] path, Vehicle vehicle, float stoppingDistance, float desacelerationOnCurvesIntensity, ref int currentPathCornerId, WaypointPath.OnEndPathAction onPathEnd = WaypointPath.OnEndPathAction.Stop, bool TheresWallInVehicleFront = false, bool CheckClosestPoint = false)
        {
            if (!vehicle.IsOn || !vehicle.IsGrounded || path.Length == 0) return;

            //Reset target waypoint
            if (path.Length - 1 < currentPathCornerId)
            {
                currentPathCornerId = 0;
            }

            // >>> Create parameters to control vehicle

            //Get direction of the target waypoint
            Vector3 TargetDirection = (path[currentPathCornerId] - vehicle.transform.position).normalized;
            //Get direction of the closest waypoint
            Vector3 ClosestPointDirection = (WaypointUtilities.GetClosestPoint(vehicle.transform.position, path, 1) - vehicle.transform.position).normalized;
            //Get distance between the vehicle and target waypoint
            float DistanceToNextWaypoint = Vector3.Distance(vehicle.transform.position, path[currentPathCornerId]);
            //Get angle between target direction and vehicle forward
            float AngleBetweenNormalAndVehicleForward = Vector3.SignedAngle(vehicle.transform.forward, TargetDirection, Vector3.up);
            //Get angle between vehicle forward and closest waypoint
            float AngleBetweenClosestWaypointAndVehicleForward = Vector3.SignedAngle(vehicle.transform.forward, ClosestPointDirection, Vector3.up);

            //Get right direction intensity (1 = right direction | -1 = wrong direction)
            float RightDirectionIntensity = Vector3.Dot(vehicle.transform.forward, TargetDirection);


            // >>> Create Rewrited Inputs
            float HorizontalInput = Mathf.Clamp(AngleBetweenNormalAndVehicleForward, -90, 90) / 90 * (1 + Mathf.Clamp(RightDirectionIntensity, 0, 1));
            float VerticalInput = 0;
            bool BrakeInput = false;

            //Set the next waypoint to follow
            if (DistanceToNextWaypoint + Mathf.Abs(vehicle.ForwardSpeed * 0.2f) < stoppingDistance && currentPathCornerId < path.Length - 1)
            {
                currentPathCornerId++;
            }

            // >>> Vehicle Locomotion
            if (currentPathCornerId != path.Length - 1 && (DistanceToNextWaypoint * 2) > stoppingDistance)
            {
                //Accelerate Vehicle
                float ClampedAngle = Mathf.Clamp(Mathf.Abs(AngleBetweenNormalAndVehicleForward), 0, 90);
                float DesacelerationValue = desacelerationOnCurvesIntensity * ((ClampedAngle / 360) + Mathf.Abs(vehicle.ForwardSpeed * 0.05f) / 4);
                VerticalInput = 1 - Mathf.Clamp(DesacelerationValue, -1f, 0.5f);

                VerticalInput = Mathf.Clamp(VerticalInput, -1, 1);
                BrakeInput = false;
            }

            //Brake vehicle if going on wrong direction
            if (RightDirectionIntensity > 0.3f && vehicle.FinalThrottle < -1f)
            {
                BrakeInput = true;
            }

            //Vehicle reverse when theres a wall in front of vehicle
            if (TheresWallInVehicleFront)
            {
                //vehicle reversing
                VerticalInput = -2f;
                //vehicle turn
                HorizontalInput = (AngleBetweenNormalAndVehicleForward > 0) ? -1 : 1;
                //HorizontalInput = RightDirectionIntensity > 0.2f ? HorizontalInput : -HorizontalInput;
                //vehicle.transform.Rotate(0,HorizontalInput * 90 * Time.deltaTime,0);
                BrakeInput = false;
            }

            //Check Closest Point of The Path
            if (CheckClosestPoint)
            {
                Vector3 closestWaypoint = WaypointUtilities.GetClosestPoint(vehicle.transform.position, path);
                int closestCornerID = System.Array.IndexOf(path, closestWaypoint);
                if (Vector3.Distance(vehicle.transform.position, closestWaypoint) < DistanceToNextWaypoint && closestCornerID > currentPathCornerId && closestCornerID != path.Length - 1)
                {
                    //Debug.Log("Detected Closest Point on the path");
                    currentPathCornerId = closestCornerID;
                }
            }

            // >>> On vehicle reaches at the end of the path
            if (currentPathCornerId >= path.Length - 1 && DistanceToNextWaypoint < stoppingDistance)
            {
                switch (onPathEnd)
                {
                    case WaypointPath.OnEndPathAction.Stop:
                        VerticalInput = 0;
                        BrakeInput = true;
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

            vehicle.Throttle = VerticalInput;
            vehicle.Steer = HorizontalInput;
            vehicle.Brake = BrakeInput ? 1 : 0;
        }
        public static float GetVehicleRightDirectionIntensity(Vehicle vehicle, Vector3 currentTargetPathPosition)
        {
            Vector3 TargetDirection = (currentTargetPathPosition - vehicle.transform.position).normalized;
            float RightDirectionIntensity = Vector3.Dot(vehicle.transform.forward, TargetDirection);
            return RightDirectionIntensity;
        }

#if UNITY_EDITOR

        Color randomTargetIndicatorLineColor = Color.clear;

        private void OnDrawGizmos()
        {
            Vehicle.VehicleGizmo.DrawRaycastHit(FrontCheck, transform, transform.forward);
            if (Application.isPlaying == false)
            {
                if (EnablePathfinding) Gizmos.DrawWireSphere(Destination, 1);
            }
            else
            {
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