using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using JUTPS.Utilities;

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(JUCharacterController))]
    public class TPSControllerEditor : Editor
    {
        private bool _isLocomotionSettingsOpen;
        private bool _isRoundCheckSettingsOpen;
        private bool _isWallCheckSettingsOpen;
        private bool _isStepCorrectionSettingsOpen;
        private bool _isFireModeSettingsTabOpen;
        private bool _isEventsSettingsOpen;
        private bool _isAnimatorSettingsOpen;
        private bool _isStatesOpen;
        private bool _isAdditionalSettingsOpen;

        public List<GameObject> _prefabListToAdd;

        public JUCharacterController JUCharacter { get; private set; }

        public TPSControllerEditor()
        {
            _prefabListToAdd = new List<GameObject>();
        }

        private void OnEnable()
        {
            _isLocomotionSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isLocomotionSettingsOpen)}");
            _isRoundCheckSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isRoundCheckSettingsOpen)}");
            _isWallCheckSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isWallCheckSettingsOpen)}");
            _isStepCorrectionSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isStepCorrectionSettingsOpen)}");
            _isFireModeSettingsTabOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isFireModeSettingsTabOpen)}");
            _isEventsSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isEventsSettingsOpen)}");
            _isAnimatorSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isAnimatorSettingsOpen)}");
            _isStatesOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isStatesOpen)}");
            _isAdditionalSettingsOpen = EditorPrefs.GetBool($"{nameof(TPSControllerEditor)}.{nameof(_isAdditionalSettingsOpen)}");

            JUCharacter = (JUCharacterController)target;
            CharacterLayerMasksStartup();
        }

        private void OnDestroy()
        {
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isLocomotionSettingsOpen)}", _isLocomotionSettingsOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isRoundCheckSettingsOpen)}", _isRoundCheckSettingsOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isWallCheckSettingsOpen)}", _isWallCheckSettingsOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isStepCorrectionSettingsOpen)}", _isStepCorrectionSettingsOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isFireModeSettingsTabOpen)}", _isFireModeSettingsTabOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isEventsSettingsOpen)}", _isEventsSettingsOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isAnimatorSettingsOpen)}", _isAnimatorSettingsOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isStatesOpen)}", _isStatesOpen);
            EditorPrefs.SetBool($"{nameof(TPSControllerEditor)}.{nameof(_isAdditionalSettingsOpen)}", _isAdditionalSettingsOpen);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var toolbarStyle = JUTPSEditor.CustomEditorStyles.Toolbar();

            JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Character Controller");

            _isLocomotionSettingsOpen = GUILayout.Toggle(_isLocomotionSettingsOpen, "Locomotion", toolbarStyle);
            MovementSettingsVariables();

            _isRoundCheckSettingsOpen = GUILayout.Toggle(_isRoundCheckSettingsOpen, "Ground Check", toolbarStyle);
            GroundCheckSettingsVariables();

            _isWallCheckSettingsOpen = GUILayout.Toggle(_isWallCheckSettingsOpen, "Wall Check", toolbarStyle);
            WallCheckSettingsVariables();

            _isStepCorrectionSettingsOpen = GUILayout.Toggle(_isStepCorrectionSettingsOpen, "Auto Step Up", toolbarStyle);
            StepCorrectionSettingsVariables();

            _isFireModeSettingsTabOpen = GUILayout.Toggle(_isFireModeSettingsTabOpen, "Fire Mode", toolbarStyle);
            FireModeSettings();

            _isAnimatorSettingsOpen = GUILayout.Toggle(_isAnimatorSettingsOpen, "Animator", toolbarStyle);
            AnimatorSettingsVariables();

            _isEventsSettingsOpen = GUILayout.Toggle(_isEventsSettingsOpen, "Default Events", toolbarStyle);
            EventsSettingsVariables();

            _isAdditionalSettingsOpen = GUILayout.Toggle(_isAdditionalSettingsOpen, "Controller Options", toolbarStyle);
            AdditionalSettingsDrawer();

            _isStatesOpen = GUILayout.Toggle(_isStatesOpen, "Controller States", toolbarStyle);
            StatesViewVariables();

            serializedObject.ApplyModifiedProperties();
        }

        private void ExempleSettingsVariables()
        {
            if (_isLocomotionSettingsOpen)
                CharacterSettingsGizmosViewer();
        }

        private void MovementSettingsVariables()
        {
            if (!_isLocomotionSettingsOpen)
                return;

            serializedObject.FindProperty(nameof(JUCharacter.UseDefaultControllerInput)).boolValue = EditorGUILayout.Toggle("Use Default Inputs", JUCharacter.UseDefaultControllerInput);

            //Move On Forward When Isnt Aiming
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.LocomotionMode)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.SetRigidbodyVelocity)));

            serializedObject.FindProperty(nameof(JUCharacter.Speed)).floatValue = EditorGUILayout.Slider("  General Speed", JUCharacter.Speed, 0, 30);

            serializedObject.FindProperty(nameof(JUCharacter.WalkSpeed)).floatValue = EditorGUILayout.Slider("    Walk Speed", JUCharacter.WalkSpeed, 0, 5);
            serializedObject.FindProperty(nameof(JUCharacter.CrouchSpeed)).floatValue = EditorGUILayout.Slider("    Crouch Speed", JUCharacter.CrouchSpeed, 0, 5);
            serializedObject.FindProperty(nameof(JUCharacter.RunSpeed)).floatValue = EditorGUILayout.Slider("    Run Speed", JUCharacter.RunSpeed, 0, 5);
            serializedObject.FindProperty(nameof(JUCharacter.SprintingSpeedMax)).floatValue = EditorGUILayout.Slider("    Sprint Max Speed", JUCharacter.SprintingSpeedMax, 0, 5);

            serializedObject.FindProperty(nameof(JUCharacter.RotationSpeed)).floatValue = EditorGUILayout.Slider("  Rotation Speed", JUCharacter.RotationSpeed, 0, 30);
            serializedObject.FindProperty(nameof(JUCharacter.JumpForce)).floatValue = EditorGUILayout.Slider("  Jump Force", JUCharacter.JumpForce, 1, 10);
            serializedObject.FindProperty(nameof(JUCharacter.AirInfluenceControll)).floatValue = EditorGUILayout.Slider("  In Air Control Force", JUCharacter.AirInfluenceControll, 0, 100);
            serializedObject.FindProperty(nameof(JUCharacter.StoppingSpeed)).floatValue = EditorGUILayout.Slider("  Stopping Speed", JUCharacter.StoppingSpeed, 0.1f, 5);
            serializedObject.FindProperty(nameof(JUCharacter.MaxWalkableAngle)).floatValue = EditorGUILayout.Slider("  Max Walkable Angle", JUCharacter.MaxWalkableAngle, 0, 89);

            serializedObject.FindProperty(nameof(JUCharacter.MovementAffectsWeaponAccuracy)).boolValue = EditorGUILayout.ToggleLeft("  Movement Affects Weapon Accuracy", JUCharacter.MovementAffectsWeaponAccuracy, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
            if (JUCharacter.MovementAffectsWeaponAccuracy)
            {
                serializedObject.FindProperty(nameof(JUCharacter.OnMovePrecision)).floatValue = EditorGUILayout.Slider("  On Move Precision", JUCharacter.OnMovePrecision, 0, 16);
            }


            serializedObject.FindProperty(nameof(JUCharacter.GroundAngleDesaceleration)).boolValue = EditorGUILayout.ToggleLeft("  High Inclines Slow Down", JUCharacter.GroundAngleDesaceleration, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
            if (JUCharacter.GroundAngleDesaceleration)
            {
                serializedObject.FindProperty(nameof(JUCharacter.GroundAngleDesacelerationMultiplier)).floatValue = EditorGUILayout.Slider("  Intensity", JUCharacter.GroundAngleDesacelerationMultiplier, 0, 2);
            }

            serializedObject.FindProperty(nameof(JUCharacter.CurvedMovement)).boolValue = EditorGUILayout.ToggleLeft("  Curved Movement", JUCharacter.CurvedMovement, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
            serializedObject.FindProperty(nameof(JUCharacter.LerpRotation)).boolValue = EditorGUILayout.ToggleLeft("  Lerp Rotation", JUCharacter.LerpRotation, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

            serializedObject.FindProperty(nameof(JUCharacter.BodyInclination)).boolValue = EditorGUILayout.ToggleLeft("  Body Lean", JUCharacter.BodyInclination, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

            serializedObject.FindProperty(nameof(JUCharacter.RootMotion)).boolValue = EditorGUILayout.ToggleLeft("  Root Motion", JUCharacter.RootMotion, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

            if (JUCharacter.RootMotion)
            {
                serializedObject.FindProperty(nameof(JUCharacter.RootMotionSpeed)).floatValue = EditorGUILayout.Slider("  Root Motion Speed", JUCharacter.RootMotionSpeed, 0, 10);
                serializedObject.FindProperty(nameof(JUCharacter.RootMotionRotation)).boolValue = EditorGUILayout.Toggle("  Root Motion Rotation", JUCharacter.RootMotionRotation);
            }

            serializedObject.FindProperty(nameof(JUCharacter.AutoRun)).boolValue = EditorGUILayout.ToggleLeft("  Auto Run", JUCharacter.AutoRun, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
            if (JUCharacter.AutoRun)
            {
                serializedObject.FindProperty(nameof(JUCharacter.WalkOnRunButton)).boolValue = EditorGUILayout.Toggle("  Walk On Run Button", JUCharacter.WalkOnRunButton);
                serializedObject.FindProperty(nameof(JUCharacter.SprintOnRunButton)).boolValue = EditorGUILayout.Toggle("  Sprint On Run Button", JUCharacter.SprintOnRunButton);
                serializedObject.FindProperty(nameof(JUCharacter.UnlimitedSprintDuration)).boolValue = EditorGUILayout.Toggle("  Unlimited Sprint Duration", JUCharacter.UnlimitedSprintDuration);

            }
            serializedObject.FindProperty(nameof(JUCharacter.SprintingSkill)).boolValue = EditorGUILayout.ToggleLeft("  Enable Sprint Skill", JUCharacter.SprintingSkill, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
            if (JUCharacter.SprintingSkill)
            {
                serializedObject.FindProperty(nameof(JUCharacter.SprintingAcceleration)).floatValue = EditorGUILayout.Slider("  Sprinting Acceleration", JUCharacter.SprintingAcceleration, 0, 10);
                serializedObject.FindProperty(nameof(JUCharacter.SprintingDeceleration)).floatValue = EditorGUILayout.Slider("  Sprinting Deceleration", JUCharacter.SprintingDeceleration, 0, 10);
            }
            serializedObject.FindProperty(nameof(JUCharacter.DecreaseSpeedOnJump)).boolValue = EditorGUILayout.ToggleLeft("  Decrease Speed On Jump", JUCharacter.DecreaseSpeedOnJump, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());

            CharacterSettingsGizmosViewer();
        }

        private void GroundCheckSettingsVariables()
        {
            if (!_isRoundCheckSettingsOpen)
                return;

            LayerMask tempMask = EditorGUILayout.MaskField("  Ground Layer", UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(JUCharacter.WhatIsGround), UnityEditorInternal.InternalEditorUtility.layers);
            serializedObject.FindProperty(nameof(JUCharacter.WhatIsGround)).intValue = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            serializedObject.FindProperty(nameof(JUCharacter.GroundCheckRadius)).floatValue = EditorGUILayout.Slider("  Radius", JUCharacter.GroundCheckRadius, 0.01f, 0.2f);
            serializedObject.FindProperty(nameof(JUCharacter.GroundCheckSize)).floatValue = EditorGUILayout.Slider("  Height", JUCharacter.GroundCheckSize, 0.05f, 0.5f);
            serializedObject.FindProperty(nameof(JUCharacter.GroundCheckHeighOfsset)).floatValue = EditorGUILayout.Slider("  Up Ofsset", JUCharacter.GroundCheckHeighOfsset, -1f, 1f);
            CharacterSettingsGizmosViewer();
        }

        private void WallCheckSettingsVariables()
        {
            if (!_isWallCheckSettingsOpen == true)
                return;

            LayerMask tempMask = EditorGUILayout.MaskField("  Wall Layers", UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(JUCharacter.WhatIsWall), UnityEditorInternal.InternalEditorUtility.layers);
            serializedObject.FindProperty(nameof(JUCharacter.WhatIsWall)).intValue = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            serializedObject.FindProperty(nameof(JUCharacter.WallRayHeight)).floatValue = EditorGUILayout.Slider("  Wall Ray Height", JUCharacter.WallRayHeight, -5, 5);
            serializedObject.FindProperty(nameof(JUCharacter.WallRayDistance)).floatValue = EditorGUILayout.Slider("  Wall Ray Distance", JUCharacter.WallRayDistance, 0.1f, 5f);
            CharacterSettingsGizmosViewer();
        }

        private void StepCorrectionSettingsVariables()
        {
            if (!_isStepCorrectionSettingsOpen)
                return;

            serializedObject.FindProperty(nameof(JUCharacter.EnableStepCorrection)).boolValue = EditorGUILayout.Toggle("  Step Correction", JUCharacter.EnableStepCorrection);
            serializedObject.FindProperty(nameof(JUCharacter.UpStepSpeed)).floatValue = EditorGUILayout.Slider("  Up Step Speed", JUCharacter.UpStepSpeed, 2, 15);

            LayerMask tempMask = EditorGUILayout.MaskField("  Step Correction Layers", UnityEditorInternal.InternalEditorUtility.LayerMaskToConcatenatedLayersMask(JUCharacter.StepCorrectionMask), UnityEditorInternal.InternalEditorUtility.layers);
            serializedObject.FindProperty(nameof(JUCharacter.StepCorrectionMask)).intValue = UnityEditorInternal.InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            serializedObject.FindProperty(nameof(JUCharacter.FootstepHeight)).floatValue = EditorGUILayout.Slider("  Step Raycast Distance", JUCharacter.FootstepHeight, 0.1f, 1f);
            serializedObject.FindProperty(nameof(JUCharacter.ForwardStepOffset)).floatValue = EditorGUILayout.Slider("  Forward Offset", JUCharacter.ForwardStepOffset, 0f, 1f);
            serializedObject.FindProperty(nameof(JUCharacter.StepHeight)).floatValue = EditorGUILayout.Slider("  Step Height", JUCharacter.StepHeight, 0.01f, JUCharacter.FootstepHeight);
            GUILayout.Space(10);
            serializedObject.FindProperty(nameof(JUCharacter.EnableUngroundedStepUp)).boolValue = EditorGUILayout.Toggle("  Enable Ungrounded Step Up", JUCharacter.EnableUngroundedStepUp);
            serializedObject.FindProperty(nameof(JUCharacter.UngroundedStepUpSpeed)).floatValue = EditorGUILayout.FloatField("  UngroundedStepUp Speed", JUCharacter.UngroundedStepUpSpeed);
            serializedObject.FindProperty(nameof(JUCharacter.UngroundedStepUpRayDistance)).floatValue = EditorGUILayout.FloatField("  UngroundedStepUp Ray Distance", JUCharacter.UngroundedStepUpRayDistance);
            serializedObject.FindProperty(nameof(JUCharacter.StoppingTimeOnStepPosition)).floatValue = EditorGUILayout.FloatField("  Stopping Time On StepPosition", JUCharacter.StoppingTimeOnStepPosition);

            CharacterSettingsGizmosViewer();
        }

        private void DropItensField()
        {
            //Get current events
            Event GUIEvent = Event.current;

            //Draw drop box area
            Rect DragAndDropItemArea = GUILayoutUtility.GetRect(0.0f, 35.0f, GUILayout.ExpandWidth(true));
            GUI.Box(DragAndDropItemArea, "Drop item prefab here to add +", JUTPSEditor.CustomEditorStyles.MiniToolbar());

            // Receive dropped itens
            switch (GUIEvent.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!DragAndDropItemArea.Contains(GUIEvent.mousePosition)) { return; }
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (GUIEvent.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (GameObject DropedGameObject in DragAndDrop.objectReferences)
                        {
                            _prefabListToAdd.Add(DropedGameObject);
                        }
                    }
                    break;
            }

            //ADD ITENS
            if (_prefabListToAdd.Count > 0)
            {
                foreach (GameObject ItemToAdd in _prefabListToAdd)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(" + | " + ItemToAdd.name, JUTPSEditor.CustomEditorStyles.NormalStateStyle(), GUILayout.Width(200));
                    if (GUILayout.Button("X", JUTPSEditor.CustomEditorStyles.DangerButtonStyle(), GUILayout.Width(20)))
                    {
                        _prefabListToAdd.Remove(ItemToAdd);
                        Debug.Log("deleted");
                    }
                    GUILayout.EndHorizontal();
                }
                if (GUILayout.Button("Add Itens", EditorStyles.miniButtonMid))
                {
                    //Null error
                    if (JUCharacter.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand) == null)
                    {
                        Debug.LogError("Items cannot be added because the character's right hand cannot be found.");
                        return;
                    }
                    //Add itens
                    foreach (GameObject ItemToAdd in _prefabListToAdd)
                    {
                        Vector3 rotation = new Vector3(-100, -180, 280);
                        Vector3 position = JUCharacter.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).position;
                        var item = Instantiate(ItemToAdd, position, Quaternion.identity, JUCharacter.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand));
                        item.transform.localEulerAngles = rotation;
                    }
                    _prefabListToAdd.Clear();
                }
                GUILayout.Space(20);
            }
        }

        private void FireModeSettings()
        {
            if (_isFireModeSettingsTabOpen)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.PivotItemRotation)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.HumanoidSpine)));

                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.AimMode)));
                serializedObject.FindProperty(nameof(JUCharacter.FireModeMaxTime)).floatValue = EditorGUILayout.Slider("FireMode Max Time", JUCharacter.FireModeMaxTime, 0, 50);
                serializedObject.FindProperty(nameof(JUCharacter.FireModeWalkSpeed)).floatValue = EditorGUILayout.Slider("FireMode Walk Speed", JUCharacter.FireModeWalkSpeed, 0, 5);
                serializedObject.FindProperty(nameof(JUCharacter.FireModeRunSpeed)).floatValue = EditorGUILayout.Slider("FireMode Run Speed", JUCharacter.FireModeRunSpeed, 0, 5);
                serializedObject.FindProperty(nameof(JUCharacter.FireModeCrouchSpeed)).floatValue = EditorGUILayout.Slider("FireMode Crouch Speed", JUCharacter.FireModeCrouchSpeed, 0, 5);
            }
        }

        private void AnimatorSettingsVariables()
        {
            if (_isAnimatorSettingsOpen)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.anim)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.AnimatorParameters)));
            }
        }

        private void EventsSettingsVariables()
        {
            if (_isEventsSettingsOpen)
            {
                serializedObject.FindProperty(nameof(JUCharacter.RagdollWhenDie)).boolValue = EditorGUILayout.ToggleLeft("Enable Ragdoll When Die", JUCharacter.RagdollWhenDie, JUTPSEditor.CustomEditorStyles.MiniLeftButtonStyle());
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.Events)));
            }
        }

        private void StatesViewVariables()
        {
            if (_isStatesOpen)
            {
                if (JUCharacter.CharacterHealth != null)
                {
                    //Style
                    var lifestyle = new GUIStyle(JUTPSEditor.CustomEditorStyles.Toolbar());
                    Color fullifecolor = new Color(0.2f, 1, 0.1f);
                    Color nolifecolor = new Color(1f, 0.5f, 0.5f);
                    lifestyle.normal.textColor = Color.Lerp(nolifecolor, fullifecolor, JUCharacter.CharacterHealth.Health / JUCharacter.CharacterHealth.MaxHealth);
                    lifestyle.alignment = TextAnchor.MiddleCenter;
                    lifestyle.fontSize = 12;

                    //Health Display
                    int health_int = (int)JUCharacter.CharacterHealth.Health;
                    EditorGUILayout.LabelField("Health: " + health_int.ToString() + "%", lifestyle, GUILayout.Width(120));
                    //Health Slider
                    JUCharacter.CharacterHealth.Health = GUILayout.HorizontalSlider(JUCharacter.CharacterHealth.Health, 0, JUCharacter.CharacterHealth.MaxHealth, GUILayout.Width(120), GUILayout.Height(2));
                }
                else
                {
                    EditorGUILayout.LabelField("Without health status, add the JU Health component");
                    if (JUCharacter.GetComponent<JUHealth>() != null)
                    {
                        JUCharacter.CharacterHealth = JUCharacter.GetComponent<JUHealth>();
                    }
                }
                GUILayout.Space(20);


                GUILayout.BeginHorizontal();
                GUILayout.Toggle(JUCharacter.IsDead, "Dead", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.IsMoving, "Moving", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.IsRunning, "Running", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Toggle(JUCharacter.IsRolling, "Rolling", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.IsGrounded, "Grounded", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.IsJumping, "Jumping", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Toggle(JUCharacter.IsMeleeAttacking, "Attacking", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.IsItemEquiped, "Armed", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.IsDriving, "Driving", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Toggle(JUCharacter.ToPickupItem, "To Pick Up Weapon", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.FiringModeIK, "Inverse Kinematics", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.EndHorizontal();


                GUILayout.BeginHorizontal();
                GUILayout.Toggle(JUCharacter.WallAHead, "Wall Ahead", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.Toggle(JUCharacter.UsedItem, "Shooting", JUTPSEditor.CustomEditorStyles.StateStyle(), GUILayout.Width(120));
                GUILayout.EndHorizontal();


                GUILayout.Space(10);

            }
        }

        private void AdditionalSettingsDrawer()
        {
            if (_isAdditionalSettingsOpen)
            {
                EditorGUILayout.LabelField("Block Movement Input", EditorStyles.boldLabel);
                serializedObject.FindProperty(nameof(JUCharacter.BlockVerticalInput)).boolValue = EditorGUILayout.Toggle("  Block Vertical Input", JUCharacter.BlockVerticalInput);
                serializedObject.FindProperty(nameof(JUCharacter.BlockHorizontalInput)).boolValue = EditorGUILayout.Toggle("  Block Horizontal Input", JUCharacter.BlockHorizontalInput);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Block Firing Mode", EditorStyles.boldLabel);
                serializedObject.FindProperty(nameof(JUCharacter.BlockFireModeOnCursorVisible)).boolValue = EditorGUILayout.Toggle("  Block FireMode On Cursor Visible", JUCharacter.BlockFireModeOnCursorVisible);
                serializedObject.FindProperty(nameof(JUCharacter.BlockFireModeOnPunching)).boolValue = EditorGUILayout.Toggle("  Block FireMode On Punching", JUCharacter.BlockFireModeOnPunching);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Default Skills", EditorStyles.boldLabel);
                serializedObject.FindProperty(nameof(JUCharacter.EnablePunchAttacks)).boolValue = EditorGUILayout.Toggle("  Enable Punch Attacks", JUCharacter.EnablePunchAttacks);
                serializedObject.FindProperty(nameof(JUCharacter.EnableRoll)).boolValue = EditorGUILayout.Toggle("  Enable Roll", JUCharacter.EnableRoll);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Artificial Intelligence", EditorStyles.boldLabel);
                serializedObject.FindProperty("IsArtificialIntelligence").boolValue = EditorGUILayout.Toggle("  Is AI", JUCharacter.IsArtificialIntelligence);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Physical Damage System", EditorStyles.boldLabel);
                serializedObject.FindProperty(nameof(JUCharacter.PhysicalDamage)).boolValue = EditorGUILayout.Toggle("  Enable Physical Damage", JUCharacter.PhysicalDamage);
                serializedObject.FindProperty(nameof(JUCharacter.DoRagdollPhysicalDamage)).boolValue = EditorGUILayout.Toggle("  Do Ragdoll On Physical Damage", JUCharacter.DoRagdollPhysicalDamage);
                serializedObject.FindProperty(nameof(JUCharacter.PhysicalDamageStartAt)).floatValue = EditorGUILayout.FloatField("  Physical Damage Start At", JUCharacter.PhysicalDamageStartAt);
                serializedObject.FindProperty(nameof(JUCharacter.PhysicalDamageMultiplier)).floatValue = EditorGUILayout.FloatField("  Physical Damage Multiplier", JUCharacter.PhysicalDamageMultiplier);
                serializedObject.FindProperty(nameof(JUCharacter.RagdollStartAtDamage)).floatValue = EditorGUILayout.FloatField("  Ragdoll Start At Damage", JUCharacter.RagdollStartAtDamage);
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(JUCharacter.PhysicalDamageIgnoreTags)));

                EditorGUILayout.Space();
            }
        }

        private void CharacterSettingsGizmosViewer()
        {
            if (!JUCharacter.TryGetComponent(out JUCharacterSettingsDrawer pldrw))
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Warning: there is no *JU Character Settings Drawer* on your character", MessageType.Warning);
                if (GUILayout.Button("Add Component ''JU Character Settings Drawer'' ", EditorStyles.miniButtonMid))
                {
                    JUCharacterSettingsDrawer settingsDrawer = (JUCharacterSettingsDrawer)Undo.AddComponent(JUCharacter.gameObject, typeof(JUCharacterSettingsDrawer));
                    settingsDrawer.GroundCheck = true;
                    settingsDrawer.StepCorrection = true;
                    Debug.Log("Added JU Character Settings Drawer Component for the character");
                }
                GUILayout.Space(10);
            }
        }

        private void CharacterLayerMasksStartup()
        {
            if (JUCharacter.WhatIsGround == 0)
                JUCharacter.WhatIsGround = JUTPSEditor.LayerMaskUtilities.GroundMask();
            if (JUCharacter.StepCorrectionMask == 0)
                JUCharacter.StepCorrectionMask = JUTPSEditor.LayerMaskUtilities.GroundMask();
        }
    }
}