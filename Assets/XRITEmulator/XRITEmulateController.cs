using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

public class ControllerInputState
{
    public bool GripActive { get; private set; }
    public bool TriggerActive { get; private set; }
    public bool JoystickActive { get; private set; }
    public bool PrimaryActive { get; private set; }
    public bool SecondaryActive { get; private set; }
    public bool GripLocked { get; private set; }

    public void ActivateGrip(InputAction.CallbackContext ctx)
    {
        UnlockGrip();
        GripActive = true;
    }

    public void DeactivateGrip(InputAction.CallbackContext ctx)
    {
        if (!GripLocked)
            GripActive = false;
    }

    public void ToggleLockGrip(InputAction.CallbackContext ctx)
    {
        if (!GripLocked && GripActive)
        {
            GripLocked = true;
            Debug.Log("Grip locked");
        }
        else
        {
            UnlockGrip();
        }
    }

    private void UnlockGrip()
    {
        if (GripLocked)
        {
            GripLocked = false;
            GripActive = false;
            Debug.Log("Grip unlocked");
        }
    }

    public void ActivateTrigger(InputAction.CallbackContext ctx) => TriggerActive = true;
    public void DeactivateTrigger(InputAction.CallbackContext ctx) => TriggerActive = false;

    public void ActivateJoystick(InputAction.CallbackContext ctx) => JoystickActive = true;
    public void DeactivateJoystick(InputAction.CallbackContext ctx) => JoystickActive = false;

    public void ActivatePrimary(InputAction.CallbackContext ctx) => PrimaryActive = true;
    public void DeactivatePrimary(InputAction.CallbackContext ctx) => PrimaryActive = false;

    public void ActivateSecondary(InputAction.CallbackContext ctx) => SecondaryActive = true;
    public void DeactivateSecondary(InputAction.CallbackContext ctx) => SecondaryActive = false;
}

[DefaultExecutionOrder(XRInteractionUpdateOrder.k_DeviceSimulator)]
public class XRITEmulateController : MonoBehaviour
{
    [SerializeField] private InteractorHandedness side = InteractorHandedness.Left;

    [SerializeField] private InputActionReference gripAction;
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference toggleActiveGripAction;
    [SerializeField] private InputActionReference joystickAction;
    [SerializeField] private InputActionReference primaryButtonAction;
    [SerializeField] private InputActionReference secondaryButtonAction;

    private XRSimulatedControllerState _controllerState;
    private XRSimulatedController _controllerDevice;

    private ControllerInputState _inputState = new();

    private bool _isInitialized;

    public InteractorHandedness Side => side;

    private void OnEnable()
    {
        StartCoroutine(InitRoutine());

        IEnumerator InitRoutine()
        {
            yield return new WaitUntil(() => XRITEmulator.IsReady);

            if (!_isInitialized)
            {
                InitializeController();
                _isInitialized = true;
            }

            SubscribeToInputActions();
        }
    }


    private void OnDisable()
    {
        UnsubscribeFromInputActions();
        DestroyControllerDevice();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!_isInitialized) return;

        if (hasFocus)
            InitializeController();
        else
            DestroyControllerDevice();
    }

    private void InitializeController()
    {
        if (_controllerDevice != null)
            DestroyControllerDevice();

        CreateControllerDevice();
        ResetControllerState();
    }

    private void ResetControllerState()
    {
        _controllerState.Reset();
        _controllerState.devicePosition = XRITEmulator.Instance.Config.GetControllerPosition(side);
        _controllerState.deviceRotation = XRITEmulator.Instance.Config.GetControllerRotation(side);
        _controllerState.isTracked = true;
    }

    private void CreateControllerDevice()
    {
        var usage = GetControllerUsage();
        var deviceDesc = new InputDeviceDescription
        {
            product = nameof(XRSimulatedController),
            capabilities = new XRDeviceDescriptor
            {
                deviceName = $"{nameof(XRSimulatedController)} - {usage}",
                characteristics = GetInputDeviceCharacteristics(),
            }.ToJson(),
        };

        _controllerDevice = InputSystem.AddDevice(deviceDesc) as XRSimulatedController;
        if (_controllerDevice != null)
        {
            InputSystem.SetDeviceUsage(_controllerDevice, usage);
        }
    }

    private InternedString GetControllerUsage()
    {
        return side == InteractorHandedness.Left
            ? CommonUsages.LeftHand
            : CommonUsages.RightHand;
    }

    private InputDeviceCharacteristics GetInputDeviceCharacteristics()
    {
        return side == InteractorHandedness.Left
            ? XRInputTrackingAggregator.Characteristics.leftController
            : XRInputTrackingAggregator.Characteristics.rightController;
    }

    private void DestroyControllerDevice()
    {
        if (_controllerDevice != null)
        {
            InputSystem.RemoveDevice(_controllerDevice);
            _controllerDevice = null;
        }
    }

    private void SubscribeToInputActions()
    {
        SubscribeAction(gripAction, _inputState.ActivateGrip, _inputState.DeactivateGrip);
        SubscribeAction(triggerAction, _inputState.ActivateTrigger, _inputState.DeactivateTrigger);
        SubscribeAction(toggleActiveGripAction, _inputState.ToggleLockGrip, null);
        SubscribeAction(joystickAction, _inputState.ActivateJoystick, _inputState.DeactivateJoystick);
        SubscribeAction(primaryButtonAction, _inputState.ActivatePrimary, _inputState.DeactivatePrimary);
        SubscribeAction(secondaryButtonAction, _inputState.ActivateSecondary, _inputState.DeactivateSecondary);
    }

    private void UnsubscribeFromInputActions()
    {
        UnsubscribeAction(gripAction, _inputState.ActivateGrip, _inputState.DeactivateGrip);
        UnsubscribeAction(triggerAction, _inputState.ActivateTrigger, _inputState.DeactivateTrigger);
        UnsubscribeAction(toggleActiveGripAction, _inputState.ToggleLockGrip, null);
        UnsubscribeAction(joystickAction, _inputState.ActivateJoystick, _inputState.DeactivateJoystick);
        UnsubscribeAction(primaryButtonAction, _inputState.ActivatePrimary, _inputState.DeactivatePrimary);
        UnsubscribeAction(secondaryButtonAction, _inputState.ActivateSecondary, _inputState.DeactivateSecondary);
    }

    private void SubscribeAction(InputActionReference actionRef,
        Action<InputAction.CallbackContext> performed,
        Action<InputAction.CallbackContext> canceled)
    {
        if (actionRef?.action == null) return;

        if (performed != null)
            actionRef.action.performed += performed;
        if (canceled != null)
            actionRef.action.canceled += canceled;
    }

    private void UnsubscribeAction(InputActionReference actionRef,
        Action<InputAction.CallbackContext> performed,
        Action<InputAction.CallbackContext> canceled)
    {
        if (actionRef?.action == null) return;

        if (performed != null)
            actionRef.action.performed -= performed;
        if (canceled != null)
            actionRef.action.canceled -= canceled;
    }

    private void Update()
    {
        if (_controllerDevice == null) return;

        UpdateControllerState();
        InputState.Change(_controllerDevice, _controllerState);
    }

    private void UpdateControllerState()
    {
        // Grip
        _controllerState.grip = _inputState.GripActive ? 1f : 0f;
        _controllerState.WithButton(ControllerButton.GripButton, _inputState.GripActive);

        // Trigger
        _controllerState.trigger = _inputState.TriggerActive ? 1f : 0f;
        _controllerState.WithButton(ControllerButton.TriggerButton, _inputState.TriggerActive);

        // Joystick
        _controllerState.primary2DAxis = _inputState.JoystickActive && joystickAction?.action != null
            ? joystickAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        // Buttons
        _controllerState.WithButton(ControllerButton.PrimaryButton, _inputState.PrimaryActive);
        _controllerState.WithButton(ControllerButton.SecondaryButton, _inputState.SecondaryActive);
    }
}