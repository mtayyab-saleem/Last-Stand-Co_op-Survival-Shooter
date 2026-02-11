using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.ArmorSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif
using JUTPSEditor.JUHeader;

namespace JUTPS.ArmorSystem
{
    [AddComponentMenu("JU TPS/Armor System/Damageable Body")]
    public class DamageableBody : MonoBehaviour
    {
        [JUHeader("Damageable Body Parts Intensity")]
        [Range(0, 10)] public float HeadDamageIntensity = 5;
        [Range(0, 10)] public float TorsoDamageIntensity = 1;
        [Range(0, 10)] public float LegsDamageIntensity = 0.8f;
        [Range(0, 10)] public float ArmsDamageIntensity = 0.5f;

        public DamageableBodyPart[] AllParts;
        void Awake()
        {
            Animator anim = GetComponent<Animator>();
            if (anim == null)
            {
                Debug.LogError("Damageable Body: Could not find the Animator, without it it is not possible to distribute the bones normally");
            }
            else
            {
                DistributeDamageableComponentsInTheBody(anim, HeadDamageIntensity, TorsoDamageIntensity, LegsDamageIntensity, ArmsDamageIntensity);
            }

            AllParts = GetComponentsInChildren<DamageableBodyPart>();
        }

        public static DamageableBodyPart[] DistributeDamageableComponentsInTheBody(Animator animator, float HeadValue = 5, float TorsoValue = 1, float LegValue = 0.8f, float ArmValue = 0.5f)
        {
            List<DamageableBodyPart> parts = new List<DamageableBodyPart>();
            //Get All Hips Colliders
            Collider[] bonesWithColliders = animator.GetBoneTransform(HumanBodyBones.Hips).GetComponentsInChildren<Collider>();

            //Filter Hips by Layer
            foreach (Collider bone in bonesWithColliders)
            {
                // >>> IF HAVE DMB COMPONENT
                if (bone.gameObject.layer == 15 && bone.GetComponent<DamageableBodyPart>() == null)
                {
                    parts.Add(bone.gameObject.AddComponent<DamageableBodyPart>());
                    if (bone.gameObject.TryGetComponent(out Rigidbody rb))
                    {
                        rb.isKinematic = true;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                    else
                    {
                        Rigidbody rbb = bone.gameObject.AddComponent<Rigidbody>();
                        rbb.isKinematic = true;
                        rbb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                }
                // >>> IF DONT HAVE DMB COMPONENT
                if (bone.gameObject.layer == 15 && bone.GetComponent<DamageableBodyPart>() != null)
                {
                    parts.Add(bone.gameObject.GetComponent<DamageableBodyPart>());
                    if (bone.gameObject.TryGetComponent(out Rigidbody rb))
                    {
                        rb.isKinematic = true;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                    else
                    {
                        Rigidbody rbb = bone.gameObject.AddComponent<Rigidbody>();
                        rbb.isKinematic = true;
                        rbb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                }
            }

            //Apply Values
            foreach (DamageableBodyPart damageablePart in parts.ToArray())
            {
                //Apply Torso Damage Multiplier Value
                if (damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.Hips) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.Spine)
                 || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.Chest) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.UpperChest))
                {
                    damageablePart.DamageMultiplier = TorsoValue;
                }

                //Apply Head Damage Multiplier Value
                if (damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.Head))
                {
                    damageablePart.DamageMultiplier = HeadValue;
                }

                //Apply Legs Damage Multiplier Value
                if (damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg)
                 || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.LeftFoot) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)
                 || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.RightUpperLeg) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.RightFoot))
                {
                    damageablePart.DamageMultiplier = LegValue;
                }

                //Apply Arms Damage Multiplier
                if (damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.LeftLowerArm) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)
               || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.LeftHand) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.RightLowerArm)
               || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.RightUpperArm) || damageablePart.transform == animator.GetBoneTransform(HumanBodyBones.RightHand))
                {
                    damageablePart.DamageMultiplier = ArmValue;
                }
            }

            return parts.ToArray();
        }
    }

}
#if UNITY_EDITOR
namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(DamageableBody))]
    public class DamageableBodyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DamageableBody dmBody = ((DamageableBody)target);
            if (GUILayout.Button("Distribute Values"))
            {
                Animator anim = dmBody.GetComponent<Animator>();
                if (anim == null)
                {
                    Debug.LogError("Unable to find Animator component");
                    return;
                }
                if (anim.isHuman == false)
                {
                    Debug.LogError("Your character needs to be humanoid");
                    return;
                }
                DamageableBody.DistributeDamageableComponentsInTheBody(anim, dmBody.HeadDamageIntensity,
                    dmBody.TorsoDamageIntensity, dmBody.LegsDamageIntensity, dmBody.ArmsDamageIntensity);

                Debug.Log("Damageable Body Parts values have been successfully updated");
            }
        }
    }

}
#endif

