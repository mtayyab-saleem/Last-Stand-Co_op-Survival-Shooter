using UnityEngine;
using UnityEngine.EventSystems;

namespace JUTPS.CrossPlataform
{
    [AddComponentMenu("JU TPS/Mobile/Touchfield")]
    public class Touchfield : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        //[HideInInspector]
        public Vector2 TouchDistance;
        [HideInInspector]
        public Vector2 PointerOld;
        [HideInInspector]
        protected int PointerId;
        //[HideInInspector]
        public bool Pressed;

        private PointerEventData touchEventData;
        public void OnDrag(PointerEventData eventData)
        {
            //touchEventData = eventData;
        }

        void Update()
        {
            if (Pressed)
            {
                if (touchEventData != null)
                {
                    TouchDistance = touchEventData.position - PointerOld;
                    PointerOld = touchEventData.position;
                }
                else
                {
                    TouchDistance = Vector2.zero;
                    PointerOld = Vector2.zero;
                }
            }
            else
            {
                TouchDistance = new Vector2();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            touchEventData = eventData;
            Pressed = true;
            PointerId = eventData.pointerId;
            PointerOld = eventData.position;
            TouchDistance = Vector2.zero;
        }


        public void OnPointerUp(PointerEventData eventData)
        {
            Pressed = false;
            PointerOld = eventData.position;
            TouchDistance = Vector2.zero;
            touchEventData = null;
        }
    }

}