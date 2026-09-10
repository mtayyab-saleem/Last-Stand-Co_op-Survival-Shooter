using UnityEngine;
using Mirror;

public class LSPlayer : NetworkBehaviour
{
    [Header("Lobby & Match State")]
    [SyncVar(hook = nameof(OnNameChanged))] public string playerName = "Unknown Player";
    [SyncVar(hook = nameof(OnReadyChanged))] public bool isReady = false;

    // --- NAYA VARIABLE: Ye hamesha strictly host ko pehchanne ke kaam aayega ---
    [SyncVar(hook = nameof(OnHostStateChanged))] public bool isGameHost = false;

    // Team IDs are 1-based. 0 means "not assigned yet".
    [SyncVar(hook = nameof(OnTeamValueChanged))] public int teamID = 0;

    // Sequential slot inside the team: 1, 2, 3, 4.
    [SyncVar(hook = nameof(OnTeamValueChanged))] public int teamMemberIndex = 0;

    [SyncVar] public bool isAlive = true;

    [SerializeField] private PlayerProfileSO playerProfile;

    public static LSPlayer LocalInstance { get; private set; }

    /// <summary>
    /// Raised on clients whenever data the overhead name plates depend on changes.
    /// LSMatchManager's lobby SyncList callback raises it as well.
    /// </summary>
    public static event System.Action OnTeamDataChanged;

    public static void NotifyTeamDataChanged()
    {
        OnTeamDataChanged?.Invoke();
    }

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

        // The local player is known now, so every name plate can re-evaluate itself.
        NotifyTeamDataChanged();
    }

    public override void OnStartClient()
    {
        NotifyTeamDataChanged();
    }

    public override void OnStopClient()
    {
        if (LocalInstance == this)
            LocalInstance = null;

        NotifyTeamDataChanged();
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

        // Republish so the synced lobby roster carries the real name.
        if (LSMatchManager.Instance != null)
        {
            LSMatchManager.Instance.RefreshRoster();
        }
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
    private void OnNameChanged(string oldName, string newName) { UpdateUI(); NotifyTeamDataChanged(); }
    private void OnReadyChanged(bool oldReady, bool newReady) { UpdateUI(); }
    private void OnHostStateChanged(bool oldState, bool newState) { UpdateUI(); }
    private void OnTeamValueChanged(int oldValue, int newValue) { UpdateUI(); NotifyTeamDataChanged(); }

    private void UpdateUI()
    {
        if (LSMatchManager.Instance != null) LSMatchManager.Instance.UpdateLocalUI();
    }
}