using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.AI
{
    public class WaypointPath : MonoBehaviour
    {
        [HideInInspector] public List<Transform> WaypointsTransforms = new List<Transform>();
        [HideInInspector] public Vector3[] WaypointPathPositions;

        [Header("Waypoints Settings")]
        public bool ClearWaypointsAfterGettingPathPositions;
        public bool ReverseOnStart;
        [Header("Path Gizmo Visualization")]
        public bool DrawPath = true;
        public Color LineColor = new Color(1, 1, 1, 0.2f), CornerColor = new Color(0, 1, 0, 0.5f);

        public enum OnEndPathAction { Stop, ReversePath, RestartPath }

        void Start()
        {
            RefreshWaypoints();
            if (ReverseOnStart)
            {
                System.Array.Reverse(WaypointPathPositions);
            }
        }
        private Bounds waypointBounds;
        public Vector3 GetWaypointCenter()
        {
            if (waypointBounds.center == Vector3.zero)
            {
                waypointBounds = new Bounds(WaypointPathPositions[0], Vector3.zero);
            }
            else
            {
                return waypointBounds.center;
            }

            for (int i = 1; i < WaypointPathPositions.Length; i++)
            {
                waypointBounds.Encapsulate(WaypointPathPositions[i]);
            }

            return waypointBounds.center;
        }

        public void RefreshWaypoints()
        {
            if(WaypointsTransforms.Count == 0 && Application.isPlaying == false)
            {
                Transform t0 = new GameObject("Waypoint").transform;
                Transform t1 = new GameObject("Waypoint (1)").transform;
                t0.position = transform.position;
                t1.position = transform.position + transform.forward * 2f;
                t0.parent = transform; t1.parent = transform;
            }

            WaypointsTransforms = WaypointUtilities.GetAllWaypointsChilds(transform);
            WaypointPathPositions = WaypointUtilities.GetWaypointsPositions(transform);

            if (ClearWaypointsAfterGettingPathPositions == false || Application.isPlaying == false) return;

            foreach(Transform t in WaypointsTransforms)
            {
                Destroy(t.gameObject);
            }

            waypointBounds.center = Vector3.zero;
            GetWaypointCenter();
        }

        public static void FollowPathTowards(GameObject gameObjectToMove, ref Vector3[] path, ref int currentPathCornerId, float Speed = 10, OnEndPathAction onPathEnd = OnEndPathAction.ReversePath)
        {
            if (path.Length == 0 || gameObjectToMove == null) return;

            //Create distance to set next waypoint
            float stoppingDistance = 0.1f;
            //Get distance between the gameObjectToMove and target waypoint
            float DistanceToNextWaypoint = Vector3.Distance(gameObjectToMove.transform.position, path[currentPathCornerId]);

            //Reset target waypoint
            if (path.Length - 1 < currentPathCornerId)
            {
                currentPathCornerId = 0;
            }

            //Set the next waypoint to follow
            if (DistanceToNextWaypoint < stoppingDistance && currentPathCornerId < path.Length - 1)
            {
                currentPathCornerId++;
            }

            //Object movement
            gameObjectToMove.transform.position = Vector3.MoveTowards(gameObjectToMove.transform.position, path[currentPathCornerId], Speed * Time.deltaTime);

            // >>> On vehicle reaches at the end of the path
            if (currentPathCornerId >= path.Length - 1 && DistanceToNextWaypoint < stoppingDistance)
            {
                switch (onPathEnd)
                {
                    case OnEndPathAction.Stop:
                        
                        break;
                    case OnEndPathAction.ReversePath:
                        System.Array.Reverse(path);
                        currentPathCornerId = 0;
                        break;
                    case OnEndPathAction.RestartPath:
                        currentPathCornerId = 0;
                        break;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!DrawPath) return;

            if (Application.isPlaying == false)
            {
                if (transform.childCount == 0) { RefreshWaypoints(); return; }

                if (transform.childCount != WaypointsTransforms.Count || WaypointPathPositions[transform.childCount - 1] != WaypointsTransforms[transform.childCount - 1].position)
                {
                    RefreshWaypoints();
                }
            }
            WaypointUtilities.DrawPath(WaypointPathPositions, LineColor, CornerColor);
        }
    }
}
