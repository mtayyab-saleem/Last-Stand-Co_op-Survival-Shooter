using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace JUTPS.UI
{

    public class OnScreenGameobjectDetector : MonoBehaviour
    {
        [Header("Detect")]
        [SerializeField] private string[] DetectGameobjectWithTags = new string[] { "Untagged" };
        [SerializeField] private float DetectRadius = 2;
        [SerializeField] private float RefreshRate = 0.2f;
        [SerializeField] private LayerMask Layer;
        public GameObject DetectorCenter;

        [Header("Warnings")]
        [SerializeField] private GameObject WarningPrefab;
        [SerializeField] private Vector3 PositionOffset;

        private Collider[] detectedObjects = new Collider[] { };
        private List<GameObject> warnings = new List<GameObject>();
        private void Start()
        {
            if (DetectorCenter == null) DetectorCenter = GameObject.FindGameObjectWithTag("Player");

            InvokeRepeating("Detect", RefreshRate, RefreshRate);
        }
        private void LateUpdate()
        {
            RefreshWarningsCount();
            RefreshWarningPositions();
        }

        private void RefreshWarningPositions()
        {
            if (warnings.Count == 0 || detectedObjects.Length == 0) return;

            for (int i = 0; i < warnings.Count; i++)
            {
                if (warnings[i] == null || detectedObjects[i] == null) return;

                UIElementToWorldPosition.SetUIWorldPosition(warnings[i], detectedObjects[i].transform.position, PositionOffset, ClampOffscreen: false);
                //warnings[i].transform.localScale = Vector3.Lerp(warnings[i].transform.localScale, new Vector3(1,1,1), 5 * Time.deltaTime);
            }
        }
        private void RefreshWarningsCount()
        {
            if (detectedObjects.Length == 0)
            {
                DisableAllWarnings();
            }
            if (warnings.Count != detectedObjects.Length)
            {
                foreach (GameObject w in warnings.ToArray())
                {
                    Destroy(w);
                }
                warnings.Clear();

                foreach (Collider fluffies in detectedObjects)
                {
                    GameObject warning = Instantiate(WarningPrefab, Vector3.zero, WarningPrefab.transform.rotation, transform);
                    warnings.Add(warning);
                }
            }
        }
        private void DisableAllWarnings()
        {
            foreach (GameObject w in warnings.ToArray())
            {
                w.SetActive(true);
            }
        }
        private void Detect()
        {
            List<Collider> detectedColliders = Physics.OverlapSphere(DetectorCenter.transform.position + transform.up, DetectRadius, Layer).ToList();

            foreach (Collider collider in detectedColliders.ToArray())
            {
                if (TheTagMatches(collider.tag) == false)
                {
                    detectedColliders.Remove(collider);
                }
            }

            detectedObjects = detectedColliders.ToArray();
        }

        public bool TheTagMatches(string objectTag)
        {
            bool matches = false;

            foreach (string tag in DetectGameobjectWithTags)
            {
                if (objectTag == tag) matches = true;
            }

            return matches;
        }

    }

}