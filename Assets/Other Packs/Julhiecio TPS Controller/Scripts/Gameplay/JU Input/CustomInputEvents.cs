using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace JUTPS.InputEvents
{
    public class CustomInputEvents : MonoBehaviour
    {
        [Header("Actions")]
        public List<InputEvent> Actions = new List<InputEvent>();
        private void OnEnable()
        {
            foreach (InputEvent action in Actions) action.SetupListeners();
        }
        private void OnDisable()
        {
            foreach (InputEvent action in Actions) action.RemoveListeners();
        }
    }

    [System.Serializable]
    public class InputEvent
    {
#pragma warning disable 0414
        [SerializeField]
        private string EventName = "Input Event";
#pragma warning restore 0414

        [InputControl(layout = "Button")]
        [SerializeField]
        private string Input;
        [HideInInspector] public InputAction targetInput;

        public UnityEngine.Events.UnityEvent OnInputEnter;
        public UnityEngine.Events.UnityEvent OnInputPerformed;
        public UnityEngine.Events.UnityEvent OnInputUp;
        private InputAction GenerateTargetInput()
        {
            targetInput = new InputAction("Actionn", InputActionType.Button, Input, expectedControlType: "Button");
            targetInput.Enable();
            return targetInput;
        }
        public void SetupListeners()
        {
            GenerateTargetInput();

            targetInput.started += OnEnter;
            targetInput.performed += OnPressing;
            targetInput.canceled += OnExit;
        }
        public void RemoveListeners()
        {
            targetInput.started -= OnEnter;
            targetInput.performed -= OnPressing;
            targetInput.canceled -= OnExit;
        }
        private void OnEnter(InputAction.CallbackContext ctx)
        {
            OnInputEnter.Invoke();
        }
        private void OnPressing(InputAction.CallbackContext ctx)
        {
            OnInputPerformed.Invoke();
        }
        private void OnExit(InputAction.CallbackContext ctx)
        {
            OnInputUp.Invoke();
        }
    }
}