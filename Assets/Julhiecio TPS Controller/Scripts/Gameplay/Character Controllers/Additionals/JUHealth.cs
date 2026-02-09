using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JUTPS.FX;
using JUTPSEditor.JUHeader;

namespace JUTPS
{

    [AddComponentMenu("JU TPS/Third Person System/Additionals/JU Health")]
    public class JUHealth : MonoBehaviour
    {
        [JUHeader("Settings")]
        public float Health = 100;
        public float MaxHealth = 100;

        [JUHeader("Effects")]
        public bool BloodScreenEffect = false;
        public GameObject BloodHitParticle;

        [JUHeader("On Death Event")]
        public UnityEvent OnDeath;

        [JUHeader("Stats")]
        public bool IsDead;

        void Start()
        {
            LimitHealth();
            InvokeRepeating(nameof(CheckHealthState), 0, 0.5f);
        }
        private void LimitHealth()
        {
            Health = Mathf.Clamp(Health, 0, MaxHealth);
        }
        public static void DoDamage(JUHealth health, float damage, Vector3 hitPosition = default(Vector3))
        {
            health.DoDamage(damage, hitPosition);
        }
        public void DoDamage(float damage, Vector3 hitPosition = default(Vector3))
        {
            Health -= damage;
            LimitHealth();
            Invoke(nameof(CheckHealthState), 0.016f);

            if (BloodScreenEffect) BloodScreen.PlayerTakingDamaged();
            if (hitPosition != Vector3.zero && BloodHitParticle != null)
            {
                GameObject fxParticle = Instantiate(BloodHitParticle, hitPosition, Quaternion.identity);
                fxParticle.hideFlags = HideFlags.HideInHierarchy;
                Destroy(fxParticle, 3);
            }
        }

        public void CheckHealthState()
        {
            LimitHealth();

            if (Health <= 0 && IsDead == false)
            {
                Health = 0;
                IsDead = true;

                //Disable all damagers0
                foreach (Damager dmg in GetComponentsInChildren<Damager>()) dmg.gameObject.SetActive(false);

                OnDeath.Invoke();
            }

            if (Health > 0) IsDead = false;
        }
    }

}