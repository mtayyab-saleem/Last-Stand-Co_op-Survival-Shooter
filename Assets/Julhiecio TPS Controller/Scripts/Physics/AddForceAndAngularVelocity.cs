using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.PhysicsScripts
{

    [AddComponentMenu("JU TPS/Physics/Add Force")]
    public class AddForceAndAngularVelocity : MonoBehaviour
    {
        public Vector3 Force;
        public Vector3 AngularVelocity;

        public bool GenerateRandomForce;
        [Range(0, 300)]
        public float RandomForceRange;

        public bool GenerateRandomAngularVelocity;
        [Range(0, 800)]
        public float RandomAngularVelocityRange;
        void Start()
        {
            var rb = GetComponent<Rigidbody>();

            //Random Generators
            if (GenerateRandomForce)
            {
                Force = new Vector3(Random.Range(-RandomForceRange, RandomForceRange),
                                    Random.Range(-RandomForceRange, RandomForceRange),
                                    Random.Range(-RandomForceRange, RandomForceRange));
            }

            if (GenerateRandomAngularVelocity)
            {
                AngularVelocity = new Vector3(Random.Range(-RandomAngularVelocityRange, RandomAngularVelocityRange),
                                              Random.Range(-RandomAngularVelocityRange, RandomAngularVelocityRange),
                                              Random.Range(-RandomAngularVelocityRange, RandomAngularVelocityRange));
            }

            rb.AddRelativeForce(Force, ForceMode.Impulse);

            rb.angularVelocity = AngularVelocity;
        }
    }

}