using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using JUTPS.WeaponSystem;
using JUTPS.CameraSystems;

using JUTPSEditor.JUHeader;

namespace JUTPS.ItemSystem
{
	public class JUItem : MonoBehaviour
	{
		[JUHeader("Item Setting")]
		public string ItemFilterTag = "General";
		public Sprite ItemIcon;
		public bool Unlocked;
		public int ItemQuantity;
		public int MaxItemQuantity = 1;
		public string ItemName;
		public int ItemSwitchID;

		public virtual void UseItem()
		{
			if (ItemQuantity > 0)
			{
				RemoveItem();
			}
			else
			{
				return;
			}
		}
		public virtual void RemoveItem()
		{
			ItemQuantity--;
			ItemQuantity = Mathf.Clamp(ItemQuantity, 0, MaxItemQuantity);
			//if (ItemQuantity == 0) Unlocked = false;
		}
		public virtual void AddItem()
		{
			ItemQuantity++;
			ItemQuantity = Mathf.Clamp(ItemQuantity, 0, MaxItemQuantity);

			if (ItemQuantity > 0) Unlocked = true;
		}
	}

	public class JUHoldableItem : JUItem
	{
		[HideInInspector] public GameObject Owner;
		[HideInInspector] public JUCharacterController TPSOwner;
		public WeaponAimRotationCenter WeaponRotationCenter;
		[HideInInspector] public JUCameraController CamPivot;

		[JUHeader("Use Setting")]
		public bool SingleUseItem;
		public bool ContinuousUseItem;

		public bool BlockFireMode = false;
		public GameObject ItemModelInBody;

		public float TimeToUse;
		[HideInInspector] public float CurrentTimeToUse;
		public bool CanUseItem = true;
		public bool IsUsingItem = false;

		[JUHeader("Wielding")]
		public int ItemWieldPositionID;
		public bool IsLeftHandItem;
		public bool ForceDualWielding = false;
		public JUHoldableItem DualItemToWielding;

		public ItemHoldingPose HoldPose;
		public ItemSwitchPosition PushItemFrom;
		public int GetWieldingPoseIndex() { return (int)HoldPose; }


		[JUHeader("IK Settings")]
		public Transform OppositeHandPosition;

		public enum ItemSwitchPosition { Hips, Back }
		public enum ItemHoldingPose { PistolTwoHands, PistolOneHand, Rifle, Free }

		protected virtual void Start()
		{
			RefreshItemDependencies();
			CurrentTimeToUse = TimeToUse;
		}
		private void Awake()
		{
			RefreshItemDependencies();
		}
		public void RefreshItemDependencies()
		{
			if (Owner == null || TPSOwner == null)
			{
				if (transform.GetComponentInParent<JUCharacterController>() != null)
				{
					Owner = transform.GetComponentInParent<JUCharacterController>().gameObject;
					TPSOwner = Owner.GetComponent<JUCharacterController>();
					if (TPSOwner.anim == null) TPSOwner.anim = TPSOwner.GetComponent<Animator>();

					if (TPSOwner.anim.GetBoneTransform(HumanBodyBones.LeftHand) == null)
					{
						if (IsInvoking(nameof(RefreshItemDependencies)) == false) { Invoke(nameof(RefreshItemDependencies), 0.1f); }
						return;
					}

					IsLeftHandItem = (TPSOwner.anim.GetBoneTransform(HumanBodyBones.LeftHand).transform == transform.parent) ? true : false;

					WeaponRotationCenter = TPSOwner != null ? TPSOwner.PivotItemRotation.GetComponent<WeaponAimRotationCenter>() : null;
					CamPivot = TPSOwner.MyPivotCamera;
				}
			}
		}
		public virtual void Update()
		{
			if (CanUseItem == false)
			{
				CurrentTimeToUse += Time.deltaTime;
				if (CurrentTimeToUse >= TimeToUse)
				{
					CanUseItem = true;
					CurrentTimeToUse = 0;
				}
			}
		}
		public override void UseItem()
		{
			IsUsingItem = true;
			CanUseItem = false;

			if (SingleUseItem)
			{
				ItemQuantity = 0;
			}
		}

		public virtual void StopUseItem()
		{
			IsUsingItem = false;
			if (SingleUseItem)
			{
				if (SingleUseItem)
				{
					ItemQuantity = 0;
				}
			}
		}
		public virtual void StopUseItemDelayed(float delay)
		{
			if (IsInvoking("StopUseItem")) { CancelInvoke("StopUseItem"); return; }

			Invoke("StopUseItem", delay);
		}

	}
	public class JUGeneralHoldableItem : JUHoldableItem
	{
		public bool DisableCharacterFireModeOnStopUsing;
		public UnityEvent OnUseItem;
		public UnityEvent OnStopUsingItem;
		protected bool OnUseItemEventCalled, OnStopUseItemEventCalled;


		public override void UseItem()
		{
			if (CanUseItem == false) return;
			if (OnUseItemEventCalled == false)
			{
				OnUseItem.Invoke();
				OnStopUseItemEventCalled = false;
				OnUseItemEventCalled = true;
			}

			base.UseItem();

		}
		public override void StopUseItem()
		{
			base.StopUseItem();
			if (OnStopUseItemEventCalled == false)
			{
				if (DisableCharacterFireModeOnStopUsing == true && TPSOwner != null)
				{
					TPSOwner.FiringMode = false;
				}
				OnStopUsingItem.Invoke();
				OnUseItemEventCalled = false;
				OnStopUseItemEventCalled = true;
			}
		}

	}
}
