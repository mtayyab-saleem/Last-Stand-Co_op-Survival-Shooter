using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.AI;

namespace JUTPS.Utilities
{

    public class MovingPlataform : MonoBehaviour
    {
        public WaypointPath WaypointPath;
        public WaypointPath.OnEndPathAction OnEndPath;
        public float Speed;
        public bool ParentCollidedObjects = true;
        private int waypointId;

        void FixedUpdate()
        {
            if (WaypointPath == null) return;
            WaypointPath.FollowPathTowards(gameObject, ref WaypointPath.WaypointPathPositions, ref waypointId, Speed, OnEndPath);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!ParentCollidedObjects) return;

            collision.transform.parent = transform;
        }
        private void OnCollisionExit(Collision collision)
        {
            if (!ParentCollidedObjects) return;

            collision.transform.parent = null;
        }
    }

}