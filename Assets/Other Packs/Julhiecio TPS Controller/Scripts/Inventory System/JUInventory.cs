using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;
using JUTPS.ArmorSystem;
using JUTPSEditor.JUHeader;

namespace JUTPS.InventorySystem
{

    [AddComponentMenu("JU TPS/Third Person System/Additionals/Inventory")]
    public class JUInventory : MonoBehaviour
    {
        private JUTPS.CharacterBrain.JUCharacterBrain JUCharacter;
        public enum SequentialSlotsEnum { first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth }

        [JUHeader("Settings")]
        public bool UpdateOnBodyItemsVisibility = false;
        public bool DisableAllItemsOnStart = true;
        public bool IsALoot;

        [JUHeader("Items")]
        public JUHoldableItem[] HoldableItensRightHand;
        public JUHoldableItem[] HoldableItensLeftHand;
        [HideInInspector] public JUHoldableItem[] AllHoldableItems;
        public JUItem[] AllItems;

        [JUHeader("Sequential Items")]
        public SequentialSlot[] SequenceSlot = new SequentialSlot[]
        { new SequentialSlot(SequentialSlotsEnum.first, null),
      new SequentialSlot(SequentialSlotsEnum.second, null),
      new SequentialSlot(SequentialSlotsEnum.third, null),
      new SequentialSlot(SequentialSlotsEnum.fourth, null),
      new SequentialSlot(SequentialSlotsEnum.fifth, null),
      new SequentialSlot(SequentialSlotsEnum.sixth, null),
      new SequentialSlot(SequentialSlotsEnum.seventh, null),
      new SequentialSlot(SequentialSlotsEnum.eighth, null),
      new SequentialSlot(SequentialSlotsEnum.ninth, null),
      new SequentialSlot(SequentialSlotsEnum.tenth, null)};

        [JUHeader("PickUp System")]
        public bool EnablePickup = true;
        public LayerMask ItemLayer;
        public Vector3 CheckerOffset;
        public float CheckerRadious = 1;
        public bool UseDefaultInputToPickUp = true;
        public bool AutoEquipPickedUpItems = true;
        [Range(0, 1)]
        public float HoldTimeToPickUp = 0.1f;
        private float CurrentHoldTimeToPickUp;
        private float CurrentTimeToDisablePickingUpState;
        [HideInInspector] public JUItem ItemToPickUp;
        [HideInInspector] public Collider[] ItemsAround;

        [HideInInspector] public Weapon[] WeaponsRightHand;
        [HideInInspector] public Weapon[] WeaponsLeftHand;

        [HideInInspector] public JUHoldableItem HoldableItemInUseInRightHand, HoldableItemInUseInLeftHand;
        [HideInInspector] public Weapon WeaponInUseInRightHand, WeaponInUseInLeftHand;
        [HideInInspector] public MeleeWeapon MeleeWeaponInUseInRightHand, MeleeWeaponInUseInLeftHand;

        //[HideInInspector] public GameObject ItemToPickup;
        [HideInInspector] public int CurrentRightHandItemID = -1, CurrentLeftHandItemID = -1; // [-1] = Hand

        /// <summary>
        /// Return true if the <see cref="JUCharacter"/> is picking a near item.
        /// </summary>
        public bool IsPickingItem { get; private set; }

        /// <summary>
        /// Return true if the <see cref="JUCharacter"/> is using a <see cref="JUHoldableItem"/> on any hand (left or right).
        /// </summary>
        public bool IsItemSelected
        {
            get => HoldableItemInUseInLeftHand || HoldableItemInUseInRightHand;
        }

        /// <summary>
        /// Return true if the <see cref="JUCharacter"/> is using a <see cref="JUHoldableItem"/> on left and right hand.
        /// </summary>
        public bool IsDualWielding
        {
            get => HoldableItemInUseInLeftHand && HoldableItemInUseInRightHand;
        }

        private void Start()
        {
            if (ItemLayer.value == 0) ItemLayer = LayerMask.GetMask("Item");
            //AllItems = GetComponentsInChildren<Item>();
            //AllHoldableItems = AllHoldableItems ?? GetComponentsInChildren<HoldableItem>();
            //HoldableItensLeftHand = HoldableItensLeftHand ?? GetAllItemsOnCharacterHand(gameObject, false);
            //HoldableItensRightHand = HoldableItensRightHand ?? GetAllItemsOnCharacterHand(gameObject, true);
            JUCharacter = GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();

            SetupItems();
            CorrectSwitchIDs(HoldableItensLeftHand);
            CorrectSwitchIDs(HoldableItensRightHand);

            foreach (JUItem item in AllItems)
            {
                DisableItemPhysics(item.gameObject);
            }
            if (DisableAllItemsOnStart)
            {
                foreach (JUItem item in AllItems)
                {
                    if (item is JUHoldableItem) (item as JUHoldableItem).RefreshItemDependencies();
                    item.gameObject.SetActive(false);
                }
            }

        }
        private void Update()
        {
            //PickUp Items Check
            CheckItemsAround();

            //Unequip Items
            if (HoldableItemInUseInLeftHand != null)
            {
                if (HoldableItemInUseInLeftHand.ItemQuantity == 0 || HoldableItemInUseInLeftHand.Unlocked == false)
                {
                    UnequipItem(GetGlobalItemSwitchID(HoldableItemInUseInLeftHand, this));
                }
            }
            if (HoldableItemInUseInRightHand != null)
            {
                if (HoldableItemInUseInRightHand.ItemQuantity == 0 || HoldableItemInUseInRightHand.Unlocked == false)
                {
                    UnequipItem(GetGlobalItemSwitchID(HoldableItemInUseInRightHand, this));
                }
            }

            //Loot
            if (JUCharacter != null)
            {
                if (JUCharacter.IsDead && IsALoot == false)
                {
                    IsALoot = true;
                }
            }


        }

        [ContextMenu(" >>> Setup Items", false, 100)]
        public void SetupItems()
        {
            if (IsALoot)
            {
                AllHoldableItems = GetComponentsInChildren<JUHoldableItem>();
                HoldableItensLeftHand = GetAllItemsOnCharacterHand(gameObject, false);
                HoldableItensRightHand = GetAllItemsOnCharacterHand(gameObject, true);
                AllItems = GetComponentsInChildren<JUItem>();
                return;
            }

            if (JUCharacter == null)
            {
                JUCharacter = GetComponent<JUTPS.CharacterBrain.JUCharacterBrain>();
            }
            if (JUCharacter != null)
            {
                if (JUCharacter.anim == null) { JUCharacter.anim = GetComponent<Animator>(); }
            }
            else { Debug.LogError("No JU Character Controller/Brain"); return; }

            if (JUCharacter.anim != null)
            {
                if (JUCharacter.anim.GetBoneTransform(HumanBodyBones.RightHand) == null || JUCharacter.anim.GetBoneTransform(HumanBodyBones.RightHand) == null)
                {
                    if (IsInvoking(nameof(SetupItems)))
                    {
                        Debug.LogWarning("Unable to setup items on this character on game start as there was a problem with the character's rig, inventory will try again soon");
                        Invoke(nameof(SetupItems), 0.1f);
                    }
                    return;
                }

                AllHoldableItems = GetComponentsInChildren<JUHoldableItem>();
                HoldableItensLeftHand = GetAllItemsOnCharacterHand(gameObject, false);
                HoldableItensRightHand = GetAllItemsOnCharacterHand(gameObject, true);
                AllItems = GetComponentsInChildren<JUItem>();
            }
        }
        public static void CorrectSwitchIDs(JUHoldableItem[] ItemsArray)
        {
            if (ItemsArray == null) return;

            for (int i = 0; i < ItemsArray.Length; i++)
            {
                if (ItemsArray[i].ItemSwitchID != i)
                {
                    Debug.Log("The SwitchID from the item " + "'" + ItemsArray[i].name + "'" + " was changed from [" + ItemsArray[i].ItemSwitchID + "] to [" + i + "]  | the JU Inventory fixed the Switch IDs automatically.", ItemsArray[i]);

                    ItemsArray[i].ItemSwitchID = i;
                }
            }
        }


        private void CheckItemsAround()
        {
            if (EnablePickup == false || CheckerRadious < 0.0001) return;

            ItemsAround = Physics.OverlapSphere(transform.TransformPoint(CheckerOffset), 1, ItemLayer);

            if (ItemsAround.Length > 0 && ItemToPickUp == null)
            {
                ItemToPickUp = ItemsAround[0].GetComponent<JUItem>() == null ? null : ItemsAround[0].GetComponent<JUItem>();
            }
            if (ItemToPickUp != null && ItemsAround.Length == 0)
            {
                ItemToPickUp = null;
            }


            if (JUInputSystem.JUInput.GetButton(JUInputSystem.JUInput.Buttons.PickupButton) && ItemToPickUp != null)
            {
                CurrentHoldTimeToPickUp += Time.deltaTime;
                if (CurrentHoldTimeToPickUp >= HoldTimeToPickUp)
                {
                    Debug.Log("Trying pickup");
                    PickUp();
                }
            }

            if (IsPickingItem)
            {
                CurrentTimeToDisablePickingUpState += Time.deltaTime;
                if (CurrentTimeToDisablePickingUpState >= 0.4f)
                {
                    IsPickingItem = false;
                }
            }
            else { CurrentTimeToDisablePickingUpState = 0; }

        }

        public static JUHoldableItem[] GetAllItemsOnCharacterHand(GameObject character, bool RightHand = true)
        {
            JUHoldableItem[] items;

            Animator anim = character.GetComponent<Animator>();

            if (anim == null) { Debug.LogError("Unable to find items in hands because there is no animator"); return null; }
            if (anim.isHuman == false) { Debug.LogError("Unable to find items in hands because the animator is not a humanoid"); return null; }
            if (anim.GetBoneTransform(HumanBodyBones.RightHand) == null) { Debug.LogWarning("Unable to find items in hands because the animator is not a humanoid"); return null; }

            Transform hand = RightHand ? anim.GetBoneTransform(HumanBodyBones.RightHand) : anim.GetBoneTransform(HumanBodyBones.LeftHand);
            items = hand.GetComponentsInChildren<JUHoldableItem>();

            return items;
        }


        public static void DisableItemPhysics(GameObject item)
        {
            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            Rigidbody[] rigidbodies = item.GetComponentsInChildren<Rigidbody>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.isKinematic = true;
            }
        }
        public static void EnableItemPhysic(GameObject item)
        {
            Collider[] colliders = item.GetComponentsInChildren<Collider>();
            Rigidbody[] rigidbodies = item.GetComponentsInChildren<Rigidbody>();
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.isKinematic = false;
            }
        }


        public static void PickUpNearbyItem(JUInventory InventoryToAddItem)
        {
            if (InventoryToAddItem.ItemToPickUp != null)
            {
                //Debug.Log("Called PickUpItem method 1");

                //Check if item is holdable
                if (InventoryToAddItem.ItemToPickUp is JUHoldableItem)
                {
                    //Debug.Log("Called PickUpItem method 2");

                    //Get holdable monobehaviour
                    var PickedItemToUnlock = InventoryToAddItem.ItemToPickUp.GetComponent<JUHoldableItem>();
                    foreach (JUHoldableItem ItemInInventoryToUnlock in InventoryToAddItem.AllHoldableItems)
                    {
                        //Debug.Log("Called PickUpItem method 3");
                        if (ItemInInventoryToUnlock.ItemName == PickedItemToUnlock.ItemName && ItemInInventoryToUnlock.IsLeftHandItem == PickedItemToUnlock.IsLeftHandItem)
                        {
                            //Unlock item on inventory
                            ItemInInventoryToUnlock.Unlocked = true;

                            //Transfer item data
                            InventoryToAddItem.AddPickedItemData(ItemInInventoryToUnlock, PickedItemToUnlock);
                            //ItemInInventoryToUnlock.AddItem();
                            InventoryToAddItem.RefreshInBodyItemVisibility();

                            //Destroy Item in scenary
                            Destroy(PickedItemToUnlock.gameObject);

                            //Equip Holdable Item
                            /*ItemSwitchManager switchManager = InventoryToAddItem.GetComponent<ItemSwitchManager>();
                            if (switchManager != null)
                            {
                                switchManager.SwitchToItem(ItemInInventoryToUnlock.ItemSwitchID);
                            }*/
                            InventoryToAddItem.IsPickingItem = true;

                            if (InventoryToAddItem.AutoEquipPickedUpItems)
                            {
                                InventoryToAddItem.JUCharacter.SwitchToItem(ItemInInventoryToUnlock.ItemSwitchID, !ItemInInventoryToUnlock.IsLeftHandItem);
                            }
                            return;
                        }
                    }
                }
                else
                {
                    foreach (JUItem ItemToAdd in InventoryToAddItem.AllItems)
                    {
                        var PickedToAdd = InventoryToAddItem.ItemToPickUp;

                        if (ItemToAdd.ItemSwitchID == PickedToAdd.ItemSwitchID && ItemToAdd.ItemName == PickedToAdd.ItemName)
                        {
                            //Transfer item data
                            InventoryToAddItem.AddPickedItemData(ItemToAdd, PickedToAdd);
                            InventoryToAddItem.RefreshInBodyItemVisibility();

                            //Destroy Item in scenary
                            Destroy(PickedToAdd.gameObject);

                            //Equip Armor
                            /*if(ItemToAdd is Armor)
                            {
                                InventoryToAddItem.EquipItem(GetGlobalItemSwitchID(ItemToAdd, InventoryToAddItem));
                            }*/

                            Debug.Log(InventoryToAddItem.gameObject.name + " picked the item: " + ItemToAdd.ItemName);
                            InventoryToAddItem.IsPickingItem = true;
                            return;
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("It was not possible to pick up the item because ItemToPickUp variable is null");
            }


        }
        public static JUHoldableItem GetCurrentHoldableItemInUsing(JUInventory inventory, bool RightHand = true)
        {
            JUHoldableItem itemInUse = null;
            if (RightHand)
            {
                foreach (JUHoldableItem item in inventory.HoldableItensRightHand)
                {
                    if (item.ItemSwitchID == inventory.CurrentRightHandItemID) itemInUse = item;
                }
            }
            else
            {
                foreach (JUHoldableItem item in inventory.HoldableItensLeftHand)
                {
                    if (item.ItemSwitchID == inventory.CurrentLeftHandItemID) itemInUse = item;
                }
            }
            return itemInUse;
        }

        /// <summary>
        /// Disable items that are not selected as ItemInUse and enable the selected one
        /// </summary>
        protected virtual void RefreshItemsVisibility()
        {
            for (int i = 0; i < HoldableItensRightHand.Length; i++)
            {
                if (CurrentRightHandItemID > -1)
                {
                    //If its not the item of index, disable it.
                    if (i != CurrentRightHandItemID)
                    {
                        HoldableItensRightHand[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        //If its the weapon of index and its unlocked, enable it
                        if (HoldableItensRightHand[i].Unlocked == true)
                        {
                            //Set holdable item in use
                            HoldableItemInUseInRightHand = HoldableItensRightHand[i];
                            HoldableItensRightHand[i].gameObject.SetActive(true);

                            if (HoldableItemInUseInRightHand is Weapon)
                            {
                                //Set weapon in use
                                WeaponInUseInRightHand = HoldableItemInUseInRightHand.GetComponent<Weapon>();
                                //Debug.Log("Seted Weapon In Use");
                            }
                        }
                        else
                        {
                            HoldableItemInUseInRightHand = null;
                            WeaponInUseInRightHand = null;
                        }
                    }
                }
                else
                {
                    HoldableItensRightHand[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < HoldableItensLeftHand.Length; i++)
            {
                if (CurrentLeftHandItemID > -1)
                {
                    //If its not the item of index, disable it.
                    if (i != CurrentLeftHandItemID)
                    {
                        HoldableItensLeftHand[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        //If its the weapon of index and its unlocked, enable it
                        if (HoldableItensLeftHand[i].Unlocked == true)
                        {
                            //Set holdable item in use
                            HoldableItemInUseInLeftHand = HoldableItensLeftHand[i];
                            HoldableItensLeftHand[i].gameObject.SetActive(true);

                            if (HoldableItemInUseInLeftHand is Weapon)
                            {
                                //Set weapon in use
                                WeaponInUseInLeftHand = HoldableItemInUseInLeftHand.GetComponent<Weapon>();
                                //Debug.Log("Seted Weapon In Use");
                            }
                        }
                        else
                        {
                            HoldableItemInUseInLeftHand = null;
                            WeaponInUseInLeftHand = null;
                        }
                    }
                }
                else
                {
                    HoldableItensLeftHand[i].gameObject.SetActive(false);
                }
            }
        }
        public void SwitchToItem(int id = -1, bool RightHand = true)
        {
            
            //>>> Loop Item Switching
            if (RightHand)
            {
                if (id < -1)
                {
                    CurrentRightHandItemID = HoldableItensRightHand.Length - 1;
                    HoldableItemInUseInRightHand = HoldableItensRightHand[HoldableItensRightHand.Length - 1];
                }
                if (id == HoldableItensRightHand.Length)
                {
                    CurrentRightHandItemID = -1;
                    WeaponInUseInRightHand = null;
                    HoldableItemInUseInRightHand = null;
                }
            }
            else
            {

                if (id < -1)
                {
                    CurrentLeftHandItemID = HoldableItensLeftHand.Length - 1;
                    HoldableItemInUseInLeftHand = HoldableItensLeftHand[HoldableItensLeftHand.Length - 1];
                }
                if (id == HoldableItensLeftHand.Length)
                {
                    CurrentLeftHandItemID = -1;
                    WeaponInUseInLeftHand = null;
                    HoldableItemInUseInLeftHand = null;
                }
            }
            if (RightHand == true)
            {
             
                //>>> Right Hand Switching
                foreach (JUHoldableItem item in HoldableItensRightHand)
                {
                    //Switch to hand
                    if (id < 0)
                    {
                        CurrentRightHandItemID = -1;
                        WeaponInUseInRightHand = null;
                        HoldableItemInUseInRightHand = null;
                    }
                    else
                    {
                        //Swith to item
                        if (item.ItemSwitchID == id)
                        {
                            //Return if not changed item
                            if (HoldableItemInUseInRightHand == item) return;

                            //Set item in use
                            HoldableItemInUseInRightHand = item;

                            //Set weapon in use if its weapon
                            if (item is Weapon) { WeaponInUseInRightHand = item.GetComponent<Weapon>(); } else { WeaponInUseInRightHand = null; }

                            //Set weapon in use if its weapon
                            if (item is MeleeWeapon) { MeleeWeaponInUseInRightHand = item.GetComponent<MeleeWeapon>(); } else { MeleeWeaponInUseInRightHand = null; }

                            CurrentRightHandItemID = id;
                        }
                    }
                }
            }
            if (RightHand == false)
            {
                //>>> Left Hand Switching
                foreach (JUHoldableItem item in HoldableItensLeftHand)
                {
                    //Switch to hand
                    if (id < 0)
                    {
                        CurrentLeftHandItemID = id;
                        WeaponInUseInLeftHand = null;
                        HoldableItemInUseInLeftHand = null;
                    }
                    else
                    {
                        //Swith to item
                        if (item.ItemSwitchID == id)
                        {
                            //Return if not changed item
                            if (HoldableItemInUseInLeftHand == item) return;

                            //Set item in use
                            HoldableItemInUseInLeftHand = item;

                            //Set weapon in use if its weapon
                            if (item is Weapon) { WeaponInUseInLeftHand = item.GetComponent<Weapon>(); } else { WeaponInUseInLeftHand = null; }

                            //Set weapon in use if its weapon
                            if (item is MeleeWeapon) { MeleeWeaponInUseInLeftHand = item.GetComponent<MeleeWeapon>(); } else { MeleeWeaponInUseInLeftHand = null; }

                            CurrentLeftHandItemID = id;
                        }
                    }
                }
            }
            UpdateItemInUse();
            RefreshItemsVisibility();
            RefreshInBodyItemVisibility();
        }
        public static int GetGlobalItemSwitchID(JUItem item, JUInventory inventory)
        {
            int global_id = -3;

            if (item == null) return global_id;

            for (int i = 0; i < inventory.AllItems.Length; i++)
            {
                if (inventory.AllItems[i].ItemName == item.ItemName) global_id = i;
            }
            //Debug.Log("Item ID " + global_id);
            return global_id;
        }

        public int GetNextUnlockedItemID(int CurrentID, bool LocalID = true, bool RightHand = true)
        {
            int item_id;
            if (LocalID)
            {
                if (RightHand)
                {
                    item_id = NextUnlockedItemLocalIndexRightHand(CurrentID);
                }
                else
                {
                    item_id = NextUnlockedItemLocalIndexLeftHand(CurrentID);
                }
            }
            else
            {
                if (RightHand)
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensRightHand[NextUnlockedItemLocalIndexRightHand(CurrentID)], inventory: this);
                }
                else
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensLeftHand[NextUnlockedItemLocalIndexLeftHand(CurrentID)], inventory: this);
                }
            }
            return item_id;
        }
        public int GetPreviousUnlockedItemID(int CurrentID, bool LocalID = true, bool RightHand = true)
        {
            int item_id;
            if (LocalID)
            {
                if (RightHand)
                {
                    item_id = PreviousUnlockedItemLocalIndexRightHand(CurrentID);
                }
                else
                {
                    item_id = PreviousUnlockedItemLocalIndexLeftHand(CurrentID);
                }
            }
            else
            {
                if (RightHand)
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensRightHand[PreviousUnlockedItemLocalIndexRightHand(CurrentID)], inventory: this);
                }
                else
                {
                    item_id = GetGlobalItemSwitchID(item: HoldableItensLeftHand[PreviousUnlockedItemLocalIndexLeftHand(CurrentID)], inventory: this);
                }
            }
            return item_id;
        }


        public void SetSequentialSlotItem(SequentialSlotsEnum slot, JUItem item)
        {
            foreach (SequentialSlot s in SequenceSlot)
            {
                if (s.SelectedSlot == slot)
                {
                    s.ItemInThisSlot = item;
                    if (item != null) Debug.Log(slot.ToString() + "Slot now has the item : " + item.ItemName);
                }
            }
        }
        public JUItem GetSequentialSlotItem(SequentialSlotsEnum slot)
        {
            JUItem itemSlotToReturn = null;

            foreach (SequentialSlot s in SequenceSlot)
            {
                if (s.SelectedSlot == slot) itemSlotToReturn = s.ItemInThisSlot;
            }

            return itemSlotToReturn;
        }


        protected int NextUnlockedItemLocalIndexRightHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i < HoldableItensRightHand.Length; i++)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensRightHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensRightHand[i].ItemSwitchID;
                        return HoldableItensRightHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }
        protected int NextUnlockedItemLocalIndexLeftHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i < HoldableItensLeftHand.Length; i++)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensLeftHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensLeftHand[i].ItemSwitchID;
                        return HoldableItensLeftHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }
        protected int PreviousUnlockedItemLocalIndexRightHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i > -1; i--)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensRightHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensRightHand[i].ItemSwitchID;
                        return HoldableItensRightHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }
        protected int PreviousUnlockedItemLocalIndexLeftHand(int CurrentID)
        {
            int item_id = -1;
            for (int i = CurrentID; i > -1; i--)
            {
                if (i > -1 && i != CurrentID)
                {
                    if (HoldableItensLeftHand[i].Unlocked == true)
                    {
                        item_id = HoldableItensLeftHand[i].ItemSwitchID;
                        return HoldableItensLeftHand[i].ItemSwitchID;
                    }
                }
            }
            return item_id;
        }


        public void RefreshInBodyItemVisibility()
        {
            if (UpdateOnBodyItemsVisibility == false) return;
            for (int i = HoldableItensRightHand.Length - 1; i > -1; i--)
            {
                if (i != CurrentRightHandItemID)
                {
                    if (HoldableItensRightHand[i].Unlocked == true && HoldableItensRightHand[i].ItemModelInBody != null)
                    {
                        HoldableItensRightHand[i].ItemModelInBody.gameObject.SetActive(true);
                    }
                    else if (HoldableItensRightHand[i].ItemModelInBody != null)
                    {
                        HoldableItensRightHand[i].ItemModelInBody.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (HoldableItensRightHand[i].Unlocked == true && HoldableItensRightHand[i].ItemModelInBody != null)
                    {
                        HoldableItensRightHand[i].ItemModelInBody.gameObject.SetActive(false);
                    }
                }
            }
        }
        public void PickUp()
        {
            if (ItemToPickUp == null) return;

            PickUpNearbyItem(this);
            //PickUpItem(this, GetGlobalItemSwitchID(ItemToPickUp, this));
        }

        /// <summary>
        /// This function copies or adds data according to the item type. Useful for the pickup system.
        /// </summary>
        /// <param name="OnInventoryItem"></param>
        /// <param name="ItemToPickup"></param>
        public void AddPickedItemData(JUItem OnInventoryItem, JUItem ItemToPickup)
        {
            GetLootItem(OnInventoryItem, ItemToPickUp);
        }

        /// <summary>
        /// Get Item by Switch ID
        /// </summary>
        /// <param name="globalID">Global Item Switch ID</param>
        /// <returns></returns>
        public JUItem GetItem(int globalID) { return AllItems[globalID]; }

        /// <summary>
        /// Returns an item according to the item name or gameobject name
        /// </summary>
        /// <param name="itemName">item name or gameobject name</param>
        /// <returns></returns>
        public JUItem GetItem(string itemName)
        {
            JUItem itemToReturn = null;
            foreach (JUItem currentItem in AllItems)
            {
                if (currentItem.ItemName == itemName || currentItem.name == itemName)
                {
                    itemToReturn = currentItem;
                }
            }
            return itemToReturn;
        }

        /// <summary>
        /// Returns an item according to the item name or gameobject name
        /// </summary>
        /// <param name="itemName">item name or gameobject name</param>
        /// <param name="inventory"></param>
        /// <returns></returns>
        public static JUItem GetItem(JUInventory inventory, string itemName)
        {
            JUItem itemToReturn = null;
            foreach (JUItem currentItem in inventory.AllItems)
            {
                if (currentItem.ItemName == itemName || currentItem.name == itemName)
                {
                    itemToReturn = currentItem;
                }
            }
            return itemToReturn;
        }

        public void GetLootItem(JUItem itemOnThisInventory, JUItem itemOnLoot)
        {

            if (itemOnThisInventory is Weapon)
            {
                Weapon localWeapon = itemOnThisInventory as Weapon;
                Weapon lootWeapon = itemOnLoot as Weapon;
                //localWeapon.TotalBullets = localWeapon.Unlocked ? lootWeapon.TotalBullets : (localWeapon.TotalBullets + lootWeapon.TotalBullets);

                if (itemOnThisInventory.ItemQuantity == 0)
                {
                    itemOnThisInventory.ItemQuantity += 1;
                    itemOnThisInventory.Unlocked = true;

                    localWeapon.TotalBullets = lootWeapon.TotalBullets;
                    localWeapon.BulletsAmounts = lootWeapon.BulletsAmounts;

                    lootWeapon.Unlocked = false;

                }
                else
                {
                    //Get All Bullets
                    localWeapon.TotalBullets += lootWeapon.TotalBullets;
                    itemOnThisInventory.ItemQuantity += 1;
                    //localWeapon.BulletsAmounts = lootWeapon.BulletsAmounts;

                    //lootWeapon.ItemQuantity = 0;
                    //lootWeapon.Unlocked = false;
                    //lootWeapon.TotalBullets = 0; 
                    //lootWeapon.BulletsAmounts = 0;
                }
                return;
            }
            
            itemOnThisInventory.ItemQuantity = itemOnThisInventory.Unlocked ? itemOnLoot.ItemQuantity : (itemOnThisInventory.ItemQuantity + itemOnLoot.ItemQuantity);
            itemOnThisInventory.ItemQuantity = Mathf.Clamp(itemOnThisInventory.ItemQuantity, 0, itemOnThisInventory.MaxItemQuantity);

            if (itemOnThisInventory is MeleeWeapon)
            {
                MeleeWeapon localMeleeWeapon = itemOnThisInventory as MeleeWeapon;
                MeleeWeapon _lootMeleeWeapon = itemOnLoot as MeleeWeapon;

                if (localMeleeWeapon.Unlocked == false)
                {
                    localMeleeWeapon.MeleeWeaponHealth = _lootMeleeWeapon.MeleeWeaponHealth;
                    localMeleeWeapon.Unlocked = true;
                    _lootMeleeWeapon.Unlocked = false;
                }
                else
                {
                    float oldLocalDamage = localMeleeWeapon.MeleeWeaponHealth;
                    localMeleeWeapon.MeleeWeaponHealth = _lootMeleeWeapon.MeleeWeaponHealth;
                    _lootMeleeWeapon.MeleeWeaponHealth = oldLocalDamage;
                }
            }

            if (itemOnThisInventory is Armor)
            {
                Armor localAmor = itemOnThisInventory as Armor;
                Armor _lootArmor = itemOnLoot as Armor;

                if (localAmor.Unlocked == false)
                {
                    localAmor.Health = _lootArmor.Health;
                    localAmor.Unlocked = true;
                    _lootArmor.Unlocked = false;
                }
                else
                {
                    float oldLocalArmorHealth = localAmor.Health;
                    localAmor.Health = _lootArmor.Health;
                    _lootArmor.Health = oldLocalArmorHealth;
                }
            }
        }
        public void DropItem(int ID, bool IsRightHandItem = true)
        {
            Vector3 DropPosition = transform.position + transform.forward * 0.2f + transform.up * 0.5f;
            if (IsRightHandItem)
            {
                if (HoldableItensRightHand[ID].Unlocked == false) return;

                //Get Item World Scale 
                Vector3 worldScale = HoldableItensRightHand[ID].transform.lossyScale;

                //Instantiate Item
                var dropedItem = (GameObject)Instantiate(HoldableItensRightHand[ID].gameObject, DropPosition, Quaternion.identity);
                dropedItem.transform.localScale = worldScale;
                EnableItemPhysic(dropedItem);
                dropedItem.SetActive(true);
                dropedItem.layer = 14;                
                
                //Remove / Lock Item
                HoldableItensRightHand[ID].RemoveItem();

                //Switch Item
                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: true);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: true);
                }
            }
            else
            {
                if (HoldableItensRightHand[ID].Unlocked == false) return;

                //Instantiate Item
                var dropedItem = (GameObject)Instantiate(HoldableItensLeftHand[ID].gameObject, DropPosition, Quaternion.identity);
                EnableItemPhysic(dropedItem);
                dropedItem.layer = 14;

                //Remove / Lock Item
                HoldableItensLeftHand[ID].RemoveItem();

                //Switch Item
                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: false);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: false);
                }
            }
            RefreshInBodyItemVisibility();
        }
        public void DropItem(int ID)
        {
            Vector3 DropPosition = transform.position + transform.forward * 0.2f + transform.up * 0.5f;
            if (ID < 0) return;
            if (ID > AllItems.Length) return;
            if (AllItems[ID].Unlocked == false) return;

            //Get Item World Scale 
            Vector3 worldScale = AllItems[ID].transform.lossyScale;

            if (AllItems[ID] is Armor)
            {
                foreach (GameObject armorPart in (AllItems[ID] as Armor).Parts)
                {
                    armorPart.transform.parent = AllItems[ID].transform;
                }
            }

            //Instantiate Item
            var dropedItem = (GameObject)Instantiate(AllItems[ID].gameObject, DropPosition, Quaternion.identity);
            dropedItem.transform.localScale = worldScale;
            EnableItemPhysic(dropedItem);
            dropedItem.SetActive(true);
            dropedItem.layer = 14;

            //Remove / Lock Item
            AllItems[ID].RemoveItem();
            if (AllItems[ID] is Armor && AllItems[ID].gameObject.activeInHierarchy) { AllItems[ID].gameObject.SetActive(false); }

            //Switch Item
            if (AllItems[ID] is JUHoldableItem)
            {
                if (AllItems[ID].gameObject.activeInHierarchy)
                {
                    if (JUCharacter == null)
                    {
                        SwitchToItem(-1, RightHand: true);
                    }
                    else
                    {
                        JUCharacter.SwitchToItem(-1, RightHand: true);
                    }
                }
            }
            RefreshInBodyItemVisibility();
        }
        public void EquipItem(int ID)
        {
            //Out of the bounds
            if (ID < 0 || ID > AllItems.Length) return;

            if (AllItems[ID].Unlocked == false) return;



            if (AllItems[ID] is Armor)
            {
                AllItems[ID].gameObject.SetActive(true);
                //Debug.Log("equiped " + AllItems[ID].gameObject.name);
                return;
            }
            if ((AllItems[ID].GetType()).IsSubclassOf(typeof(JUHoldableItem)) == false)
            {
                AllItems[ID].gameObject.SetActive(true);
                //Debug.Log("equiped " + AllItems[ID].gameObject.name);
                return;
            }

            //Debug.Log("Try equip holdable item: " + AllItems[ID].ItemName);
            //Get Respective Item
            JUHoldableItem holdableItem = AllItems[ID] as JUHoldableItem;

            if (JUCharacter == null)
            {
                //Equip directly to inventory
                SwitchToItem(AllItems[ID].ItemSwitchID, !holdableItem.IsLeftHandItem);
                return;
            }
            else
            {
                //Equip with the controller
                JUCharacter.SwitchToItem(AllItems[ID].ItemSwitchID, !holdableItem.IsLeftHandItem);
            }
        }
        public void UnequipItem(int ID)
        {
            //Out of the bounds
            if (ID < 0 || ID > AllItems.Length || AllItems[ID] == null) return;
            //if (AllItems[ID].Unlocked == false) return;

            if (AllItems[ID] is Armor)
            {
                AllItems[ID].gameObject.SetActive(false);
                Debug.Log("Unequiped " + AllItems[ID].gameObject.name);

                return;
            }

            if ((AllItems[ID].GetType()).IsSubclassOf(typeof(JUHoldableItem)) == false)
            {
                AllItems[ID].gameObject.SetActive(false);
                if ((AllItems[ID] as JUHoldableItem).IsLeftHandItem == false)
                {
                    HoldableItemInUseInRightHand = null;
                }
                else
                {
                    HoldableItemInUseInLeftHand = null;
                }

                Debug.Log("Unequiped " + AllItems[ID].gameObject.name);

                return;
            }



            //Get Respective Item
            JUHoldableItem holdableItem = AllItems[ID] as JUHoldableItem;

            /*
            if (JUCharacter == null)
            {
                //Equip directly to inventory
                SwitchToItem(-1, !holdableItem.IsLeftHandItem);
                return;
            }
            else
            {
                //Equip with the controller
                JUCharacter.SwitchToItem(-1, !holdableItem.IsLeftHandItem);
            }
            */

            if (!holdableItem.IsLeftHandItem)
            {
                //Switch Item
                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: true);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: true);
                }
            }
            else
            {
                //Switch Item
                if (JUCharacter == null)
                {
                    SwitchToItem(-1, RightHand: false);
                }
                else
                {
                    JUCharacter.SwitchToItem(-1, RightHand: false);
                }
            }
        }
        protected void UpdateItemInUse()
        {
            HoldableItemInUseInLeftHand = GetCurrentHoldableItemInUsing(inventory: this, RightHand: false);
            HoldableItemInUseInRightHand = GetCurrentHoldableItemInUsing(inventory: this, RightHand: true);

            if (HoldableItemInUseInLeftHand is Weapon) WeaponInUseInLeftHand = (Weapon)HoldableItemInUseInLeftHand; else WeaponInUseInLeftHand = null;
            if (HoldableItemInUseInRightHand is Weapon) WeaponInUseInRightHand = (Weapon)HoldableItemInUseInRightHand; else WeaponInUseInRightHand = null;

            if (HoldableItemInUseInLeftHand is MeleeWeapon) MeleeWeaponInUseInLeftHand = (MeleeWeapon)HoldableItemInUseInLeftHand; else MeleeWeaponInUseInLeftHand = null;
            if (HoldableItemInUseInRightHand is MeleeWeapon) MeleeWeaponInUseInRightHand = (MeleeWeapon)HoldableItemInUseInRightHand; else MeleeWeaponInUseInRightHand = null;

        }

        private void OnDrawGizmos()
        {

            //PickUp
            if (!EnablePickup) return;
            Gizmos.DrawWireSphere(transform.TransformPoint(CheckerOffset), CheckerRadious);
        }


        [System.Serializable]
        public class SequentialSlot
        {
            public SequentialSlotsEnum SelectedSlot;
            public JUItem ItemInThisSlot;

            public SequentialSlot(SequentialSlotsEnum slot, JUItem itemToSlot)
            {
                SelectedSlot = slot;
                ItemInThisSlot = itemToSlot;
            }
        }
    }

}
