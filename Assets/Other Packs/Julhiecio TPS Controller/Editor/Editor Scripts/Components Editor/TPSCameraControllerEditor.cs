using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using JUTPS.CameraSystems;

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(TPSCameraController))]
    public class TPSCameraControllerEditor : Editor
    {
        public bool CameraSettings, CameraAutoRotator, CameraRecoilSettings, CameraCustomState, CameraDefaultStates;

        private TPSCameraController camTarget;
        private void OnEnable()
        {
            camTarget = (TPSCameraController)target;
        }
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            serializedObject.Update();



            JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Camera Controller");



            CameraSettings = GUILayout.Toggle(CameraSettings, "Camera Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
            CameraSettingsVariables(camTarget);

            CameraAutoRotator = GUILayout.Toggle(CameraAutoRotator, "Camera Auto Rotator", JUTPSEditor.CustomEditorStyles.Toolbar());
            DrawAutoRotatorVariables(camTarget);

            CameraRecoilSettings = GUILayout.Toggle(CameraRecoilSettings, "Camera Recoil Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
            DrawCameraRecoilSettings(camTarget);

            CameraDefaultStates = GUILayout.Toggle(CameraDefaultStates, "Default Camera States", JUTPSEditor.CustomEditorStyles.Toolbar());
            DrawDefaultCameraStateSettings();


            CameraCustomState = GUILayout.Toggle(CameraCustomState, "Custom Camera States", JUTPSEditor.CustomEditorStyles.Toolbar());
            DrawCustomCameraStateSettings();

            serializedObject.ApplyModifiedProperties();

        }
        public void CameraSettingsVariables(TPSCameraController target)
        {
            if (CameraSettings)
            {
                serializedObject.FindProperty("TargetToFollow").objectReferenceValue =
                    EditorGUILayout.ObjectField("Target To Follow", target.TargetToFollow, typeof(Transform), true) as Transform;

                var raycast_camera_collision = serializedObject.FindProperty("CameraCollisionLayerMask");
                EditorGUILayout.PropertyField(raycast_camera_collision);

                var raycast_crosshair_camera = serializedObject.FindProperty("CrosshairRaycastLayerMask");
                EditorGUILayout.PropertyField(raycast_crosshair_camera);


                if (target.CameraCollisionLayerMask.value == 0)
                {
                    target.CameraCollisionLayerMask = JUTPSEditor.LayerMaskUtilities.CrosshairMask();
                }

                serializedObject.FindProperty("FollowUpTarget").boolValue = EditorGUILayout.ToggleLeft("  Follow Up Target", target.FollowUpTarget, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

                serializedObject.FindProperty("GeneralSensibility").floatValue = EditorGUILayout.Slider("  General Sensibility", target.GeneralSensibility, 0, 5);
                serializedObject.FindProperty("GeneralVerticalSensibility").floatValue = EditorGUILayout.Slider("  General Vertical Sensibility", target.GeneralVerticalSensibility, 0, 5);
                serializedObject.FindProperty(nameof(target.InvertHorizontal)).boolValue = EditorGUILayout.Toggle("  Invert Horizontal", target.InvertHorizontal);
                serializedObject.FindProperty(nameof(target.InvertVertical)).boolValue = EditorGUILayout.Toggle("  Invert Vertical", target.InvertVertical);
            }
        }
        public void DrawAutoRotatorVariables(TPSCameraController target)
        {
            if (CameraAutoRotator)
            {
                serializedObject.FindProperty("EnableAutoRotator").boolValue = EditorGUILayout.ToggleLeft("  Auto Rotator", target.EnableAutoRotator, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
                if (target.EnableAutoRotator)
                {
                    serializedObject.FindProperty("AutoRotateTime").floatValue = EditorGUILayout.FloatField("  Time to Auto Rotation", target.AutoRotateTime);

                    serializedObject.FindProperty("AutoRotationSpeed").floatValue =
                    EditorGUILayout.Slider("  Rotation Speed", target.AutoRotationSpeed, 0, 60);
                    EditorGUILayout.Space();
                }
                serializedObject.FindProperty("EnableVehicleAutoRotation").boolValue = EditorGUILayout.ToggleLeft("  Vehicle Auto Rotator", target.EnableVehicleAutoRotation, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
                if (target.EnableVehicleAutoRotation)
                {
                    serializedObject.FindProperty("VehicleAutoRotateTime").floatValue = EditorGUILayout.FloatField("  Time to Auto Rotation", target.VehicleAutoRotateTime);

                    serializedObject.FindProperty("VehicleAutoRotationSpeed").floatValue =
                   EditorGUILayout.Slider("  Rotation Speed", target.VehicleAutoRotationSpeed, 0, 60);
                    EditorGUILayout.Space();
                }
            }
        }
        public void DrawCameraRecoilSettings(TPSCameraController target)
        {
            if (CameraRecoilSettings)
            {
                serializedObject.FindProperty("CameraRecoilReaction").boolValue
                    = EditorGUILayout.ToggleLeft("  Camera Recoil Reaction", target.CameraRecoilReaction, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

                if (target.CameraRecoilReaction)
                {
                    serializedObject.FindProperty("RecoilRotateCamera").boolValue
                        = EditorGUILayout.ToggleLeft("  Rotate Camera On Recoil", target.RecoilRotateCamera, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

                    serializedObject.FindProperty("CameraRecoilSensibility").floatValue
                        = EditorGUILayout.Slider("  Recoil Sensibility", target.CameraRecoilSensibility, 0, 2);
                }
            }
        }

        public void DrawDefaultCameraStateSettings()
        {
            if (CameraDefaultStates)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var DefaultCamStates = serializedObject.FindProperty("NormalCameraState");
                EditorGUILayout.PropertyField(DefaultCamStates);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var DefaultCamStates2 = serializedObject.FindProperty("FireModeCameraState");
                EditorGUILayout.PropertyField(DefaultCamStates2);
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(camTarget.AimModeCameraState)));
                GUILayout.EndHorizontal();

                //Weapon Aim Sway Options
                GUILayout.BeginHorizontal();
                GUILayout.Space(30);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(camTarget.AimingSwaySettings)));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var DefaultCamStates4 = serializedObject.FindProperty("DrivingVehicleCameraState");
                EditorGUILayout.PropertyField(DefaultCamStates4);
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var DefaultCamStates5 = serializedObject.FindProperty("DeadPlayerCameraState");
                EditorGUILayout.PropertyField(DefaultCamStates5);
                GUILayout.EndHorizontal();
            }
        }
        public void DrawCustomCameraStateSettings()
        {
            if (CameraCustomState)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var CustomCamStates = serializedObject.FindProperty("CustomCameraStates");
                EditorGUILayout.PropertyField(CustomCamStates);
                GUILayout.EndHorizontal();
            }
        }
    }
}