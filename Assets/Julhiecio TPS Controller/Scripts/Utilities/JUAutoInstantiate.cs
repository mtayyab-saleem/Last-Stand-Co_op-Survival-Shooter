using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JUTPSEditor.JUHeader;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/Auto Instantiate")]
    public class JUAutoInstantiate : MonoBehaviour
    {
        [JUHeader("Auto Instantiate Prefab")]
        public GameObject Prefab;
        public bool StartInstantiateOnAwake = true;
        public float TimeToSpawn = 2;
        public bool Repeat;
        public float RepeatingTime = 1;
        public float InstanceLifeTime = -1;

        [JUHeader("Random Options")]
        public bool SwitchToRandomInstantiate;
        public GameObject[] PrefabsToInstantiate;
        public Vector3 SpawnArea;
        public Vector3 PositionOffset;
        public bool RandomRotation = true;
        public int Quantity = 1;
        public int InstancesLimit = 32;

        [Range(0, 100)]
        public float EmptyInstantiatePorcentage = 0;
        public UnityEvent OnInstantiate;

        private List<GameObject> Spawneds = new List<GameObject>();
        private void Start()
        {
            if (StartInstantiateOnAwake == false) return;

            if (Repeat == true)
            {
                InvokeRepeating(nameof(InstantiatePrefab), TimeToSpawn, RepeatingTime);
            }
            else
            {
                Invoke(nameof(InstantiatePrefab), TimeToSpawn);
            }
        }
        public void InstantiatePrefab()
        {
            ClearEmpty();

            if (EmptyInstantiatePorcentage == 100) return;

            if (EmptyInstantiatePorcentage > 0)
            {
                float chance = Random.Range(0, 100);
                if (chance < EmptyInstantiatePorcentage) return;
            }

            if (Spawneds.Count - 1 > InstancesLimit && InstancesLimit > 0) return;


            if (SwitchToRandomInstantiate == false)
            {
                GameObject new_instance = Instantiate(Prefab, transform.position, transform.rotation);
                if (InstanceLifeTime > 0) { Destroy(new_instance, InstanceLifeTime); }

                Spawneds.Add(new_instance);
            }
            else
            {
                Quantity = Mathf.Clamp(Quantity, 0, InstancesLimit);
                for (int i = 0; i < Quantity; i++)
                {
                    Vector3 randomPosOnArea = transform.position;
                    randomPosOnArea.x += Random.Range(-SpawnArea.x, SpawnArea.x);
                    randomPosOnArea.y += Random.Range(-SpawnArea.y, SpawnArea.y);
                    randomPosOnArea.z += Random.Range(-SpawnArea.z, SpawnArea.z);

                    int idToInstantiate = Random.Range(0, PrefabsToInstantiate.Length - 1);
                    GameObject new_instance = Instantiate(PrefabsToInstantiate[idToInstantiate], randomPosOnArea + PositionOffset, RandomRotation ? Quaternion.Euler(0, Random.Range(-360, 360), 0) : PrefabsToInstantiate[idToInstantiate].transform.rotation);

                    if (InstanceLifeTime > 0) { Destroy(new_instance, InstanceLifeTime); }

                    Spawneds.Add(new_instance);
                }
            }

            if (Spawneds.Count - 1 > InstancesLimit && InstancesLimit > 0)
            {
                for (int id = InstancesLimit; id < Spawneds.Count - 1; id++)
                {
                    Destroy(Spawneds[id]);
                    Spawneds.RemoveAt(id);
                }
            }

            OnInstantiate.Invoke();
        }
        private void ClearEmpty()
        {
            foreach (GameObject instance in Spawneds.ToArray())
            {
                if (instance == null) Spawneds.Remove(instance);
            }
        }
        public void SetRepeatingTime(float time)
        {
            RepeatingTime = time;
            CancelInvoke(nameof(InstantiatePrefab));
            InvokeRepeating(nameof(InstantiatePrefab), RepeatingTime, RepeatingTime);
        }
        public void AddTime(float time)
        {
            SetRepeatingTime(RepeatingTime + time);
        }
        public void AddQuantity(int Quantitites)
        {
            Quantity += Quantitites;
        }


        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position + PositionOffset, SpawnArea);
        }
    }
}