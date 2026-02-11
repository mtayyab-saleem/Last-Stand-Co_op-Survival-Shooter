using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.AI
{
    public class WaypointUtilities
    {
        public enum FollowingState { None, Started, Following, Ended }
        public static FollowingState GetPathFollowingState(Transform PathFollower, ref Vector3 OldFollowerPosition, Vector3[] path, int currentPathCornerID, float StoppingDistance = 3)
        {
            FollowingState state = FollowingState.None;
            if (path.Length < 2) return state;

            float distanceToNextCorner = Vector3.Distance(PathFollower.position, path[currentPathCornerID]);
            float distanceToLastCorner = Vector3.Distance(PathFollower.position, path[path.Length-1]);

            if (OldFollowerPosition == Vector3.zero) OldFollowerPosition = PathFollower.position;
            float followerSpeed = (PathFollower.position - OldFollowerPosition).magnitude * 2f;
            OldFollowerPosition = PathFollower.position;


            if (followerSpeed > 2)
            {
                state = FollowingState.Following;
            }
            else { state = FollowingState.None; }


            if(followerSpeed < 1 && followerSpeed > 0 && currentPathCornerID < 2)
            {
                state = FollowingState.Started;
            }

            if (distanceToLastCorner-2 < StoppingDistance)
            {
                state = FollowingState.Ended;
            }

            return state;
        }
        


        /// <summary>
        /// Returns the closest position among a list of waypoints.
        /// </summary>
        /// <param name="SourcePosition"></param>
        /// <param name="PathList"></param>
        /// <param name="SpecificIDPositionFromPath"> return a specific position of the list. 0 = closest point; PathList.Lenght = furthest point; </param>
        public static Vector3 GetClosestPoint(Vector3 SourcePosition, Vector3[] PathList, int SpecificIDPositionFromPath = -1)
        {
            float oldDistance = 9999;
            Vector3 closestPointPosition = new Vector3(0,0,0);

            List<Vector3> closestsPositionsList = new List<Vector3>();

            foreach (Vector3 CurrentPathPosition in PathList)
            {
                float dist = Vector3.Distance(SourcePosition, CurrentPathPosition);
                if (dist < oldDistance)
                {
                    closestPointPosition = CurrentPathPosition;
                    closestsPositionsList.Add(closestPointPosition);
                    oldDistance = dist;
                }
            }
            closestsPositionsList.Reverse();

            if (SpecificIDPositionFromPath == -1 || closestsPositionsList.Count != PathList.Length || SpecificIDPositionFromPath > closestsPositionsList.Count - 1)
            {
                return closestPointPosition;
            }
            else
            {
                if (closestsPositionsList[SpecificIDPositionFromPath] != null)
                {
                    return closestsPositionsList[SpecificIDPositionFromPath];
                }
                else
                {
                    return closestPointPosition;
                }
            }
        }

        /// <summary>
        /// Shoud be called on OnDrawGizmo method.
        /// </summary>
        /// <param name="Path">Vector3 Array with the path position</param>
        /// <param name="LineColor">line color</param>
        /// <param name="CornerColor">corner color</param>
        public static void DrawPath(Vector3[] Path, Color LineColor = default(Color), Color CornerColor = default(Color))
        {
            if(LineColor == Color.clear || CornerColor == Color.clear)
            {
                LineColor = new Color(1,1,1,0.2f);
                CornerColor = new Color(0,1,0, 0.5f);
            }

            for (int i = 0; i < Path.Length; ++i)
            {
                Gizmos.color = CornerColor;
                Gizmos.DrawSphere(Path[i], 0.1f);
                Gizmos.DrawWireSphere(Path[i], 0.1f);

                if (i < Path.Length - 1)
                {
                    Gizmos.color = LineColor;
                    if (Path[i + 1] != null && Path[i] != null)
                    {
                        Gizmos.DrawLine(Path[i], Path[i + 1]);
                    }
                }
            }
        }


        public static Vector3[] ConvertWaypointTransformsToVector3Path(List<Transform> waypointsList)
        {
            List<Vector3> path = new List<Vector3>();
            foreach (Transform t in waypointsList)
            {
                path.Add(t.position);
            }
            return path.ToArray();
        }
        public static List<Transform> GetAllWaypointsChilds(Transform waypointPath)
        {
            List<Transform> waypointsList = new List<Transform>();
            
            for (int i = 0; i < waypointPath.transform.childCount; ++i)
            {
                waypointsList.Add(waypointPath.transform.GetChild(i));
            }

            return waypointsList;
        }
        public static void DividePath(ref Vector3[] originalPath, float divideAtDistance = 1f)
        {
            List<Vector3> newPath = new List<Vector3>();

            for (int point = 0; point < originalPath.Length; point++)
            {
                if (point + 1 < originalPath.Length)
                {
                    float DistanceFromNextPoint = Vector3.Distance(originalPath[point], originalPath[point + 1]);
                    Vector3 DirectionToNextPoint = (originalPath[point + 1] - originalPath[point]).normalized;

                    //Divide
                    for (float pathDivision = 0; pathDivision < DistanceFromNextPoint; pathDivision += divideAtDistance)
                    {
                        newPath.Add(originalPath[point] + DirectionToNextPoint * pathDivision);
                    }
                }
                else
                {
                    //Link Final point
                    newPath.Add(originalPath[originalPath.Length-1]);
                }
            }
            originalPath = newPath.ToArray();
        }
        public static float GetPathFullSize(Vector3[] path)
        {
            float dist = 0;

            for (int i = 0; i < path.Length - 1; i++)
            {
                if (i + 1 < path.Length - 1)
                {
                    dist += Vector3.Distance(path[i], path[i + 1]);
                }
            }

            return dist;
        }
        public static Vector3[] GetWaypointsPositions(Transform waypointPath)
        {
            List<Transform> transformList = GetAllWaypointsChilds(waypointPath);
            Vector3[] vector3List = ConvertWaypointTransformsToVector3Path(transformList);
            return vector3List;
        }
    }
}
