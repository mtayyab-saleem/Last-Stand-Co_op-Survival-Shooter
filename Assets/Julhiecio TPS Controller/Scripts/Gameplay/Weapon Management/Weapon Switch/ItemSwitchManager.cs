using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using JUTPS.JUInputSystem;
using JUTPS.InventorySystem;

using JUTPSEditor.JUHeader;

namespace JUTPS.ItemSystem
{

    [AddComponentMenu("JU TPS/Item System/Item Switch Manager")]
    public class ItemSwitchManager : MonoBehaviour
    {
        [JUHeader("Settings")]
        public bool IsPlayer;
        public bool UseOldInputSystem;
        [SerializeField] private JUCharacterController JuTPSCharacter;
        public int ItemToEquipOnStart = -1;


        [JUHeader("Next-Previous Item Switch [Q-E]")]
        public bool EnableNextAndPreviousWeaponSwitch;
        [Tooltip("[OLD INPUT SYSTEM ONLY]")]
        public KeyCode CustomNextWeaponKeyCode, CustomPreviousWeaponKeycode;

        [JUHeader("Alpha Numeric Item Switch")]
        public bool EnableAlphaNumericWeaponSwitch;

        [JUHeader("Mouse Scroll Item Switch")]
        public bool EnableMouseScrollWeaponSwitch;
        public float ScrollThreshold = 0.1f;



        protected virtual void Start()
        {

            if (JuTPSCharacter == null)
            {
                JuTPSCharacter = GetComponent<JUCharacterController>();
                if (JuTPSCharacter != null)
                {
                    Invoke("EquipStartItem", 0.2f);
                }
            }
            else
            {
                Invoke(nameof(EquipStartItem), 0.2f);
            }
            IsPlayer = gameObject.tag == "Player";
        }
        protected virtual void Update()
        {
            if (IsPlayer == false)
                return;

            if (JuTPSCharacter.IsMeleeAttacking || JuTPSCharacter.IsRagdolled || JuTPSCharacter.IsDead || JuTPSCharacter.IsRolling) return;

            OldInput_ItemSwitchController();
            NewInput_ItemSwitchController();
        }

        private void EquipStartItem()
        {
            JuTPSCharacter.SwitchToItem(ItemToEquipOnStart);
        }
        protected virtual void OldInput_ItemSwitchController()
        {
            if (UseOldInputSystem == false) return;

            if (EnableNextAndPreviousWeaponSwitch)
            {
                if (CustomNextWeaponKeyCode != KeyCode.None)
                {
                    if (Input.GetKeyDown(CustomNextWeaponKeyCode))
                    {
                        JuTPSCharacter.SwitchToNextItem();
                    }
                }
                else
                {
                    if (JUInput.GetButtonDown(JUInput.Buttons.NextWeaponButton))
                    {
                        Debug.Log("Switch manager tentou trocar para o proximo item");
                        JuTPSCharacter.SwitchToNextItem();
                    }
                }
                if (CustomPreviousWeaponKeycode != KeyCode.None)
                {
                    if (Input.GetKeyDown(CustomPreviousWeaponKeycode))
                    {
                        JuTPSCharacter.SwitchToPreviousItem();
                    }
                }
                else
                {
                    if (JUInput.GetButtonDown(JUInput.Buttons.PreviousWeaponButton))
                    {
                        JuTPSCharacter.SwitchToPreviousItem();
                    }
                }
            }

            if (EnableMouseScrollWeaponSwitch)
            {
                if (Input.GetAxis("Mouse ScrollWheel") >= ScrollThreshold)
                {
                    JuTPSCharacter.SwitchToNextItem();
                }
                if (Input.GetAxis("Mouse ScrollWheel") <= -ScrollThreshold)
                {
                    JuTPSCharacter.SwitchToPreviousItem();
                }
            }

            if (EnableAlphaNumericWeaponSwitch)
            {
                for (int i = 48; i < 58; i++)
                {
                    int InputKey = i;
                    int SwitchID = i - 49;
                    if (Input.GetKeyDown((KeyCode)InputKey) && SwitchID < JuTPSCharacter.Inventory.HoldableItensRightHand.Length)
                    {
                        JuTPSCharacter.SwitchToItem(SwitchID);
                    }
                }
            }
        }
        protected virtual void NewInput_ItemSwitchController()
        {
            if (UseOldInputSystem == true) return;
            if (JUInput.Instance() == null) return;
            if (JUInput.Instance().InputActions == null) return;

            if (EnableNextAndPreviousWeaponSwitch)
            {
                if (JUInput.GetButtonDown(JUInput.Buttons.NextWeaponButton))
                {
                    JuTPSCharacter.SwitchToNextItem(true);
                }

                if (JUInput.GetButtonDown(JUInput.Buttons.PreviousWeaponButton))
                {
                    JuTPSCharacter.SwitchToPreviousItem(true);
                }
            }

            if (EnableMouseScrollWeaponSwitch && Mouse.current != null)
            {
                if (Mouse.current.scroll.ReadValue().y / 360 >= ScrollThreshold)
                {
                    JuTPSCharacter.SwitchToNextItem(true);
                }
                if (Mouse.current.scroll.ReadValue().y / 360 <= -ScrollThreshold)
                {
                    JuTPSCharacter.SwitchToPreviousItem(true);
                }
            }

            if (EnableAlphaNumericWeaponSwitch)
            {
                if (JUInput.Instance().InputActions.Player.Slot1.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.first);
                if (JUInput.Instance().InputActions.Player.Slot2.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.second);
                if (JUInput.Instance().InputActions.Player.Slot3.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.third);
                if (JUInput.Instance().InputActions.Player.Slot4.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.fourth);
                if (JUInput.Instance().InputActions.Player.Slot5.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.fifth);
                if (JUInput.Instance().InputActions.Player.Slot6.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.sixth);
                if (JUInput.Instance().InputActions.Player.Slot7.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.seventh);
                if (JUInput.Instance().InputActions.Player.Slot8.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.eighth);
                if (JUInput.Instance().InputActions.Player.Slot9.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.ninth);
                if (JUInput.Instance().InputActions.Player.Slot10.triggered) SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum.tenth);
            }
        }


        /// <summary>
        /// Changes character selected item to the next item in the list
        /// </summary>
        public virtual void NextItem()
        {
            JuTPSCharacter.SwitchToNextItem();
        }

        /// <summary>
        /// Changes character selected item to the previous item in the list
        /// </summary>
        public virtual void PreviousItem()
        {
            JuTPSCharacter.SwitchToPreviousItem();
        }

        /// <summary>
        /// Changes character selected item to a specific one in the list
        /// </summary>
        /// <param name="SwitchID">Item index</param>
        public virtual void SwitchToItem(int SwitchID)
        {
            if (SwitchID < JuTPSCharacter.Inventory.HoldableItensRightHand.Length)
            {
                if (JuTPSCharacter.IsItemEquiped == false)
                {
                    JuTPSCharacter.SwitchToItem(SwitchID);
                }
                else
                {
                    if (JuTPSCharacter.HoldableItemInUseRightHand.ItemSwitchID != SwitchID) JuTPSCharacter.SwitchToItem(SwitchID);
                }
            }
            else
            {
                Debug.LogWarning("Unable to switch to this item, this ID is out of bounds for the list");
            }
        }


        public virtual void SwitchToItemInSequentialSlot(JUInventory.SequentialSlotsEnum Slot)
        {
            JUTPS.ItemSystem.JUItem ItemToSwich = JuTPSCharacter.Inventory.GetSequentialSlotItem(Slot);
            int GlobalItemID = (ItemToSwich == null) ? -1 : JUInventory.GetGlobalItemSwitchID(ItemToSwich, JuTPSCharacter.Inventory);

            if (ItemToSwich == null)
            {
                SwitchToItem(-1);
                return;
            }

            //SwitchToItem(GlobalItemID);
            SwitchToItem(ItemToSwich.ItemSwitchID);
        }


        /// <summary>
        /// Changes character selected item to a specific one in the list
        /// </summary>
        /// <param name="SwitchID">Item index</param>
        public static void SwitchCharacterItem(JUCharacterController character, int SwitchID)
        {
            if (SwitchID < character.Inventory.HoldableItensRightHand.Length)
            {
                character.SwitchToItem(SwitchID);
            }
            else
            {
                Debug.LogWarning("Unable to switch to item with ID " + SwitchID + " , this ID is out of bounds for the list");
            }
        }


        /// <summary>
        /// Changes player selected item to a specific one in the list
        /// </summary>
        /// <param name="SwitchID">Item index</param>
        public static void SwitchPlayerItem(int SwitchID)
        {
            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                Debug.LogError("Could not find a gameobject tagged 'Player'");
                return;
            }

            JUCharacterController player = GameObject.FindGameObjectWithTag("Player").GetComponent<JUCharacterController>();
            if (SwitchID < player.Inventory.HoldableItensRightHand.Length)
            {
                player.SwitchToItem(SwitchID);
            }
            else
            {
                Debug.LogWarning("Unable to switch to item with ID " + SwitchID + " , this ID is out of bounds for the list");
            }
        }

        /// <summary>
        /// Changes player selected item to the next item in the list
        /// </summary>
        public static void NextPlayerItem()
        {
            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                Debug.LogError("Could not find a gameobject tagged 'Player'");
                return;
            }

            JUCharacterController player = GameObject.FindGameObjectWithTag("Player").GetComponent<JUCharacterController>();
            player.SwitchToNextItem();
        }

        /// <summary>
        /// Changes player selected item to the previous item in the list
        /// </summary>
        public static void PreviousPlayerItem()
        {
            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                Debug.LogError("Could not find a gameobject tagged 'Player'");
                return;
            }

            JUCharacterController player = GameObject.FindGameObjectWithTag("Player").GetComponent<JUCharacterController>();
            player.SwitchToPreviousItem();
        }
    }


}