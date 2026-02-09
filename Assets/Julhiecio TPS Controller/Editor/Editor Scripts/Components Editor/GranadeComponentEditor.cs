using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JUTPS.WeaponSystem;
using JUTPS.ItemSystem;

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(Granade))]
    public class GranadeComponentEditor : Editor
    {
        //Settings Areas
        public bool HoldableSettings, Wield, IKSettings, ThrowSettings, ExplosionSettings;
        Granade w;

        //Inspector Draw
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            w = (Granade)target;


            if (w != null)
            {
                JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Granade");

                HoldableSettings = GUILayout.Toggle(HoldableSettings, "Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
                ItemSettingsVariables(w);

                Wield = GUILayout.Toggle(Wield, "Wielding", JUTPSEditor.CustomEditorStyles.Toolbar());
                WieldingTabVariables(w);

                IKSettings = GUILayout.Toggle(IKSettings, "Left Hand IK", JUTPSEditor.CustomEditorStyles.Toolbar());
                IKVariables(w);

                ThrowSettings = GUILayout.Toggle(ThrowSettings, "Throw Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
                ThrowableSettings(w);

                ExplosionSettings = GUILayout.Toggle(ExplosionSettings, "Explosion", JUTPSEditor.CustomEditorStyles.Toolbar());
                GranadeSettings(w);
            }

            serializedObject.ApplyModifiedProperties();

            //GUILayout.Space(50);

            //DrawDefaultInspector();
        }

        public void ThrowableSettings(ThrowableItem w)
        {
            if (ThrowSettings)
            {
                serializedObject.FindProperty("AnimationTriggerParameterName").stringValue = EditorGUILayout.TextField("Animation Trigger Parameter Name", w.AnimationTriggerParameterName);
                serializedObject.FindProperty("ThrowForce").floatValue = EditorGUILayout.FloatField("Throw Force", w.ThrowForce);
                serializedObject.FindProperty("ThrowUpForce").floatValue = EditorGUILayout.FloatField("Throw UpForce", w.ThrowUpForce);
                serializedObject.FindProperty("RotationForce").floatValue = EditorGUILayout.FloatField("Rotation Force", w.RotationForce);
                //serializedObject.FindProperty("ItemMass").floatValue = EditorGUILayout.FloatField("Item Mass", w.ItemMass);
                serializedObject.FindProperty("SecondsToDestroy").floatValue = EditorGUILayout.FloatField("Seconds To Destroy", w.SecondsToDestroy);

                serializedObject.FindProperty("PositionToThrow").vector3Value = EditorGUILayout.Vector3Field("Position To Throw", w.PositionToThrow);
                serializedObject.FindProperty("DirectionToThrow").vector3Value = EditorGUILayout.Vector3Field("Direction To Throw", w.DirectionToThrow);
            }
        }
        public void GranadeSettings(Granade w)
        {
            if (ExplosionSettings)
            {
                serializedObject.FindProperty("ExplosionPrefab").objectReferenceValue = EditorGUILayout.ObjectField("Explosion Prefab", w.ExplosionPrefab, typeof(GameObject), false) as GameObject;
                serializedObject.FindProperty("TimeToExplode").floatValue = EditorGUILayout.FloatField("Time To Explode", w.TimeToExplode);
                serializedObject.FindProperty("TimeToDestroyExplosionPrefab").floatValue = EditorGUILayout.FloatField("Time To Destroy Explosion Prefab", w.TimeToDestroyExplosionPrefab);
            }
        }

        //Inspector Drawers
        public void ItemSettingsVariables(Granade w)
        {
            if (HoldableSettings)
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
                serializedObject.FindProperty("ItemModelInBody").objectReferenceValue = EditorGUILayout.ObjectField("Item on Body", w.ItemModelInBody, typeof(GameObject), true) as GameObject;
                serializedObject.FindProperty("SingleUseItem").boolValue = EditorGUILayout.Toggle("Single Use Item", w.SingleUseItem);
                serializedObject.FindProperty("ContinuousUseItem").boolValue = EditorGUILayout.Toggle("Continuous Use Item", w.ContinuousUseItem);
                serializedObject.FindProperty("BlockFireMode").boolValue = EditorGUILayout.Toggle("Block Fire Mode", w.BlockFireMode);
            }
        }
        public void WieldingTabVariables(Granade w)
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
                serializedObject.FindProperty("DualItemToWielding").objectReferenceValue = EditorGUILayout.ObjectField("Dual Item To Wielding", w.DualItemToWielding, typeof(GameObject), true) as GameObject;
            }
        }

        public void IKVariables(Granade w)
        {
            if (IKSettings)
            {
                serializedObject.FindProperty("OppositeHandPosition").objectReferenceValue = EditorGUILayout.ObjectField("Left Hand Position", w.OppositeHandPosition, typeof(Transform), true) as Transform;
            }
        }
    }
}
