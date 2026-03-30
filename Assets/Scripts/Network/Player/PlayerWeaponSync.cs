// ============================================================
// PlayerWeaponSync.cs
// PURPOSE: Sync WEAPON SWITCHING across all players.
//          When local player switches weapon → all clients see it.
//
// REPLACES: Weapon sync part of JU_NetworkCompleteSync.cs
// SINGLE RESPONSIBILITY: Weapon switching only.
//
// WHY REFLECTION AGAIN?
//   JU TPS's ItemSwitchManager stores its weapon list privately.
//   We use Reflection to access it, same reason as PlayerMovementSync.
// ============================================================

using JUTPS;
using JUTPS.ItemSystem;
using JUTPS.JUInputSystem;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PlayerWeaponSync : NetworkBehaviour
{
    // ── Component References ─────────────────────────────────
    private JUCharacterController _juController;

    // ItemSwitchManager stored as generic MonoBehaviour
    // because we're using Reflection to access it
    private MonoBehaviour _itemSwitchManager;

    // ── Reflection Field ─────────────────────────────────────
    // This lets us access the private "Items" list inside ItemSwitchManager
    private FieldInfo _itemsListField;

    // ── SyncVar: Current Weapon ──────────────────────────────
    // When this value changes on server, OnWeaponIDChanged() is called
    // on all clients (that's what the hook = does)
    [SyncVar(hook = nameof(OnWeaponIDChanged))]
    private int _syncWeaponID = -1;

    // ════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════

    private void Awake()
    {
        _juController = GetComponent<JUCharacterController>();
        _itemSwitchManager = GetComponent<ItemSwitchManager>();

        SetupReflection();
    }

    private void Update()
    {
        // Only local player needs to detect weapon switches
        if (!isLocalPlayer) return;

        // Check if player pressed next/previous weapon button
        bool weaponSwitchPressed =
            JUInput.GetButtonDown(JUInput.Buttons.NextWeaponButton) ||
            JUInput.GetButtonDown(JUInput.Buttons.PreviousWeaponButton);

        if (weaponSwitchPressed)
        {
            // Small delay to let JU TPS finish switching internally first
            // Then we read the new weapon ID and sync it
            StartCoroutine(SyncWeaponAfterSwitch());
        }
    }

    // ════════════════════════════════════════════════════════
    // REFLECTION SETUP
    // ════════════════════════════════════════════════════════

    private void SetupReflection()
    {
        if (_itemSwitchManager == null) return;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var managerType = _itemSwitchManager.GetType();

        // Try "Items" first — if not found, try "HoldableItems" (older JU TPS versions)
        _itemsListField = managerType.GetField("Items", flags)
                       ?? managerType.GetField("HoldableItems", flags);

        if (_itemsListField == null)
            Debug.Log("[PlayerWeaponSync] Could not find 'Items' field in ItemSwitchManager. " +
                           "Weapon sync will not work. Check JU TPS version.");
    }

    // ════════════════════════════════════════════════════════
    // LOCAL PLAYER — Detect and Send Weapon Change
    // ════════════════════════════════════════════════════════

    private IEnumerator SyncWeaponAfterSwitch()
    {
        // Wait one frame for JU TPS to finish switching internally
        yield return new WaitForSeconds(0.1f);

        // Read the current weapon ID from JU TPS inventory
        if (_juController.Inventory != null)
        {
            int currentWeaponID = _juController.Inventory.CurrentRightHandItemID;
            CmdChangeWeapon(currentWeaponID);
        }
    }

    // ════════════════════════════════════════════════════════
    // COMMANDS (Client → Server)
    // ════════════════════════════════════════════════════════

    [Command]
    private void CmdChangeWeapon(int newWeaponID)
    {
        // Server updates the SyncVar — triggers OnWeaponIDChanged on all clients
        _syncWeaponID = newWeaponID;
    }

    // ════════════════════════════════════════════════════════
    // SYNCVAR HOOK (Runs on ALL clients when weapon changes)
    // ════════════════════════════════════════════════════════

    private void OnWeaponIDChanged(int oldID, int newID)
    {
        // Local player's weapon is already showing correctly from their own input
        if (isLocalPlayer) return;

        // For remote players: manually show the correct weapon
        // Retry logic handles cases where the player isn't fully loaded yet
        StartCoroutine(ApplyWeaponSwitchWithRetry(newID));
    }

    private IEnumerator ApplyWeaponSwitchWithRetry(int weaponID)
    {
        // Wait a bit to ensure the player is fully initialized
        yield return new WaitForSeconds(0.2f);

        bool success = TryApplyWeaponSwitch(weaponID);

        if (!success)
        {
            // Wait longer and try one more time
            yield return new WaitForSeconds(0.3f);
            TryApplyWeaponSwitch(weaponID);
        }
    }

    /// <summary>
    /// Manually enables the correct weapon GameObject and disables the rest.
    /// Returns true if successful, false if something went wrong.
    /// </summary>
    private bool TryApplyWeaponSwitch(int targetID)
    {
        if (_itemsListField == null || _itemSwitchManager == null)
            return false;

        try
        {
            // Get the weapon list using Reflection
            var itemsList = _itemsListField.GetValue(_itemSwitchManager) as IList;

            if (itemsList == null || itemsList.Count == 0)
                return false;

            // Loop through weapons: enable the target one, disable the rest
            for (int i = 0; i < itemsList.Count; i++)
            {
                var item = itemsList[i];
                if (item == null) continue;

                // Access the weapon's GameObject via Reflection
                var goProperty = item.GetType().GetProperty("gameObject");
                if (goProperty == null) continue;

                var weaponObject = goProperty.GetValue(item) as GameObject;
                if (weaponObject == null) continue;

                // Only the matching weapon is visible
                weaponObject.SetActive(i == targetID);
            }

            return true;
        }
        catch (System.Exception ex)
        {
            // Don't crash — just log a warning. Weapon sync is nice-to-have,
            // not game-breaking if it fails once.
            Debug.LogWarning($"[PlayerWeaponSync] Weapon switch failed (will retry): {ex.Message}");
            return false;
        }
    }
}