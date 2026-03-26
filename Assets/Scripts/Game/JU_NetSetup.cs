using UnityEngine;
using Mirror;
using JUTPS.CameraSystems;
using JUTPS;
using JUTPS.CrossPlataform;
using JUTPS.JUInputSystem;

public class JU_NetSetup : NetworkBehaviour
{
    [Header("Disable on Remote Players")]
    public MonoBehaviour[] scriptsToDisable;
    public GameObject[] objectsToDisable;

    [Header("Mobile Controls")]
    public bool ForceMobileMode = false;

    [Header("Animation")]
    public Animator playerAnimator;

    void Start()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        if (!isLocalPlayer)
            SetupRemotePlayer();
        else
            SetupLocalPlayer();
    }

    void SetupRemotePlayer()
    {
        foreach (var s in scriptsToDisable) if (s != null) s.enabled = false;
        foreach (var o in objectsToDisable) if (o != null) o.SetActive(false);

        if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;

        gameObject.tag = "Untagged";

        // Keep animator enabled so synced animations can play
        if (playerAnimator != null) playerAnimator.enabled = true;
    }

    void SetupLocalPlayer()
    {
        gameObject.tag = "Player";

        var character = GetComponent<JUCharacterController>();
        if (character == null)
        {
            Debug.LogError("[JU_NetSetup] JUCharacterController not found!");
            return;
        }

        JUGameManager.PlayerController = character;

        var cam = FindFirstObjectByType<TPSCameraController>(FindObjectsInactive.Include);
        if (cam != null)
        {
            cam.TargetToFollow = this.transform;
            cam.characterTarget = character;
            cam.gameObject.SetActive(true);
            cam.enabled = true;
        }

        if (ForceMobileMode) SetupMobileControls();

        SetupNetworkAnimator();
    }

    void SetupNetworkAnimator()
    {
        var netAnimator = GetComponent<NetworkAnimator>();

        if (netAnimator == null && playerAnimator != null)
        {
            netAnimator = gameObject.AddComponent<NetworkAnimator>();
            netAnimator.animator = playerAnimator;
        }
        else if (netAnimator != null && netAnimator.animator == null)
        {
            netAnimator.animator = playerAnimator;
        }
    }

    void SetupMobileControls()
    {
        var mobileRig = FindFirstObjectByType<MobileRig>(FindObjectsInactive.Include);
        if (mobileRig != null)
        {
            mobileRig.gameObject.SetActive(true);
            mobileRig.enabled = true;
            JUInput.Instance()?.EnableBlockStandardInputs();
        }
        else
        {
            Debug.LogWarning("[JU_NetSetup] MobileRig not found!");
        }
    }

    void OnDestroy()
    {
        if (!isLocalPlayer) return;

        if (JUGameManager.PlayerController == GetComponent<JUCharacterController>())
            JUGameManager.PlayerController = null;

        var input = JUInput.Instance();
        if (input != null && input.IsBlockingDefaultInputs)
            input.DisableBlockStandardInputs();
    }
}