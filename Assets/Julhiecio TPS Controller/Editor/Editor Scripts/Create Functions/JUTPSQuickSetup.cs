using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using JUTPS;
using JUTPS.AI;
using JUTPS.ActionScripts;
using JUTPS.InventorySystem;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;
using JUTPS.PhysicsScripts;
using JUTPS.FX;

namespace JUTPSEditor
{
    public class JUTPSQuickSetup
    {
        [MenuItem("GameObject/JUTPS Create/Quick Setup/JU Character/Advanced TPS Controller", false, 0)]
        public static void SetupAdvancedTPSCharacter()
        {
            if (Selection.activeGameObject == null)
            {
                //Debug.LogWarning("No character selected. Please select a Humanoid Character and try again");
                JUTPSEditor.MessageWindow.ShowMessage("No character selected. Please select a HUMANOID character and try again", "No Selection", "OK", 125, 276, 14, MessageType.Warning);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                SetupCharacterController(pl, moveSpeed: 3, rotationSpeed: 3, stoppingSpeed: 2f, curvedMovement: true, lerpRotation: true, useRootMotion: true, addFootplacer: true, addFootstep: true, addDrivingAnimation: true, addBodyLean: true, addRagdoller: true, blockDefaultInputs: false);
            }
        }
        [MenuItem("GameObject/JUTPS Create/Quick Setup/JU Character/Simple TPS Controller", false, 0)]
        public static void SetupSimpleTPSCharacter()
        {
            if (Selection.activeGameObject == null)
            {
                //Debug.LogWarning("No character selected. Please select a Humanoid Character and try again");
                JUTPSEditor.MessageWindow.ShowMessage("No character selected. Please select a HUMANOID character and try again", "No Selection", "OK", 125, 276, 14, MessageType.Warning);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                SetupCharacterController(pl, moveSpeed: 3, rotationSpeed: 3, stoppingSpeed: 2f, curvedMovement: true, lerpRotation: true, useRootMotion: false, addFootplacer: false, addFootstep: true, addDrivingAnimation: false, addBodyLean: false, addRagdoller: false, addInventory: false, blockDefaultInputs: false);
            }
        }
        [MenuItem("GameObject/JUTPS Create/Quick Setup/JU Character/2.5D Sidescroller Controller", false, 0)]
        public static void SetupSidescrollerCharacter()
        {
            if (Selection.activeGameObject == null)
            {
                //Debug.LogWarning("No character selected. Please select a Humanoid Character and try again");
                JUTPSEditor.MessageWindow.ShowMessage("No character selected. Please select a HUMANOID character and try again", "No Selection", "OK", 125, 276, 14, MessageType.Warning);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                SetupCharacterController(pl, moveSpeed: 3, rotationSpeed: 5, stoppingSpeed: 2f, curvedMovement: true, lerpRotation: true, useRootMotion: true, addFootplacer: true, addFootstep: true, addDrivingAnimation: false, addBodyLean: false, addRagdoller: false, blockDefaultInputs: false);
                Undo.RecordObject(pl, "Sidescroller Setup");
                pl.GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>().JumpForce = 4;
                Undo.AddComponent(pl, typeof(SidescrollerLocomotion));
                AimOnMousePosition aimMouse = (AimOnMousePosition)Undo.AddComponent(pl, typeof(AimOnMousePosition));
                aimMouse.TwoDimensional = true;
            }
        }
        [MenuItem("GameObject/JUTPS Create/Quick Setup/JU Character/TopDown Controller", false, 0)]
        public static void SetupTopDownCharacter()
        {
            if (Selection.activeGameObject == null)
            {
                //Debug.LogWarning("No character selected. Please select a Humanoid Character and try again");
                JUTPSEditor.MessageWindow.ShowMessage("No character selected. Please select a HUMANOID character and try again", "No Selection", "OK", 125, 276, 14, MessageType.Warning);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                SetupCharacterController(pl, moveSpeed: 3, rotationSpeed: 3, stoppingSpeed: 3f, curvedMovement: true, lerpRotation: true, useRootMotion: true, addFootplacer: true, addFootstep: true, addDrivingAnimation: true, addBodyLean: true, addRagdoller: true, blockDefaultInputs: false);
                Undo.RecordObject(pl, "TopDownSetup");
                pl.GetComponent<JUCharacterController>().BlockFireModeOnCursorVisible = false;
                AimOnMousePosition aimMouse = (AimOnMousePosition)Undo.AddComponent(pl, typeof(AimOnMousePosition));
                aimMouse.PreventResetingAimPosition = true;

                JUTPSEditor.MessageWindow.ShowMessage("Top Down Setup Note: the variable 'BlockFireModeOnCursorInvisible' has been disabled for the TopDown Locomotion to work, this means the cursor doesn't need to be hidden to be able to aim and shoot", "Top Down Setup Note", "OK", 150, 276, 14, MessageType.Info);

                //Debug.Log("Top Down Setup Note: the variable 'BlockFireModeOnCursorInvisible' has been disabled for the TopDown Locomotion to work, this means the cursor doesn't need to be hidden to be able to aim and shoot");
            }
        }



        [MenuItem("GameObject/JUTPS Create/Quick Setup/Add/Setup Character Hit Boxes", false, 0)]
        public static void SetupHitBoxes()
        {
            if (Selection.activeGameObject == null)
            {
                JUTPSEditor.MessageWindow.ShowMessage("No JU CHARACTER selected. Please select a JU CHARACTER and try again", "No JU Character", "OK", 125, 276, 14, MessageType.Warning);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                if (pl.TryGetComponent(out JUCharacterController tps))
                {

                    if (Resources.Load("HitBox") == null)
                    {
                        //Debug.LogError("Unable to load hitbox prefab from resources folder");
                        JUTPSEditor.MessageWindow.ShowMessage("HITBOX setup could not be done because cannot load HitBox prefab from Resources folder", "Error", "OK", 125, 276, 14, MessageType.Error);
                    }
                    else
                    {
                        // >>> HIT BOX SETUP
                        GameObject HitBoxPrefab = Resources.Load("HitBox") as GameObject;

                        Animator anim = tps.GetComponent<Animator>();
                        if (anim == null)
                        {
                            JUTPSEditor.MessageWindow.ShowMessage("HITBOX setup could not be done because there's no Animator in the selected GameObject", "Error", "OK", 125, 276, 14, MessageType.Error);
                            return;
                        }
                        else
                        {
                            //Humanoid error
                            if (anim.isHuman == false)
                            {
                                Debug.LogError("Your character rig is not HUMANOID type, please use a humanoid type character.", anim);
                                JUTPSEditor.MessageWindow.ShowMessage("Your character rig is not HUMANOID type, please use a humanoid type character.", "Setup could not be done", "OK", 125, 276, 14, MessageType.Error);
                                return;
                            }
                        }

                        Transform leftArm = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
                        Transform rightArm = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
                        Transform leftKnee = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
                        Transform rightKnee = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

                        if (leftArm.GetComponentInChildren<Damager>(true) == false)
                        {
                            GameObject leftArmHitBox = (GameObject)PrefabUtility.InstantiatePrefab(HitBoxPrefab, leftArm);
                            leftArmHitBox.transform.localPosition = Vector3.zero;
                            leftArmHitBox.transform.localRotation = Quaternion.identity;
                            leftArmHitBox.transform.position = leftArmHitBox.transform.position + leftArmHitBox.transform.up * 0.7f;
                            Undo.RegisterCreatedObjectUndo(leftArmHitBox, "Hit Box Setup");
                        }

                        if (rightArm.GetComponentInChildren<Damager>(true) == false)
                        {
                            GameObject rightArmHitBox = (GameObject)PrefabUtility.InstantiatePrefab(HitBoxPrefab, rightArm);
                            rightArmHitBox.transform.localPosition = Vector3.zero;
                            rightArmHitBox.transform.localRotation = Quaternion.identity;
                            rightArmHitBox.transform.position = rightArmHitBox.transform.position + rightArmHitBox.transform.up * 0.7f;
                            Undo.RegisterCreatedObjectUndo(rightArmHitBox, "Hit Box Setup");

                        }

                        if (leftKnee.GetComponentInChildren<Damager>(true) == false)
                        {
                            GameObject leftLegHitBox = (GameObject)PrefabUtility.InstantiatePrefab(HitBoxPrefab, leftKnee);
                            leftLegHitBox.transform.localPosition = Vector3.zero;
                            leftLegHitBox.transform.localRotation = Quaternion.identity;
                            leftLegHitBox.transform.position = leftLegHitBox.transform.position + leftLegHitBox.transform.up * 0.7f;
                            Undo.RegisterCreatedObjectUndo(leftLegHitBox, "Hit Box Setup");

                        }

                        if (rightKnee.GetComponentInChildren<Damager>(true) == false)
                        {
                            GameObject rightLegHitBox = (GameObject)PrefabUtility.InstantiatePrefab(HitBoxPrefab, rightKnee);
                            rightLegHitBox.transform.localPosition = Vector3.zero;
                            rightLegHitBox.transform.localRotation = Quaternion.identity;
                            rightLegHitBox.transform.position = rightLegHitBox.transform.position + rightLegHitBox.transform.up * 0.7f;
                            Undo.RegisterCreatedObjectUndo(rightLegHitBox, "Hit Box Setup");

                        }
                    }
                }
                else
                {
                    JUTPSEditor.MessageWindow.ShowMessage("No JU CHARACTER selected, please select a JU CHARACTER and try again", "No JU Character", "OK", 125, 276, 14, MessageType.Warning);
                }
            }
        }


        [MenuItem("GameObject/JUTPS Create/Quick Setup/Add/Setup Zombie AI", false, 0)]
        public static void SetupZombieAI()
        {
            if (Selection.activeGameObject == null)
            {
                //Debug.LogWarning("No character selected. Please select a JU Character and try again");
                JUTPSEditor.MessageWindow.ShowMessage("No JU character selected, please select a JU Character and try again", "Error", "OK", 125, 276, 14, MessageType.Error);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                if (pl.TryGetComponent(out JUCharacterController tps))
                {
                    Undo.RecordObject(tps, "Zombie Setup");
                    tps.IsArtificialIntelligence = true;
                    tps.tag = "Enemy";

                    if (tps.GetComponent<JUHealth>() == null)
                    {
                        Undo.AddComponent(tps.gameObject, typeof(JUHealth));
                    }
                    if (tps.TryGetComponent(out ZombieAI zombie))
                    {
                        Undo.RecordObject(zombie, "Zombie Setup");
                        zombie.SensorLayerMask = LayerMask.GetMask("Default", "Vehicle", "Character", "Terrain", "Walls");
                        zombie.StartRunningAtDistance = 1;
                        zombie.FieldOfView.Angle = 220;
                        zombie.FieldOfView.Radious = 20;
                        zombie.AttackAtDistance = 1;
                        zombie.AttackDuration = 0;
                        zombie.MinTimeToAttack = 1;
                        zombie.MaxTimeToAttack = 5;
                    }
                    else
                    {
                        ZombieAI zombieAI = (ZombieAI)Undo.AddComponent(tps.gameObject, typeof(ZombieAI));
                        Undo.RecordObject(zombieAI, "Zombie Setup");
                        zombieAI.SensorLayerMask = LayerMask.GetMask("Default", "Vehicle", "Character", "Terrain", "Walls");
                        zombieAI.StartRunningAtDistance = 1;
                        zombieAI.FieldOfView.Angle = 220;
                        zombieAI.FieldOfView.Radious = 20;
                        zombieAI.AttackAtDistance = 1;
                        zombieAI.AttackDuration = 0;
                        zombieAI.MinTimeToAttack = 1;
                        zombieAI.MaxTimeToAttack = 5;
                    }
                }
                else
                {
                    JUTPSEditor.MessageWindow.ShowMessage("Selected GameObject(" + Selection.gameObjects[0].name + ") is not a JU Character, please select a JU Character and try again", "Error", "OK", 125, 276, 14, MessageType.Error);
                }
            }
        }

        [MenuItem("GameObject/JUTPS Create/Quick Setup/Add/Setup Patrol AI", false, 0)]
        public static void SetupPatrolAI()
        {
            if (Selection.activeGameObject == null)
            {
                //Debug.LogWarning("No character selected. Please select a JU Character and try again");
                JUTPSEditor.MessageWindow.ShowMessage("No JU character selected, please select a JU Character and try again", "Error", "OK", 125, 276, 14, MessageType.Error);
            }
            else
            {
                var pl = Selection.gameObjects[0];
                if (pl.TryGetComponent(out JUCharacterController tps))
                {
                    Undo.RecordObject(tps, "Zombie Setup");
                    tps.IsArtificialIntelligence = true;
                    tps.tag = "Enemy";

                    //Health
                    if (tps.GetComponent<JUHealth>() == null)
                    {
                        Undo.AddComponent(tps.gameObject, typeof(JUHealth));
                    }
                    //Inventory
                    if (tps.GetComponent<JUInventory>() == null)
                    {
                        Undo.AddComponent(tps.gameObject, typeof(JUInventory));
                    }
                    //Item Switch
                    if (tps.GetComponent<ItemSwitchManager>() == null)
                    {
                        Undo.AddComponent(tps.gameObject, typeof(ItemSwitchManager));
                    }


                    if (tps.TryGetComponent(out PatrolAI patrol))
                    {
                        Undo.RecordObject(patrol.gameObject, "Patrol Setup");
                        patrol.SensorLayerMask = LayerMask.GetMask("Default", "Vehicle", "Character", "Terrain", "Walls");
                        patrol.StartRunningAtDistance = 5;
                        patrol.FieldOfView.Angle = 220;
                        patrol.FieldOfView.Radious = 20;
                        patrol.AttackAtDistance = 10;
                        patrol.AttackDuration = 1;
                        patrol.MinTimeToAttack = 0.5f;
                        patrol.MaxTimeToAttack = 3;
                    }
                    else
                    {
                        PatrolAI patrolAi = (PatrolAI)Undo.AddComponent(tps.gameObject, typeof(PatrolAI));
                        Undo.RecordObject(patrolAi.gameObject, "Patrol Setup");
                        patrolAi.SensorLayerMask = LayerMask.GetMask("Default", "Vehicle", "Character", "Terrain", "Walls");
                        patrolAi.StartRunningAtDistance = 1;
                        patrolAi.FieldOfView.Angle = 220;
                        patrolAi.FieldOfView.Radious = 20;
                        patrolAi.AttackAtDistance = 10;
                        patrolAi.AttackDuration = 1;
                        patrolAi.MinTimeToAttack = 0.5f;
                        patrolAi.MaxTimeToAttack = 3;
                    }

                }
                else
                {
                    JUTPSEditor.MessageWindow.ShowMessage("Selected GameObject(" + Selection.gameObjects[0].name + ") is not a JU Character, please select a JU Character and try again", "Error", "OK", 125, 276, 14, MessageType.Error);
                }
            }
        }


        public static void SetupCharacterController(GameObject CharacterGameObject, float moveSpeed = 3, float rotationSpeed = 3, float stoppingSpeed = 1f, bool curvedMovement = true, bool lerpRotation = true, bool useRootMotion = false, bool addFootplacer = true, bool addFootstep = true, bool addDrivingAnimation = false, bool addBodyLean = false, bool addRagdoller = false, bool addInventory = true, bool blockDefaultInputs = false, string animatorControllerPath = "Assets/Julhiecio TPS Controller/Animations/Animator/AnimatorTPS Controller.controller")
        {
            if (CharacterGameObject.GetComponent<JUCharacterController>() != null)
            {
                JUTPSEditor.MessageWindow.ShowMessage("Setup could not be done because the selected GameObject already has the JU Character Controller added", "Setup could not be done", "OK", 125, 276, 14, MessageType.Warning);
            }
            Undo.RecordObject(CharacterGameObject, "Setup Character");
            var anim = CharacterGameObject.GetComponent<Animator>();

            if (anim == null)
            {
                //Debug.LogError(CharacterGameObject.gameObject.name + " GameObject does not have an animator", CharacterGameObject);
                JUTPSEditor.MessageWindow.ShowMessage("Setup could not be done because " + CharacterGameObject.gameObject.name + " GameObject does not have an animator", "Setup could not be done", "OK", 125, 276, 14, MessageType.Error);
                return;
            }
            else
            {
                //Humanoid error
                if (anim.isHuman == false)
                {
                    Debug.LogError("Your character rig is not humanoid type, please use a humanoid type character.", anim);
                    JUTPSEditor.MessageWindow.ShowMessage("Your character rig is not humanoid type, please use a humanoid type character.", "Setup could not be done", "OK", 125, 276, 14, MessageType.Error);
                    return;
                }
            }
            if (!CharacterGameObject.TryGetComponent<CapsuleCollider>(out var col))
                col = (CapsuleCollider)Undo.AddComponent(CharacterGameObject, typeof(CapsuleCollider));

            var noSlip = (PhysicsMaterial)Resources.Load("NoSlip", typeof(PhysicsMaterial));
            col.material = noSlip;

            Undo.AddComponent(CharacterGameObject, typeof(ResizableCapsuleCollider));

            if (!CharacterGameObject.TryGetComponent<Rigidbody>(out var rb))
                rb = (Rigidbody)Undo.AddComponent(CharacterGameObject, typeof(Rigidbody));

            var tps = (JUCharacterController)Undo.AddComponent(CharacterGameObject, typeof(JUCharacterController));
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorControllerPath);

            //Setup Animator Controller
            bool animatorError = false;
            if (animatorController != null)
            {
                anim.runtimeAnimatorController = animatorController;
            }
            else
            {
                animatorError = true;
                Debug.LogError("Could not load default Animator Controller, assign manually", anim);
                JUTPSEditor.MessageWindow.ShowMessage("Could not load default Animator Controller, assign it manually", "Could not load Animator Controller", "OK", 125, 276, 14, MessageType.Warning);
            }

            //Setup character Tag and Layer
            CharacterGameObject.tag = "Player";
            CharacterGameObject.layer = 9;

            //Create new item rotation center
            JUTPSEditor.JUTPSCreate.CreateNewWeaponRotationCenter();

            //Assign item rotation center
            tps.PivotItemRotation = CharacterGameObject.GetComponentInChildren<WeaponAimRotationCenter>().gameObject;

            //Change Controller Variables
            tps.Speed = moveSpeed;
            tps.CurvedMovement = curvedMovement;
            tps.RotationSpeed = rotationSpeed;
            tps.LerpRotation = lerpRotation;
            tps.StoppingSpeed = stoppingSpeed;
            tps.RootMotion = useRootMotion;

            //Rigidbody setup
            rb.mass = 85;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            //Capsule Collider Setup
            col.height = 1.7f;
            col.center = new Vector3(0, 0.85f, 0);
            col.radius = 0.4f;

            // >>> Additional FXs Setup <<<

            //Footplacer
            if (addFootplacer) Undo.AddComponent(CharacterGameObject, typeof(JUTPS.JUFootPlacement));

            //Footstep
            if (addFootstep)
            {
                JUFootstep footstep = (JUFootstep)Undo.AddComponent(CharacterGameObject, typeof(JUFootstep));
                footstep.LoadDefaultFootstepInInspector();
            }
            //Body Lean Effect
            if (addBodyLean) Undo.AddComponent(CharacterGameObject, typeof(BodyLeanInert));
            //Driving Animation Effect
            if (addDrivingAnimation) Undo.AddComponent(CharacterGameObject, typeof(ProceduralDrivingAnimation));
            //Ragdoller
            if (addRagdoller) Undo.AddComponent(CharacterGameObject, typeof(AdvancedRagdollController));
            //Inventory
            if (addInventory)
            {
                Undo.AddComponent(CharacterGameObject, typeof(JUInventory));
                Undo.AddComponent(CharacterGameObject, typeof(ItemSwitchManager));
            }

            if (!animatorError)
            {
                JUTPSEditor.MessageWindow.ShowMessage("Successful character setup", "Quick Character Setup", "OK", 115, 276, 14);
            }
            else
            {
                JUTPSEditor.MessageWindow.ShowMessage("Partial success, Animator Controller cannot be loaded, please assign it manually.", "Animator Controller cannot be loaded", "OK", 140, 276, 14, MessageType.Warning);
            }

        }
    }
}