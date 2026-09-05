using UnityEngine;
using Mirror;

public class LSPlayer : NetworkBehaviour
{
    [Header("Lobby & Match State")]
    [SyncVar(hook = nameof(OnNameChanged))] public string playerName = "Unknown Player";
    [SyncVar(hook = nameof(OnReadyChanged))] public bool isReady = false;

    // --- NAYA VARIABLE: Ye hamesha strictly host ko pehchanne ke kaam aayega ---
    [SyncVar(hook = nameof(OnHostStateChanged))] public bool isGameHost = false;

    [SyncVar] public int teamID = -1;
    [SyncVar] public bool isAlive = true;

    [SerializeField] private PlayerProfileSO playerProfile;

    public static LSPlayer LocalInstance { get; private set; }

    public override void OnStartLocalPlayer()
    {
        LocalInstance = this;

        // Automatically create and load the profile if it wasn't assigned in the Inspector
        if (playerProfile == null)
        {
            playerProfile = ScriptableObject.CreateInstance<PlayerProfileSO>();
        }
        
        playerProfile.LoadProfile();
        string myName = playerProfile.playerName;
        
        CmdSetPlayerName(myName);
    }

    public override void OnStartServer()
    {
        if (LSMatchManager.Instance != null)
        {
            LSMatchManager.Instance.RegisterPlayer(this);
        }
    }

    public override void OnStopServer()
    {
        if (LSMatchManager.Instance != null)
        {
            LSMatchManager.Instance.UnregisterPlayer(this);
        }
    }

    [Command]
    public void CmdSetPlayerName(string name)
    {
        playerName = name;
    }

    [Command]
    public void CmdSetReady(bool readyState)
    {
        isReady = readyState;
        if (LSMatchManager.Instance != null)
        {
            LSMatchManager.Instance.UpdateReadyState();
        }
    }

    // ==========================================
    // HOOKS
    // ==========================================
    private void OnNameChanged(string oldName, string newName) { UpdateUI(); }
    private void OnReadyChanged(bool oldReady, bool newReady) { UpdateUI(); }
    private void OnHostStateChanged(bool oldState, bool newState) { UpdateUI(); }

    private void UpdateUI()
    {
        if (LSMatchManager.Instance != null) LSMatchManager.Instance.UpdateLocalUI();
    }
}