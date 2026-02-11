using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JUTPS.WeaponSystem;

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(WeaponAimRotationCenter))]
    public class WeaponAimRotEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            WeaponAimRotationCenter w = (WeaponAimRotationCenter)target;

            JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Weapon Aim Rotation Center");
            EditorGUILayout.Space(10);

            if (w.WeaponPositionName.Count == 0)
            {
                EditorGUILayout.HelpBox("You still have no weapon position, you will need one to adjust the position of a weapon type. For example: ''Pistol Weapon Position Reference''.", MessageType.Warning);
                EditorGUILayout.Space(10);
            }

            for (int i = 0; i < w.WeaponPositionName.Count; i++)
            {
                DrawWeaponPositionSettings(w, i);
            }

            if (GUILayout.Button("Add Weapon Position Reference", JUTPSEditor.CustomEditorStyles.MiniButtonStyle(), GUILayout.Width(200)))
            {
                w.CreateWeaponPositionReference(w.WeaponPositionName.Count + " | New Weapon Position Reference");
            }
            serializedObject.ApplyModifiedProperties();
        }

        public void DrawWeaponPositionSettings(WeaponAimRotationCenter w, int index)
        {
            GUILayout.BeginHorizontal();

            //NAME
            serializedObject.FindProperty("WeaponPositionName").GetArrayElementAtIndex(index).stringValue = EditorGUILayout.TextField(w.WeaponPositionName[index], JUTPSEditor.CustomEditorStyles.MiniToolbar());

            //ID
            string stringid = "↔ SWITCH ID:" + w.ID[index].ToString();
            EditorGUILayout.LabelField(stringid, JUTPSEditor.CustomEditorStyles.MiniToolbar(), GUILayout.Width(130));

            if (index == w.WeaponPositionTransform.Count - 1)
            {
                //DELETE BUTTON
                if (GUILayout.Button("X", JUTPSEditor.CustomEditorStyles.DangerButtonStyle(), GUILayout.Width(20)))
                {
                    w.RemoveWeaponPositionReference(index);
                }
            }
            GUILayout.EndHorizontal();

            //TRANSFORM REFERENCE
            if (index < w.WeaponPositionTransform.Count)
            {
                serializedObject.FindProperty("WeaponPositionTransform").GetArrayElementAtIndex(index).objectReferenceValue = EditorGUILayout.ObjectField("  Transform Reference", w.WeaponPositionTransform[index], typeof(Transform), true) as Transform;
            }
            EditorGUILayout.Space(5);
        }
    }
}