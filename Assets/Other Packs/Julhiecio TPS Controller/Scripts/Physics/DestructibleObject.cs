using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JUTPS.FX;

namespace JUTPS.DestructibleSystem
{

    [AddComponentMenu("JU TPS/Physics/Destructible")]
    public class DestructibleObject : MonoBehaviour
    {
        [Header("Destructible Settings")]

        [Range(0, 50)]
        public float Strength;
        public GameObject FracturedObject;
        public Vector3 PositionOffset;
        public float TimeToDestroy = 15;
        private bool IsFractured = false;
        [Header("Destroy Events")]
        public bool DoSlowmotionWhenDestroy;
        public bool DoSlowmotionWhenPlayerIsJumping; // (Bullet time system)

        [Header("FX")]
        public float TimeToFracture = 0f;
        public GameObject DestructionFX;
        IEnumerator _DestroyObject()
        {
            /*if (TimeToFracture > 0 && GlowEffect != null)
            {
                Instantiate(GlowEffect, transform.position, transform.rotation, transform);
                yield return new WaitForSeconds(TimeToFracture);
            }*/
            if (IsFractured == false)
            {
                if (FracturedObject != null)
                {
                    Invoke("FractureThisObject", TimeToFracture);

                    if (DestructionFX != null)
                    {
                        Instantiate(DestructionFX, transform.position, transform.rotation, transform);
                    }
                }
                else
                {
                    Debug.LogWarning("There is no 'Fractured Object' linked in " + gameObject.name);
                }

                if (DoSlowmotionWhenDestroy)
                {
                    JUSlowmotion.DoSlowMotion(0.1f, 5f);
                }
                if (DoSlowmotionWhenPlayerIsJumping && FindObjectOfType<JUCharacterController>().IsJumping)
                {
                    JUSlowmotion.DoSlowMotion(0.1f, 5f);
                }


            }
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// Destroy the gameobject and instantiate the fractured prefab
        /// </summary>
        public void FractureThisObject()
        {
            if (IsFractured == true) return;

            //Instantiate fracture
            var fractured_obj = (GameObject)Instantiate(FracturedObject, transform.position + PositionOffset, transform.rotation);

            //Destroy this
            Destroy(this.gameObject, 0.01f);

            //Destroy fracture timer
            Destroy(fractured_obj, TimeToDestroy);

            //Check the bool
            IsFractured = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Bullet")
            {
                StartCoroutine(_DestroyObject());
            }

        }
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag == "Bullet")
            {
                StartCoroutine(_DestroyObject());
            }
            if (other.gameObject.TryGetComponent(out Rigidbody rb))
            {
                if (rb.linearVelocity.magnitude > 5f)
                {
                    StartCoroutine(_DestroyObject());
                }
            }
        }
        private void OnCollisionStay(Collision other)
        {
            if (other.gameObject.tag == "Bullet")
            {
                StartCoroutine(_DestroyObject());
            }
        }
    }

}