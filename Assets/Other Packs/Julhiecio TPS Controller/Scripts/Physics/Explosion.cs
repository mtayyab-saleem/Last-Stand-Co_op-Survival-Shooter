using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.DestructibleSystem;

namespace JUTPS.PhysicsScripts
{

    [AddComponentMenu("JU TPS/Physics/Explosion")]
    public class Explosion : MonoBehaviour
    {
        [Header("Explosion Settings")]
        public bool ExplodeOnAwake;
        public float ExplosionForce = 5f;
        public float ExplosionUpForce = 3f;
        public float ExplosionRadious = 5f;

        [Header("Damage Characters")]
        public bool DamageCharacters = false;
        public LayerMask CharacterLayer;
        public float Damage = 100;
        void Start()
        {
            if (ExplodeOnAwake) Explode();
        }
        /// <summary>
        /// Create a explosion force with settings of arguments
        /// </summary>
        public void AddExplode(float ExplosionForce, float ExplosionUpForce, float ExplosionRadious)
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, ExplosionRadious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    rb.AddExplosionForce(ExplosionForce, explosionPos, ExplosionRadious, ExplosionUpForce);
            }
        }
        /// <summary>
        /// Create a explosion force with current settings
        /// </summary>
        public void Explode()
        {
            Invoke(nameof(doExplosionForce), 0.1f);
            //>>> Character Damaging
            if (DamageCharacters == false) return;

            Collider[] characters = Physics.OverlapSphere(transform.position, ExplosionRadious, CharacterLayer);
            foreach (Collider hittedCharacter in characters)
            {
                //Get character
                JUTPS.CharacterBrain.JUCharacterBrain character = hittedCharacter.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
                JUHealth health = hittedCharacter.GetComponent<JUHealth>();

                if (hittedCharacter.TryGetComponent(out DestructibleObject destructible))
                {
                    destructible.FractureThisObject();
                }

                if (character != null)
                {

                    Debug.DrawLine(character.transform.position, transform.position, Color.yellow, 2f, true);

                    //Check visibility
                    //Ray rayToCharacter = new Ray(transform.position + Vector3.up * 0.05f, (character.transform.position - transform.position).normalized);
                    RaycastHit viewHit; Physics.Linecast(transform.position, character.HumanoidSpine.position, out viewHit);

                    //Avoid damage a hidden character
                    if (viewHit.collider != null)
                    {
                        //Is visible ? 
                        if (viewHit.collider.gameObject == character.gameObject)
                        {
                            //Calculate Damage
                            float damage = (int)Mathf.Lerp(Damage, Damage / 10, Vector3.Distance(character.transform.position, transform.position) / ExplosionRadious);

                            //Apply damage
                            if (character != null) character.TakeDamage(damage);
                        }
                    }
                }


                if (character == null && health != null)
                {
                    //Calculate Damage
                    float damage = (int)Mathf.Lerp(Damage, Damage / 10, Vector3.Distance(health.transform.position, transform.position) / ExplosionRadious);

                    health.DoDamage(damage);
                }
            }

        }
        public void doExplosionForce()
        {
            Vector3 explosionPos = transform.position;
            Collider[] colliders = Physics.OverlapSphere(explosionPos, ExplosionRadious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                    rb.AddExplosionForce(ExplosionForce, explosionPos, ExplosionRadious, ExplosionUpForce, ForceMode.Impulse);
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, ExplosionRadious);
        }
    }

}