using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.Layouts;

using JUTPS.JUInputSystem;
using JUTPS.ItemSystem;
using JUTPS.CameraSystems;
using JUTPSEditor.JUHeader;


namespace JUTPS.InventorySystem.UI
{

    public class InventoryUIManager : MonoBehaviour
    {
        [JUHeader("Inventory Settings")]
        public GameObject InventoryScreen;
        public JUInventory TargetInventory;
        public InventorySlotUI SlotPrefab;

        public bool HideCursorWhenExitInventory, LockCursorWhenExitInventory;
        public bool ShowCursorWhenOpenInventory = true;

        [JUHeader("Slots Settings")]
        public bool FilterLeftHandItems = true;
        public int SlotsQuantity = -1;
        public GridLayoutGroup InventoryScrollViewContent;
        public List<InventorySlotUI> Slots = new List<InventorySlotUI>();
        private RectTransform inventoryScrollViewRectTransform;

        [JUHeader("Loot View Settings")]
        public bool IsLootView = false;
        public Transform Player;
        public string PlayerTag = "Player";
        public LayerMask CharacterLayer;
        public float CheckLootRadius = 1f;
        private JUInventory LootToGetItems;

        void Awake()
        {
            if (InventoryScrollViewContent != null) inventoryScrollViewRectTransform = InventoryScrollViewContent.GetComponent<RectTransform>();

            if (IsLootView)
            {
                //Get player 
                if (Player == null && GameObject.FindGameObjectWithTag(PlayerTag) != null) { Player = GameObject.FindGameObjectWithTag(PlayerTag).transform; }
                return;
            }

            if (TargetInventory == null)
            {
                JUInventory inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<JUInventory>();
                TargetInventory = inventory;
            }
            if (TargetInventory == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (Slots.Count == 0)
            {
                CreateInventorySlots(ref Slots, SlotsQuantity, TargetInventory, SlotPrefab, InventoryScrollViewContent);
                SetSlots(ref Slots, TargetInventory);
            }
            else
            {
                SetSlots(ref Slots, TargetInventory);
            }


            InvokeRepeating("RefreshInventory", 1, 1);
            if (Slots.Count > 0) { RenameAllSlotWithIndex(Slots); }
        }
        private void Update()
        {
            if (InventoryScreen == null || InventoryScrollViewContent == null) return;

            inventoryScrollViewRectTransform.sizeDelta = new Vector3(inventoryScrollViewRectTransform.sizeDelta.x, Slots.Count * InventoryScrollViewContent.cellSize.y);
            if (IsLootView == true)
            {
                if (Player == null) return;

                //Check nearby inventories 
                Collider[] characters = Physics.OverlapBox(Player.position, new Vector3(CheckLootRadius, CheckLootRadius, CheckLootRadius), Quaternion.identity, CharacterLayer);

                //Debug.Log(characters.Length + " On Loot Sensor");
                if (characters.Length > 1)
                {
                    foreach (Collider col in characters)
                    {
                        if (col.gameObject != Player.gameObject && col.gameObject != null && LootToGetItems == null)
                        {
                            if (col.TryGetComponent(out JUInventory LootInventory))
                            {
                                if (LootInventory.IsALoot && LootInventory != LootToGetItems)
                                {
                                    LootToGetItems = LootInventory;
                                    TargetInventory = LootInventory;

                                    OpenInventory();
                                    CreateInventorySlots(ref Slots, LootInventory.AllItems.Length, LootInventory, SlotPrefab, InventoryScrollViewContent);
                                    SetActiveSlotsOptions(false);
                                }
                            }
                        }
                        if (col.gameObject == null)
                        {
                            ClearAllSlots();
                            LootToGetItems = null;
                            TargetInventory = null;
                            ExitInventory();
                        }
                    }
                }
                else
                {
                    if (InventoryScreen.activeInHierarchy)
                    {
                        ClearAllSlots();
                        LootToGetItems = null;
                        TargetInventory = null;
                        ExitInventory();
                    }
                }

                if (InventoryScreen.activeInHierarchy && LootToGetItems == null || TargetInventory == null) { ExitInventory(); }
                return;
            }

            if (JUInputSystem.JUInput.GetButtonDown(JUInputSystem.JUInput.Buttons.OpenInventory))
            {
                if (!InventoryScreen.activeInHierarchy) { OpenInventory(); } else { ExitInventory(); }
            }
        }
        public void OpenInventory()
        {
            if (InventoryScreen == null) return;

            InventoryScreen.SetActive(true);
            if (IsLootView) return;
            if (ShowCursorWhenOpenInventory)
            {
                JUCameraController.LockMouse(false, false);
            }

        }
        public void ExitInventory()
        {
            if (InventoryScreen == null) return;

            InventoryScreen.SetActive(false);
            if (IsLootView) return;
            JUCameraController.LockMouse(LockCursorWhenExitInventory, HideCursorWhenExitInventory);
        }

        public static void CreateInventorySlots(ref List<InventorySlotUI> SlotsList, int SlotQuantity, JUInventory inventory, InventorySlotUI slotPrefab, GridLayoutGroup scrollViewContentGridLayout)
        {
            if (SlotQuantity <= 0)
            {
                for (int i = 0; i < inventory.AllItems.Length; i++)
                {
                    var slot = InstantiateSlot(slotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, i, scrollViewContentGridLayout.transform);
                    SlotsList.Add(slot);
                }
            }
            else
            {
                for (int i = 0; i < SlotQuantity; i++)
                {
                    var slot = InstantiateSlot(slotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, i, scrollViewContentGridLayout.transform);
                    SlotsList.Add(slot);
                }
            }
            RenameAllSlotWithIndex(SlotsList);
        }

        private static InventorySlotUI InstantiateSlot(InventorySlotUI SlotPrefab, InventorySlotUI.ItemArePlacedIn PlacedIn, int IDToDraw, Transform parent)
        {
            InventorySlotUI slot = (InventorySlotUI)Instantiate(SlotPrefab, parent);
            slot.PlacedIn = PlacedIn;
            slot.ItemIDToDraw = IDToDraw;
            return slot;
        }
        private static void RenameAllSlotWithIndex(List<InventorySlotUI> SlotsList)
        {
            int i = 0;
            foreach (InventorySlotUI slot in SlotsList)
            {
                slot.gameObject.name = "Slot " + i;
                i++;
            }
        }
        public static void CreateInventorySlots(int SlotQuantity, InventorySlotUI slotPrefab, GridLayoutGroup scrollViewContentGridLayout)
        {
            if (SlotQuantity <= 0) return;

            for (int i = 0; i < SlotQuantity; i++)
            {
                InventorySlotUI slot = (InventorySlotUI)Instantiate(slotPrefab, scrollViewContentGridLayout.transform);
                slot.ItemIDToDraw = -1;
            }
        }
        public static void SetSlots(ref List<InventorySlotUI> SlotsList, JUInventory inventory)
        {
            for (int i = 0; i < inventory.AllItems.Length; i++)
            {
                SlotsList[i].ItemIDToDraw = i;
                SlotsList[i].RefreshSlot();
            }
        }
        public void SetActiveSlotsOptions(bool enabled)
        {
            foreach (InventorySlotUI slot in Slots)
            {
                slot.HideOptions();
                slot.EnableOptions = enabled;
            }
        }
        public void RefreshAllSlots()
        {
            foreach (InventorySlotUI currentSlot in Slots)
            {
                currentSlot.RefreshSlot();
                //Delete duplicated slots
                foreach (InventorySlotUI slotToVerify in Slots)
                {
                    if (currentSlot != slotToVerify && currentSlot.ItemIDToDraw == slotToVerify.ItemIDToDraw)
                    {
                        slotToVerify.ItemIDToDraw = -2;
                        slotToVerify.RefreshSlot();
                    }
                }
            }

            List<JUItem> NonDrawedItems = GetNonDrawedItems(TargetInventory.AllItems, Slots, FilterLeftHandItems);
            SetupNonDrawedItemsInSlots(NonDrawedItems, inventory: this);
        }
        public static void SetupNonDrawedItemsInSlots(List<JUItem> nonDrawedItems, InventoryUIManager inventory)
        {
            if (nonDrawedItems.Count == 0 || inventory == null || inventory.Slots.Count == 0) return;

            foreach (JUItem item in nonDrawedItems)
            {
                //GET EMPTY SLOT
                InventorySlotUI emptySlot = GetFirstEmptySlot(inventory.Slots);
                if (emptySlot == null) return;
                //EMPTY IS NO LONGER EMPTY
                emptySlot.ItemIDToDraw = JUInventory.GetGlobalItemSwitchID(item, inventory.TargetInventory);
                emptySlot.RefreshSlot();
                emptySlot.IsEmpty = false;
            }

        }
        public static List<JUItem> GetNonDrawedItems(JUItem[] items, List<InventorySlotUI> slots, bool filterLeftHandItems)
        {
            List<JUItem> NonDrawed = items.ToList();

            foreach (JUItem item in items)
            {
                if (item is JUHoldableItem && filterLeftHandItems)
                {
                    if ((item as JUHoldableItem).IsLeftHandItem)
                    {
                        NonDrawed.Remove(item);
                    }
                    else
                    {
                        if (IsItemDrawingInSomeSlots(item, slots, filterLeftHandItems))
                        {
                            NonDrawed.Remove(item);
                        }
                    }
                }
                else
                {
                    if (IsItemDrawingInSomeSlots(item, slots, filterLeftHandItems))
                    {
                        NonDrawed.Remove(item);
                    }
                }

                //foreach(InventorySlotUI slot in slots)
                //{
                //    if (slot.CurrentSlotItem() == item) NonDrawed.Remove(item);
                //}
            }

            return NonDrawed;
        }
        public static bool IsItemDrawingInSomeSlots(JUItem item, List<InventorySlotUI> slots, bool filterLeftHandItems)
        {
            bool isdrawing = false;

            foreach (InventorySlotUI slot in slots.ToArray())
            {
                if (filterLeftHandItems)
                {
                    if (item is JUHoldableItem)
                    {
                        if ((item as JUHoldableItem).IsLeftHandItem == true)
                        {
                            return false;
                        }
                        else
                        {
                            if (item == slot.CurrentSlotItem()) return true;
                        }
                    }
                    else
                    {
                        if (item == slot.CurrentSlotItem()) return true;

                    }
                }
                else
                {
                    if (item == slot.CurrentSlotItem()) return true;
                }
            }

            return isdrawing;
        }
        public static InventorySlotUI GetFirstEmptySlot(List<InventorySlotUI> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].ItemIDToDraw < 0) return slots[i];
            }
            Debug.LogWarning("Cannot find an empty slot in the list");
            return null;
        }
        public List<InventorySlotUI> GetSlots()
        {
            List<InventorySlotUI> slots = new List<InventorySlotUI>();
            slots = gameObject.GetComponentsInChildren<InventorySlotUI>().ToList();
            return slots;
        }
        public void ClearAllSlots()
        {
            foreach (InventorySlotUI slot in Slots)
            {
                Destroy(slot.gameObject);
            }
            Slots.Clear();
        }
        public static void EmptyAllSlots(List<InventorySlotUI> SlotList)
        {
            foreach (InventorySlotUI slot in SlotList)
            {
                slot.ItemIDToDraw = -2;
                slot.RefreshSlot();
            }
        }
        /*
        public void FilterSlots(ref List<InventorySlotUI> slotList, SlotGenerationMode ShowOnly = SlotGenerationMode.RightHandItemsAndNonHoldableItems)
        {
            switch (ShowOnly)
            {
                // >>> Do nothing
                case SlotGenerationMode.AllItems:

                    break;
                // >>> Remove Holdable Left Hand Items and normal items
                case SlotGenerationMode.RightHandItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot); 
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                        else
                        {
                            Destroy(slot.gameObject); slotList.Remove(slot); 
                            var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                        }
                    }
                    break;

                // >>> Remove Holdable Left Hand Items Only
                case SlotGenerationMode.RightHandItemsAndNonHoldableItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot);
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                    }
                    break;

                // >>> Remove Holdable Right Hand Items and normal items
                case SlotGenerationMode.LeftHandItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem == false)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot);
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                        else
                        {
                            Destroy(slot.gameObject); slotList.Remove(slot);
                            var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                        }
                    }
                    break;

                // >>> Remove Holdable Right Hand Items Only
                case SlotGenerationMode.LeftHandItemsAndNonHoldableItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem == false)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot);
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                    }
                    break;
            }

        }
        */
        public void FilterSlots(List<InventorySlotUI> slotList)
        {
            // >>> Remove Holdable Left Hand Items Only
            foreach (InventorySlotUI slot in slotList.ToList())
            {
                if (slot.CurrentSlotItem() is JUHoldableItem)
                {
                    if ((slot.CurrentSlotItem() as JUHoldableItem).IsLeftHandItem)
                    {
                        slot.ItemIDToDraw = -2;
                        slot.RefreshSlot();
                    }

                    //if (TargetInventory.HoldableItensLeftHand.ToList().Contains(slot.CurrentSlotItem()))
                    //{
                    //    slot.ItemIDToDraw = -2;
                    //    slot.RefreshSlot();
                    //}
                }
            }
        }

        public static void Move<T>(List<T> list, int oldIndex, int newIndex)
        {
            T item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }

        public void RefreshInventory()
        {
            //if (InventoryScreen.activeInHierarchy == false) return;
            RefreshAllSlots();
            if (FilterLeftHandItems)
            {
                FilterSlots(Slots);
            }
        }
    }

}