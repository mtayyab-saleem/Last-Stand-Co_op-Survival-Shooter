using UnityEngine;
using JUTPS.JUInputSystem;
using UnityEngine.UIElements;
using UnityEngine.Events;

namespace JUTPS.VehicleSystem
{
    /// <summary>
    /// Add speed boost to a <seealso cref="VehicleSystem.Vehicle"/>.
    /// </summary>
    public class JUVehicleNitro : MonoBehaviour
    {
        /// <summary>
        /// Stores boost properties.
        /// </summary>
        [System.Serializable]
        public class NitroSettings
        {
            /// <summary>
            /// The boost force multiplier, useful to make power-ups.
            /// </summary>
            [Min(0)] public float ForceMultiplier;

            /// <summary>
            /// The boost force.
            /// </summary>
            [Min(0)] public float Force;

            /// <summary>
            /// The min amout of boost accepted to use.
            /// </summary>
            [Range(0, 0.9f)] public float MinAmountToUse;

            /// <summary>
            /// If true, after use the boost, the amount will refuel automatically.
            /// </summary>
            public bool AutomaticReload;

            /// <summary>
            /// The delay to start refuel the boost.
            /// </summary>
            [Min(0)] public float StartReloadDelay;
            /// <summary>
            /// The speed to refuel the boost. 
            /// </summary>
            [Min(0.1f)] public float ReloadTime;

            /// <summary>
            /// The speed to deflate the boost when using.
            /// </summary>
            [Min(0.1f)] public float UseSpeed;

            /// <summary>
            /// Creates a <see cref="NitroSettings"/> instance.
            /// </summary>
            public NitroSettings()
            {
                ForceMultiplier = 1;
                Force = 100;
                ReloadTime = 4;
                UseSpeed = 1;
                MinAmountToUse = 0;
            }
        }

        /// <summary>
        /// The mode of the nitro force is applied to the vehicle.
        /// </summary>
        public enum ApplyBoostModes
        {
            /// <summary>
            /// Apply the nitro force directly on the vehicle rigidbody.
            /// </summary>
            Rigidbody,

            /// <summary>
            /// Apply the force on each wheel of the vehicle.
            /// </summary>
            Wheels
        }

        private float _reloadDelayNitroTimer;

        /// <summary>
        /// The mode of the nitro force.
        /// </summary>
        public ApplyBoostModes ApplyForceMode;

        /// <summary>
        /// Nitro boost settings.
        /// </summary>
        public NitroSettings Settings;

        /// <summary>
        /// Use default <see cref="JUInput"/> to provide controls?
        /// </summary>
        public bool UseDefaultControls;

        /// <summary>
        /// If true, the boost can be used.
        /// </summary>
        public bool NitroEnabled;

        /// <summary>
        /// If true, the boost will has effect only when the vehicle is on ground.
        /// </summary>
        public bool UseOnlyGrounded;

        /// <summary>
        /// Invoked when the nitro is actived or disabled.
        /// </summary>
        public UnityEvent<bool> OnSetActiveNitro;

        /// <summary>
        /// The target vehicle of this boost system.
        /// </summary>
        public Vehicle Vehicle { get; private set; }

        /// <summary>
        /// Return true if the boost is active.
        /// </summary>
        public bool IsUsingNitro { get; private set; }

        /// <summary>
        /// A value between 0 and 1 where 0 is empty boost and 1 is full boost.
        /// </summary>
        public float CurrentNitroAmount { get; private set; }

        /// <summary>
        /// Return true if the boost can be active.
        /// </summary>
        public bool CanUseNitro
        {
            get
            {
                if (IsReloadingNitro && CurrentNitroAmount <= Settings.MinAmountToUse)
                    return false;

                return CurrentNitroAmount > 0 && Vehicle && Vehicle.RigidBody;
            }
        }

        /// <summary>
        /// Return true if the boost can be refull automatically.
        /// </summary>
        public bool CanReloadNitro
        {
            get => _reloadDelayNitroTimer >= Settings.StartReloadDelay;
        }

        /// <summary>
        /// Return true if the boost is reloading.
        /// </summary>
        public bool IsReloadingNitro
        {
            get => CanReloadNitro && CurrentNitroAmount < 1 && !IsUsingNitro;
        }

        /// <summary>
        /// Return true if <seealso cref="IsApplyingBoost"/> is true and if is applying boost force to the <see cref="Vehicle"/>.
        /// </summary>
        public bool IsApplyingBoost
        {
            get
            {
                if (!IsUsingNitro)
                    return false;

                return !UseOnlyGrounded || (UseOnlyGrounded && Vehicle.IsGrounded);
            }
        }

        /// <summary>
        /// Creates a <see cref="JUVehicleNitro"/> component for <see cref="VehicleSystem.Vehicle"/>.
        /// </summary>
        public JUVehicleNitro()
        {
            Settings = new NitroSettings();
            UseDefaultControls = true;
            NitroEnabled = true;
            UseOnlyGrounded = true;
        }

        private void Start()
        {
            Vehicle = GetComponent<Vehicle>();
            ReloadComplete();
        }

        private void Update()
        {
            if (!Vehicle.IsOn)
                return;

            if (UseDefaultControls)
            {
                if (JUInput.GetButton(JUInput.Buttons.RunButton))
                    UseNitro();
            }

            UpdateBoost();
            AutoReloadNitro();
        }

        private void UpdateBoost()
        {
            if (!IsUsingNitro)
                return;

            CurrentNitroAmount -= Settings.UseSpeed * Time.deltaTime;
            CurrentNitroAmount = Mathf.Clamp01(CurrentNitroAmount);

            if (IsApplyingBoost)
            {
                float forceMagnetude = Settings.Force * 1000 * Settings.ForceMultiplier * Time.deltaTime;
                switch (ApplyForceMode)
                {
                    case ApplyBoostModes.Rigidbody:
                        Vehicle.RigidBody.AddRelativeForce(Vector3.forward * forceMagnetude);
                        break;
                    case ApplyBoostModes.Wheels:

                        forceMagnetude /= Vehicle.WheelsCount;
                        for (int i = 0; i < Vehicle.WheelsCount; i++)
                        {
                            Vehicle.WheelData wheelData = Vehicle.GetWheel(i);
                            if (IsWheelApplyingBoost(wheelData))
                            {
                                Vector3 force = wheelData.WheelForward * forceMagnetude;
                                Vehicle.RigidBody.AddForceAtPosition(force, wheelData.WheelPosition);
                            }
                        }

                        break;
                    default:
                        Debug.LogError("Boost mode not implemented");
                        break;
                }
            }
        }

        private void AutoReloadNitro()
        {
            if (_reloadDelayNitroTimer < Settings.StartReloadDelay)
                _reloadDelayNitroTimer += Time.deltaTime;

            if (!IsReloadingNitro)
                return;

            CurrentNitroAmount += (1 / Settings.ReloadTime) * Time.deltaTime;
            CurrentNitroAmount = Mathf.Clamp01(CurrentNitroAmount);
        }

        private void DisableUsingNitro()
        {
            IsUsingNitro = false;
            OnSetActiveNitro.Invoke(false);
        }

        private bool IsWheelApplyingBoost(Vehicle.WheelData wheel)
        {
            if (!IsUsingNitro)
                return false;

            return !UseOnlyGrounded || (UseOnlyGrounded && wheel.IsGrounded);
        }

        /// <summary>
        /// Apply nitro force to the vehicle.
        /// </summary>
        public void UseNitro()
        {
            if (!CanUseNitro)
                return;

            _reloadDelayNitroTimer = 0;

            if (!IsUsingNitro)
            {
                IsUsingNitro = true;
                OnSetActiveNitro.Invoke(true);
            }

            CancelInvoke(nameof(DisableUsingNitro));
            Invoke(nameof(DisableUsingNitro), 0.1f);
        }

        /// <summary>
        /// Refuel the all nitro.
        /// </summary>
        public void ReloadComplete()
        {
            CurrentNitroAmount = 1;
        }

        /// <summary>
        /// Add nitro amout, a value between 0 and 1 where 1 is full refuel.
        /// </summary>
        /// <param name="amount"></param>
        public void AddNitro(float amount)
        {
            CurrentNitroAmount = Mathf.Clamp01(CurrentNitroAmount += amount);
        }
    }
}