using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

using JUTPS.WeaponSystem;

namespace JUTPS.CustomEditors
{

    [CustomEditor(typeof(Weapon))]
    public class WeaponComponentEditor : Editor
    {
        //Settings Areas
        public bool WeaponSettings, Precision, Shooting, Wield, ProceduralAnimations, IKSettings, Audio;
        Weapon w;

        //Inspector Draw
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            w = (Weapon)target;

            if (w != null)
            {
                JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Weapon System");

                WeaponSettings = GUILayout.Toggle(WeaponSettings, "Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
                WeaponSettingsVariables(w);

                Wield = GUILayout.Toggle(Wield, "Wielding", JUTPSEditor.CustomEditorStyles.Toolbar());
                WieldingTabVariables(w);

                Precision = GUILayout.Toggle(Precision, "Precision", JUTPSEditor.CustomEditorStyles.Toolbar());
                PrecisionVariables(w);

                Shooting = GUILayout.Toggle(Shooting, "Shooting", JUTPSEditor.CustomEditorStyles.Toolbar());
                ShootingVariables(w);

                ProceduralAnimations = GUILayout.Toggle(ProceduralAnimations, "Procedural Animations", JUTPSEditor.CustomEditorStyles.Toolbar());
                ProceduralAnimationVariables(w);

                IKSettings = GUILayout.Toggle(IKSettings, "Left Hand IK", JUTPSEditor.CustomEditorStyles.Toolbar());
                IKVariables(w);

                Audio = GUILayout.Toggle(Audio, "Audios", JUTPSEditor.CustomEditorStyles.Toolbar());
                AudioVariables(w);
            }

            serializedObject.ApplyModifiedProperties();

            //GUILayout.Space(50);

            //DrawDefaultInspector();
        }

        //Inspector Drawers
        public void WeaponSettingsVariables(Weapon w)
        {
            if (WeaponSettings)
            {
                serializedObject.FindProperty("Unlocked").boolValue = EditorGUILayout.Toggle("Unlocked", w.Unlocked);
                serializedObject.FindProperty("ItemName").stringValue = EditorGUILayout.TextField("Item Name", w.ItemName);
                serializedObject.FindProperty("ItemSwitchID").intValue = EditorGUILayout.IntField("Item Switch ID", w.ItemSwitchID);

                GUILayout.Space(3);
                serializedObject.FindProperty("ItemFilterTag").stringValue = EditorGUILayout.TextField("Item Filter Tag", w.ItemFilterTag);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ItemIcon"));

                GUILayout.Space(3);
                serializedObject.FindProperty("ItemQuantity").intValue = EditorGUILayout.IntField("Item Quantity", w.ItemQuantity);
                serializedObject.FindProperty("MaxItemQuantity").intValue = EditorGUILayout.IntField("Max Item Quantity", w.MaxItemQuantity);

                GUILayout.Space(3);
                //serializedObject.FindProperty("ItemModelInBody").objectReferenceValue = EditorGUILayout.ObjectField("Item on Body", w.ItemModelInBody, typeof(GameObject), true) as GameObject;
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(w.ItemModelInBody)));
                serializedObject.FindProperty("SingleUseItem").boolValue = EditorGUILayout.Toggle("Single Use Item", w.SingleUseItem);
                serializedObject.FindProperty("ContinuousUseItem").boolValue = EditorGUILayout.Toggle("Continuous Use Item", w.ContinuousUseItem);
                serializedObject.FindProperty("BlockFireMode").boolValue = EditorGUILayout.Toggle("Block Fire Mode", w.BlockFireMode);

            }
        }
        public void PrecisionVariables(Weapon w)
        {
            if (Precision == true)
            {
                serializedObject.FindProperty("Precision").floatValue = EditorGUILayout.FloatField("Accuracy", w.Precision);
                serializedObject.FindProperty("LossOfAccuracyPerShot").floatValue = EditorGUILayout.FloatField("Loss of Acurracy per Shot", w.LossOfAccuracyPerShot);
            }
        }
        public void WieldingTabVariables(Weapon w)
        {
            if (Wield == true)
            {
                var HoldPose = serializedObject.FindProperty(nameof(w.HoldPose));
                EditorGUILayout.PropertyField(HoldPose);
                var PushItemSwitchMovement = serializedObject.FindProperty(nameof(w.PushItemFrom));
                EditorGUILayout.PropertyField(PushItemSwitchMovement);
                GUILayout.Space(10);

                serializedObject.FindProperty("ItemWieldPositionID").intValue = EditorGUILayout.IntField("Item Wield Position ID", w.ItemWieldPositionID);
                serializedObject.FindProperty("IsLeftHandItem").boolValue = EditorGUILayout.Toggle("Is Left Hand Item", w.IsLeftHandItem);

                GUILayout.Space(10);
                serializedObject.FindProperty("ForceDualWielding").boolValue = EditorGUILayout.Toggle("Force Dual Wielding", w.ForceDualWielding);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(w.DualItemToWielding)));
            }
        }
        public void ShootingVariables(Weapon w)
        {
            if (Shooting == true)
            {
                serializedObject.FindProperty("Shoot_Position").objectReferenceValue = EditorGUILayout.ObjectField("Shooting Position", w.Shoot_Position, typeof(Transform), true) as Transform;
                serializedObject.FindProperty("BulletPrefab").objectReferenceValue = EditorGUILayout.ObjectField("Bullet Prefab", w.BulletPrefab, typeof(GameObject), false);
                serializedObject.FindProperty("MuzzleFlashParticlePrefab").objectReferenceValue = EditorGUILayout.ObjectField("Muzzle Flash Prefab", w.MuzzleFlashParticlePrefab, typeof(GameObject), false);

                GUILayout.Space(10);
                var firemode = serializedObject.FindProperty("FireMode");
                EditorGUILayout.PropertyField(firemode);

                var aimmode = serializedObject.FindProperty("AimMode");
                EditorGUILayout.PropertyField(aimmode);

                if (w.AimMode == Weapon.WeaponAimMode.Scope)
                {
                    var scopetexture = serializedObject.FindProperty("ScopeTexture");
                    EditorGUILayout.PropertyField(scopetexture);

                    var cameraposition = serializedObject.FindProperty("CameraAimingPosition");
                    EditorGUILayout.PropertyField(cameraposition);

                    serializedObject.FindProperty("CameraFOV").floatValue = EditorGUILayout.Slider("Camera FOV", w.CameraFOV, 10, 75);
                }
                if (w.AimMode == Weapon.WeaponAimMode.CameraApproach)
                {
                    var cameraposition = serializedObject.FindProperty("CameraAimingPosition");
                    EditorGUILayout.PropertyField(cameraposition);
                    serializedObject.FindProperty("CameraFOV").floatValue = EditorGUILayout.Slider("Camera FOV", w.CameraFOV, 10, 75);
                }
                GUILayout.Space(10);
                serializedObject.FindProperty("Fire_Rate").floatValue = EditorGUILayout.FloatField("Fire Rate", w.Fire_Rate);
                serializedObject.FindProperty("BulletsAmounts").intValue = EditorGUILayout.IntField("Bullets in the Gun", w.BulletsAmounts);
                serializedObject.FindProperty("TotalBullets").intValue = EditorGUILayout.IntField("Total Amount of Bullets", w.TotalBullets);
                serializedObject.FindProperty("BulletsPerMagazine").intValue = EditorGUILayout.IntField("Bullets For Reload", w.BulletsPerMagazine);
                GUILayout.Space(5);
                serializedObject.FindProperty("NumberOfShotgunBulletsPerShot").intValue = EditorGUILayout.IntField("Number Of Shotgun Bullets Per Shot", w.NumberOfShotgunBulletsPerShot);
                serializedObject.FindProperty(nameof(w.InfiniteAmmo)).boolValue = EditorGUILayout.Toggle("Infinite Ammo", w.InfiniteAmmo);

            }
        }
        public void ProceduralAnimationVariables(Weapon w)
        {
            if (ProceduralAnimations)
            {
                serializedObject.FindProperty("GenerateProceduralAnimation").boolValue = EditorGUILayout.Toggle("Enable", w.GenerateProceduralAnimation);

                serializedObject.FindProperty("WeaponPositionSpeed").floatValue = EditorGUILayout.Slider("Weapon Position Speed", w.WeaponPositionSpeed, 0, 60);
                serializedObject.FindProperty("WeaponRotationSpeed").floatValue = EditorGUILayout.Slider("Weapon Rotation Speed", w.WeaponRotationSpeed, 0, 60);


                serializedObject.FindProperty("RecoilForce").floatValue = EditorGUILayout.Slider("Recoil Force", w.RecoilForce, -1f, 1f);
                serializedObject.FindProperty("RecoilForceRotation").floatValue = EditorGUILayout.Slider("Recoil Rotation Force", w.RecoilForceRotation, -60, 60);
                serializedObject.FindProperty("CameraRecoilMultiplier").floatValue = EditorGUILayout.Slider("Camera Recoil Multiplier", w.CameraRecoilMultiplier, 0f, 5f);

                GUILayout.Space(10);

                var SliderMovementAxisProperty = serializedObject.FindProperty("SliderMovementAxis");
                EditorGUILayout.PropertyField(SliderMovementAxisProperty);
                serializedObject.FindProperty("GunSlider").objectReferenceValue = EditorGUILayout.ObjectField("Gun Bolt/Slider", w.GunSlider, typeof(Transform), true) as Transform;
                serializedObject.FindProperty("SliderMovementOffset").floatValue = EditorGUILayout.Slider("Slider/Bolt Movement", w.SliderMovementOffset, -0.1f, 0.1f);
                serializedObject.FindProperty("SliderMovementSpeed").floatValue = EditorGUILayout.Slider("Slider/Bolt Speed", w.SliderMovementSpeed, 0f, 2f);

                GUILayout.Space(10);
                serializedObject.FindProperty("BulletCasingPrefab").objectReferenceValue = EditorGUILayout.ObjectField("Bullet Casing", w.BulletCasingPrefab, typeof(GameObject), true);
                serializedObject.FindProperty("IsParticle").boolValue = EditorGUILayout.Toggle("Is Particle System", w.IsParticle);
            }
        }
        public void IKVariables(Weapon w)
        {
            if (IKSettings)
            {
                serializedObject.FindProperty("OppositeHandPosition").objectReferenceValue = EditorGUILayout.ObjectField("Left Hand Position", w.OppositeHandPosition, typeof(Transform), true) as Transform;
            }
        }
        public void AudioVariables(Weapon w)
        {
            if (Audio == true)
            {
                var ShotAudio = serializedObject.FindProperty("ShootAudio");
                EditorGUILayout.PropertyField(ShotAudio);

                var ReloadAudio = serializedObject.FindProperty("ReloadAudio");
                EditorGUILayout.PropertyField(ReloadAudio);

                var WeaponEquipAudio = serializedObject.FindProperty("WeaponEquipAudio");
                EditorGUILayout.PropertyField(WeaponEquipAudio);

                var EmptyMagazineAudio = serializedObject.FindProperty("EmptyMagazineAudio");
                EditorGUILayout.PropertyField(EmptyMagazineAudio);
            }
        }


        //Scene View Handles
        private void OnSceneGUI()
        {
            if (w == null) return;
            Vector3 RealCameraAimingPosition = w.transform.position + w.transform.right * w.CameraAimingPosition.x + w.transform.up * w.CameraAimingPosition.y + w.transform.forward * w.CameraAimingPosition.z;
            //w.CameraAimingPosition = Handles.PositionHandle(w.transform.position + w.CameraAimingPosition, w.transform.rotation);
            if (Shooting)
            {
                if (w.Shoot_Position != null)
                {
                    w.Shoot_Position.position = Handles.PositionHandle(w.Shoot_Position.position, w.transform.rotation);
                    Handles.Label(w.Shoot_Position.position, "Shooting Position");
                    Handles.DrawWireDisc(w.Shoot_Position.position, w.Shoot_Position.forward, 0.04f);
                }
                else
                {
                    //Create Shooting Position
                    GameObject shootposition = new GameObject("Shooting Position");
                    shootposition.transform.position = w.transform.position + w.transform.forward * 0.15f + w.transform.up * 0.1f;
                    shootposition.transform.rotation = w.transform.rotation;
                    w.Shoot_Position = shootposition.transform;
                    shootposition.transform.SetParent(w.transform);
                }
            }

            if (IKSettings)
            {
                if (w.OppositeHandPosition != null)
                {
                    w.OppositeHandPosition.rotation = Handles.RotationHandle(w.OppositeHandPosition.rotation, w.OppositeHandPosition.position);
                    w.OppositeHandPosition.position = Handles.PositionHandle(w.OppositeHandPosition.position, w.transform.rotation);
                }
                else
                {
                    //Create Left Hand IK Position
                    GameObject lefthandikposition = new GameObject("Left Hand IK Position");
                    lefthandikposition.transform.position = w.transform.position + w.transform.forward * 0.03f;
                    lefthandikposition.transform.rotation = w.transform.rotation;
                    w.OppositeHandPosition = lefthandikposition.transform;
                    lefthandikposition.transform.SetParent(w.transform);
                }
            }
            if (WeaponSettings)
            {
                Handles.Label(w.transform.position + Vector3.up * 0.23f, "Name: " + w.ItemName);
                Handles.Label(w.transform.position + Vector3.up * 0.2f, "Switch ID: " + w.ItemSwitchID);
                Handles.Label(w.transform.position + Vector3.up * 0.18f, "Ammunition: " + w.BulletsAmounts + " / " + w.TotalBullets + " Bullets");
            }
        }
    }

}