using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPSEditor.JUHeader;

namespace JUTPS.ItemSystem
{

    public class ThrowableItem : JUHoldableItem
    {
        [JUHeader("Throw Settings")]
        public string AnimationTriggerParameterName = "Throw";
        public float ThrowForce = 10, ThrowUpForce = 10, RotationForce = 10;
        //public float ItemMass;
        public float SecondsToDestroy = 5;
        public Vector3 PositionToThrow = new Vector3(0, 1, 0.8f);
        public Vector3 DirectionToThrow = Vector3.forward;

        [HideInInspector] public bool IsThrowed = false;
        public override void UseItem()
        {
            if (ItemQuantity <= 0 || CanUseItem == false || IsThrowed == true) return;

            ThrowThis(ThrowForce, ThrowUpForce, PositionToThrow, DirectionToThrow, RotationForce);


            base.UseItem();

        }
        public virtual GameObject ThrowThis(float forceToThrow, float ThrowUpForce, Vector3 positionToThrow, Vector3 directionToThrow, float angularForce = 0)
        {
            RemoveItem();

            Vector3 throwPosition = Owner.transform.TransformPoint(positionToThrow);
            Vector3 throwDirection = Owner.transform.rotation * directionToThrow;
            Vector3 throwedScale = transform.lossyScale;

            GameObject throwedGameobject = Instantiate(gameObject, throwPosition, transform.rotation);
            throwedGameobject.transform.localScale = throwedScale;

            throwedGameobject.GetComponent<ThrowableItem>().IsThrowed = true;

            if (SecondsToDestroy > 0) Destroy(throwedGameobject, SecondsToDestroy);

            if (throwedGameobject.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.AddForce(throwDirection * forceToThrow, ForceMode.Impulse);
                rb.AddForce(((Owner != null) ? Owner.transform.up : Vector3.up) * ThrowUpForce, ForceMode.Impulse);
                rb.AddTorque(new Vector3(Random.Range(-angularForce, angularForce), Random.Range(-angularForce, angularForce), Random.Range(-angularForce, angularForce)), ForceMode.Impulse);
            }

            if (throwedGameobject.TryGetComponent(out Collider col))
            {
                col.enabled = true;
                col.isTrigger = false;
            }

            return throwedGameobject;
        }

        private void OnDrawGizmos()
        {
            if (Owner == null) { RefreshItemDependencies(); return; }

            Vector3 throwPosition = Owner.transform.TransformPoint(PositionToThrow);
            Vector3 throwDirection = Owner.transform.rotation * DirectionToThrow;


            Gizmos.DrawSphere(throwPosition, 0.05f);
            Gizmos.DrawRay(throwPosition, throwDirection);
        }
    }

}