using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Flashing Glow")]
    public class GlowBlink : MonoBehaviour
    {
        private Renderer[] Meshes;
        public Color EmissiveColor = Color.white;
        [Range(0, 10)]
        public float EmissiveIntensity = 0.5f;
        public float Interval = 2;
        public float Speed = 5;
        private float EmissiveValue;
        private bool IsBlinking;
        private float currentime;
        void Start()
        {
            Meshes = transform.GetComponentsInChildren<Renderer>();
            foreach (var mesh in Meshes)
            {
                for (int i = 0; i < mesh.sharedMaterials.Length; i++)
                {
                    Material newCopyFromOriginalMaterial = Instantiate(mesh.sharedMaterials[i]);
                    mesh.sharedMaterials[i] = newCopyFromOriginalMaterial;
                    mesh.sharedMaterials[i].EnableKeyword("_EMISSION");
                }
                /*foreach (Material m in mesh.sharedMaterials) {
                    mesh.sharedMaterials[0] = Instantiate(m);
                    m.EnableKeyword("_EMISSION");
                }*/
            }
        }

        // Update is called once per frame
        void Update()
        {

            if (IsBlinking)
            {
                EmissiveValue = Mathf.MoveTowards(EmissiveValue, 1, Speed * Time.deltaTime);
            }
            else
            {
                EmissiveValue = Mathf.MoveTowards(EmissiveValue, 0, Speed * Time.deltaTime);
            }

            if (currentime < Interval)
            {
                currentime += Time.deltaTime;
                if (EmissiveValue >= 1) IsBlinking = false;
            }
            else
            {
                IsBlinking = true;
                currentime = 0;
            }


            foreach (var meshes in Meshes)
            {
                foreach (Material mat in meshes.materials)
                {
                    mat.SetColor("_EmissionColor", EmissiveColor * (EmissiveValue * EmissiveIntensity));
                }
            }
        }
        public void DisableEmission()
        {
            if (Meshes == null) return;
            foreach (var meshes in Meshes)
            {
                foreach (Material mat in meshes.sharedMaterials)
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.clear);
                }
            }
        }
        private void OnDestroy()
        {
            DisableEmission();
        }
        private void OnEnable()
        {
            DisableEmission();
        }
    }
}