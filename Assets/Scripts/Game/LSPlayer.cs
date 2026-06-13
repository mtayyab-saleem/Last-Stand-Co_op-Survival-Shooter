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

    // Memory for original states
    private Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();
    private Dictionary<Transform, string> originalTags = new Dictionary<Transform, string>();
    private bool isInitialized = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        Invoke(nameof(SaveOriginalStates), 1.5f);

        // Background loop: Har 0.2 sec baad block confirm karega
        InvokeRepeating(nameof(EnforceFriendlyFire), 2f, 0.2f);
    }

    private void SaveOriginalStates()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            originalLayers[t] = t.gameObject.layer;
            originalTags[t] = t.gameObject.tag;
        }
        isInitialized = true;
    }

    public override void OnStartLocalPlayer()
    {
        CmdSetPlayerName(PlayerPrefs.GetString("PlayerName", "Player " + Random.Range(1000, 9999)));
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

    // ==========================================
    // THE PERFECT AAA FIX: SOLID BODY & ZERO EFFECTS
    // ==========================================
    private void EnforceFriendlyFire()
    {
        if (!isClient || !isInitialized || isLocalPlayer) return;

        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();
        if (localPlayer != null && LSMatchManager.Instance != null)
        {
            bool isTeammate = (LSMatchManager.Instance.currentMode != LSMatchManager.GameMode.Solo) &&
                              (this.teamID != -1) &&
                              (this.teamID == localPlayer.teamID);

            Collider[] teammateColliders = this.GetComponentsInChildren<Collider>(true);

            int ignoreLayer = 2; // Ignore Raycast (Goli block karne ke liye)
            string ignoreTag = "Untagged"; // Blood effect block karne ke liye

            foreach (var tCol in teammateColliders)
            {
                if (tCol == null) continue;
                Transform t = tCol.transform;

                if (isTeammate)
                {
                    // 1. LAYER & TAG FIX (Taake raycast aur JUTPS hit tag detect na kare)
                    // Physics.IgnoreCollision yahan use nahi kiya taake body 100% solid rahe
                    if (t.gameObject.layer != ignoreLayer) t.gameObject.layer = ignoreLayer;
                    if (t.gameObject.tag != ignoreTag) t.gameObject.tag = ignoreTag;

                    // 2. HITBOX KILLER (Sirf damage/hitbox scripts ko turn off kar rahe hain)
                    MonoBehaviour[] scripts = tCol.GetComponents<MonoBehaviour>();
                    foreach (var script in scripts)
                    {
                        if (script == null) continue;
                        string sName = script.GetType().Name.ToLower();

                        if (sName.Contains("hitbox") || sName.Contains("damage") || sName.Contains("health"))
                        {
                            script.enabled = false;
                        }
                    }
                }
                else
                {
                    // Dushman hai toh sab wapas ON kar do
                    if (originalLayers.ContainsKey(t) && t.gameObject.layer != originalLayers[t])
                        t.gameObject.layer = originalLayers[t];
                    if (originalTags.ContainsKey(t) && t.gameObject.tag != originalTags[t])
                        t.gameObject.tag = originalTags[t];

                    MonoBehaviour[] scripts = tCol.GetComponents<MonoBehaviour>();
                    foreach (var script in scripts)
                    {
                        if (script == null) continue;
                        string sName = script.GetType().Name.ToLower();
                        if (sName.Contains("hitbox") || sName.Contains("damage") || sName.Contains("health"))
                        {
                            script.enabled = true;
                        }
                    }
                }
            }
        }
    }

    private void OnNameChanged(string oldName, string newName) { UpdateUI(); }
    private void OnReadyChanged(bool oldReady, bool newReady) { UpdateUI(); }
    private void OnHostStateChanged(bool oldState, bool newState) { UpdateUI(); }
    private void OnTeamIDChanged(int oldID, int newID) { UpdateUI(); }

    private void UpdateUI()
    {
        if (LSMatchManager.Instance != null) LSMatchManager.Instance.UpdateLocalUI();
    }
}