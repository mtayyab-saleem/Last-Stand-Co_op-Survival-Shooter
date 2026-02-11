using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.InputEvents;
using UnityEngine.Events;

namespace JUTPS.InputEvents
{

    [System.Serializable]
    public class MultipleActionEvent
    {
        public List<InputEvent> Actions;

        public UnityEvent OnButtonsDown;
        public UnityEvent OnButtonsPressing;
        public UnityEvent OnButtonsUp;

        public void Enable()
        {
            foreach (InputEvent action in Actions) action.SetupListeners();
            AddListenersToEvents();
        }
        public void Disable()
        {
            foreach (InputEvent action in Actions) action.RemoveListeners();
            RemoveListenersToEvent();
        }

        public void AddListenersToEvents()
        {
            foreach (InputEvent action in Actions)
            {
                action.OnInputEnter.AddListener(OnButtonsDown.Invoke);
                action.OnInputPerformed.AddListener(OnButtonsPressing.Invoke);
                action.OnInputUp.AddListener(OnButtonsUp.Invoke);
            }
        }
        public void RemoveListenersToEvent()
        {
            foreach (InputEvent action in Actions)
            {
                action.OnInputEnter.RemoveListener(OnButtonsDown.Invoke);
                action.OnInputPerformed.RemoveListener(OnButtonsPressing.Invoke);
                action.OnInputUp.RemoveListener(OnButtonsUp.Invoke);
            }
        }
    }

    public class MultipleInputEvent : MonoBehaviour
    {
        public List<InputEvent> Actions;

        public UnityEvent OnButtonsDown;
        public UnityEvent OnButtonsPressing;
        public UnityEvent OnButtonsUp;

        public void SetupListeners()
        {
            foreach (InputEvent action in Actions) action.SetupListeners();

            AddListenersToEvents();
        }
        private void OnEnable()
        {
            foreach (InputEvent action in Actions) action.SetupListeners();

            AddListenersToEvents();
        }
        private void OnDisable()
        {
            foreach (InputEvent action in Actions) action.RemoveListeners();


            RemoveListenersToEvent();
        }

        private void AddListenersToEvents()
        {
            foreach (InputEvent action in Actions)
            {
                action.OnInputEnter.AddListener(OnButtonsDown.Invoke);
                action.OnInputPerformed.AddListener(OnButtonsPressing.Invoke);
                action.OnInputUp.AddListener(OnButtonsUp.Invoke);
            }
        }
        private void RemoveListenersToEvent()
        {
            foreach (InputEvent action in Actions)
            {
                action.OnInputEnter.RemoveListener(OnButtonsDown.Invoke);
                action.OnInputPerformed.RemoveListener(OnButtonsPressing.Invoke);
                action.OnInputUp.RemoveListener(OnButtonsUp.Invoke);
            }
        }
    }

}