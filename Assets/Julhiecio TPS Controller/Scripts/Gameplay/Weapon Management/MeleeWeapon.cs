using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPSEditor.JUHeader;
using JUTPS.InventorySystem;
namespace JUTPS.WeaponSystem
{
    public class MeleeWeapon : JUTPS.ItemSystem.JUHoldableItem
    {
        [JUHeader("Melee Weapon Settings")]
        public string AttackAnimatorParameterName = "OneHandMeleeAttack";
        public Damager DamagerToEnable;

        [JUHeader("Damage Settings")]
        public bool EnableHealthLoss;
        public float MeleeWeaponHealth = 100;
        public float DamagePerUse = 1;

        protected override void Start()
        {
            base.Start();
            DamagerToEnable = DamagerToEnable ?? GetComponentInChildren<Damager>();
        }
        public override void Update()
        {
            base.Update();
            DamagerToEnable.gameObject.SetActive(IsUsingItem);
            if (DamagerToEnable.IsColliding && EnableHealthLoss)
            {
                MeleeWeaponHealth -= DamagePerUse;
                if (MeleeWeaponHealth <= 0)
                {
                    if (ItemQuantity > 0 || Unlocked == true)
                    {
                        JUInventory inventory = GetComponentInParent<JUInventory>();
                        inventory.UnequipItem(JUInventory.GetGlobalItemSwitchID(this, inventory));

                        RemoveItem();
                    }
                }
            }
        }
    }

}
