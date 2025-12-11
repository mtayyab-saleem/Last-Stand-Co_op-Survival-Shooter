using UnityEngine;
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
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "Player")
            {
                var pl = other.GetComponent<JUCharacterController>();
                if (pl.IsItemEquiped)
                {
                    if (pl.WeaponInUseLeftHand == null && pl.WeaponInUseRightHand == null) return;

                    if (pl.WeaponInUseRightHand != null)
                    {
                        if (pl.WeaponInUseRightHand.ItemName == WeaponName) pl.WeaponInUseRightHand.TotalBullets += pl.WeaponInUseLeftHand == null ? AmmoCount : AmmoCount / 2;
                    }
                    if (pl.WeaponInUseLeftHand != null)
                    {
                        if (pl.WeaponInUseLeftHand.ItemName == WeaponName) pl.WeaponInUseLeftHand.TotalBullets += pl.WeaponInUseRightHand == null ? AmmoCount : AmmoCount / 2;
                    }
                    if (WeaponName == "AnyWeapon")
                    {
                        if (pl.WeaponInUseRightHand != null)
                            pl.WeaponInUseRightHand.TotalBullets += pl.WeaponInUseLeftHand == null ? AmmoCount : AmmoCount / 2;
                        if (pl.WeaponInUseLeftHand != null)
                            pl.WeaponInUseLeftHand.TotalBullets += pl.WeaponInUseRightHand == null ? AmmoCount : AmmoCount / 2;
                    }
                    GameObject fx = Instantiate(Effect, transform.position, transform.rotation);
                    Destroy(fx, 5);
                    Destroy(this.gameObject, 0.1f);
                }
            }
        }
    }

}