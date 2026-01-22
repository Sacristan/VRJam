using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using CommonUsages = UnityEngine.InputSystem.CommonUsages;

[DefaultExecutionOrder(XRInteractionUpdateOrder.k_DeviceSimulator)]
public class XRITEmulateController : MonoBehaviour
{
    [SerializeField] private InteractorHandedness side = InteractorHandedness.Left;

    [SerializeField] private Vector3 controllerPos = new(-0.15f, -0.15f, 0.3f); //invert x value for Right controller

    [SerializeField] private Vector3 controllerRot = new(0f, -10f, 0f); //invert y value for Right controller

    [Header("Inputs")] [SerializeField] private InputActionReference gripAction;
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference toggleActiveGripAction;
    [SerializeField] private InputActionReference joystickAction;

    private XRSimulatedControllerState _controllerState;
    private XRSimulatedController _controllerDevice;

    private bool _gripInputActive;
    private bool _triggerInputActive;
    private bool _lockActiveGrip;
    private bool _joystickInputActive;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Starting XRITEmulateController");
    }

    private void OnEnable()
    {
        Set();

        gripAction.action.performed += ActivateGripInput;
        gripAction.action.canceled += DeactivateGripInput;

        triggerAction.action.performed += ActivateTriggerInput;
        triggerAction.action.canceled += DeactivateTriggerInput;

        toggleActiveGripAction.action.performed += ToggleLockActiveGrip;

        joystickAction.action.performed += ActivateJoystickInput;
        joystickAction.action.canceled += DeactivateJoystickInput;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Set();
        }
        else
        {
            Unset();
        }
    }

    void Set()
    {
        if (_controllerDevice != null)
        {
            DestroyControllerDevice();
        }

        CreateControllerDevice();

        _controllerState.Reset();
        _controllerState.devicePosition = controllerPos;
        _controllerState.deviceRotation = Quaternion.Euler(controllerRot);
        _controllerState.isTracked = true;
    }

    void Unset()
    {
        DestroyControllerDevice();
    }

    private void OnDisable()
    {
        Unset();

        gripAction.action.performed -= ActivateGripInput;
        gripAction.action.canceled -= DeactivateGripInput;

        triggerAction.action.performed -= ActivateTriggerInput;
        triggerAction.action.canceled -= DeactivateTriggerInput;

        toggleActiveGripAction.action.performed -= ToggleLockActiveGrip;

        joystickAction.action.performed -= ActivateJoystickInput;
        joystickAction.action.canceled -= DeactivateJoystickInput;
    }

    private void CreateControllerDevice()
    {
        InternedString usage = DefineUsage();
        InputDeviceDescription descRightHand = new InputDeviceDescription
        {
            product = nameof(XRSimulatedController),
            capabilities = new XRDeviceDescriptor
            {
                deviceName = $"{nameof(XRSimulatedController)} - {usage}",
                characteristics = DefineInputDeviceCharacteristics(),
            }.ToJson(),
        };

        // SimulatedInputLayoutLoader already registered layout so device can be added
        _controllerDevice = InputSystem.AddDevice(descRightHand) as XRSimulatedController;
        InputSystem.SetDeviceUsage(_controllerDevice, usage);
    }

    private InternedString DefineUsage()
    {
        switch (side)
        {
            case InteractorHandedness.Left: return CommonUsages.LeftHand;
            case InteractorHandedness.Right: return CommonUsages.RightHand;

            default: throw new ArgumentOutOfRangeException();
        }
    }

    private InputDeviceCharacteristics DefineInputDeviceCharacteristics()
    {
        switch (side)
        {
            case InteractorHandedness.Left: return XRInputTrackingAggregator.Characteristics.leftController;
            case InteractorHandedness.Right: return XRInputTrackingAggregator.Characteristics.rightController;

            default: throw new ArgumentOutOfRangeException();
        }
    }

    private void DestroyControllerDevice()
    {
        Debug.Log($"{nameof(DestroyControllerDevice)}");
        InputSystem.RemoveDevice(_controllerDevice);
        _controllerDevice = null;
    }

    private void ActivateGripInput(InputAction.CallbackContext ctx)
    {
        UnlockActiveGrip();
        _gripInputActive = true;
    }

    private void DeactivateGripInput(InputAction.CallbackContext ctx)
    {
        if (!_lockActiveGrip)
        {
            _gripInputActive = false;
        }
    }

    private void ActivateTriggerInput(InputAction.CallbackContext ctx)
    {
        _triggerInputActive = true;
    }

    private void DeactivateTriggerInput(InputAction.CallbackContext ctx)
    {
        _triggerInputActive = false;
    }

    private void ActivateJoystickInput(InputAction.CallbackContext ctx)
    {
        _joystickInputActive = true;
    }

    private void DeactivateJoystickInput(InputAction.CallbackContext ctx)
    {
        _joystickInputActive = false;
    }

    private void ToggleLockActiveGrip(InputAction.CallbackContext ctx)
    {
        bool newValue = !_lockActiveGrip;
        if (newValue && _gripInputActive)
        {
            _lockActiveGrip = true;
            Debug.Log("Active grip locked");
        }
        else
        {
            UnlockActiveGrip();
        }
    }

    private void UnlockActiveGrip()
    {
        if (_lockActiveGrip)
        {
            _lockActiveGrip = false;
            _gripInputActive = false;
            Debug.Log("Active grip unlocked");
        }
    }

    private void Update()
    {
        ProcessControlInput();
        InputState.Change(_controllerDevice, _controllerState);
    }

    protected virtual void ProcessControlInput()
    {
        _controllerState.grip = _gripInputActive ? 1f : 0f;
        _controllerState.WithButton(ControllerButton.GripButton, _gripInputActive);
        _controllerState.trigger = _triggerInputActive ? 1f : 0f;
        _controllerState.WithButton(ControllerButton.TriggerButton, _triggerInputActive);
        _controllerState.primary2DAxis = _joystickInputActive
            ? joystickAction.action.ReadValue<Vector2>()
            : Vector2.zero;
    }
}