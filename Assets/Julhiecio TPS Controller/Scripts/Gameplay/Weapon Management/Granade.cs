using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.ItemSystem;
using JUTPSEditor.JUHeader;

namespace JUTPS.WeaponSystem
{

    [AddComponentMenu("JU TPS/Weapon System/Granade")]
    public class Granade : ThrowableItem
    {
        [JUHeader("Granade Settings")]
        public GameObject ExplosionPrefab;
        public float TimeToExplode;
        public float TimeToDestroyExplosionPrefab = 5;
        private float currentTimeToExplode;

        public override void Update()
        {
            base.Update();

            if (IsThrowed == true)
            {
                //Timer
                currentTimeToExplode += Time.deltaTime;
                if (currentTimeToExplode >= TimeToExplode)
                {
                    //Spawn a explosion Prefab
                    GameObject explosion = Instantiate(ExplosionPrefab, transform.position, Quaternion.identity);

                    //Destroy explosion prefab at seconds
                    Destroy(explosion, TimeToDestroyExplosionPrefab);

                    //Destroy granade imediately
                    Destroy(gameObject);
                }
            }
        }
    }

}
