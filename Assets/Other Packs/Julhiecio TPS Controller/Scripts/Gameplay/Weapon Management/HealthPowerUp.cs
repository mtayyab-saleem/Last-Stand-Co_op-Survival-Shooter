using JUTPS;
using Mirror;
using UnityEngine;

namespace JUTPS.PowerUps
{

    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("JU TPS/Weapon System/Health Power Up")]
    public class HealthPowerUp : MonoBehaviour
    {
        [Header("Health")]
        public float HealthToAdd = 30;
        public GameObject Effect;

        private bool isCollectible = false;

        void Start()
        {
            // Small delay before it can be picked up to prevent the dropping player from instantly consuming it
            Invoke(nameof(EnablePickup), 0.5f);
        }

        void EnablePickup()
        {
            isCollectible = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!isCollectible) return;

            if (other.gameObject.tag == "Player")
            {
                var netSetup = other.GetComponent<PlayerNetworkSetup>();
                if (netSetup != null && netSetup.isLocalPlayer)
                {
                    var juHealth = other.GetComponent<JUHealth>();
                    if (juHealth != null)
                    {
                        if (juHealth.IsDead) return;
                        if (juHealth.Health >= juHealth.MaxHealth) return;
                    }
                    // Play effect locally immediately
                    if (Effect != null)
                    {
                        GameObject fx = Instantiate(Effect, transform.position, transform.rotation);
                        Destroy(fx, 5);
                    }

                    // Request server to apply health and destroy the object
                    netSetup.CmdCollectPowerup(this.gameObject);
                }
            }
        }
    }

}
