using UnityEngine;
namespace JUTPS.UI
{
    [AddComponentMenu("JU TPS/Utilities/Auto Disable")]
    public class AutoDisable : MonoBehaviour
    {
        public float SecondsToDisable;
        void Start()
        {
            Invoke("Disable", SecondsToDisable);
        }
        private void Disable()
        {
            gameObject.SetActive(false);
        }

    }
}