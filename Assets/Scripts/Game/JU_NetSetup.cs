using UnityEngine;
using Mirror;
using JUTPS.CameraSystems;
using JUTPS;
using JUTPS.CrossPlataform;
using JUTPS.JUInputSystem;

public class JU_NetSetup : NetworkBehaviour
{
    [Header("Drag Components Here")]
    public MonoBehaviour[] scriptsToDisable;
    public GameObject[] objectsToDisable;

    [Header("Mobile Controls")]
    [Tooltip("Mobile controls force enable? (Testing ke liye)")]
    public bool ForceMobileMode = false;

    [Header("Network Sync Components")]
    [Tooltip("Player ka Animator (Auto-find hoga)")]
    public Animator playerAnimator;

    void Start()
    {
        // Auto-find animator agar assign nahi hai
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = GetComponentInChildren<Animator>();
            }
        }

        // === REMOTE PLAYER ===
        if (!isLocalPlayer)
        {
            foreach (var s in scriptsToDisable) s.enabled = false;
            foreach (var o in objectsToDisable) o.SetActive(false);
            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            gameObject.tag = "Untagged";

            // ✅ IMPORTANT: Remote player ka animator ENABLE rakho
            // Taaki animations sync ho sakein
            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
                Debug.Log($"✅ Remote player animator enabled: {gameObject.name}");
            }
        }
        // === LOCAL PLAYER ===
        else
        {
            gameObject.tag = "Player";
            SetupLocalPlayer();
        }
    }

    void SetupLocalPlayer()
    {
        // 1. GAME MANAGER SETUP
        var character = GetComponent<JUCharacterController>();
        if (character != null)
        {
            JUGameManager.PlayerController = character;
            Debug.Log("✅ Player registered with JUGameManager");
        }
        else
        {
            Debug.LogError("❌ JUCharacterController not found on player!");
            return;
        }

        // 2. CAMERA SETUP
        var cam = FindFirstObjectByType<TPSCameraController>(FindObjectsInactive.Include);
        if (cam != null)
        {
            cam.TargetToFollow = this.transform;
            cam.characterTarget = character;
            cam.gameObject.SetActive(true);
            cam.enabled = true;
            Debug.Log("✅ Camera setup complete");
        }

        // 3. MOBILE MODE SETUP
        if (ForceMobileMode)
        {
            SetupMobileControls();
        }

        // 4. NETWORK ANIMATOR SETUP
        SetupNetworkAnimator();
    }

    void SetupNetworkAnimator()
    {
        // Check if NetworkAnimator already exists
        var netAnimator = GetComponent<NetworkAnimator>();

        if (netAnimator == null && playerAnimator != null)
        {
            // Add NetworkAnimator component
            netAnimator = gameObject.AddComponent<NetworkAnimator>();
            netAnimator.animator = playerAnimator;
            Debug.Log("✅ NetworkAnimator automatically added and configured");
        }
        else if (netAnimator != null && netAnimator.animator == null)
        {
            // Configure existing NetworkAnimator
            netAnimator.animator = playerAnimator;
            Debug.Log("✅ NetworkAnimator configured with player animator");
        }

        if (netAnimator != null)
        {
            Debug.Log($"✅ Animation sync enabled for {gameObject.name}");
        }
    }

    void SetupMobileControls()
    {
        var mobileRig = FindFirstObjectByType<MobileRig>(FindObjectsInactive.Include);
        if (mobileRig != null)
        {
            mobileRig.gameObject.SetActive(true);
            mobileRig.enabled = true;
            Debug.Log("✅ Mobile Rig enabled");

            if (JUInput.Instance() != null)
            {
                JUInput.Instance().EnableBlockStandardInputs();
                Debug.Log("✅ Keyboard inputs blocked - Mobile mode active");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ Mobile Rig not found!");
        }
    }

    void OnDestroy()
    {
        if (isLocalPlayer)
        {
            if (JUGameManager.PlayerController == GetComponent<JUCharacterController>())
            {
                JUGameManager.PlayerController = null;
                Debug.Log("🔄 Player unregistered from JUGameManager");
            }

            if (JUInput.Instance() != null && JUInput.Instance().IsBlockingDefaultInputs)
            {
                JUInput.Instance().DisableBlockStandardInputs();
            }
        }
    }
}