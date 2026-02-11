using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.UI
{
    public class UIElementToWorldPosition : MonoBehaviour
    {
        public Vector3 WorldPosition;
        public Vector3 Offset;
        Camera cam;
        void Start()
        {
            cam = Camera.main;
        }
        void Update()
        {
            if (cam == null) return;

            Vector3 onScreenPosition = cam.WorldToScreenPoint(WorldPosition + Offset);
            if (transform.position != onScreenPosition) transform.position = onScreenPosition;
        }

        public static void SetUIWorldPosition(GameObject UIElement, Vector3 position, Vector3 offset, bool ClampOffscreen = false, float OffScreenOffset = 20)
        {
            Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(position + offset);

            if (UIElement.transform.position != onScreenPosition) UIElement.transform.position = onScreenPosition;

            if (ClampOffscreen)
            {
                RectTransform rt = UIElement.GetComponent<RectTransform>();
                RectTransform Parent = rt.parent.GetComponentInParent<RectTransform>();
                float screenWidth = Parent.rect.width / 2;
                float screenHeight = Parent.rect.height / 2;

                float clampedPositionX = Mathf.Clamp(rt.localPosition.x, -screenWidth+OffScreenOffset, screenWidth-OffScreenOffset);
                float clampedPositionY = Mathf.Clamp(rt.localPosition.y, -screenHeight+OffScreenOffset, screenHeight-OffScreenOffset);

                Vector3 rectPosition = new Vector3(clampedPositionX, clampedPositionY, rt.localPosition.z);
                
                rt.localPosition = rectPosition;
            }
        }
    }
}
