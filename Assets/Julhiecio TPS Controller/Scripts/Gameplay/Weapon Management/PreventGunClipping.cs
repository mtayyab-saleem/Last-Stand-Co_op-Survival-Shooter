using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace JUTPS.WeaponSystem
{
    [RequireComponent(typeof(Weapon))]
    [AddComponentMenu("JU TPS/Weapon System/Prevent Gun Clipping")]
    public class PreventGunClipping : MonoBehaviour
    {
        private Weapon gun;
        private int startGunWieldingID;

        private WeaponAimRotationCenter center;
        private Transform GunWieldTransform;

        private float distanceCenterToWieldPosition;

        [Header("Settings")]
        public int ToUpDirectionWieldingID;
        public float RayDistance = 0.5f;
        public bool BlockGunFireOnPreventClipping = true;
        public LayerMask WallsLayer;
        public RaycastHit ClippingWallHit;

        [Header("State")]
        public bool IsPreventing;

        [Header("Events")]
        public UnityEvent OnPrevent;
        public UnityEvent OnStopPrevent;
        private bool calledPrevent, calledStopPrevent;


        private void Start()
        {
            gun = GetComponent<Weapon>();
            if (gun != null) startGunWieldingID = gun.ItemWieldPositionID;
            center = gun.Owner != null ? gun.Owner.GetComponentInChildren<WeaponAimRotationCenter>() : null;
            if (center != null) GunWieldTransform = center.WeaponPositionTransform[startGunWieldingID];
            
            //Get layers
            if (WallsLayer == 0) WallsLayer = LayerMask.GetMask("Default", "Terrain", "Walls", "VehicleMeshCollider", "Vehicle");

            //Set Events
            OnPrevent.AddListener(SetPreventWieldingID);
            OnStopPrevent.AddListener(SetNormalWieldingID);
        }

        private void Update()
        {
            if (gun == null || center == null) return;

            distanceCenterToWieldPosition = Vector3.Distance(GunWieldTransform.position, center.transform.position);
            Vector3 origin = GunWieldTransform.position - center.transform.forward * distanceCenterToWieldPosition;
            
            IsPreventing = Physics.Raycast(origin, center.transform.forward, out ClippingWallHit, RayDistance + distanceCenterToWieldPosition, WallsLayer);

            //Block Shots
            if (BlockGunFireOnPreventClipping && IsPreventing) gun.CanUseItem = false;
            
            //Call OnPrevent Event
            if(IsPreventing == true&& calledPrevent == false)
            {
                OnPrevent.Invoke();
                calledPrevent = true;
                calledStopPrevent = false;
            }

            //Call OnStopPrevent Event
            if (IsPreventing == false && calledStopPrevent == false)
            {
                OnStopPrevent.Invoke();
                calledPrevent = false;
                calledStopPrevent = true;
            }
        }

        public void SetPreventWieldingID() { gun.ItemWieldPositionID = ToUpDirectionWieldingID; }
        public void SetNormalWieldingID() { gun.ItemWieldPositionID = startGunWieldingID; }


        private void OnDrawGizmos()
        {
            if(gun != null)
            {
                if(gun.TPSOwner != null)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 origin = GunWieldTransform.position - center.transform.forward * distanceCenterToWieldPosition;
                    Gizmos.DrawLine(origin, origin + center.transform.forward * RayDistance);
                }
            }
        }
    }
}