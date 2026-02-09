using UnityEngine;
using UnityEditor;
using JUTPS.ItemSystem;

namespace JUTPS.WeaponSystem
{

	[AddComponentMenu("JU TPS/Weapon System/Weapon")]
	[RequireComponent(typeof(AudioSource))]
	public class Weapon : JUHoldableItem
	{
		[HideInInspector] public JUCharacterController TPSControllerUser;
		[HideInInspector] public Transform mCamera;
		[HideInInspector] public Vector3 ShootDirection;
		[HideInInspector] public Vector3 CameraPosition;

		[Header("Weapon Setting")]
		public LayerMask RaycastingLayers;
		//Bullets
		[Range(1, 200)]
		public int BulletsPerMagazine = 10;
		public int TotalBullets = 150;
		public int BulletsAmounts = 10;
		public int NumberOfShotgunBulletsPerShot = 12;
		public bool InfiniteAmmo = false;
		//Fire Rate
		public float Fire_Rate = 0.3f;
		[HideInInspector] public float CurrentFireRateToShoot;

		//Precision
		[Range(0.1f, 50f)]
		public float Precision = 0.5f;
		[Range(0.01f, 1f)]
		public float LossOfAccuracyPerShot = 1;
		[HideInInspector] public float ShotErrorProbability;

		//Shooting 
		public GameObject BulletPrefab;
		public GameObject MuzzleFlashParticlePrefab;
		public Transform Shoot_Position;

		//Physics
		[HideInInspector] public Collider[] ListToIgnoreBulletCollision;

		//Aim Mode
		public WeaponFireMode FireMode;
		public WeaponAimMode AimMode;
		public Vector3 CameraAimingPosition = new Vector3(0, 0.1f, -0.2f);
		public Sprite ScopeTexture;
		public float CameraFOV = 30f;


		[Header("Procedural Animation")]
		public bool GenerateProceduralAnimation = true;

		[Range(0f, 0.3f)]
		public float RecoilForce = 0.1f;
		[Range(-100, 100)]
		public float RecoilForceRotation = 20;
		public Axis SliderMovementAxis;
		public Transform GunSlider;
		[Range(-0.1f, 0.1f)]
		public float SliderMovementOffset;
		[Range(0, 2)]
		public float SliderMovementSpeed = 0.5f;
		[HideInInspector] public Vector3 SliderStartLocalPosition;
		public float WeaponPositionSpeed = 20;
		public float WeaponRotationSpeed = 20;
		public float CameraRecoilMultiplier = 1;

		[Header("Bullet Casing Emitter")]
		public GameObject BulletCasingPrefab;
		public ParticleSystem BulletCasingParticle;
		public bool IsParticle;

		[Header("Weapon Sounds")]
		public AudioClip ShootAudio;
		public AudioClip ReloadAudio;
		public AudioClip EmptyMagazineAudio;
		public AudioClip WeaponEquipAudio;
		private AudioSource mAudioSource;
		private bool PlayedEmptySound;


		private bool enableBulletDirectionCorrection = true;
		public enum WeaponFireMode { Auto, SemiAuto, BoltAction, Shotgun }
		public enum WeaponAimMode { None, CameraApproach, Scope }
		public enum Axis { Z, X, Y }

		protected override void Start()
		{
			base.Start();

			CurrentFireRateToShoot = Fire_Rate - 0.05f;
			mCamera = (CamPivot != null) ? CamPivot.mCamera.transform : null;
			mAudioSource = GetComponent<AudioSource>();

			if (RaycastingLayers.value == 0)
			{
				RaycastingLayers = LayerMask.GetMask("Character", "Bones", "Default", "Walls", "Terrain", "Vehicle", "VehicleMeshCollider");
			}

			if (Owner != null)
			{
				if (Owner.TryGetComponent(out JUCharacterController thirdPersonController))
				{
					CamPivot = thirdPersonController.MyPivotCamera;
					TPSControllerUser = thirdPersonController;
					ListToIgnoreBulletCollision = thirdPersonController.CharacterHitBoxes;
				}

				if (Owner.TryGetComponent(out JUTPS.ActionScripts.AimOnMousePosition aimOnMousePos))
				{
					enableBulletDirectionCorrection = false;
				}
				if (Owner.TryGetComponent(out JUTPS.ActionScripts.AimOnRightJoystickDirection aimOnJoystickPos))
				{
					enableBulletDirectionCorrection = false;
				}
			}


			if (GunSlider != null)
			{
				SliderStartLocalPosition = GunSlider.localPosition;
			}

			if (BulletCasingPrefab != null && IsParticle)
			{
				BulletCasingParticle = BulletCasingPrefab.GetComponent<ParticleSystem>();
			}
		}
		protected virtual void OnEnable()
		{
			if (mAudioSource != null && WeaponEquipAudio != null)
			{
				mAudioSource.clip = null;
				mAudioSource.PlayOneShot(WeaponEquipAudio);
			}
		}
		public override void Update()
		{
			WeaponControl();

			if (GenerateProceduralAnimation == false) return;
			ProceduralAnimation();
		}
		// >>> Control
		private void WeaponControl()
		{
			// >>> Fire Rate Control
			if (CanUseItem == false)
			{
				CurrentFireRateToShoot += Time.deltaTime;
				if (CurrentFireRateToShoot >= Fire_Rate)
				{
					//CurrentFireRateToShoot = 0;
					CanUseItem = true;
					IsUsingItem = false;
					CancelInvoke("StopUseItemDelayed");
				}
			}
			else
			{
				if (CurrentFireRateToShoot < Fire_Rate)
				{
					CanUseItem = false;
				}
			}
			// >>> Blocks the shot when bullets == 0
			if (BulletsAmounts == 0 && CanUseItem == true)
			{
				CanUseItem = false;
				//CurrentFireRateToShoot = 0;
			}

			// >>> Weapon Accuracy Control
			ShotErrorProbability = Mathf.Lerp(ShotErrorProbability, 0, Precision * Time.deltaTime);
		}
		private void ProceduralAnimation()
		{
			if (WeaponRotationCenter == null) return;
			// >>> Recoil Animation

			if (ItemWieldPositionID >= WeaponRotationCenter.WeaponPositionTransform.Count) { ItemWieldPositionID -= 1; return; }
			//if (ItemWieldPositionID < 0) { ItemWieldPositionID = 0; return; }

			//Get stored transform properties
			Vector3 stored_weapon_pos = WeaponRotationCenter._storedLocalPositions[ItemWieldPositionID];
			Quaternion stored_weapon_rot = WeaponRotationCenter._storedLocalRotations[ItemWieldPositionID];
			if (TPSOwner)
			{
				if (TPSOwner.IsAiming)
				{
					WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].localPosition = stored_weapon_pos;
					WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].localRotation = stored_weapon_rot;
					return;
				}

			}
			//Set transform position smoothed
			WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].localPosition = Vector3.Lerp(
				WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].localPosition, stored_weapon_pos, WeaponPositionSpeed * Time.deltaTime);
			//Set transform rotation smoothed
			WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].localRotation = Quaternion.Lerp(
				WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].localRotation, stored_weapon_rot, WeaponRotationSpeed * Time.deltaTime);

			// >>> Weapon Slider/Bolt Animation
			if (GunSlider != null && BulletsAmounts > 0 && FireMode != WeaponFireMode.BoltAction)
			{
				GunSlider.transform.localPosition = Vector3.MoveTowards(GunSlider.transform.localPosition, SliderStartLocalPosition, SliderMovementSpeed * Time.deltaTime);
			}
		}

		// >>> Actions
		public override void UseItem()
		{
			// >>> FireModes
			if (CanUseItem && BulletsAmounts > 0)
			{
				Shot();
			}
			else
			{
				if (BulletsAmounts <= 0)
				{
					WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].Rotate(0, 2, 0);
					if (mAudioSource != null && EmptyMagazineAudio != null)
					{
						mAudioSource.PlayOneShot(EmptyMagazineAudio);
					}
				}
			}

			if (BulletsAmounts <= 0)
			{
				if (mAudioSource != null && EmptyMagazineAudio != null && PlayedEmptySound == false && IsInvoking("enableEmptyGunSound") == false)
				{
					mAudioSource.PlayOneShot(EmptyMagazineAudio);
					PlayedEmptySound = true;
					Invoke("enableEmptyGunSound", 0.2f);
				}
			}

			base.UseItem();
		}
		private void enableEmptyGunSound()
		{
			PlayedEmptySound = false;
		}
		private void BulletSpawn(GameObject BulletPrefab, Vector3 ShootStart, Vector3 ShootEnd, Quaternion ShootDirection, Vector3 FinalPointNormal, float SecondsToDestroy = 10)
		{
			if (TPSOwner == null)
			{
				RefreshItemDependencies();
			}
			//Spawn bullet
			var bullet = (GameObject)Instantiate(BulletPrefab, ShootStart, ShootDirection);
			if (bullet.TryGetComponent(out Bullet _bullet))
			{
				_bullet.FinalPoint = ShootEnd != Vector3.zero ? ShootEnd : Vector3.zero;
				_bullet.FinalPointNormal = FinalPointNormal;

				_bullet.SetOwner(TPSControllerUser.gameObject);
				_bullet.Ignore(ListToIgnoreBulletCollision);
			}
			Destroy(bullet, 10f);
		}
		/// <summary>
		/// Call it before shooting. Creates an orientation for the shot. 
		/// Leave the cameraPosition null if you want to take a shot that doesn't go towards the center of the camera. call it before the shooting
		/// </summary>
		/// <param name="cameraPosition"></param>
		/// <param name="shootDirection"></param>
		public void SetWeaponOrientation(Vector3 cameraPosition = default(Vector3), Vector3 shootDirection = default(Vector3))
		{
			CameraPosition = cameraPosition;
			ShootDirection = shootDirection;
		}
		public void Shot()
		{
			if (CanUseItem == false)
			{
				Debug.Log("Tried to shot but the CanUseItem variable is false, if using Prevent Gun Clipping ignore this message.");
				return;
			}
			RaycastHit CrosshairHit;

			if (FireMode != Weapon.WeaponFireMode.Shotgun)
			{
				RaycastHit CameraRaycastHit;
				if (enableBulletDirectionCorrection && CameraPosition != Vector3.zero)
				{
					if (Physics.Raycast(CameraPosition, ShootDirection, out CameraRaycastHit, 500, RaycastingLayers))
					{
						ShootDirection = (CameraRaycastHit.point - Shoot_Position.position).normalized;
						if (Vector3.Dot(ShootDirection, Shoot_Position.forward) < 0.3f)
						{
							ShootDirection = Shoot_Position.forward;
						}
						//Debug.Log("Modified Direction");
					}
				}

				if (Vector3.Dot(ShootDirection, Shoot_Position.forward) < 0.3f)
				{
					ShootDirection = Shoot_Position.forward;
				}

				// Generate Random Direction
				var BulletRotationPrecision = ShootDirection;
				BulletRotationPrecision.x += Random.Range(-ShotErrorProbability / 2, ShotErrorProbability / 2);
				BulletRotationPrecision.y += Random.Range(-ShotErrorProbability / 2, ShotErrorProbability / 2);
				BulletRotationPrecision.z += Random.Range(-ShotErrorProbability / 2, ShotErrorProbability / 2);

				// If raycast collide, apply random direction
				if (Physics.Raycast(Shoot_Position.transform.position, BulletRotationPrecision, out CrosshairHit, 500, RaycastingLayers))
				{
					Shoot_Position.LookAt(CrosshairHit.point);
					ShotErrorProbability += LossOfAccuracyPerShot;
					BulletSpawn(BulletPrefab, Shoot_Position.position, CrosshairHit.point, Shoot_Position.rotation, CrosshairHit.normal);

					Debug.DrawLine(Shoot_Position.transform.position, CrosshairHit.point, Color.red);


					//Audio
					if (ShootAudio != null)
					{
						mAudioSource.pitch = Random.Range(0.7f, 1.3f);
						mAudioSource.PlayOneShot(ShootAudio);
					}

					//Debug.Log("Shoot from raycast");
				}
				else
				{
					//Debug.Log("Shoot from shoot position");
					Shoot_Position.rotation = Quaternion.LookRotation(BulletRotationPrecision);
					BulletSpawn(BulletPrefab, Shoot_Position.position, Vector3.zero, Quaternion.LookRotation(BulletRotationPrecision), -Shoot_Position.forward);
					ShotErrorProbability += LossOfAccuracyPerShot;


					//Audio
					if (ShootAudio != null && mAudioSource != null)
					{
						mAudioSource.pitch = Random.Range(0.7f, 1.1f);
						mAudioSource.PlayOneShot(ShootAudio);
					}
				}

				if (FireMode == WeaponFireMode.Auto || FireMode == WeaponFireMode.SemiAuto)
				{
					EmitBulletShell();
				}
			}
			else
			{

				RaycastHit CameraRaycastHit;
				if (enableBulletDirectionCorrection && CameraPosition != Vector3.zero)
				{
					if (Physics.Raycast(CameraPosition, ShootDirection, out CameraRaycastHit, 500, RaycastingLayers))
					{
						ShootDirection = (CameraRaycastHit.point - Shoot_Position.position).normalized;
						if (Vector3.Dot(ShootDirection, Shoot_Position.forward) < 0.3f)
						{
							ShootDirection = Shoot_Position.forward;
						}
						//Debug.Log("Modified Direction");
					}
				}

				if (Vector3.Dot(ShootDirection, Shoot_Position.forward) < 0.3f)
				{
					ShootDirection = Shoot_Position.forward;
				}

				// >>> Shotgun Shoots
				for (int i = 0; i < NumberOfShotgunBulletsPerShot; i++)
				{
					//Generate Random Direction
					var BulletRotationPrecision = ShootDirection;
					BulletRotationPrecision.x += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);
					BulletRotationPrecision.y += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);
					BulletRotationPrecision.z += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);
					ShotErrorProbability = ShotErrorProbability + 5 * LossOfAccuracyPerShot;

					//If raycast collide, apply random direction
					if (Physics.Raycast(Shoot_Position.transform.position, BulletRotationPrecision, out CrosshairHit, 500, RaycastingLayers))
					{
						Shoot_Position.LookAt(CrosshairHit.point);
						Debug.DrawLine(Shoot_Position.transform.position, CrosshairHit.point, Color.red);

						//Spawn bullet
						var bullet = (GameObject)Instantiate(BulletPrefab, Shoot_Position.position, Shoot_Position.rotation);
						if (bullet.TryGetComponent(out Bullet _bullet))
						{
							_bullet.SetOwner(TPSControllerUser.gameObject);
							_bullet.FinalPoint = CrosshairHit.point;
							_bullet.FinalPointNormal = CrosshairHit.normal;
							_bullet.Ignore(ListToIgnoreBulletCollision);
						}

						Destroy(bullet, 10f);
					}
					else
					{
						//Generate Random Direction
						var BulletRotationPrecisionShootgun = Shoot_Position.transform.rotation;
						BulletRotationPrecisionShootgun.x += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);
						BulletRotationPrecisionShootgun.y += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);
						BulletRotationPrecisionShootgun.z += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);
						BulletRotationPrecisionShootgun.w += Random.Range(-LossOfAccuracyPerShot, LossOfAccuracyPerShot);

						//Spawn bullet
						var bullet = (GameObject)Instantiate(BulletPrefab, Shoot_Position.position, BulletRotationPrecisionShootgun);
						if (bullet.TryGetComponent(out Bullet _bullet))
						{
							_bullet.SetOwner(TPSControllerUser.gameObject);
							_bullet.Ignore(ListToIgnoreBulletCollision);
						}
						Destroy(bullet, 10f);
					}
				}

				//Audio
				if (ShootAudio != null)
				{
					mAudioSource.pitch = Random.Range(0.7f, 1.1f);
					mAudioSource.PlayOneShot(ShootAudio);
				}
			}

			//Update shooting state
			IsUsingItem = true;

			//Reset Shoot Direction
			Shoot_Position.localEulerAngles = Vector3.zero;

			//Spawn Muzzle Flash
			if (MuzzleFlashParticlePrefab != null)
			{
				var muzzleflesh = (GameObject)Instantiate(MuzzleFlashParticlePrefab, Shoot_Position.position, Shoot_Position.rotation, transform);
				Destroy(muzzleflesh, 2);
			}


			//Reset Fire Rate
			CurrentFireRateToShoot = 0;
			CanUseItem = false;

			//Subtracts Ammunition
			if (!InfiniteAmmo) BulletsAmounts -= 1;

			//Procedural Animation Trigger
			if (GenerateProceduralAnimation == true)
			{
				//Slider Animation
				if (GunSlider != null)
				{
					Vector3 RecoilSliderPosition = new Vector3(SliderStartLocalPosition.x, SliderStartLocalPosition.y, SliderStartLocalPosition.z - SliderMovementOffset);
					switch (SliderMovementAxis)
					{
						case Axis.X:
							RecoilSliderPosition = new Vector3(SliderStartLocalPosition.x - SliderMovementOffset, SliderStartLocalPosition.y, SliderStartLocalPosition.z);
							break;
						case Axis.Y:
							RecoilSliderPosition = new Vector3(SliderStartLocalPosition.x, SliderStartLocalPosition.y - SliderMovementOffset, SliderStartLocalPosition.z);
							break;
					}
					GunSlider.localPosition = RecoilSliderPosition;
				}
				//Recoil Animation
				Invoke("WeaponRecoil", 0.06f);
			}
		}
		public void EmitBulletShell()
		{
			//Spawn Bullet Casing
			if (BulletCasingPrefab != null)
			{
				if (IsParticle)
				{
					BulletCasingParticle.Emit(1);
				}
				else
				{
					var bulletcasing = (GameObject)Instantiate(BulletCasingPrefab, GunSlider.position, transform.rotation);
					bulletcasing.hideFlags = HideFlags.HideInHierarchy;
					Destroy(bulletcasing, 5f);
				}
			}
		}
		public void WeaponRecoil()
		{
			if (CamPivot != null) CamPivot.RecoilReaction(CameraRecoilMultiplier * 20 * RecoilForce);

			if (WeaponRotationCenter == null) return;

			//Apply recoil
			WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].Translate(0, 0, -RecoilForce);

			if (CamPivot == null)
			{
				WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].Rotate(Random.Range(-RecoilForceRotation / 2, RecoilForceRotation / 2), Random.Range(-RecoilForceRotation, 0), Random.Range(-RecoilForceRotation / 8, RecoilForceRotation / 8));
				return;
			}


			if (!CamPivot.Aiming)
			{
				WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].Rotate(Random.Range(-RecoilForceRotation / 2, RecoilForceRotation / 2), Random.Range(-RecoilForceRotation, 0), Random.Range(-RecoilForceRotation / 8, RecoilForceRotation / 8));
			}
			else
			{
				WeaponRotationCenter.WeaponPositionTransform[ItemWieldPositionID].Rotate(0, Random.Range(-RecoilForceRotation / 2, 0), 0);
			}
		}
		public void Reload()
		{
			//Reload
			if (BulletsAmounts < BulletsPerMagazine)
			{
				if (TotalBullets >= BulletsPerMagazine)
				{
					BulletsAmounts = BulletsPerMagazine;
					TotalBullets -= BulletsPerMagazine;
				}
				else
				{
					BulletsAmounts = TotalBullets;
					TotalBullets = 0;
				}
			}
			//Play reloading audio
			mAudioSource.PlayOneShot(ReloadAudio);
		}

#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			// DRAW LEFT HAND PREVIEW PREVIEW
			if (OppositeHandPosition != null)
			{
				var pos = OppositeHandPosition.position;
				var rot = OppositeHandPosition.eulerAngles;

				Color color = new Color(0.9F, 0.6F, 0.3F, 1F);
				JUGizmoDrawer.DrawGizmoMesh(JUGizmoDrawer.GetJUGizmoDefaultMesh(JUGizmoDrawer.DrawMesh.Hand), JUGizmoDrawer.DrawType.Solid, pos, rot);
			}

			if (GunSlider != null)
			{
				Gizmos.color = Color.white;

				Vector3 sliderTargetPos = Vector3.zero;
				switch (SliderMovementAxis)
				{
					case Axis.Z:
						sliderTargetPos = GunSlider.position - GunSlider.forward * SliderMovementOffset;
						break;
					case Axis.X:
						sliderTargetPos = GunSlider.position - GunSlider.right * SliderMovementOffset;
						break;
					case Axis.Y:
						sliderTargetPos = GunSlider.position - GunSlider.up * SliderMovementOffset;
						break;
				}
				Gizmos.DrawWireSphere(sliderTargetPos, 0.005f);
				Gizmos.DrawLine(GunSlider.position, sliderTargetPos);
			}

			if (AimMode != WeaponAimMode.None)
			{
				Gizmos.DrawWireSphere(transform.position + transform.right * CameraAimingPosition.x + transform.up * CameraAimingPosition.y + transform.forward * CameraAimingPosition.z, 0.01f);
				if (Application.isPlaying)
				{
					if (Camera.current != null)
					{
						Transform cam = Camera.current.transform;
						Vector3 CamCenterPos = cam.position + cam.forward * 0.2f;
						float LineLenght = 0.01f;
						Handles.color = Color.green;
						Handles.DrawLine(CamCenterPos + cam.right * LineLenght, CamCenterPos - cam.right * LineLenght);
						Handles.DrawLine(CamCenterPos + cam.up * LineLenght, CamCenterPos - cam.up * LineLenght);
					}
				}
			}
		}
#endif
	}

}