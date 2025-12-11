using System;
using System.Linq;
using UnityEngine;
using JUTPS.FX;
using JUTPS.ArmorSystem;
using JUTPSEditor.JUHeader;
using System.Collections.Generic;

namespace JUTPS
{
    /// <summary>
    /// A hit detector that apply damage to another collider with <see cref="JUHealth"/> or a <see cref="DamageableBodyPart"/>.
    /// </summary>
    [AddComponentMenu("JU TPS/Armor System/JU Damager")]
    public class Damager : MonoBehaviour
    {
        private float _currentHitTime;
        private Vector3 _lastPosition;
        private Vector3 _startLocalPosition;
        private Collider _oldHit;

        /// <summary>
        /// If true, show an UI indicator with the damage force on the other hitted collider.
        /// </summary>
        public bool ShowHitMarker;

        /// <summary>
        /// Starts the game with this <see cref="Damager"/> disabled by default?
        /// </summary>
        public bool DisableOnStart;

        /// <summary>
        /// The damage force.
        /// </summary>
        [JUHeader("Damager Settings")]
        public float Damage;

        /// <summary>
        /// Used to avoid multi damage calls, set the damage interval on each hit.
        /// </summary>
        public float HitMinTime = 0.5f;

        /// <summary>
        /// Detect hit using raycast.
        /// </summary>
        [JUHeader("Damage Detection Settings")]
        public bool RaycastingMode;
        public float RaycastDistance;
        public LayerMask RaycastLayer;

        [JUHeader("Collision Detection Mode Settings")]
        public bool IgnoreRootColliders;
        public bool LockStartPosition;
        public Collider[] AllCollidersToIgnore;

        [JUHeader("FX Settings")]
        public string[] TagsToDamage = { "Untagged", "Skin", "Player", "Enemy" };
        public SurfaceAudiosWithFX[] HitParticlesList;
        public AudioSource HitSoundsAudioSource;

        public bool CanHit { get; private set; }
        public bool IsColliding { get; private set; }
        public Rigidbody Rigidbody { get; private set; }

        public Damager()
        {
            CanHit = true;

            Damage = 20;
            HitMinTime = 0.5f;
            DisableOnStart = true;
            ShowHitMarker = false;

            RaycastingMode = true;
            RaycastDistance = 0.9f;
            RaycastLayer = 1;

            IgnoreRootColliders = true;
            LockStartPosition = true;

            TagsToDamage = new string[] { "Untagged", "Skin", "Player", "Enemy" };
            HitParticlesList = new SurfaceAudiosWithFX[0];
            HitSoundsAudioSource = null;
        }

        private void Awake()
        {
            _startLocalPosition = transform.localPosition;
            Rigidbody = GetComponent<Rigidbody>();

            if (IgnoreRootColliders)
                SetupCollidersToIgnore();
        }
        private void Start()
        {
            if (DisableOnStart)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
        }

        private void OnDisable()
        {
            _oldHit = null;
        }

        private void Update()
        {
            if (LockStartPosition)
            {
                transform.localPosition = _startLocalPosition;
                if (Rigidbody)
                {
                    Rigidbody.linearVelocity = Vector3.zero;
                    Rigidbody.isKinematic = false;
                }
            }

            if (RaycastingMode)
                CheckRaycastHit();
        }

        private void OnCollisionEnter(Collision collision)
        {
            Collider collider = collision.collider;
            Vector3 point = collision.contacts[0].point;
            Vector3 normal = collision.contacts[0].normal;

            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
                point = collider.ClosestPoint(point);

            CheckCollisionHit(collision.collider, point, normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            Vector3 point = transform.position;
            Vector3 normal = -transform.forward;

            if (other is BoxCollider || other is SphereCollider || other is CapsuleCollider)
                point = other.ClosestPoint(point);

            CheckCollisionHit(other, point, normal);
        }

        private void CheckCollisionHit(Collider other, Vector3 point, Vector3 normal)
        {
            if (!CanHit)
                return;

            if (!TagsToDamage.Contains(other.tag))
                return;

            if (other.gameObject.layer == 9) // Characters layer
            {
                if (other.gameObject.GetComponentInChildren<DamageableBodyPart>() != null)
                    return;
            }

            IsColliding = true;
            DoDamage(other, point, normal, Damage, HitParticlesList, HitSoundsAudioSource);
            Invoke(nameof(DisableCollidedState), 0.1f);
            DisableDamagingForSeconds(HitMinTime);
        }

        private void CheckRaycastHit()
        {
            if (!CanHit || RaycastDistance == 0)
                return;

            var hits = Physics.RaycastAll(_lastPosition, transform.forward, RaycastDistance, RaycastLayer);
            _lastPosition = transform.position;

            var hitsCount = hits.Length;
            for (int i = 0; i < hitsCount; i++)
            {
                var hitCollider = hits[i].collider;

                // Don't apply damage on the same object multiple times.
                if (hitCollider == _oldHit)
                    continue;

                if (AllCollidersToIgnore.Contains(hitCollider))
                    continue;
                
                if (TagsToDamage.Contains(hitCollider.tag))
                {
                    IsColliding = true;
                    _oldHit = hitCollider;
                    DoDamage(hitCollider, hits[i].point, hits[i].normal, Damage, HitParticlesList, HitSoundsAudioSource);
                    Invoke(nameof(DisableCollidedState), 0.1f);
                    DisableDamagingForSeconds(HitMinTime);
                    break;
                }
            }

            if (hitsCount == 0)
                _oldHit = null;
        }

        private void SetupCollidersToIgnore()
        {
            Collider thisCollider = GetComponent<Collider>();

            // Setup default colliders to ignore collision.
            if (!IgnoreRootColliders && thisCollider)
            {
                for (int i = 0; i < AllCollidersToIgnore.Length; i++)
                    if (AllCollidersToIgnore[i])
                        Physics.IgnoreCollision(AllCollidersToIgnore[i], thisCollider, true);

                return;
            }
            else if (!IgnoreRootColliders)
                return;

            // Get all root colliders.
            var rootCollidersList = transform.root.GetComponentsInChildren<Collider>().ToList();
            if (thisCollider)
                rootCollidersList.Remove(thisCollider);

            // Merge root colliders into AllCollidersToIgnore list.
            var rootColliders = rootCollidersList.ToArray();
            int oldLength = AllCollidersToIgnore.Length;
            int rootLength = rootColliders.Length;

            if (oldLength > 0)
            {
                // Add all root colliders colliders to ignore list.
                Array.Resize(ref AllCollidersToIgnore, oldLength + rootLength - 1);
                for (int i = 0; i < rootLength; i++)
                    AllCollidersToIgnore[Mathf.Max(oldLength - 1, 0) + i] = rootColliders[i];
            }
            else
                AllCollidersToIgnore = rootColliders;

            // Remove all duplicated items.
            AllCollidersToIgnore = new HashSet<Collider>(AllCollidersToIgnore).ToArray();

            // Setup default colliders to ignore collision.
            if (thisCollider)
            {
                foreach (Collider collider in AllCollidersToIgnore)
                    if (collider && collider != thisCollider)
                        Physics.IgnoreCollision(collider, thisCollider, true);
            }
        }

        private void DisableCollidedState()
        {
            _oldHit = null;
            IsColliding = false;
        }

        private void EnableDamaging()
        {
            CanHit = true;
        }

        public void DisableDamagingForSeconds(float disabledSeconds)
        {
            if (IsInvoking(nameof(EnableDamaging)))
                return;

            CanHit = false;
            Invoke(nameof(EnableDamaging), disabledSeconds);
        }

        public void DoDamage(Collider collider, Vector3 point, Vector3 normal, float damage, SurfaceAudiosWithFX[] hitParticles, AudioSource hitAudioSource)
        {
            DamageableBodyPart bodyPart = collider.GetComponentInChildren<DamageableBodyPart>();
            float realDamage = damage;

            if (!bodyPart)
            {
                JUHealth health = collider.GetComponentInParent<JUHealth>();

                if (health)
                {
                    health.DoDamage(damage);
                    if (ShowHitMarker)
                        if (!health.IsDead && realDamage > 0)
                            HitMarkerEffect.HitCheck(health.transform.tag, point, realDamage);
                }
            }
            else
            {
                realDamage = bodyPart.DoDamage(damage);
                if (ShowHitMarker)
                {
                    if (!bodyPart.Health.IsDead && realDamage > 0)
                        HitMarkerEffect.HitCheck(bodyPart.transform.tag, point, realDamage);
                }
            }

            //Instantiate Particle FX
            Quaternion particleRotation = Quaternion.LookRotation(normal);
            string tag = collider.tag;

            GameObject fx = SurfaceAudiosWithFX.Play(hitAudioSource, hitParticles, point, particleRotation, null, tag);

            if (fx)
                fx.transform.parent = collider.transform;
        }

        private void OnDrawGizmos()
        {
            if (RaycastingMode)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, transform.position + (transform.forward * RaycastDistance));
            }
            else
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
                Gizmos.color = new Color(1, 1, 1, 0.25f);
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }
    }

}