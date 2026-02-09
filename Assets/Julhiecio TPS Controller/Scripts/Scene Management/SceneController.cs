using UnityEngine;
using UnityEngine.SceneManagement;
using JUTPS.CharacterBrain;
using JUTPS.PhysicsScripts;
using JUTPS.InventorySystem;

namespace JUTPS
{
    /// <summary>
    /// Manage  the scene instance and player spawn.
    /// Can reset player position or reload current scene.
    /// </summary>
    [AddComponentMenu("JU TPS/Scene Management/Scene Controller")]
    public class SceneController : MonoBehaviour
    {
        private JUCharacterBrain _playerController;

        /// <summary>
        /// Reload the current scene if the player <see cref="JUCharacterBrain.IsDead"/> is true.
        /// </summary>
        public bool ReloadLevelWhenDie;

        /// <summary>
        /// Time to spawn the player or reload the scene when <see cref="JUCharacterBrain.IsDead"/> is true.
        /// </summary>
        public float SecondsToResetLevel;

        /// <summary>
        /// Respawn the player 
        /// </summary>
        public bool JustRespawnPlayer;

        [SerializeField] private bool UseDebugLogs;

        /// <summary>
        /// Set player respawn position.
        /// </summary>
        public Vector3 RespawnPlayerPostion { get; set; }

        /// <summary>
        /// The player controller, returns an <see cref="JUCharacterBrain"/> with a <see cref="GameObject.tag"/> = "Player".
        /// </summary>
        public JUCharacterBrain PlayerController
        {
            get
            {
                if (!_playerController)
                    _playerController = GameObject.FindGameObjectWithTag("Player")?.GetComponent<JUCharacterBrain>();

                return _playerController;
            }
        }

        /// <summary>
        ///  Create <see cref="SceneController"/> component instance.
        /// </summary>
        public SceneController()
        {
            _playerController = null;

            SecondsToResetLevel = 2;
            JustRespawnPlayer = true;
            ReloadLevelWhenDie = false;

            UseDebugLogs = false;
        }

        private void Start()
        {
            RespawnPlayerPostion = PlayerController ? PlayerController.transform.position : Vector3.zero;
        }

        private void Update()
        {
            if (!PlayerController)
                return;

            if (PlayerController.IsDead && !IsInvoking(nameof(ResetLevel)) && ReloadLevelWhenDie && !JustRespawnPlayer)
            {
                Invoke(nameof(ResetLevel), SecondsToResetLevel);
                enabled = false;
            }


            if (PlayerController.IsDead && !IsInvoking(nameof(RespawnPlayer)) && JustRespawnPlayer)
                Invoke(nameof(RespawnPlayer), SecondsToResetLevel);
        }

        public void ResetLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void RespawnPlayer()
        {
            if (!PlayerController)
                return;

            //Reset Ragdoll
            if (PlayerController.TryGetComponent(out AdvancedRagdollController ARC))
            {
                PlayerController.anim.GetBoneTransform(HumanBodyBones.Hips).SetParent(ARC.HipsParent);
                ARC.State = AdvancedRagdollController.RagdollState.BlendToAnim;
                ARC.TimeToGetUp = 2;
                ARC.BlendAmount = 0;
                ARC.SetActiveRagdoll(false);
            }

            //Reset Position
            PlayerController.transform.position = RespawnPlayerPostion;

            //Reset Health
            PlayerController.CharacterHealth.Health = PlayerController.CharacterHealth.MaxHealth;
            PlayerController.IsDead = false;

            //Reset layer
            PlayerController.gameObject.layer = 9;

            //Reset Collider
            if (PlayerController.TryGetComponent<Collider>(out var collider))
            {
                collider.isTrigger = false;
                collider.enabled = true;
            }

            //Reset Rigidbody
            if (PlayerController.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.useGravity = true;
                rigidbody.isKinematic = false;
                rigidbody.linearVelocity = transform.up * PlayerController.GetComponent<Rigidbody>().linearVelocity.y;
                rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
            }

            //Enable Tps Script
            PlayerController.enabled = true;
            PlayerController.enableMove();

            //Reset Inventory
            PlayerController.GetComponent<JUInventory>().IsALoot = false;

            //Reset Animator
            PlayerController.anim.enabled = true;
            PlayerController.anim.SetBool(PlayerController.AnimatorParameters.Dying, false);
            PlayerController.anim.Play("Locomotion Blend Tree", 0);
            PlayerController.ResetDefaultLayersWeight();

            if (PlayerController.HoldableItemInUseRightHand != null)
                PlayerController.SwitchToItem(-1);

            if (UseDebugLogs)
                Debug.Log("Player has respawned");
        }
    }
}