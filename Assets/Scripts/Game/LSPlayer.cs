using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class LSPlayer : NetworkBehaviour
{
    [Header("Lobby & Match State")]
    [SyncVar(hook = nameof(OnNameChanged))] public string playerName = "Unknown Player";
    [SyncVar(hook = nameof(OnReadyChanged))] public bool isReady = false;
    [SyncVar(hook = nameof(OnHostStateChanged))] public bool isGameHost = false;
    [SyncVar(hook = nameof(OnTeamIDChanged))] public int teamID = -1;
    [SyncVar] public bool isAlive = true;

    private Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();
    private bool isInitialized = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        Invoke(nameof(SaveOriginalStates), 1.5f);

        // NAYA SYSTEM: Har 0.5 seconds baad yeh background check chalega
        // Taake game mein agar aap naya weapon (talwar/gun) uthayen, toh wo bhi teammate ko ignore kare
        InvokeRepeating(nameof(EnforceAAAFriendlyFire), 2f, 0.5f);
    }

    private void SaveOriginalStates()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            originalLayers[t] = t.gameObject.layer;
        }
        isInitialized = true;
    }

    public override void OnStartLocalPlayer()
    {
        string myName = PlayerPrefs.GetString("PlayerName", "Player " + Random.Range(1000, 9999));
        CmdSetPlayerName(myName);
    }

    public override void OnStartServer()
    {
        if (LSMatchManager.Instance != null) LSMatchManager.Instance.RegisterPlayer(this);
    }

    public override void OnStopServer()
    {
        if (LSMatchManager.Instance != null) LSMatchManager.Instance.UnregisterPlayer(this);
    }

    [Command] public void CmdSetPlayerName(string name) { playerName = name; }

    [Command]
    public void CmdSetReady(bool readyState)
    {
        isReady = readyState;
        if (LSMatchManager.Instance != null) LSMatchManager.Instance.UpdateReadyState();
    }

    // THE PROFESSIONAL FIX: DEEP PHYSICS COLLISION BYPASS
    private void EnforceAAAFriendlyFire()
    {
        if (!isClient || !isInitialized || isLocalPlayer) return;

        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();
        if (localPlayer != null && LSMatchManager.Instance != null)
        {
            bool isTeammate = (LSMatchManager.Instance.currentMode != LSMatchManager.GameMode.Solo) &&
                              (this.teamID != -1) &&
                              (this.teamID == localPlayer.teamID);

            // STEP 1: TALWAR AUR MUKKAY (Melee) KI PHYSICS BLOCK KARNA
            Collider[] localColliders = localPlayer.GetComponentsInChildren<Collider>(true);
            Collider[] teammateColliders = this.GetComponentsInChildren<Collider>(true);

            foreach (var lCol in localColliders)
            {
                foreach (var tCol in teammateColliders)
                {
                    if (lCol != null && tCol != null)
                    {
                        // Unity engine ko directly order de diya ke Local Player aur Teammate ki kisi cheez ko physical touch mat do
                        Physics.IgnoreCollision(lCol, tCol, isTeammate);
                    }
                }
            }

            // STEP 2: GUNS AUR BULLETS (Raycast) KO BLOCK KARNA
            int ignoreLayer = 2; // Unity's Ignore Raycast Layer
            foreach (var kvp in originalLayers)
            {
                Transform t = kvp.Key;
                if (t != null)
                {
                    int targetLayer = isTeammate ? ignoreLayer : kvp.Value;
                    if (t.gameObject.layer != targetLayer)
                    {
                        t.gameObject.layer = targetLayer;
                    }
                }
            }
        }
    }

    private void OnNameChanged(string oldName, string newName) { UpdateUI(); }
    private void OnReadyChanged(bool oldReady, bool newReady) { UpdateUI(); }
    private void OnHostStateChanged(bool oldState, bool newState) { UpdateUI(); }
    private void OnTeamIDChanged(int oldID, int newID) { UpdateUI(); }
    private void UpdateUI() { if (LSMatchManager.Instance != null) LSMatchManager.Instance.UpdateLocalUI(); }
}