using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS.PhysicsScripts
{

    [AddComponentMenu("JU TPS/Third Person System/Additionals/Slip Capsule")]
    public class JUSlipCapsule : MonoBehaviour
    {
        [SerializeField] private Vector3 Center = new Vector3(0, 0.85f, 0);
        [SerializeField] private float Radius = 0.5f;
        [SerializeField] private float Height = 1.25f;

        private CapsuleCollider defaultCapsuleCollider;
        private CapsuleCollider slipCapsule;
        void Awake()
        {
            defaultCapsuleCollider = GetComponent<CapsuleCollider>();

            GenerateSlipCapsuleCollider(gameObject, Center, Radius, Height, out slipCapsule);
        }
        void Update()
        {
            if (defaultCapsuleCollider == null || slipCapsule == null) return;
            slipCapsule.isTrigger = defaultCapsuleCollider.isTrigger;
            slipCapsule.enabled = defaultCapsuleCollider.enabled;
            slipCapsule.center = Center;
            slipCapsule.radius = Radius;
            slipCapsule.height = Height;
        }
        public static void GenerateSlipCapsuleCollider(GameObject target, Vector3 Center, float Radius, float Height, out CapsuleCollider outCapsuleCollider)
        {
            CapsuleCollider cap = target.AddComponent<CapsuleCollider>();
            cap.center = Center;
            cap.radius = Radius;
            cap.height = Height;
            cap.material = (PhysicsMaterial)Resources.Load("Slip");
            outCapsuleCollider = cap;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.color = Color.yellow;
            Vector3 centerPosition = transform.position + transform.right * Center.x + transform.up * Center.y + transform.forward * Center.z;
            Handles.DrawWireDisc(centerPosition + transform.up * Height / 4f, transform.up, Radius);
            Handles.DrawWireDisc(centerPosition - transform.up * Height / 4f, transform.up, Radius);
            Handles.DrawWireDisc(centerPosition + transform.up * Height / 4f, transform.up, Radius - 0.1f);
            Handles.DrawWireDisc(centerPosition - transform.up * Height / 4f, transform.up, Radius - 0.1f);

            //Lines
            Handles.DrawLine((centerPosition + transform.right * Radius) + transform.up * Height / 4f, (centerPosition + transform.right * Radius) - transform.up * Height / 4f);
            Handles.DrawLine((centerPosition - transform.right * Radius) + transform.up * Height / 4f, (centerPosition - transform.right * Radius) - transform.up * Height / 4f);
            Handles.DrawLine((centerPosition + transform.forward * Radius) + transform.up * Height / 4f, (centerPosition + transform.forward * Radius) - transform.up * Height / 4f);
            Handles.DrawLine((centerPosition - transform.forward * Radius) + transform.up * Height / 4f, (centerPosition - transform.forward * Radius) - transform.up * Height / 4f);
        }
#endif
    }

}