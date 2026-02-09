using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using JUTPS;
using JUTPS.JUInputSystem;
using JUTPS.CameraSystems;
using JUTPS.WeaponSystem;
using JUTPS.Utilities;

namespace JUTPSEditor
{
    public static class JUTPSCreate
    {
        [MenuItem("GameObject/JUTPS Create/Game Manager", false, -100)]
        public static void CreateGameManager()
        {
            var gameManager = new GameObject("Game Manager");
            Undo.RegisterCreatedObjectUndo(gameManager, "game manager creation");
            gameManager.AddComponent<JUGameManager>();
        }
        [MenuItem("GameObject/JUTPS Create/Input Manager", false, -99)]
        public static void CreateInputManager()
        {
            var inputManager = new GameObject("JU Input Manager");
            Undo.RegisterCreatedObjectUndo(inputManager, "input manager creation");
            inputManager.AddComponent<JUInputManager>();
        }

        [MenuItem("GameObject/JUTPS Create/Camera/Simple Camera Controller", false, 1)]
        public static void CreateNewCamera()
        {
            var CameraRoot_Controller = new GameObject("Camera Controller");
            Undo.RegisterCreatedObjectUndo(CameraRoot_Controller, "CameraRoot_Controller create");
            CameraRoot_Controller.AddComponent<TPSCameraController>();
            CameraRoot_Controller.transform.position = SceneViewInstantiatePosition();



            var mainCamera = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(mainCamera, "Main Camera create");
            mainCamera.AddComponent<Camera>();
            mainCamera.AddComponent<AudioListener>();
            mainCamera.transform.tag = "MainCamera";
            mainCamera.transform.position = CameraRoot_Controller.transform.position - CameraRoot_Controller.transform.forward * 1f;
            mainCamera.transform.parent = CameraRoot_Controller.transform;

            var mainCamera_Pivot = new GameObject("Camera Pivot");
            Undo.RegisterCreatedObjectUndo(mainCamera_Pivot, "mainCamera_Pivot create");
            mainCamera_Pivot.transform.position = mainCamera.transform.position;
            mainCamera.transform.parent = mainCamera_Pivot.transform;
            mainCamera_Pivot.transform.parent = CameraRoot_Controller.transform;
        }

        [MenuItem("GameObject/JUTPS Create/Quick Scene Setup", false, -200)]
        public static void QuickSceneSetup()
        {
            CreateInputManager();
            CreateGameManager();
            CreateNewCamera();
        }



        //>>>> WEAPONS

        //[MenuItem("GameObject/JUTPS Create/Weapons.../Weapon Aim Rotation Center", false, 0)]
        public static void CreateNewWeaponRotationCenter()
        {
            var ItemWieldPivotRotation = new GameObject("Item Wield Rotation Center");
            Undo.RegisterCreatedObjectUndo(ItemWieldPivotRotation, "Hit Box Setup");

            WeaponAimRotationCenter center = ItemWieldPivotRotation.AddComponent<WeaponAimRotationCenter>();
            if (Selection.activeGameObject != null)
            {
                ItemWieldPivotRotation.transform.position = Selection.transforms[0].position + ItemWieldPivotRotation.transform.up * 1.2f;
                ItemWieldPivotRotation.transform.SetParent(Selection.transforms[0]);
            }
            else
            {
                ItemWieldPivotRotation.transform.position = SceneViewInstantiatePosition();
            }
            var WeaponPositionsParent = new GameObject("Item Wielding Hands Positions");
            WeaponPositionsParent.transform.position = ItemWieldPivotRotation.transform.position;
            WeaponPositionsParent.transform.parent = ItemWieldPivotRotation.transform;

            center.CreateWeaponPositionReference("Small Weapon Position Reference");
            center.WeaponPositionTransform[0].localPosition = new Vector3(0.212f, 0.227f, 0.407f);
            center.WeaponPositionTransform[0].localRotation = Quaternion.Euler(-8.626f, 12.322f, -84.111f);

            center.CreateWeaponPositionReference("Big Weapon Position Reference");
            center.WeaponPositionTransform[1].localPosition = new Vector3(0.207f, 0.140f, 0.24f);
            center.WeaponPositionTransform[1].localRotation = Quaternion.Euler(0, 11.383f, -94.913f);

            center.CreateWeaponPositionReference("Flash Light");
            center.WeaponPositionTransform[2].localPosition = new Vector3(0.302f, 0.167f, 0.258f);
            center.WeaponPositionTransform[2].localRotation = Quaternion.Euler(-81.350f, -33.581f, -49.971f);

            center.CreateWeaponPositionReference("Left Hand Small Weapon Position");
            center.WeaponPositionTransform[3].localPosition = new Vector3(0.055f, 0.253f, 0.489f);
            center.WeaponPositionTransform[3].localRotation = Quaternion.Euler(-6.916f, -2.793f, 79.35f);

            center.CreateWeaponPositionReference("Small Gun Prevent Cliping");
            center.WeaponPositionTransform[3].localPosition = new Vector3(0.223f, 0.081f, 0.22f);
            center.WeaponPositionTransform[3].localRotation = Quaternion.Euler(-80.399f, -267.951f, 178.884f);

            center.CreateWeaponPositionReference("Big Gun Prevent Clipping");
            center.WeaponPositionTransform[3].localPosition = new Vector3(0.217f, 0.046f, 0.259f);
            center.WeaponPositionTransform[3].localRotation = Quaternion.Euler(-83.967f, -349.849f, 228.624f);

            center.StoreLocalTransform();
        }



        //>>>> UTILITIES
        [MenuItem("GameObject/JUTPS Create/Utilities/Trigger Message", false, 0)]
        public static void CreateTriggerMessage()
        {
            var triggermessage = new GameObject("Trigger Message");
            Undo.RegisterCreatedObjectUndo(triggermessage, "triggermessage creation");
            triggermessage.AddComponent<BoxCollider>();
            triggermessage.GetComponent<BoxCollider>().isTrigger = true;
            triggermessage.AddComponent<Rigidbody>();
            triggermessage.GetComponent<Rigidbody>().isKinematic = true;
            triggermessage.AddComponent<UIMenssengerTrigger>();

            triggermessage.gameObject.layer = 2;
            triggermessage.transform.position = SceneViewInstantiatePosition();
        }

        //>>>> UTILITIES
        [MenuItem("GameObject/JUTPS Create/Camera/Create Camera State Trigger", false, 0)]
        public static void CreateCameraStateTrigger()
        {
            var camstatetrigger = new GameObject("Camera State Trigger");
            Undo.RegisterCreatedObjectUndo(camstatetrigger, "camstatetrigger creation");

            camstatetrigger.AddComponent<CameraStateTrigger>();

            camstatetrigger.gameObject.layer = 2;
            camstatetrigger.transform.position = SceneViewInstantiatePosition();
        }
        //>>>> UTILITIES
        [MenuItem("GameObject/JUTPS Create/AI/Create Waypoint Path", false, 0)]
        public static void CreateWaypointPath()
        {
            var waypointpath = new GameObject("Waypoint Path");
            Undo.RegisterCreatedObjectUndo(waypointpath, "waypointpath creation");
            waypointpath.AddComponent<JUTPS.AI.WaypointPath>().RefreshWaypoints();
            waypointpath.transform.position = SceneViewInstantiatePosition();
        }
        public static Vector3 SceneViewInstantiatePosition()
        {
            var view = SceneView.lastActiveSceneView.camera;

            Ray ray = view.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                return hit.point;
            }

            if (view != null)
            {
                Vector3 pos = view.transform.position + view.transform.forward * 10;
                return pos;
            }
            else
            {
                return Vector3.zero;
            }
        }

        [UnityEditor.MenuItem("GameObject/JUTPS Create/Cover Trigger", false, 0)]
        public static void CreateCoverTrigger()
        {
            GameObject Cover = new GameObject("Cover Trigger");
            Undo.RegisterCreatedObjectUndo(Cover, "waypointpath creation");

            Cover.tag = "CoverTrigger";
            Cover.layer = LayerMask.NameToLayer("Ignore Raycast");

            Cover.AddComponent<JUTPS.CoverSystem.JUCoverTrigger>();
            Cover.transform.position = SceneViewInstantiatePosition();
            Cover.transform.rotation = SceneViewDirection();
            Cover.transform.localScale = new Vector3(2, 1.5f, 0.4f);

            Cover.GetComponent<BoxCollider>().center = new Vector3(0, 0.5f, 0);
            Cover.GetComponent<BoxCollider>().isTrigger = true;
        }

        public static Quaternion SceneViewDirection()
        {
            var view = SceneView.lastActiveSceneView.camera;


            if (view != null)
            {
                Vector3 euler = new Vector3(0,view.transform.eulerAngles.y,0);
                return Quaternion.Euler(euler);
            }
            else
            {
                return Quaternion.identity;
            }
        }
    }
}