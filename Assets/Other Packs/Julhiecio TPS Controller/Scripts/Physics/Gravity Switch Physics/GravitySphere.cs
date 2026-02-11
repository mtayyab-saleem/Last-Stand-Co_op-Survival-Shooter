using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.GravitySwitchSystem;
namespace JUTPS.GravitySwitchSystem
{
    [AddComponentMenu("JU TPS/Third Person System/Gravity Switcher/Gravity Sphere")]
    public class GravitySphere : MonoBehaviour
    {
        [Header("Settings")]
        public bool Activated;
        public float Radious = 10, Force = 9.8f;

        [Header("AlignSettings")]
        public bool AlignRigidbodies = false;
        public float AlignForce = 35;
        public float DistanceToStopAligning = 5;
        public bool AlignJUTPSCharacters;
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            Collider[] colliders;

            JUGravity.SimulateGravityPoint(transform.position, out colliders, Radious, Force, AlignRigidbodies, DistanceToStopAligning, AlignForce);
            JUGravity.AlignJUTPSCharacterUpOrientation(transform.position, colliders, DistanceToStopAligning);

            //Change(transform.position, out colliders, Radious, Force, AlignRigidbodies, DistanceToStopAligning, AlignForce);
            
            /*
            foreach (Collider hit in colliders)
            {
                JUTPS.CharacterBrain.JUCharacterBrain character = hit.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();

                if (character != null)
                {
                    // >>> GRAVITY
                    float distance = Vector3.Distance(character.transform.position, transform.position);
                    float attractionIntensity = (character.rb.mass / (distance * Radious));
                    Vector3 gravityDirection = (character.transform.position - transform.position).normalized;

                    // >>> ALIGN
                    if (distance < DistanceToStopAligning && AlignJUTPSCharacters)
                    {
                        character.UpDirection = gravityDirection;
                    }
                }
            }*/
        }
        public static void Change(Vector3 GravityCenterPosition, float Radious = 10, float GravityForce = 9.8f, bool AlignRigidBodies = false, float DistanceToStopAligning = 5, float AlingForce = 35)
        {
            Vector3 gravityCenter = GravityCenterPosition;
            Collider[] colliders = Physics.OverlapSphere(gravityCenter, Radious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // >>> GRAVITY
                    float distance = Vector3.Distance(rb.position, gravityCenter);
                    float attractionIntensity = (rb.mass / (distance * Radious));
                    Vector3 gravityDirection = (rb.position - gravityCenter).normalized;
                    rb.AddForce(gravityDirection * ((100 * GravityForce) * Time.deltaTime) * attractionIntensity);
                    //Debug.Log("Gravity Intensity: " + attractionIntensity);

                    // >>> ALIGN
                    if (distance > DistanceToStopAligning && AlignRigidBodies)
                    {
                        rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation,
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlingForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }
        public static void Change(Vector3 GravityCenterPosition, out Collider[] rblist, float Radious = 10, float GravityForce = 9.8f, bool AlignRigidBodies = false, float DistanceToStopAligning = 5, float AlingForce = 35)
        {
            Vector3 gravityCenter = GravityCenterPosition;
            Collider[] colliders = Physics.OverlapSphere(gravityCenter, Radious);
            rblist = colliders;
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // >>> GRAVITY
                    float distance = Vector3.Distance(rb.position, gravityCenter);
                    float attractionIntensity = (rb.mass / (distance * Radious));
                    Vector3 gravityDirection = (rb.position - gravityCenter).normalized;
                    rb.AddForce(gravityDirection * ((100 * GravityForce) * Time.deltaTime) * attractionIntensity);
                    //Debug.Log("Gravity Intensity: " + attractionIntensity);

                    // >>> ALIGN
                    if (distance > DistanceToStopAligning && AlignRigidBodies)
                    {
                        rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation,
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlingForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }


        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawSphere(transform.position, Radious);

            Gizmos.color = new Color(0.5f, 1, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, Radious);


            Gizmos.color = new Color(1, 0, 0, 0.1f);
            Gizmos.DrawSphere(transform.position, DistanceToStopAligning);

            Gizmos.color = new Color(1, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, DistanceToStopAligning);
        }
    }
}