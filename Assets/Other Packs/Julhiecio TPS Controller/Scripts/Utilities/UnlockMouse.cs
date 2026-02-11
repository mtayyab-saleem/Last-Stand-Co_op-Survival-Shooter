using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/Mouse Unlocker")]
    public class UnlockMouse : MonoBehaviour
    {
        public bool UnlockCursor;
        public bool UnhideCursor;
        public bool UnlockOnAwake = true;
        public bool LockOnAwake = false;
        void Awake()
        {
            if (UnlockOnAwake) Unlock(UnlockCursor, UnhideCursor);
            if (LockOnAwake) Lock (!UnlockCursor, !UnhideCursor);
        }

        public static void Unlock(bool UnlockCursor, bool UnhideCursor)
        {
            if (UnlockCursor) Cursor.lockState = CursorLockMode.None;

            if (UnhideCursor) Cursor.visible = true;
        }
        public static void Lock(bool LockCursor, bool HideCursor)
        {
            if (LockCursor) Cursor.lockState = CursorLockMode.Locked;

            if (HideCursor) Cursor.visible = false;
        }
    }
}
