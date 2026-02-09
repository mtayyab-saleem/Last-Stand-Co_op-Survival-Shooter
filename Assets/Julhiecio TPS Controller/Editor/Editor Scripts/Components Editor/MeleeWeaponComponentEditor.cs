using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JUTPS.WeaponSystem;
namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(MeleeWeapon))]
    public class MeleeComponentEditor : Editor
    {
        //Settings Areas
        public bool HoldableSettings, Wield, IKSettings, MeleeWeaponSettings;
        MeleeWeapon w;

        //Inspector Draw
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            w = (MeleeWeapon)target;


            if (w != null)
            {
                JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Melee Weapon");

                HoldableSettings = GUILayout.Toggle(HoldableSettings, "Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
                ItemSettingsVariables(w);

                MeleeWeaponSettings = GUILayout.Toggle(MeleeWeaponSettings, "Melee Weapon Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
                MeleeSettings(w);

                Wield = GUILayout.Toggle(Wield, "Wielding", JUTPSEditor.CustomEditorStyles.Toolbar());
                WieldingTabVariables(w);

                IKSettings = GUILayout.Toggle(IKSettings, "Left Hand IK", JUTPSEditor.CustomEditorStyles.Toolbar());
                IKVariables(w);
            }

            serializedObject.ApplyModifiedProperties();

            //GUILayout.Space(50);
            //DrawDefaultInspector();
        }

        public void MeleeSettings(MeleeWeapon w)
        {
            if (MeleeWeaponSettings)
            {
                serializedObject.FindProperty("AttackAnimatorParameterName").stringValue = EditorGUILayout.TextField("Attack Animator Parameter Name", w.AttackAnimatorParameterName);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(w.DamagerToEnable)));
                EditorGUILayout.Space();
                serializedObject.FindProperty("EnableHealthLoss").boolValue = EditorGUILayout.Toggle("Enable Health Loss", w.EnableHealthLoss);
                serializedObject.FindProperty("MeleeWeaponHealth").floatValue = EditorGUILayout.FloatField("Health", w.MeleeWeaponHealth);
                serializedObject.FindProperty("DamagePerUse").floatValue = EditorGUILayout.FloatField("Damage Per Use", w.DamagePerUse);
            }
        }

        //Inspector Drawers
        public void ItemSettingsVariables(MeleeWeapon w)
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
        public void WieldingTabVariables(MeleeWeapon w)
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

        public void IKVariables(MeleeWeapon w)
        {
            if (IKSettings)
            {
                serializedObject.FindProperty("OppositeHandPosition").objectReferenceValue = EditorGUILayout.ObjectField("Left Hand Position", w.OppositeHandPosition, typeof(Transform), true) as Transform;
            }
        }
    }
}