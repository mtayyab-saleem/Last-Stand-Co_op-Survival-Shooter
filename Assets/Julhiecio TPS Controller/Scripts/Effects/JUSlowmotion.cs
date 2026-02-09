using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.FX
{
	[AddComponentMenu("JU TPS/FX/Slow Motion")]
	public class JUSlowmotion : MonoBehaviour
	{
		public static JUSlowmotion Instance;

		[Header("Slowmotion Settings")]
		public bool EnableSlowmotion = true;

		float SlowDownFactor = 0.05f;
		float SlowDownLenght = 1;
		protected virtual void Start()
		{
			Instance = this;
			Time.fixedDeltaTime = 0.015f;
		}

		// Update is called once per frame
		protected virtual void Update()
		{
			if (!EnableSlowmotion) { return; }
			Time.timeScale += (1f / SlowDownLenght) * Time.unscaledDeltaTime;
			Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
			Time.fixedDeltaTime = Mathf.Clamp(Time.fixedDeltaTime, 0.01f, 0.333f);
		}

		/// <summary>
		/// Do a slowmotion effect
		/// </summary>
		/// <param name="timescale"> time scale at the start of slow motion </param>
		/// <param name="duration"> duration of slowmotion </param>
		public static void DoSlowMotion(float timescale = 0.1f, float duration = 2)
		{
			if (Instance == null) return;
			if (Instance.EnableSlowmotion == false)
			{
				Debug.LogWarning("Called Slow Motion effect but it is not enabled");
				return;
			}

			Instance.SlowDownFactor = timescale;
			Instance.SlowDownLenght = duration;
			Time.timeScale = timescale;
			Time.fixedDeltaTime = Time.timeScale * .01f;
			Instance.Invoke("DisableSlowmotion", 0.4f * duration);
		}
		/// <summary>
		/// Do a slowmotion effect
		/// </summary>
		/// <param name="timescale"> time scale at the start of slow motion </param>
		/// <param name="duration"> duration of slowmotion </param>
		public static void DoSlowMotion()
		{
			if (Instance == null) return;
			if (Instance.EnableSlowmotion == false)
			{
				Debug.LogWarning("Called Slow Motion effect but it is not enabled");
				return;
			}

			Instance.SlowDownFactor = 0.1f;
			Instance.SlowDownLenght = 2;
			Time.timeScale = Instance.SlowDownFactor;
			Time.fixedDeltaTime = Time.timeScale * .01f;
			Instance.Invoke("DisableSlowmotion", 0.4f * Instance.SlowDownLenght);
		}

		/// <summary>
		/// Disable slowmotion effect and reset fixed time step to 0.015f
		/// </summary>
		public void DisableSlowmotion()
		{
			SlowDownFactor = 1;
			SlowDownLenght = 1;
			Time.timeScale = 1;
			Time.fixedDeltaTime = 0.015f;
		}
	}

}