using UnityEngine;

namespace JUTPS.AI
{
    /// <summary>
    /// Control a vehicle AI to move to random positions.
    /// </summary>
    public class VehicleAIRandomDestination : MonoBehaviour
    {
        /// <summary>
        /// Modes of random destionation generation.
        /// </summary>
        private enum Modes
        {
            /// <summary>
            /// Generate positions around the vehicle.
            /// </summary>
            RelativePosition,

            /// <summary>
            /// Generate positions based on world center.
            /// </summary>
            WorldPosition
        }

        /// <summary>
        /// The random destination generation mode.
        /// </summary>
        [SerializeField] private Modes RandomMode;

        /// <summary>
        /// The delay to generate new random <see cref="VehicleAI.Destination"/> (a random position around the vehicle).
        /// </summary>
        [SerializeField, Min(0.01f)] private float RefreshRate;

        /// <summary>
        /// The distance of the <see cref="Vehicle"/> from the <see cref="RandomDestination"/> to call <see cref="Refresh"/> to generate a new random destination.
        /// </summary>
        [SerializeField, Min(1)] private float RefreshByDistance;

        /// <summary>
        /// The range to generate a random position to the vehicle.
        /// </summary>
        [SerializeField, Min(5)] private float Range;

        /// <summary>
        /// The vehicle AI controller.
        /// </summary>
        public VehicleAI Vehicle { get; private set; }

        /// <summary>
        /// The current random destionation.
        /// </summary>
        public Vector3 RandomDestination { get; private set; }

        /// <summary>
        /// Create a component instance.
        /// </summary>
        public VehicleAIRandomDestination()
        {
            RefreshRate = 10;
            Range = 50;
            RandomMode = Modes.RelativePosition;
            RefreshByDistance = 10;
        }

        private void Start()
        {
            Vehicle = GetComponent<VehicleAI>();
            InvokeRepeating(nameof(Refresh), 0, RefreshRate);
        }

        private void Update()
        {
            if (!Vehicle)
                return;

            if (Vector3.Distance(Vehicle.transform.position, RandomDestination) < RefreshByDistance)
                Refresh();
        }

        private void OnDrawGizmos()
        {
            Vector3 drawPosition = Vector3.zero;
            if (Application.isPlaying && Vehicle && RandomMode == Modes.RelativePosition)
                drawPosition = Vehicle.transform.position;

            Gizmos.DrawWireCube(drawPosition, new Vector3(Range * 2, 0, Range * 2));
        }

        /// <summary>
        /// Generate a random destination to <see cref="Vehicle"/> and set to the <seealso cref="VehicleAI.Destination"/>. <para/>
        /// Get's the value on <seealso cref="RandomDestination"/>. 
        /// </summary>
        public void Refresh()
        {
            if (!Vehicle)
                return;

            RandomDestination = new Vector3(Random.Range(-Range, Range), 0, Random.Range(-Range, Range));

            if (RandomMode == Modes.RelativePosition)
                RandomDestination += Vehicle.transform.position;

            Vehicle.SetVehicleDestination(RandomDestination);
            Vehicle.RecalculatePath();
        }
    }
}