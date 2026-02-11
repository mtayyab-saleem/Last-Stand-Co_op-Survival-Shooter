using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;

namespace JUTPS.InventorySystem.UI
{
    public class UIItemInformation : MonoBehaviour
    {
        private JUHoldableItem CurrentItem;
        private JUCharacterController Player;

        [Header("Essentials")]
        public Sprite EmptySprite;
        public Image Icon;
        public Text ItemName;
        public Text ItemQuantity;
        public GameObject BulletLabel;
        public Text BulletQuantity;
        public Image ItemHealth;
        void Start()
        {
            //Player = JUGameManager.PlayerController;
        }

        // Update is called once per frame
        void Update()
        {
            if (Player == null)
            {
                Player = JUGameManager.PlayerController;
                return;
            }

            if (Player.Inventory == null) return;

            CurrentItem = Player.HoldableItemInUseRightHand;

            if (CurrentItem == null)
            {
                Icon.sprite = EmptySprite;
                BulletLabel.SetActive(false);
                ItemName.text = "Hand";
                ItemQuantity.text = "";
                ItemHealth.fillAmount = 1;
            }
            else
            {
                if (CurrentItem is Weapon)
                {
                    Icon.sprite = CurrentItem.ItemIcon;
                    ItemName.text = CurrentItem.ItemName;
                    ItemQuantity.text = CurrentItem.ItemQuantity + "/" + CurrentItem.MaxItemQuantity;

                    BulletLabel.SetActive(true);
                    BulletQuantity.text = ((Weapon)CurrentItem).BulletsAmounts + "/" + ((Weapon)CurrentItem).TotalBullets;
                    ItemHealth.fillAmount = (float)((Weapon)CurrentItem).BulletsAmounts / (float)((Weapon)CurrentItem).BulletsPerMagazine;
                    return;
                }

                if (CurrentItem is JUHoldableItem || CurrentItem is ThrowableItem)
                {
                    Icon.sprite = CurrentItem.ItemIcon;
                    ItemName.text = CurrentItem.ItemName;
                    ItemQuantity.text = CurrentItem.ItemQuantity + "/" + CurrentItem.MaxItemQuantity;

                    BulletLabel.SetActive(false);
                    ItemHealth.fillAmount = (float)CurrentItem.ItemQuantity / (float)CurrentItem.MaxItemQuantity;
                }

                if (CurrentItem is MeleeWeapon)
                {
                    Icon.sprite = CurrentItem.ItemIcon;
                    ItemName.text = CurrentItem.ItemName;
                    ItemQuantity.text = CurrentItem.ItemQuantity + "/" + CurrentItem.MaxItemQuantity;

                    BulletLabel.SetActive(false);
                    ItemHealth.fillAmount = (float)((MeleeWeapon)CurrentItem).MeleeWeaponHealth / 100;

                }
            }
        }
    }

}