using UnityEngine;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/Auto Move")]
    public class JUAutoMove : MonoBehaviour
    {
        public Vector3 Movement;
        public Space MoveSpace;
        public bool UseDeltaTime = true;
        void Update() => transform.Translate(Movement * (UseDeltaTime ? Time.deltaTime : 0), MoveSpace);
    }
}