using UnityEngine;
using UnityEditor;
using JUTPS.PhysicsScripts;

namespace JUTPS.CustomEditors
{
    /// <summary>
    /// Custom editor of the <see cref="AdvancedRagdollController"/> component.
    /// </summary>
    [CustomEditor(typeof(AdvancedRagdollController))]
    public class AdvancedRagdollEditor : Editor
    {
        private bool _isSettingsOpen;
        private bool _isStatesOpen;
        private bool _isDebugEnabled;

        /// <summary>
        /// The <see cref="AdvancedRagdollController"/> instance of this editor.
        /// </summary>
        public AdvancedRagdollController RagdollController
        {
            get => (AdvancedRagdollController)target;
        }

        private void OnEnable()
        {
            // Loading editor state
            _isSettingsOpen = EditorPrefs.GetBool($"{nameof(AdvancedRagdollController)}.{nameof(_isSettingsOpen)}");
            _isStatesOpen = EditorPrefs.GetBool($"{nameof(AdvancedRagdollController)}.{nameof(_isStatesOpen)}");
            _isDebugEnabled = EditorPrefs.GetBool($"{nameof(AdvancedRagdollController)}.{nameof(_isDebugEnabled)}");

            if (RagdollController.RagdollBones == null || RagdollController.RagdollBones.Length == 0)
                RagdollController.StartAdvancedRagdollController();
        }

        private void OnDestroy()
        {
            // Saving editor state
            EditorPrefs.SetBool($"{nameof(AdvancedRagdollController)}.{nameof(_isSettingsOpen)}", _isSettingsOpen);
            EditorPrefs.SetBool($"{nameof(AdvancedRagdollController)}.{nameof(_isStatesOpen)}", _isStatesOpen);
            EditorPrefs.SetBool($"{nameof(AdvancedRagdollController)}.{nameof(_isDebugEnabled)}", _isDebugEnabled);
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Advanced Ragdoll Controller");
            if (RagdollController.RagdollBones == null)
            {
                GUILayout.Label("No Ragdoll Bones found, please create a Ragdoll", JUTPSEditor.CustomEditorStyles.ErrorStyle());
                RagdollController.StartAdvancedRagdollController();
            }
            else if (RagdollController.RagdollBones.Length > 0)
            {
                //SETTINGS
                _isSettingsOpen = GUILayout.Toggle(_isSettingsOpen, "Ragdoll Transition Blending Settings", JUTPSEditor.CustomEditorStyles.Toolbar());
                if (_isSettingsOpen)
                {
                    serializedObject.FindProperty(nameof(RagdollController.TimeToGetUp)).floatValue = EditorGUILayout.Slider("  Time To Get Up", RagdollController.TimeToGetUp, 1f, 4f);
                    serializedObject.FindProperty(nameof(RagdollController.BlendSpeed)).floatValue = EditorGUILayout.Slider("  Blend Speed", RagdollController.BlendSpeed, 0f, 4f);
                    serializedObject.FindProperty(nameof(RagdollController.RagdollDrag)).floatValue = EditorGUILayout.Slider("  Ragdoll Bones Drag", RagdollController.RagdollDrag, 0.001f, 4f);
                }

                //STATE
                _isStatesOpen = GUILayout.Toggle(_isStatesOpen, "Ragdoll States", JUTPSEditor.CustomEditorStyles.Toolbar());
                if (_isStatesOpen)
                    GUILayout.Label(RagdollController.State.ToString(), JUTPSEditor.CustomEditorStyles.EnabledStyle());

                //DEBUG
                _isDebugEnabled = GUILayout.Toggle(_isDebugEnabled, "Debugging", JUTPSEditor.CustomEditorStyles.Toolbar());
                if (_isDebugEnabled == true)
                {
                    serializedObject.FindProperty(nameof(RagdollController.RagdollWhenPressKeyG)).boolValue = EditorGUILayout.Toggle("  Ragdoll When Press G", RagdollController.RagdollWhenPressKeyG);
                    serializedObject.FindProperty(nameof(RagdollController.ViewBodyDirection)).boolValue = EditorGUILayout.Toggle("  View Body Direction", RagdollController.ViewBodyDirection);
                    serializedObject.FindProperty(nameof(RagdollController.ViewBodyPhysics)).boolValue = EditorGUILayout.Toggle("  View Body Phyics", RagdollController.ViewBodyPhysics);
                    serializedObject.FindProperty(nameof(RagdollController.ViewHumanBodyBones)).boolValue = EditorGUILayout.Toggle("  View Body Bones", RagdollController.ViewHumanBodyBones);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}