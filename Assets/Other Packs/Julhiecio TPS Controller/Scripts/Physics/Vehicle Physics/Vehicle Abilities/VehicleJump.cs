using UnityEngine;

namespace JUTPS.VehicleSystem
{
    /// <summary>
    /// This component allow the vehicle jump.
    /// </summary>
    public class VehicleJump : MonoBehaviour
    {
        /// <summary>
        /// The jump force.
        /// </summary>
        public float JumpForce;

        /// <summary>
        /// Use <see cref="JUInputSystem.JUInput"/> control to do vehicle jump?
        /// </summary>
        public bool UseDefaultInput;

        /// <summary>
        /// The vehicle that be controled by this <see cref="VehicleJump"/> component.
        /// </summary>
        public Vehicle Vehicle { get; private set; }

        /// <summary>
        /// Create a <see cref="VehicleJump"/> component instance.
        /// </summary>
        public VehicleJump()
        {
            JumpForce = 100;
            UseDefaultInput = true;
        }

        private void Start()
        {
            Vehicle = GetComponent<Vehicle>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (!UseDefaultInput)
                return;

            if (JUInputSystem.JUInput.GetButtonDown(JUInputSystem.JUInput.Buttons.JumpButton))
                Jump(JumpForce);
        }

        /// <summary>
        /// Do vehicle jump.
        /// </summary>
        /// <param name="force">The jump force</param>
        public void Jump(float force)
        {
            if (!Vehicle ||!Vehicle.IsOn || !Vehicle.IsGrounded)
                return;

            Vehicle.RigidBody.AddRelativeForce(0, force, 0, ForceMode.Impulse);
        }
    }
}