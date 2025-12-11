using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEditor;

namespace JUTPS.PhysicsScripts
{

	//This class is made to store the position and rotation parameters of a bone.
	//Mainly used for the transition from Ragdoll to Animator.
	public class BoneTransformInfo
	{
		public Transform Transform;
		public Vector3 StoredPosition;
		public Quaternion StoredRotation;
		public BoneTransformInfo(Transform t)
		{
			Transform = t;
		}
	}

	[AddComponentMenu("JU TPS/Physics/Advanced Ragdoll Controller")]
	public class AdvancedRagdollController : MonoBehaviour
	{
		private Vector3 groundNormal;
		private GameObject character;
		private Animator animator;
		private bool isStartAnimatorUpdateInPhysics;
		private Rigidbody rigidBody;

		[HideInInspector]
		public bool RagdollEnabled;

		//Ragdoll States
		public enum RagdollState
		{
			/// Animator is fully in control
			Animated,
			/// Animator turned off, but when stable position will be found, the transition to Animated will heppend
			WaitStablePosition,
			/// Animator turned off, physics controls the ragdoll
			Ragdolled,
			/// Animator in control, but LateUpdate() is used to partially blend in the last ragdolled pose
			BlendToAnim,
		}
		public RagdollState State;
		private bool GetUpFromBelly;

		//Human Body Bones
		public Transform[] AllBones;
		public Rigidbody[] RagdollBones;

		public Transform Hips;
		public Transform HipsParent;
		private Transform Head;
		private Rigidbody HipsRigidbody;

		//List of stored bone transformation information
		private List<BoneTransformInfo> bones = new List<BoneTransformInfo>();

		//Current transition weight
		public float BlendAmount;

		//>>> Settings
		[Range(1, 5f)]
		public float TimeToGetUp = 3f;

		[Range(1, 5f)]
		public float BlendSpeed = 2f;

		public float RagdollDrag = 0.5f;


		//Debbuging
		public bool RagdollWhenPressKeyG;
		public bool ViewHumanBodyBones;
		public bool ViewBodyPhysics;
		public bool ViewBodyDirection;

		public bool FilterByBoneLayer = true;
		private void Start()
		{
			Invoke(nameof(StartAdvancedRagdollController), 0.001f);
		}
		private void Update()
		{
			if (RagdollWhenPressKeyG && Keyboard.current.gKey.isPressed)
			{
				State = RagdollState.Ragdolled;
			}
			RagdollStatesController();
		}
		private void LateUpdate()
		{
			BlendRagdollToAnimator();
		}
		private void OnDisable()
		{
			if (Hips != null) Hips.gameObject.SetActive(false);
		}
		private void OnEnable()
		{
			if (Hips != null) Hips.gameObject.SetActive(true);
		}
		private void OnDestroy()
		{
			if (Hips != null) Destroy(Hips.gameObject);
		}
		public void StartAdvancedRagdollController()
		{
			character = this.gameObject;
			animator = GetComponent<Animator>();
			rigidBody = GetComponent<Rigidbody>();
			isStartAnimatorUpdateInPhysics = (animator.updateMode == AnimatorUpdateMode.Fixed);

			//Stores Humanoid hips transform, hips parent and rigidbody of humanoid hips
			if (animator == null || character == null) return;

			Hips = animator.GetBoneTransform(HumanBodyBones.Hips);
			HipsParent = Hips.parent;
			HipsRigidbody = Hips.GetComponent<Rigidbody>();
			Head = animator.GetBoneTransform(HumanBodyBones.Head);

			//Stores All bones and Rigidbody Bones used for ragdoll
			RagdollBones = Hips.GetComponentsInChildren<Rigidbody>();
			AllBones = Hips.GetComponentsInChildren<Transform>();

			//Filtering Bones
			if (FilterByBoneLayer == true)
			{
				//Filter All Bones
				List<Transform> AllBonesFiltered = new List<Transform>();
				foreach (Transform bone in AllBones)
				{
					if (bone.gameObject.layer == 15)
					{
						AllBonesFiltered.Add(bone);
					}
				}
				AllBones = AllBonesFiltered.ToArray();

				//Filter Ragoll Bones
				List<Rigidbody> RagdollBonesFiltered = new List<Rigidbody>();
				foreach (Rigidbody rbBone in RagdollBones)
				{
					if (rbBone.gameObject.layer == 15)
					{
						RagdollBonesFiltered.Add(rbBone);
					}
				}
				RagdollBones = RagdollBonesFiltered.ToArray();
			}

			//adds a bone reference to a list of a class that stores
			//the bone information needed for the transition from ragdoll to animator.
			foreach (var bone in AllBones)
			{
				bones.Add(new BoneTransformInfo(bone.transform));
			}

			//Disable Ragdoll Physics
			SetActiveRagdoll(false);


			//print("JU TPS Advanced Ragdoll Controller Started");
		}

		public void RagdollStatesController()
		{
			if (HipsRigidbody == null)
			{
				State = RagdollState.Animated;
				return;
			}
			//Animated
			if (State == RagdollState.Animated && animator.enabled == false)
			{
				animator.enabled = true;
				RagdollEnabled = false;
				SetActiveRagdoll(false);
				Hips.parent = HipsParent;
			}

			//Ragdolled
			if (State == RagdollState.Ragdolled)
			{

				if (RagdollEnabled == false)
				{
					SetActiveRagdoll(true, true);
					Hips.parent = null;
				}

				if (HipsRigidbody.linearVelocity.magnitude < 0.01f && IsInvoking("SetWaitStablePositionInvoked") == false)
				{
					Invoke("SetWaitStablePositionInvoked", TimeToGetUp);
				}
				if (Hips.parent == null)
				{
					RaycastHit hipshitground;
					LayerMask groundmask = LayerMask.GetMask("Default", "Terrain", "Walls");
					Physics.Raycast(Hips.position, -transform.up, out hipshitground, 0.5f, groundmask);

					var HipsPosition = Hips.position;
					HipsPosition.y = hipshitground.point.y;
					transform.position = HipsPosition;

					groundNormal = hipshitground.normal != Vector3.zero ? hipshitground.normal : Vector3.up;
					SetTransformRotationToBodyDirection();
				}
				transform.position = Hips.position;
			}

			//Wait to stable position
			if (State == RagdollState.WaitStablePosition)
			{
				Hips.parent = HipsParent;

				foreach (var Bone in bones)
				{
					Bone.StoredPosition = Bone.Transform.localPosition;
					Bone.StoredRotation = Bone.Transform.localRotation;
				}
				GetUp();
				State = RagdollState.BlendToAnim;
			}

			// >>> Change Animator Update Mode to Normal (Update Physic cause a glitch)
			if (State == RagdollState.BlendToAnim)
			{
				animator.updateMode = AnimatorUpdateMode.Normal;
			}
			else if (isStartAnimatorUpdateInPhysics)
			{
				animator.updateMode = AnimatorUpdateMode.Fixed;
			}

			RaycastHit hit;
			LayerMask mask = LayerMask.GetMask("Default", "Terrain", "Walls");
			if (Physics.Raycast(Hips.position, Hips.forward, out hit, 0.5f, mask))
			{
				GetUpFromBelly = true;
			}
			else
			{
				GetUpFromBelly = false;
			}
		}
		public void BlendRagdollToAnimator()
		{
			if (State == RagdollState.BlendToAnim)
			{
				foreach (var Bone in bones)
				{
					Bone.Transform.localPosition = Vector3.Slerp(Bone.Transform.localPosition, Bone.StoredPosition, BlendAmount);
					Bone.Transform.localRotation = Quaternion.Slerp(Bone.Transform.localRotation, Bone.StoredRotation, BlendAmount);
				}

				BlendAmount = Mathf.MoveTowards(BlendAmount, 0.0f, BlendSpeed * Time.deltaTime);

				if (BlendAmount <= 0)
				{
					State = RagdollState.Animated;
				}

				//Cancel invoke timer
				if (IsInvoking("SetWaitStablePositionInvoked"))
				{
					CancelInvoke("SetWaitStablePositionInvoked");
				}
			}
		}

		public void SetActiveRagdoll(bool Enabled, bool Inertia = default)
		{
			foreach (Rigidbody boneRigidbody in RagdollBones)
			{
				boneRigidbody.isKinematic = !Enabled;
			}
			RagdollEnabled = Enabled;
			animator.enabled = !Enabled;

			if (Inertia == true)
			{
				foreach (Rigidbody rb in RagdollBones)
				{
					rb.linearVelocity = GetComponent<Rigidbody>().linearVelocity;
					rb.angularVelocity = Vector3.zero;
					rb.angularDamping = RagdollDrag;
				}
			}
			if (Enabled == true)
			{
				rigidBody.isKinematic = true;
				rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			}
			else
			{
				rigidBody.isKinematic = false;
				rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			}

			character.GetComponent<CapsuleCollider>().enabled = !Enabled;
		}
		private void SetWaitStablePositionInvoked()
		{
			State = RagdollState.WaitStablePosition;
		}
		public void GetUp()
		{
			SetTransformRotationToBodyDirection();
			//Disable Ragdoll
			SetActiveRagdoll(false);

			//Animation
			BlendAmount = 1f;

			//Play Animation
			if (GetUpFromBelly)
			{
				animator.Play("Get Up From Belly", 0, 0);
			}
			else
			{
				animator.Play("Get Up From Back", 0, 0);
			}
		}
		Vector3 UpDirection;
		public void SetTransformRotationToBodyDirection()
		{
			//Smooth Up Direction
			UpDirection = transform.up;
			UpDirection = Vector3.Lerp(UpDirection, groundNormal, 5 * Time.deltaTime);

			//Set Rotation to BodyDirection

			transform.rotation = Quaternion.FromToRotation(transform.forward, BodyDirection()) * transform.rotation;
			Hips.rotation = Quaternion.FromToRotation(BodyDirection(), transform.forward) * Hips.rotation;
			transform.rotation = Quaternion.FromToRotation(transform.up, UpDirection) * transform.rotation;
		}
		public Vector3 BodyDirection()
		{
			Vector3 ragdolldirection = Hips.position - Head.position;
			ragdolldirection.y = 0;
			if (GetUpFromBelly)
			{
				return -ragdolldirection.normalized;
			}
			else
			{
				return ragdolldirection.normalized;
			}
		}

		public void Fall()
		{
			State = RagdollState.Ragdolled;
		}


#if UNITY_EDITOR
		[HideInInspector] private Camera MainCamera;
		private void OnDrawGizmos()
		{
			if (MainCamera == null)
			{
				MainCamera = Camera.current;
			}

			//BODY DIRECTION
			if (State == RagdollState.Ragdolled && ViewBodyDirection)
			{
				Handles.Label(Hips.position + MainCamera.transform.right * 1f, "Ragdoll Body Direction");
				if (GetUpFromBelly)
				{
					Handles.color = Color.cyan;
					Handles.ArrowHandleCap(0, Hips.position + MainCamera.transform.right * 1f, Quaternion.LookRotation(BodyDirection()), 0.5f, EventType.Repaint);
				}
				else
				{
					Handles.color = Color.white;
					Handles.ArrowHandleCap(0, Hips.position + MainCamera.transform.right * 1f, Quaternion.LookRotation(BodyDirection()), 0.5f, EventType.Repaint);
				}
			}

			if (AllBones != null)
			{
				//DRAW BONES OUTLINE
				if (ViewHumanBodyBones)
				{
					foreach (var CurrentBone in AllBones)
					{
						if (CurrentBone.transform.parent == null)
							continue;
						if (CurrentBone.transform.parent == transform)
							continue;
						float distparent = Vector3.Distance(CurrentBone.position, CurrentBone.transform.parent.position);
						Vector3 direction = CurrentBone.transform.parent.position - CurrentBone.position;
						if (State == RagdollState.Animated)
						{
							Handles.color = Color.yellow;
							Handles.DrawDottedLine(CurrentBone.position, CurrentBone.transform.parent.position, 0.3f);
							Gizmos.color = Color.red;
							Gizmos.DrawSphere(CurrentBone.position, 0.02f);
						}
						else
						{
							Handles.color = Color.grey;
							Handles.DrawDottedLine(CurrentBone.position, CurrentBone.transform.parent.position, 0.3f);
						}
					}
				}



				//DRAW PHYSICS
				if (ViewBodyPhysics)
				{
					foreach (var CurrentBone in RagdollBones)
					{
						if (!CurrentBone)
							continue;
						if (CurrentBone.transform.parent == null)
							continue;
						if (CurrentBone.transform.parent == transform)
							continue;
						if (State == RagdollState.Ragdolled)
						{
							Color green = new Color(0, 1, 0, .5f);
							Gizmos.color = green;
							Gizmos.DrawSphere(CurrentBone.position + CurrentBone.transform.up * 0.2f, 0.05f);

							Gizmos.color = green;
							Gizmos.DrawLine(CurrentBone.position, CurrentBone.position + CurrentBone.transform.up * 0.2f);
						}
						else
						{
							Gizmos.color = Color.gray;
							Gizmos.DrawSphere(CurrentBone.position + CurrentBone.transform.up * 0.2f, 0.05f);
							Gizmos.DrawWireSphere(CurrentBone.position + CurrentBone.transform.up * 0.2f, 0.05f);
						}
					}
				}



			}
		}
#endif


	}

}