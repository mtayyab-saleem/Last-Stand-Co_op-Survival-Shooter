using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace JUTPS.Events
{
    [AddComponentMenu("JU TPS/Third Person System/Additionals/JU Animation Event Receiver")]
    public class JUAnimationEventReceiver : MonoBehaviour
    {
        public JUAnimationEvent[] JUAnimationEvents;
        public void CallEvent(string EventName)
        {
            foreach (JUAnimationEvent juevent in JUAnimationEvents)
            {
                if (juevent.EventName == EventName)
                {
                    juevent.Event.Invoke();
                    return;
                }
            }

            Debug.LogError("There is no animation event in the named list of '" + EventName + "'");
        }

    }

    [System.Serializable]
    public class JUAnimationEvent
    {
        public string EventName;
        public UnityEvent Event;
    }
}