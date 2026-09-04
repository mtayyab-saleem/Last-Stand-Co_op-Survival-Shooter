using UnityEngine;
using Mirror;
namespace JUTPS.WeaponSystem
{

    [AddComponentMenu("JU TPS/Weapon System/Ammunition Box")]
    public class AmmoBox : MonoBehaviour
    {
        [Header("Bullet Amount")]
        public int AmmoCount = 32;
        public GameObject Effect;
        [Header("Weapon ID")]
        public string WeaponName = "AnyWeapon";

        private bool isCollectible = false;

        void Start()
        {
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
                    if (juHealth != null && juHealth.IsDead) return;

                    var pl = other.GetComponent<JUCharacterController>();
                    if (pl == null || !pl.IsItemEquiped) return;
                    if (pl.WeaponInUseLeftHand == null && pl.WeaponInUseRightHand == null) return;

                    // Play effect locally immediately
                    if (Effect != null)
                    {
                        GameObject fx = Instantiate(Effect, transform.position, transform.rotation);
                        Destroy(fx, 5);
                    }

                    // Request server to apply ammo and destroy the object
                    netSetup.CmdCollectPowerup(this.gameObject);
                }
            }
        }
    }

}