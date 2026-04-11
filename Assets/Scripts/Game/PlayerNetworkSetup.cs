using UnityEngine;
using Mirror;
using JUTPS.CameraSystems;
using JUTPS;
using JUTPS.CrossPlataform;
using JUTPS.JUInputSystem;

public class PlayerNetworkSetup : NetworkBehaviour
{
    public MonoBehaviour[] scriptsToDisable;
    [Header("Network Sync Components")]
    [Tooltip("Player's Animator (Auto-find if not assigned)")]
    public Animator playerAnimator;

    void Start()
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }

        // REMOTE PLAYERS
        if (!isLocalPlayer)
        {
            foreach (var s in scriptsToDisable) s.enabled = false;

            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            gameObject.tag = "Untagged";

            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
                Debug.Log($"Remote player animator enabled: {gameObject.name}");
            }
        }
        // LOCAL PLAYER
        else
        {
            gameObject.tag = "Player";
            SetupLocalPlayer();
        }
    }

    void SetupLocalPlayer()
    {
        // GAME MANAGER SETUP
        var character = GetComponent<JUCharacterController>();
        if (character != null)
        {
            JUGameManager.PlayerController = character;
            Debug.Log("Player registered with JUGameManager");
        }
        else
        {
            Debug.LogError("JUCharacterController not found on player!");
            return;
        }

        //CAMERA SETUP
        var cam = CameraManager.MainCam;
        if (cam != null)
        {
            cam.TargetToFollow = this.transform;
            cam.characterTarget = character;
            cam.gameObject.SetActive(true);
            cam.enabled = true;
            Debug.Log("Camera setup complete");
        }


        SetupNetworkAnimator();
    }

    void SetupNetworkAnimator()
    {
        
        if (TryGetComponent(out NetworkAnimator netAnimator))
        {
            Debug.Log($"Animation sync is perfectly active for {gameObject.name}");
        }
        else
        {
            Debug.LogError("NetworkAnimator missing on Player Prefab!");
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (isLocalPlayer)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.TriggerDisconnectSequence();
            }
        }
    }

    void OnDestroy()
    {
        if (isLocalPlayer)
        {
            if (TryGetComponent(out JUCharacterController character))
            {
                if (JUGameManager.PlayerController == character)
                {
                    JUGameManager.PlayerController = null;
                    Debug.Log("Player safely unregistered from JUGameManager");
                }
            }

            if (JUInput.Instance() != null && JUInput.Instance().IsBlockingDefaultInputs)
            {
                JUInput.Instance().DisableBlockStandardInputs();
            }
            var mobileRig = Object.FindFirstObjectByType<MobileRig>(FindObjectsInactive.Include);
            if (mobileRig != null)
            {
                mobileRig.gameObject.SetActive(false);
            }
        }
    }
}