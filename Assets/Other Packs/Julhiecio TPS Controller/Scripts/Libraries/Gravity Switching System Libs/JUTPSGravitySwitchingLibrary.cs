using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.CharacterBrain;

namespace JUTPS.GravitySwitchSystem
{
    public class JUGravity
    {
        public static void SimulateGravityPoint(Vector3 GravityCenterPosition, float Radious = 10, float GravityForce = -200, bool AlignRigidBodies = false, float DistanceToStopAligning = 5, float AlignForce = 35)
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
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlignForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }
        public static void SimulateGravityPoint(Vector3 GravityCenterPosition, out Collider[] rblist, float Radious = 10, float GravityForce = -200, bool AlignRigidBodies = false, float DistanceToStopAligning = 5, float AlignForce = 35, string[] TagsToIgnore = null)
        {
            //Get gravity center
            Vector3 gravityCenter = GravityCenterPosition;
            //Get colliders and return
            Collider[] colliders = Physics.OverlapSphere(gravityCenter, Radious);
            rblist = colliders;
            
            //for each collider, get rigibody and apply a gravity point
            foreach (Collider hit in colliders)
            {
                //ignora some tags
                if (TagsToIgnore != null)
                {
                    foreach (string tag in TagsToIgnore) if (hit.tag == tag) return;
                }

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
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlignForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }
        public static void SimulateGravityBox(Vector3 BoxPosition, Vector3 BoxScale, Quaternion BoxOrientation, Vector3 GravityDirection, float GravityForce, bool AlignRigidBodies, float AlignForce, float DistanceToStopAligning, out Collider[] collider, string[] TagsToIgnore = null)
        {
            //Get colliders and return
            Collider[] colliders = Physics.OverlapBox(BoxPosition, BoxScale, BoxOrientation);
            collider = colliders;

            //for each collider, get rigibody and apply a gravity point
            foreach (Collider hit in colliders)
            {
                //ignora some tags
                if (TagsToIgnore.Length > 0)
                {
                    foreach (string tag in TagsToIgnore) if (hit.tag == tag) return;
                }

                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // >>> GRAVITY
                    float distance = Vector3.Distance(rb.position, BoxPosition);
                    float attractionIntensity = (rb.mass / (distance * 1));
                    Vector3 gravityDirection = -GravityDirection;
                    rb.AddForce(gravityDirection * ((100 * GravityForce) * Time.deltaTime) * attractionIntensity);
                    //Debug.Log("Gravity Intensity: " + attractionIntensity);

                    // >>> ALIGN
                    if (distance > DistanceToStopAligning && AlignRigidBodies)
                    {
                        rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation,
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlignForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }
        public static void AlignJUTPSCharacterUpOrientation(Vector3 GravityCenterPosition, Collider[] collidersReturnedBySimulation, float DistanceToAlign)
        {
            foreach (Collider hit in collidersReturnedBySimulation)
            {
                JUCharacterBrain character = hit.GetComponent<JUCharacterBrain>();
                float distance = Vector3.Distance(hit.transform.position, GravityCenterPosition);
                if (character != null && distance < DistanceToAlign)
                {
                    character.UpDirection = (hit.transform.position - GravityCenterPosition).normalized;
                }
            }
        }
        public static void AlignJUTPSCharacterUpOrientation(Collider[] collidersReturnedBySimulation, Vector3 UpOrientation)
        {
            foreach (Collider hit in collidersReturnedBySimulation)
            {
                JUCharacterBrain character = hit.GetComponent<JUCharacterBrain>();

                if (character != null)
                {
                    character.UpDirection = UpOrientation;
                }
            }
        }
    }
}
