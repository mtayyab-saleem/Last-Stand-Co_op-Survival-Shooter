using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.ArmorSystem
{


    [AddComponentMenu("JU TPS/Armor System/Armor")]
    public class Armor : JUTPS.ItemSystem.JUItem
    {
        [Header("Visual Settings")]
        public GameObject[] Parts;
        [Header("Armor Settings")]
        public bool EnableArmorHealth;
        public float Health = 100;
        [HideInInspector] public float MaxHealth = 100;
        public bool EnableArmorProtection;
        public float DamageMultiplier = 0.5f;
        public DamageableBodyPart[] DamageablesToProtect;
        private List<float> defaultDamageMultiplier = new List<float>();
        void Awake()
        {
            MaxHealth = Health;
            MaxItemQuantity = 1;
            foreach (DamageableBodyPart dmg in DamageablesToProtect)
            {
                if (dmg != null)
                {
                    defaultDamageMultiplier.Add(dmg.DamageMultiplier);
                }
            }
        }

        private void OnEnable()
        {
            EnableAllParts();
            ProtectParts(DamageablesToProtect, DamageMultiplier);
        }
        private void OnDisable()
        {
            DisableAllParts();
            UnprotectParts(DamageablesToProtect, defaultDamageMultiplier);
        }
        public void ProtectParts(DamageableBodyPart[] parts, float targetDamageMultiplier)
        {
            if (!EnableArmorProtection) return;
            foreach (DamageableBodyPart dmg in parts)
            {
                if (dmg != null)
                {
                    dmg.DamageMultiplier = targetDamageMultiplier;
                }
            }
        }
        public void UnprotectParts(DamageableBodyPart[] parts, List<float> defaultValues)
        {
            if (!EnableArmorProtection) return;
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].DamageMultiplier = defaultValues[i];
            }
        }
        public void DisableAllParts()
        {
            foreach (GameObject armorpart in Parts)
            {
                armorpart.SetActive(false);
            }
        }
        public void EnableAllParts()
        {
            foreach (GameObject armorpart in Parts)
            {
                armorpart.SetActive(true);
            }
        }

        public void DoDamageOnArmor(float damage)
        {
            if (!EnableArmorHealth) return;
            Health -= damage;
            if (Health <= 0)
            {
                RemoveItem();
                Health = 0;
            }
        }
    }

}