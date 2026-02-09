using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JUTPS.InventorySystem.UI
{

    public class DropArea : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            InventorySlotUI DropedSlotData = eventData.pointerDrag.GetComponentInParent<InventorySlotUI>();
            if (DropedSlotData != null)
            {
                if (DropedSlotData.ItemIDToDraw <= -1) return;

                DropedSlotData.Drop();
                DropedSlotData.RefreshSlot();
            }
        }
    }

}
