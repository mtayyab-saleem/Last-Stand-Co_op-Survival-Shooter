using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Utilities/Hide UI")]
    public class HideUI : MonoBehaviour
    {
        [Header("Press Tab to hide target gameobject")]
        public GameObject target;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                target.SetActive(!target.activeSelf);
        }
        public void Hide(bool hide)
        {
            target.SetActive(!hide);
        }
    }
}
