using UnityEngine;
using JUTPS.FX;
using JUTPS.ArmorSystem;

namespace JUTPS.WeaponSystem
{

	[AddComponentMenu("JU TPS/Weapon System/Bullet")]
	[RequireComponent(typeof(Rigidbody))]
	public class Bullet : MonoBehaviour
	{
		public enum BulletMovementType
		{
			Physics,
			Calculated,
			Teleport,
		}
		private GameObject Owner;
		private Vector3 OldPosition;

		private Rigidbody rb;
		[Header("Bullet Settings")]
		public float BulletVelocity;
		public float BulletDamage = 20;
		public float DestroyTime = 0;
		public bool HightPrecisionCollisionDetection = true;
		public bool ImpactAddForce = true;
		[Header("Ricochet")]
		public bool Ricochet = false;
		public float RicochetAngle = 45;
		public int MaxRicochets = 1;
		[HideInInspector] public int RicochetsCount;



		[Header("Physics: It is calculated by physics")]
		[Header("Calculated: Follow a path between two points")]
		[Header("Teleport: It is teleported to the hit point.")]
		public BulletMovementType MovementType;

		[HideInInspector] public Vector3 FinalPoint;  //It's the camera raycast hit position
		[HideInInspector] public Vector3 FinalPointNormal;

		private string CollidedGameObjectTag;

		public SurfaceFX[] ImpactFX;

		void Start()
		{
			rb = GetComponent<Rigidbody>();
			rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

			if (IsDestinationAHead() == false && MovementType != BulletMovementType.Physics) MovementType = BulletMovementType.Physics;

			//If movement is physic type
			if (MovementType == BulletMovementType.Physics)
			{
				rb.linearVelocity = transform.forward * BulletVelocity;
			}
			else
			{
				rb.useGravity = false;
			}

			OldPosition = transform.position;
			//If movement is teleport type
			if (MovementType == BulletMovementType.Teleport && FinalPoint != Vector3.zero)
			{
				transform.position = FinalPoint;
			}
		}
		void FixedUpdate()
		{
			if (MovementType == BulletMovementType.Teleport) return;

			// >>> Check Collision
			if (HightPrecisionCollisionDetection)
			{
				CheckCollisionDetection();
			}

			// >>> Calculated/Translation Movement
			if (MovementType == BulletMovementType.Calculated && FinalPoint != Vector3.zero)
			{
				MoveToFinalPoint();
			}
			else if (MovementType == BulletMovementType.Calculated)
			{
				MovePhysically();
			}

			// >>> Physically Movement
			if (MovementType == BulletMovementType.Physics)
			{
				MovePhysically();
			}


			if (transform.position == FinalPoint && MovementType != BulletMovementType.Physics)
			{
				MovementType = BulletMovementType.Physics;
			}

		}

		private void MovePhysically()
		{
			rb.linearVelocity = transform.forward * BulletVelocity;
		}
		private void MoveForward()
		{
			transform.Translate(0, 0, BulletVelocity * Time.deltaTime);
		}
		private void MoveToFinalPoint()
		{
			transform.position = Vector3.MoveTowards(transform.position, FinalPoint, BulletVelocity * Time.deltaTime);
		}


		private void CheckCollisionDetection()
		{
			if (Physics.Linecast(transform.position, OldPosition, out RaycastHit hit))
			{
				FinalPoint = hit.point;
				FinalPointNormal = hit.normal;
				transform.position = hit.point;
				MovementType = BulletMovementType.Calculated;
				//rb.velocity = Vector3.zero;
			}
			OldPosition = transform.position;
		}

		void OnCollisionEnter(Collision col)
		{
			if (col.gameObject.tag != "Bullet")
			{
				float RealDamage = BulletDamage;
				//Check Character and Bones Layers
				if (col.gameObject.layer == 15 || col.gameObject.layer == 9)
				{
					// If there are bones and damageable parts -> ignore primary capsule collider
					if (col.gameObject.layer == 9 && col.gameObject.GetComponentInChildren<DamageableBodyPart>() != null)
					{
						Physics.IgnoreCollision(col.collider, GetComponent<Collider>());
						return;
					}
					// >>> Character taking damage
					if (col.gameObject.TryGetComponent(out DamageableBodyPart bodyPart))
					{
						//Take body part damage
						RealDamage = bodyPart.DoDamage(BulletDamage);
					}
					else
					{
						if (col.gameObject.GetComponentInParent<JUTPS.CharacterBrain.JUCharacterBrain>())
						{
							//Take Damage
							col.gameObject.GetComponentInParent<JUTPS.CharacterBrain.JUCharacterBrain>().TakeDamage(BulletDamage);
						}
					}
				}
				else
				{
					if (col.gameObject.TryGetComponent(out JUHealth health))
					{
						health.DoDamage(BulletDamage);
					}
				}

				// >>> Impact Add Force
				if (ImpactAddForce)
				{
					if (col.gameObject.TryGetComponent(out Rigidbody other))
					{
						other.AddForce(transform.forward * BulletVelocity * rb.mass, ForceMode.Impulse);
					}
				}

				//Set a new final point
				if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 1))
				{
					FinalPoint = hit.point;
					FinalPointNormal = hit.normal;
					//Debug.Log("Bullet Finded new final point");
				}

				//Get hitted gameobject tag
				CollidedGameObjectTag = col.gameObject.tag;

				//Instantiate hitted Surface Impact Particle
				SurfaceFX.InstantiateParticleFX(ImpactFX, col.gameObject.tag, FinalPoint, Quaternion.FromToRotation(transform.forward, FinalPointNormal) * transform.rotation, col.gameObject.transform);

				//Hit Marker
				if (GetOwner() != null && Owner.CompareTag("Player"))
				{
					if (col.gameObject.layer == 15 || col.gameObject.layer == 9)
					{
						if (col.gameObject.GetComponentInParent<JUHealth>() != null)
						{
							if (col.gameObject.GetComponentInParent<JUHealth>().IsDead == false)
							{
								HitMarkerEffect.HitCheck(CollidedGameObjectTag, FinalPoint, RealDamage);
							}
						}
					}
					else
					{
						HitMarkerEffect.HitCheck(CollidedGameObjectTag);
					}
				}

				//Check Ricochet Angle
				if (Ricochet)
				{
					if (Vector3.Dot(transform.forward, FinalPointNormal) < -(RicochetAngle / 360))
					{
						//Destroy bullet
						DestroyBullet(DestroyTime);
					}
					else
					{
						RicochetBullet(FinalPointNormal);
					}
				}
				else
				{
					DestroyBullet(DestroyTime);
				}
			}
		}

		public void RicochetBullet(Vector3 WallNormal)
		{
			if (Ricochet == false || RicochetsCount > MaxRicochets)
			{
				DestroyBullet(DestroyTime);
				return;
			}
			RicochetsCount++;
			//Ricochet bullet
			//DestroyBullet(RicochetDestroyTime);
			MovementType = BulletMovementType.Physics;
			//rb.velocity = Vector3.Reflect(transform.forward, WallNormal) * BulletVelocity;
			transform.rotation = Quaternion.FromToRotation(transform.forward, Vector3.Reflect(transform.forward, WallNormal)) * transform.rotation;
			//Debug.Log("Ricocheted By Weight: " + Vector3.Dot(transform.forward, WallNormal));
		}

		private bool IsDestinationAHead()
		{
			bool isAhead = true;

			if (FinalPoint != Vector3.zero && MovementType != BulletMovementType.Physics)
			{
				isAhead = Vector3.Dot(transform.forward, (FinalPoint - transform.position).normalized) > 0.3f;
			}

			return isAhead;
		}

		/// <summary>
		/// use this for ignore a gameobject collision, for example, player capsule collider.
		/// </summary>
		/// <param name="collider">collider to ignore</param>
		public void Ignore(Collider[] collider)
		{
			foreach (Collider col in collider)
			{
				Physics.IgnoreCollision(GetComponent<Collider>(), col);
			}
		}

		/// <summary>
		/// Set the character or gameobject that fired the bullet, some components compare the owners in some events to know whether or not to perform an action, for example the Hit Marker Component.
		/// </summary>
		/// <param name="owner">bullet owner</param>
		public void SetOwner(GameObject owner)
		{
			Owner = owner;
		}

		/// <summary>
		/// Get the character or gameobject that fired the bullet, some components compare the owners in some events to know whether or not to perform an action, for example the Hit Marker Component.
		/// </summary>
		/// <param name="owner">bullet owner</param>
		public GameObject GetOwner()
		{
			if (Owner != null)
			{
				return Owner;
			}
			else
			{
				return gameObject;
			}
		}

		public void DestroyBullet(float destroyTime = 0)
		{
			Destroy(gameObject, destroyTime);
		}
	}

}