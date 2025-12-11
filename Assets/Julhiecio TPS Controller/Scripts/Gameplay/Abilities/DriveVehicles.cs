using UnityEngine;
using UnityEngine.Events;
using JUTPS.FX;
using JUTPS.JUInputSystem;
using JUTPS.VehicleSystem;
using JUTPS.PhysicsScripts;
using System.Collections.Generic;

namespace JUTPS.ActionScripts
{
    /// <summary>
    /// Able the <see cref="JUCharacterController"/> enter or exit of any <see cref="VehicleSystem.Vehicle"/>.
    /// </summary>
    [AddComponentMenu("JU TPS/Third Person System/Actions/Drive Vehicles System")]
    public class DriveVehicles : JUTPSActions.JUTPSAction
    {
        /// <summary>
        /// Stores the properties that allow to find closest vehicles by colliders as trigger with a specific tag <seealso cref="CheckNearVehiclesSettings.EnterVehiclesAreaTag"/>
        /// to the character using <see cref="FindNearVehicles"/>.
        /// </summary>
        [System.Serializable]
        public class CheckNearVehiclesSettings
        {
            /// <summary>
            /// The layer that contains colliders with <see cref="EnterVehiclesAreaTag"/> and obstacles like walls or buildings. 
            /// </summary>
            public LayerMask Layer;

            /// <summary>
            /// Find vehicles only if there are not obstacles between the vehicle and the character.
            /// </summary>
            public bool AvoidObstacles;

            /// <summary>
            /// Used to find only specifics colliders as trigger with this tag to be used as a area to enter on the vehicle.
            /// </summary>
            public string EnterVehiclesAreaTag;

            /// <summary>
            /// The max distance to find vehicles.
            /// </summary>
            public float CheckRange;

            /// <summary>
            /// If true, <see cref="FindNearVehicles"/> will be called each <see cref="AutoCheckInterval"/>.
            /// </summary>
            [Header("Auto Check")]
            public bool AutoCheck;

            /// <summary>
            /// Used to auto find near vehicles several times per second if <see cref="AutoCheck"/> is true. <para />
            /// Can't be less than 0.1 to avoid performance issues.
            /// </summary>
            [Min(0.1f)] public float AutoCheckInterval;

            /// <summary>
            /// Create an instance of properties to use with <see cref="FindNearVehicles"/>.
            /// </summary>
            public CheckNearVehiclesSettings()
            {
                Layer = 1;
                CheckRange = 2;
                AutoCheck = true;
                AutoCheckInterval = 0.5f;
                AvoidObstacles = true;

                EnterVehiclesAreaTag = "VehicleArea";
            }
        }

        private float _checkNearVehiclesTimer;

        private JUTPSInputControlls _inputs;
        private JUFootstep _footstepSoundsToDisable;
        private AdvancedRagdollController _ragdoller;

        /// <summary>
        /// Called when the character start enter on a <see cref="Vehicle"/>.
        /// </summary>
        public UnityAction OnStartEnterVehicle;

        /// <summary>
        /// Called when the character start exit from a <see cref="CurrentVehicle"/>.
        /// </summary>
        public UnityAction OnStartExitVehicle;

        /// <summary>
        /// Called when the character cancel enter on a <see cref="Vehicle"/> caused by character movement or ragdoll during the enter vehicle state.
        /// </summary>
        public UnityAction OnCancelEnterVehicle;

        /// <summary>
        /// Called when the character cancel exit from a <see cref="CurrentVehicle"/> caused by character ragdoll during the exit vehicle state.
        /// </summary>
        public UnityAction OnCancelExitVehicle;

        /// <summary>
        /// The vehicle used to start game driving.
        /// </summary>
        [SerializeField] private Vehicle _startVehicle;

        /// <summary>
        /// If true, the character can enter and drive vehicles.
        /// </summary>
        public bool EnterVehiclesEnabled;

        /// <summary>
        /// If true, the character can exit from the <see cref="CurrentVehicle"/> if is driving.
        /// </summary>
        public bool ExitVehiclesEnabled;

        /// <summary>
        /// Disable the character after enter on the vehicle, usefull for vehicles that not have IK settings for drive animations.
        /// </summary>
        public bool DisableCharacterOnEnter;

        /// <summary>
        /// The time to reactive enter vehicle action after start enter or exit of some <see cref="Vehicle"/>. <para />
        /// Can't be less than 0.1.
        /// </summary>
        [Min(0.1f)] public float DelayToReenableAction;

        /// <summary>
        /// Use default player controls to enter and exit vehicles.
        /// </summary>
        public bool UseDefaultInputs;

        /// <summary>
        /// Stores the properties that allow to find closest vehicles by colliders as trigger with <see cref="CheckNearVehiclesSettings.EnterVehiclesAreaTag"/> tag to the character using <see cref = "FindNearVehicles" />.
        /// </summary>
        public CheckNearVehiclesSettings CheckNearVehicles;

        /// <summary>
        /// Don't allow the character enter on the <see cref="NearestVehicle"/> if the <see cref="Vehicle"/> speed is greater than <see cref="MaxVehicleSpeedToEnter"/>.
        /// </summary>
        [Min(1)] public float MaxVehicleSpeedToEnter;

        /// <summary>
        /// Don't allow the character exit from the <see cref="CurrentVehicle"/> if the <see cref="Vehicle"/> speed is greater than <see cref="MaxVehicleSpeedToExit"/>.
        /// </summary>
        [Min(1)] public float MaxVehicleSpeedToExit;

        /// <summary>
        /// Don't allow the character enter to the <see cref="NearestVehicle"/> if the character <see cref="Rigidbody.velocity"/> is greater than <see cref="MaxCharacterSpeedToEnter"/>.
        /// </summary>
        [Min(0.1f)] public float MaxCharacterSpeedToEnter;

        /// <summary>
        /// The layer used to detect the ground to set the character position when exit from the <see cref="Vehicle"/>.
        /// </summary>
        public LayerMask GroundLayer;

        /// <summary>
        /// Called when the character enter on a <see cref="Vehicle"/>.
        /// </summary>
        public UnityEvent OnEnterVehicle;

        /// <summary>
        /// Called when the character exit from a <see cref="Vehicle"/>.
        /// </summary>
        public UnityEvent OnExitVehicle;

        /// <summary>
        /// Return true if the character is inside of a <see cref="Vehicle"/>.
        /// </summary>
        public bool IsDriving { get; private set; }

        /// <summary>
        /// Return false if the character has started to enter on a <see cref="Vehicle"/>. <para/>
        /// Must wait the <see cref="DelayToReenableAction"/> to able interact with a <see cref="Vehicle"/> again.
        /// </summary>
        public bool IsCharacterEntering { get; private set; }

        /// <summary>
        /// Return false if the character has started to exit from a <see cref="Vehicle"/>. <para/>
        /// Must wait the <see cref="DelayToReenableAction"/> to able interact with a <see cref="Vehicle"/> again.
        /// </summary>
        public bool IsCharacterExiting { get; private set; }

        /// <summary>
        /// Returns a <see cref="Vehicle"/> near of the character if the character pass over a collider with VehicleArea" tag.
        /// </summary>
        public Vehicle NearestVehicle { get; private set; }

        /// <summary>
        /// Return the <see cref="NearestVehicle"/> character IK settings to drive the <see cref="Vehicle"/>.
        /// </summary>
        public JUVehicleCharacterIK NearestVehicleCharacterIK { get; private set; }

        /// <summary>
        /// The current vehicle that the character is driving.
        /// </summary>
        public Vehicle CurrentVehicle { get; private set; }

        /// <summary>
        /// Returns a component of <see cref="CurrentVehicle"/> that contains all character IK settings if is driving a <see cref="Vehicle"/>.
        /// </summary>
        public JUVehicleCharacterIK CurrentVehicleCharacterIK { get; private set; }

        /// <summary>
        /// Return true if can enter on a vehicle following this rules: <para/>
        /// The character can't enter on a vehicle if the character is rolling.
        /// The character can't enter on a vehicle if is already entering or exiting from a 
        /// <see cref="Vehicle"/>, check <see cref="IsCharacterEntering"/> and <see cref="IsCharacterExiting"/>
        /// <see cref="EnterVehiclesEnabled"/> is true. <para/>
        /// <see cref="IsDriving"/> is false.
        /// The <see cref="JUCharacterController"/> must isn't ragdolled.
        /// </summary>
        public bool CanEnterVehicle
        {
            get
            {
                if (IsCharacterEntering || IsCharacterExiting)
                    return false;

                // Enter vehicle ability is disabled.
                if (!EnterVehiclesEnabled)
                    return false;

                // Can't enter on a vehicle if is already driving.
                if (IsDriving)
                    return false;

                // Can't enter on vehicle if is rolling
                if (TPSCharacter.IsRolling)
                    return false;

                // Can't enter on a vehicle if the character isn't standing.
                if (_ragdoller)
                {
                    if (_ragdoller.State != AdvancedRagdollController.RagdollState.Animated)
                        return false;
                }

                if (!isActiveAndEnabled)
                    return false;

                return true;
            }
        }

        /// <summary>
        /// Return true if the character is driving and can exit from the <see cref="CurrentVehicle"/>.
        /// </summary>
        public bool CanExitVehicle
        {
            get => ExitVehiclesEnabled && IsDriving && !IsCharacterExiting && !IsCharacterEntering && CurrentVehicle.Velocity.magnitude < MaxVehicleSpeedToExit;
        }

        /// <summary>
        /// Create an instance of <see cref="DriveVehicles"/> component.
        /// </summary>
        public DriveVehicles()
        {
            EnterVehiclesEnabled = true;
            ExitVehiclesEnabled = true;
            DelayToReenableAction = 0.2f;
            GroundLayer = 1;

            MaxCharacterSpeedToEnter = 1;
            MaxVehicleSpeedToEnter = 3;
            MaxVehicleSpeedToExit = 1000;

            UseDefaultInputs = true;
        }

        private void Start()
        {
            _inputs = JUInput.Instance().InputActions;
            _footstepSoundsToDisable = GetComponent<JUFootstep>();
            _ragdoller = GetComponent<AdvancedRagdollController>();

            if (_startVehicle)
            {
                DriveVehicle(_startVehicle, _startVehicle.GetComponent<JUVehicleCharacterIK>(), true);
            }
            _inputs.Player.Interact.started += OnPressEnterVehicleButton;
        }
        private void OnEnable()
        {
            if(_inputs == null) _inputs = JUInput.Instance().InputActions;
            _inputs.Player.Interact.started += OnPressEnterVehicleButton;
        }
        private void OnDestroy()
        {
            _inputs.Player.Interact.started -= OnPressEnterVehicleButton;
        }

        private void Update()
        {
            if (IsDriving)
            {
                if (_ragdoller && _ragdoller.State == AdvancedRagdollController.RagdollState.Ragdolled)
                {
                    ExitVehicle();
                    return;
                }

                UpdateDrivingState();
            }
            else if (IsCharacterEntering || IsCharacterExiting)
                UpdateEnteringExitingState();

            if (CheckNearVehicles.AutoCheck)
            {
                _checkNearVehiclesTimer += Time.deltaTime;
                if (_checkNearVehiclesTimer > CheckNearVehicles.AutoCheckInterval)
                {
                    _checkNearVehiclesTimer = 0;
                    FindNearVehicles();
                }
            }
        }

        private void UpdateDrivingState()
        {
            // Physic Changes.
            if (rb)
                rb.linearVelocity = CurrentVehicle.RigidBody.linearVelocity;

            // Update character position inside vehicle.
            if (CurrentVehicleCharacterIK && CurrentVehicleCharacterIK.InverseKinematicTargetPositions.CharacterPosition)
            {
                var seat = CurrentVehicleCharacterIK.InverseKinematicTargetPositions.CharacterPosition;
                transform.position = seat.position;
                transform.rotation = seat.rotation;
            }
            else
            {
                transform.position = CurrentVehicle.transform.position;
                transform.rotation = CurrentVehicle.transform.rotation;
            }
        }

        /// <summary>
        /// Find vehicles near of the character using <see cref="CheckNearVehicles"/>. <para />
        /// The vehicle must have a collider as trigger with <see cref="CheckNearVehiclesSettings.EnterVehiclesAreaTag"/> to be found.
        /// Use <see cref="NearestVehicle"/> to get the <see cref="Vehicle"/> after call it.
        /// </summary>
        public void FindNearVehicles()
        {
            if (string.IsNullOrEmpty(CheckNearVehicles.EnterVehiclesAreaTag))
            {
                Debug.Log($"The {nameof(DriveVehicles)} of the {nameof(gameObject)} {name} hasn't {nameof(CheckNearVehicles.EnterVehiclesAreaTag)} configured. Can't find near vehicles.");
                return;
            }

            Vector3 characterCenter = TPSCharacter.coll.bounds.center;
            Collider[] colliders = Physics.OverlapSphere(characterCenter, CheckNearVehicles.CheckRange, CheckNearVehicles.Layer);

            List<Collider> _vehicleAreas = new List<Collider>(colliders.Length);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].CompareTag(CheckNearVehicles.EnterVehiclesAreaTag))
                    _vehicleAreas.Add(colliders[i]);
            }

            if (_vehicleAreas.Count == 0)
            {
                NearestVehicle = null;
                NearestVehicleCharacterIK = null;
                return;
            }

            // Find the nearest vehicle to enter.
            _vehicleAreas.Sort((a, b) =>
            {
                var aDistance = (characterCenter - a.transform.position).magnitude;
                var bDistance = (characterCenter - b.transform.position).magnitude;
                return aDistance > bDistance ? 1 : -1;
            });

            var nearestVehicleArea = _vehicleAreas[0];

            if (CheckNearVehicles.AvoidObstacles)
            {
                Physics.Linecast(characterCenter, nearestVehicleArea.bounds.center, out RaycastHit hit, CheckNearVehicles.Layer, QueryTriggerInteraction.Ignore);
                if (hit.collider && hit.collider != nearestVehicleArea)
                {
                    NearestVehicle = null;
                    NearestVehicleCharacterIK = null;
                    return;
                }
            }

            NearestVehicle = nearestVehicleArea.GetComponentInParent<Vehicle>();
            if (NearestVehicle)
                NearestVehicleCharacterIK = NearestVehicle.GetComponent<JUVehicleCharacterIK>();

            return;
        }

        public void TryDriveNearestVehicle()
        {
            DriveVehicle(NearestVehicle, NearestVehicleCharacterIK);
        }

        /// <summary>
        /// Enter on a vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle to enter.</param>
        /// <param name="vehicleCharacterIK">The vehicle settings for character animation.</param>
        /// <param name="immediately">If true, the character will not have a delay to enter, it's made immediately.</param>
        public void DriveVehicle(Vehicle vehicle, JUVehicleCharacterIK vehicleCharacterIK, bool immediately = false)
        {
            if (!vehicle || !CanEnterVehicle || !TPSCharacter || rb.linearVelocity.magnitude > MaxCharacterSpeedToEnter)
                return;

            if (vehicleCharacterIK && !vehicleCharacterIK.CharactersCanGetVehicle)
                return;

            CurrentVehicle = vehicle;
            CurrentVehicleCharacterIK = vehicleCharacterIK;

            if (_footstepSoundsToDisable)
                _footstepSoundsToDisable.enabled = false;

            StartEnterVehicleState();

            if (immediately)
                EndEnterVehicleState();
            else
                Invoke(nameof(EndEnterVehicleState), DelayToReenableAction);
        }

        /// <summary>
        /// Exit from the <see cref="CurrentVehicle"/> if is driving.
        /// </summary>
        public void ExitVehicle()
        {
            if (!CanExitVehicle || !TPSCharacter || !IsDriving)
                return;

            if (_footstepSoundsToDisable)
                _footstepSoundsToDisable.enabled = true;

            if (CurrentVehicleCharacterIK)
                TPSCharacter.transform.position = CurrentVehicleCharacterIK.GetExitPosition(GroundLayer);

            // Try spawn the character on any position around the vehicle if not have an exit position assigned.
            else
                TPSCharacter.transform.position = CurrentVehicle.transform.position + (-CurrentVehicle.transform.right * 3);

            OnCharacterStopDriving();
            StartExitVehicleState();

            Invoke(nameof(EndExitVehicleState), DelayToReenableAction);
        }

        private void UpdateEnteringExitingState()
        {
            TPSCharacter.DisableLocomotion();

            bool isMoving = TPSCharacter.IsMoving || rb.linearVelocity.magnitude > MaxCharacterSpeedToEnter;
            bool isRagdolling = _ragdoller.State == AdvancedRagdollController.RagdollState.Ragdolled;

            if (IsCharacterEntering && (isMoving || isRagdolling)) CancelEnterVehicle();
            if (IsCharacterExiting && isRagdolling) CancelExitVehicle();
        }

        private void StartEnterVehicleState()
        {
            if (IsCharacterEntering || IsCharacterExiting)
                return;

            IsCharacterEntering = true;

            OnStartEnterVehicle?.Invoke();
        }

        private void CancelEnterVehicle()
        {
            if (!IsCharacterEntering || IsCharacterExiting)
                return;

            IsCharacterEntering = false;
            CurrentVehicle = null;
            CurrentVehicleCharacterIK = null;
            TPSCharacter.enableMove();
            CancelInvoke(nameof(EndEnterVehicleState));

            OnCancelEnterVehicle?.Invoke();
        }

        private void EndEnterVehicleState()
        {
            if (!IsCharacterEntering || IsCharacterExiting)
                return;

            OnCharacterStartDriving();

            IsDriving = true;
            CurrentVehicle.IsOn = true;
            IsCharacterEntering = false;

            if (DisableCharacterOnEnter)
                TPSCharacter.gameObject.SetActive(false);

            TPSCharacter.DrivingCheck();
            TPSCharacter.PhysicalIgnore(CurrentVehicle.gameObject, ignore: true);

            OnEnterVehicle?.Invoke();
        }

        private void StartExitVehicleState()
        {
            if (IsCharacterEntering || IsCharacterExiting)
                return;

            IsDriving = false;
            IsCharacterExiting = true;
            CurrentVehicle.IsOn = false;

            if (DisableCharacterOnEnter)
                TPSCharacter.gameObject.SetActive(true);

            OnStartExitVehicle?.Invoke();
        }

        private void CancelExitVehicle()
        {
            EndEnterVehicleState();

            OnCancelExitVehicle?.Invoke();
        }

        private void EndExitVehicleState()
        {
            if (!IsCharacterExiting || IsCharacterEntering)
                return;

            if (!IsDriving && CurrentVehicle)
                TPSCharacter.PhysicalIgnore(CurrentVehicle.gameObject, ignore: false);

            IsCharacterExiting = false;
            CurrentVehicle = null;
            CurrentVehicleCharacterIK = null;

            TPSCharacter.enableMove();

            OnExitVehicle?.Invoke();
        }

        private void OnPressEnterVehicleButton(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!UseDefaultInputs)
                return;

            float vehicleSpeed = 0f;
            if (IsDriving) vehicleSpeed = CurrentVehicle.RigidBody.linearVelocity.magnitude;
            else if (NearestVehicle) vehicleSpeed = NearestVehicle.RigidBody.linearVelocity.magnitude;

            if (IsDriving && vehicleSpeed < MaxVehicleSpeedToExit)
                ExitVehicle();

            if (!IsDriving && vehicleSpeed < MaxVehicleSpeedToEnter)
                DriveVehicle(NearestVehicle, NearestVehicleCharacterIK);
        }

        private void OnCharacterStartDriving()
        {
            if (!TPSCharacter || !anim || DisableCharacterOnEnter)
                return;

            //Set Driving Animation
            anim.SetBool(TPSCharacter.AnimatorParameters.Driving, true);

            //Change Some Character States

            TPSCharacter.IsJumping = false;
            TPSCharacter.IsGrounded = false;
            TPSCharacter.VelocityMultiplier = 0;

            TPSCharacter.SwitchToItem(-1);
            TPSCharacter.DisableLocomotion();

            // Disable physics.
            if (rb)
                rb.useGravity = false;

            // Change Character Collider Properties.
            if (coll)
                coll.isTrigger = true;

            // Disable Default Animator Layers.
            for (int i = 1; i < 4; i++)
                anim.SetLayerWeight(i, 0);

            // Disable All Locomotion Animator Parameters.
            anim.SetBool(TPSCharacter.AnimatorParameters.Crouch, false);
            anim.SetBool(TPSCharacter.AnimatorParameters.ItemEquiped, false);
            anim.SetBool(TPSCharacter.AnimatorParameters.FireMode, false);
            anim.SetBool(TPSCharacter.AnimatorParameters.Grounded, true);
            anim.SetBool(TPSCharacter.AnimatorParameters.Running, false);
            anim.SetFloat(TPSCharacter.AnimatorParameters.IdleTurn, 0);
            anim.SetFloat(TPSCharacter.AnimatorParameters.Speed, 0);
        }

        private void OnCharacterStopDriving()
        {
            if (!TPSCharacter || !anim)
                return;

            TPSCharacter.DisableAllMove = false;

            if (DisableCharacterOnEnter)
            {
                TPSCharacter.transform.eulerAngles = new Vector3(0, TPSCharacter.transform.eulerAngles.y, 0);
                return;
            }

            // Set Driving Animation.
            anim.SetBool(TPSCharacter.AnimatorParameters.Driving, false);

            // Enable Character Locomotion.
            TPSCharacter.EnableMove();
            TPSCharacter.transform.eulerAngles = new Vector3(0, TPSCharacter.transform.eulerAngles.y, 0);

            // Change Character Collider Properties.
            if (coll) coll.isTrigger = false;
            if (rb) rb.useGravity = true;

            // Disable Default Animator Layers.
            TPSCharacter.ResetDefaultLayersWeight();
        }
    }
}