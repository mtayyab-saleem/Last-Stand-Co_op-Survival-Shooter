using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using JUTPS.ArmorSystem;
using JUTPS.WeaponSystem;

using JUTPS.ItemSystem;
using JUTPSEditor.JUHeader;

namespace JUTPS.InventorySystem.UI
{

    public class InventorySlotUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IDropHandler
    {
        private Canvas canvas;
        private InventoryUIManager inventoryManager;
        private int mSlotIndex = 0;

        [JUHeader("Settings")]
        public JUInventory inventory;
        public int ItemIDToDraw = -2;

        [JUHeader("Sequential Item Switch")]
        public bool DrawSequentialItem = false;
        public JUInventory.SequentialSlotsEnum SequentialToDraw = JUInventory.SequentialSlotsEnum.first;
        public bool SetSequentialOnDrop = false;

        [JUHeader("Visual and FX Settings")]
        public Image SlotItemImage;
        public Image SlotHealthBar;

        public Sprite ItemWithoutIconSprite;
        public Sprite EmptySlotSprite;

        public Text ItemQuantityText;
        public GameObject Outline;

        [JUHeader("Options Settings")]
        public bool EnableOptions = true;
        public bool AutoEquipOnDrop = false;
        public bool IsLootSlot = false;

        public string[] AllowItemsWithTags = new[] { "General", "Weapon", "Melee Weapon", "Primary Weapon", "Secundary Weapon", "Tertiary Weapon", "Hand Gun", "Hat", "TShirt", "Pants", "Shoes" };
        public bool AllowAnyItem = false;

        public GameObject OptionsPanel;
        public Button EquipButton, UnequipButton, UseButton, DropButton;



        [JUHeader("States")]
        public bool IsEmpty = true;
        public bool IsDragging = false;
        public ItemArePlacedIn PlacedIn;
        public enum ItemArePlacedIn { AllBody, RightHand, LeftHand }

        public JUItem[] RespectiveItemList;
        private JUItem slotItem;
        private void Awake()
        {
            if (EquipButton != null) EquipButton.onClick.AddListener(Equip);

            if (UnequipButton != null) UnequipButton.onClick.AddListener(Unequip);

            if (UseButton != null) UseButton.onClick.AddListener(Use);

            if (DropButton != null) DropButton.onClick.AddListener(Drop);

        }
        private void Start()
        {
            inventoryManager = gameObject.GetComponentInParent<InventoryUIManager>();
            inventory = inventoryManager.TargetInventory;
            canvas = GetComponent<Canvas>();

            RespectiveItemList = inventory.AllItems;

            slotItem = CurrentSlotItem();
            mSlotIndex = inventoryManager.Slots.IndexOf(this) == -1 ? 0 : inventoryManager.Slots.IndexOf(this);

            RefreshSlot();
            DisableOverriding();
        }
        private void OnDisable() => Outline.SetActive(false);
        private void OnEnable() => RefreshSlot();
        public JUItem CurrentSlotItem()
        {
            if (inventory == null)
            {
                inventory = gameObject.GetComponentInParent<InventoryUIManager>().TargetInventory;
                if (inventory == null)
                {
                    // Debug.LogError("Target Inventory is NULL");
                }
                return null;
            }
            JUItem item = null;
            if (DrawSequentialItem == false)
            {
                if (ItemIDToDraw < inventory.AllItems.Length && ItemIDToDraw > -1)
                {
                    item = inventory.AllItems[ItemIDToDraw];
                }
            }
            else
            {
                item = inventory.GetSequentialSlotItem(SequentialToDraw);
                ItemIDToDraw = JUInventory.GetGlobalItemSwitchID(item, inventory);
            }
            return item;
        }


        public void ShowOptions()
        {
            RefreshSlot();
            if (ItemIDToDraw < 0 || EnableOptions == false) { OptionsPanel.SetActive(false); return; }

            OptionsPanel.SetActive(true);
            if (slotItem != null)
            {
                if (slotItem.ItemQuantity <= 0 || slotItem.Unlocked == false)
                {
                    OptionsPanel.SetActive(false);
                }
                if (slotItem is JUHoldableItem || slotItem is Armor)
                {
                    UseButton.gameObject.SetActive(false);
                    if (slotItem.gameObject.activeInHierarchy == false)
                    {
                        //Equip Item
                        EquipButton.gameObject.SetActive(true);
                        UnequipButton.gameObject.SetActive(false);
                    }
                    else
                    {
                        //Unequip Item
                        UnequipButton.gameObject.SetActive(true);
                        EquipButton.gameObject.SetActive(false);
                    }
                }
                else
                {
                    UseButton.gameObject.SetActive(true);
                    UnequipButton.gameObject.SetActive(false);
                    EquipButton.gameObject.SetActive(false);
                }
            }
            else { OptionsPanel.SetActive(false); }

        }
        public void HideOptions()
        {
            OptionsPanel.SetActive(false);
            //RefreshSlot();
        }

        public void RefreshSlot()
        {
            //Get empty slot sprite
            if (EmptySlotSprite == null) EmptySlotSprite = SlotItemImage.sprite;

            //get current item
            slotItem = CurrentSlotItem();

            //Empty
            SlotItemImage.sprite = EmptySlotSprite;

            //Try to get inventory if its null
            if (inventory == null) { inventory = gameObject.GetComponentInParent<InventoryUIManager>().TargetInventory; }

            //No inventory
            if (inventory == null) return;

            // >>> ITEM HEALTH BAR __________________________________________

            //Disable health bar if item is null
            if (SlotHealthBar != null && slotItem == null) { SlotHealthBar.gameObject.SetActive(false); }

            //Item Health bar system
            if (SlotHealthBar != null)
            {
                if (slotItem != null)
                {
                    if (slotItem.Unlocked == true)
                    {
                        SlotHealthBar.gameObject.SetActive(true); DoHealthBarFillAmout(slotItem, SlotHealthBar, true);
                    }
                    else { SlotHealthBar.gameObject.SetActive(false); }
                }
                else
                {
                    SlotHealthBar.gameObject.SetActive(false);
                }
            }
            //_______________________________________________________________

            //Empty
            if (ItemQuantityText != null) ItemQuantityText.gameObject.SetActive(false);

            //Empty
            if (slotItem == null || ItemIDToDraw < 0 || ItemIDToDraw > RespectiveItemList.Length) { IsEmpty = true; return; }

            if (slotItem.Unlocked == false) { IsEmpty = true; return; }

            //Move Slot
            if (IsItemAllowedOnThisSlot(slotItem) == false) { MoveToACloserSlot(this, inventoryManager.Slots); return; }

            //Theres no item icon
            if (CurrentSlotItem().ItemIcon == null) { SlotItemImage.sprite = ItemWithoutIconSprite; return; }


            //Theres Item but are locked 
            if (slotItem.Unlocked == false || slotItem.ItemQuantity <= 0)
            {
                SlotHealthBar.gameObject.SetActive(false);
                SlotItemImage.sprite = EmptySlotSprite;
                IsEmpty = false;
                return;
            }

            //Enable count text
            if (ItemQuantityText != null)
            {
                ItemQuantityText.gameObject.SetActive(true);
                ItemQuantityText.text = slotItem.ItemQuantity.ToString() + "/" + slotItem.MaxItemQuantity;

                /*
                if (slotItem is Weapon)
                {
                    ItemQuantityText.text = (slotItem as Weapon).BulletsAmounts + "/" + (slotItem as Weapon).TotalBullets;
                }
                else
                {
                    ItemQuantityText.text = slotItem.ItemQuantity.ToString() + "/" + slotItem.MaxItemQuantity;
                }
                */
            }

            //Set null icon to draw
            if (slotItem.ItemIcon == null) { SlotItemImage.sprite = ItemWithoutIconSprite; return; }

            //Set respective item icon to draw
            SlotItemImage.sprite = slotItem.ItemIcon;
        }

        public void Equip()
        {
            HideOptions();
            inventory.EquipItem(ItemIDToDraw);
            Debug.Log("Equiped " + slotItem.name);
        }
        public void Unequip()
        {
            HideOptions();
            inventory.UnequipItem(ItemIDToDraw);
            RefreshSlot();
            if (slotItem != null) Debug.Log("Unequiped " + slotItem.name);
        }
        public void Use()
        {
            HideOptions();
            slotItem.UseItem();
            Debug.Log("Used " + slotItem.name);
        }
        public void Drop()
        {
            HideOptions();

            //Debug.Log("Droped " + slotItem.name);
            inventory.DropItem(ItemIDToDraw);

            ItemIDToDraw = -1;
            RefreshSlot();
        }


        public void DoHealthBarFillAmout(JUItem item, Image healthBarImage, bool ChangeColor = true, Color FullHPColor = default(Color), Color NoHPColor = default(Color))
        {
            if (item == null) return;

            float health = 0;
            float maxHealth = 1;

            // >>> Get fill amount value
            if (item is JUItem)
            {
                health = item.ItemQuantity;
                maxHealth = item.MaxItemQuantity;
            }

            if (item is Armor)
            {
                health = (item as Armor).Health;
                maxHealth = (item as Armor).MaxHealth;
            }

            if (item is MeleeWeapon)
            {
                health = (item as MeleeWeapon).MeleeWeaponHealth;
                maxHealth = 100;
            }

            if (item is Weapon)
            {
                health = (item as Weapon).BulletsAmounts;
                maxHealth = (item as Weapon).BulletsPerMagazine;
            }

            //Set health bar value
            healthBarImage.fillAmount = Mathf.Lerp(0, 1, health / maxHealth);

            // >>> Change Color
            if (!ChangeColor) return;

            //Set colors
            if (FullHPColor == Color.clear) FullHPColor = Color.green;
            if (NoHPColor == Color.clear) NoHPColor = Color.red;

            //Set health bar color
            healthBarImage.color = Color.Lerp(NoHPColor, FullHPColor, health / maxHealth);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            EnableOverriding();
            Outline.SetActive(false);
            HideOptions();
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            Outline.SetActive(true);
            ShowOptions();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            Outline.SetActive(false);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            Outline.SetActive(false);
            DisableOverriding();
            HideOptions();
        }




        public void OnDrag(PointerEventData eventData)
        {
            if (ItemIDToDraw < 0) return;
            if (slotItem == null) return;
            if (slotItem.Unlocked == false) return;

            EnableOverriding();
            SlotItemImage.rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (slotItem == null)
            {
                SlotItemImage.rectTransform.localScale = new Vector3(0.95f, 0.95f, 0.95f);
                Outline.SetActive(true);
                return;
            }

            if (slotItem.Unlocked)
            {
                SlotItemImage.rectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                SlotItemImage.CrossFadeAlpha(0.7f, 0.3f, true);
                Outline.SetActive(true);
                EnableOverriding();
                IsDragging = true;
            }
            else
            {
                SlotItemImage.rectTransform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
                SlotItemImage.CrossFadeAlpha(0.2f, 0.3f, true);
            }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            SlotItemImage.rectTransform.anchoredPosition = Vector2.zero;
            SlotItemImage.rectTransform.localScale = new Vector3(1, 1, 1);
            SlotItemImage.CrossFadeAlpha(1, 0.2f, true);
            IsDragging = false;
            Outline.SetActive(false);

            DisableOverriding();
            HideOptions();
        }

        //Get extern items drops
        public void OnDrop(PointerEventData eventData)
        {
            HideOptions();

            InventorySlotUI DropedSlotData = eventData.pointerDrag.GetComponentInParent<InventorySlotUI>();
            if (DropedSlotData != null)
            {
                if (DropedSlotData.ItemIDToDraw <= -1 || DropedSlotData.slotItem == null) return;
                if (DropedSlotData.slotItem.Unlocked == false) return;

                //Get loot items
                if (DropedSlotData.inventory != this.inventory)
                {
                    if (SetSequentialOnDrop) inventory.SetSequentialSlotItem(SequentialToDraw, DropedSlotData.CurrentSlotItem());

                    MoveItemDrawPropertyToThisSlot(DropedSlotData.slotItem.name);
                    PickAndUnlockLootItem(DropedSlotData.slotItem.name, DropedSlotData.inventory);

                    inventoryManager.RefreshAllSlots();

                    if (AutoEquipOnDrop && !EnableOptions)
                    {
                        this.Equip();
                    }
                    //else if (EnableOptions)
                    //{
                    //    Unequip();
                    //}
                }
                else
                {
                    //Locally transfer items (change item slot)
                    if (IsItemAllowedOnThisSlot(DropedSlotData.CurrentSlotItem()))
                    {
                        if (SetSequentialOnDrop) inventory.SetSequentialSlotItem(SequentialToDraw, DropedSlotData.CurrentSlotItem());

                        TransferSlotData(DropedSlotData, this);
                        inventoryManager.RefreshAllSlots();

                        if (AutoEquipOnDrop && !EnableOptions)
                        {
                            this.Equip();
                        }
                        //else if (EnableOptions)
                        //{
                        //    Unequip();
                        //}

                        return;
                    }
                }
            }

            Debug.Log("On Droped");
        }
        private void PickAndUnlockLootItem(string itemName, JUInventory lootInventory)
        {
            if (itemName == "") return;
            if (this.inventory == lootInventory) return;
            if (lootInventory.IsALoot == false) return;

            //Get items
            JUItem itemOnThisInventory = null;
            JUItem itemOnLoot = null;

            foreach (JUItem item in inventory.AllItems) { if (item.name == itemName) { itemOnThisInventory = item; } }
            foreach (JUItem item in lootInventory.AllItems) { if (item.name == itemName) { itemOnLoot = item; } }

            if (itemOnThisInventory == null)
            {
                Debug.Log("Não foi possível encontrar o item: " + itemName);
                return;
            }

            inventory.GetLootItem(itemOnThisInventory, itemOnLoot);

            // UnlockLootItemOnTargetInventory(itemOnThisInventory, itemOnLoot);
        }
        private void MoveItemDrawPropertyToThisSlot(string ItemName)
        {
            foreach (InventorySlotUI slot in inventoryManager.Slots)
            {
                if (slot.slotItem != null)
                {
                    if (slot.slotItem.name == ItemName)
                    {
                        //Get data from especified item
                        TransferSlotData(slot, this);
                        RefreshSlot();
                        slot.RefreshSlot();
                    }
                }
            }
        }

        private void EnableOverriding()
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
        }
        private void DisableOverriding()
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
        public bool IsItemAllowedOnThisSlot(JUItem itemSlot)
        {
            bool allowed = false;

            foreach (string filterTag in AllowItemsWithTags)
            {
                if (itemSlot != null)
                {
                    if (itemSlot.ItemFilterTag == filterTag)
                    {
                        allowed = true;
                    }
                }
            }
            if (AllowAnyItem) allowed = true;
            return allowed;
        }
        public static void TransferSlotData(InventorySlotUI SlotA, InventorySlotUI SlotB, List<InventorySlotUI> slotList = null)
        {
            if (SlotA.RespectiveItemList != SlotB.RespectiveItemList || SlotA.PlacedIn != SlotB.PlacedIn)
            {
                SlotB.PlacedIn = SlotA.PlacedIn;
                SlotB.RespectiveItemList = SlotA.RespectiveItemList;
            }
            //Unequip Old Slot Item
            if (SlotA.AutoEquipOnDrop)
            {
                SlotA.Unequip();
            }

            //Update Image IDs
            int DropedSlotID = SlotA.ItemIDToDraw;
            SlotA.ItemIDToDraw = SlotB.ItemIDToDraw;
            SlotB.ItemIDToDraw = DropedSlotID;

            //Clear Old Slot
            if (SlotA.ItemIDToDraw == SlotB.ItemIDToDraw)
            {
                ClearSlot(SlotA);
            }

            //Update Sequential
            if (SlotA.DrawSequentialItem == true)
            {
                SlotA.inventory.SetSequentialSlotItem(SlotA.SequentialToDraw, null);
            }

            //Refresh
            SlotB.RefreshSlot();
            SlotA.RefreshSlot();

            //Move Slot Data in Slot List
            if (slotList != null)
            {
                for (int i = 0; i < slotList.Count - 1; i++)
                {
                    if (slotList[i].ItemIDToDraw == SlotB.ItemIDToDraw && slotList[i] != SlotB)
                    {
                        ClearSlot(slotList[i]);
                        Debug.Log("Cleaned a duplicated Slot");
                    }
                }
            }

            Debug.Log("Tranferred data from " + SlotA.gameObject.name + " to " + SlotB.gameObject.name);
        }
        public static void ClearSlot(InventorySlotUI slotToClear)
        {
            slotToClear.ItemIDToDraw = -1;
            slotToClear.IsEmpty = true;
        }
        public static void MoveToACloserSlot(InventorySlotUI SlotToMoveData, List<InventorySlotUI> slotList)
        {
            if (SlotToMoveData.CurrentSlotItem() == null) return;

            //Debug.Log(SlotToMoveData.mSlotIndex);
            //seaching
            for (int i = SlotToMoveData.mSlotIndex; i < slotList.Count - 1; i++)
            {
                if (slotList[i].CurrentSlotItem() == null && slotList[i].IsItemAllowedOnThisSlot(SlotToMoveData.slotItem) == true)
                {
                    TransferSlotData(SlotToMoveData, slotList[i]);
                    Debug.Log("an item cannot stay in the slot, so it has been moved to a next slot");
                    return;
                }
            }
            //seaching
            for (int i = SlotToMoveData.mSlotIndex; i > 0; i--)
            {
                if (slotList[i].CurrentSlotItem() == null && slotList[i].IsItemAllowedOnThisSlot(SlotToMoveData.slotItem) == true)
                {
                    TransferSlotData(SlotToMoveData, slotList[i]);
                    Debug.Log("an item cannot stay in the slot, so it has been moved to a previous slot");
                    return;
                }
            }
            //Failed
            SlotToMoveData.Drop();
            Debug.Log("an item can't stay in the slot, but it couldn't be moved to another one, so it was dropped");

        }
    }

}