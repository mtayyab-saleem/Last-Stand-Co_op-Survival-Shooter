using UnityEngine;

namespace JUTPS
{
	/// <summary>
	/// Stores informations about player and platform input system.
	/// </summary>
	[AddComponentMenu("JU TPS/Gameplay/Game/Game Manager")]
	public class JUGameManager : MonoBehaviour
	{
		[SerializeField] private bool SimulateMobileDevice = false;

		/// <summary>
		/// The player controll instance.
		/// </summary>
		public static JUCharacterController PlayerController { get; set; }
		public static JUGameManager Instance { get; protected set; }
		/// <summary>
		/// Return true if is using touch inputs.
		/// </summary>
		public static bool IsMobileControls { get; private set; }

		private void Awake()
		{
			Instance = this;
#if UNITY_ANDROID && !UNITY_EDITOR
			SimulateMobileDevice = SystemInfo.deviceType == DeviceType.Handheld;
#endif
		}

		private void Start()
		{
			if (!PlayerController)
			{
				PlayerController = GetPlayer();
			}
		}
		public JUCharacterController GetPlayer()
        {
			if (PlayerController == null)
			{
				GameObject playerGameObject = GameObject.FindGameObjectWithTag("Player");
				if (playerGameObject)
				{
					if (playerGameObject.TryGetComponent(out JUCharacterController tpsChar)) { return tpsChar; } else { return null; }
                }
                else
                {
					return null;
                }
			}
			else
			{
				return PlayerController;
			}
        }
		void Update()
		{
			IsMobileControls = SimulateMobileDevice;
		}

		private void OnDestroy()
		{
			PlayerController = null;
		}
	}
}