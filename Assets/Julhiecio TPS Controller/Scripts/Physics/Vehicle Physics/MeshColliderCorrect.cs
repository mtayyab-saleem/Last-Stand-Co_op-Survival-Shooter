using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.Utilities
{

    [AddComponentMenu("JU TPS/Utilities/Mesh Collider Correct")]
    public class MeshColliderCorrect : MonoBehaviour
    {
        public GameObject Mesh;
        private Transform[] gameobjectschilds;

        void OnCollisionEnter(Collision col)
        {
            //This will make all the gameobjects children of the "Mesh Collider Correct" child of the mesh,
            //this will make the gameobjects (bullet holes for example) depend exclusively on the movement of the vehicle
            if (transform.childCount != 0)
            {
                gameobjectschilds = GetComponentsInChildren<Transform>();

                for (int i = 0; i < gameobjectschilds.Length - 1; i++)
                {
                    if (gameobjectschilds[i] != this.transform)
                    {
                        gameobjectschilds[i].transform.SetParent(Mesh.transform);
                    }
                }
            }
        }
    }

}
