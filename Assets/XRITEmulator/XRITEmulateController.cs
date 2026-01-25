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

public class EmulatedControllerInputState
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

    private XRSimulatedController _xrSimulatedController;
    private XRSimulatedControllerState _xrSimulatedControllerState;

    private readonly EmulatedControllerInputState _emulatedControllerInputState = new();

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
        // else
        //     DestroyControllerDevice();
    }

    private void InitializeController()
    {
        if (_xrSimulatedController != null)
            DestroyControllerDevice();

        CreateControllerDevice();
        ResetControllerState();
    }

    private void ResetControllerState()
    {
        _xrSimulatedControllerState.Reset();
        _xrSimulatedControllerState.devicePosition = XRITEmulator.Instance.Config.GetControllerPosition(side);
        _xrSimulatedControllerState.deviceRotation = XRITEmulator.Instance.Config.GetControllerRotation(side);
        _xrSimulatedControllerState.isTracked = true;
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

        _xrSimulatedController = InputSystem.AddDevice(deviceDesc) as XRSimulatedController;
        if (_xrSimulatedController != null)
        {
            InputSystem.SetDeviceUsage(_xrSimulatedController, usage);
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
        if (_xrSimulatedController != null)
        {
            InputSystem.RemoveDevice(_xrSimulatedController);
            _xrSimulatedController = null;
        }
    }

    private void SubscribeToInputActions()
    {
        SubscribeAction(gripAction, _emulatedControllerInputState.ActivateGrip, _emulatedControllerInputState.DeactivateGrip);
        SubscribeAction(triggerAction, _emulatedControllerInputState.ActivateTrigger, _emulatedControllerInputState.DeactivateTrigger);
        SubscribeAction(toggleActiveGripAction, _emulatedControllerInputState.ToggleLockGrip, null);
        SubscribeAction(joystickAction, _emulatedControllerInputState.ActivateJoystick, _emulatedControllerInputState.DeactivateJoystick);
        SubscribeAction(primaryButtonAction, _emulatedControllerInputState.ActivatePrimary, _emulatedControllerInputState.DeactivatePrimary);
        SubscribeAction(secondaryButtonAction, _emulatedControllerInputState.ActivateSecondary, _emulatedControllerInputState.DeactivateSecondary);
    }

    private void UnsubscribeFromInputActions()
    {
        UnsubscribeAction(gripAction, _emulatedControllerInputState.ActivateGrip, _emulatedControllerInputState.DeactivateGrip);
        UnsubscribeAction(triggerAction, _emulatedControllerInputState.ActivateTrigger, _emulatedControllerInputState.DeactivateTrigger);
        UnsubscribeAction(toggleActiveGripAction, _emulatedControllerInputState.ToggleLockGrip, null);
        UnsubscribeAction(joystickAction, _emulatedControllerInputState.ActivateJoystick, _emulatedControllerInputState.DeactivateJoystick);
        UnsubscribeAction(primaryButtonAction, _emulatedControllerInputState.ActivatePrimary, _emulatedControllerInputState.DeactivatePrimary);
        UnsubscribeAction(secondaryButtonAction, _emulatedControllerInputState.ActivateSecondary, _emulatedControllerInputState.DeactivateSecondary);
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
        if (_xrSimulatedController == null) return;

        UpdateControllerState();
        InputState.Change(_xrSimulatedController, _xrSimulatedControllerState);
    }

    private void UpdateControllerState()
    {
        // Grip
        _xrSimulatedControllerState.grip = _emulatedControllerInputState.GripActive ? 1f : 0f;
        _xrSimulatedControllerState.WithButton(ControllerButton.GripButton, _emulatedControllerInputState.GripActive);

        // Trigger
        _xrSimulatedControllerState.trigger = _emulatedControllerInputState.TriggerActive ? 1f : 0f;
        _xrSimulatedControllerState.WithButton(ControllerButton.TriggerButton, _emulatedControllerInputState.TriggerActive);

        // Joystick
        _xrSimulatedControllerState.primary2DAxis = _emulatedControllerInputState.JoystickActive && joystickAction?.action != null
            ? joystickAction.action.ReadValue<Vector2>()
            : Vector2.zero;

        // Buttons
        _xrSimulatedControllerState.WithButton(ControllerButton.PrimaryButton, _emulatedControllerInputState.PrimaryActive);
        _xrSimulatedControllerState.WithButton(ControllerButton.SecondaryButton, _emulatedControllerInputState.SecondaryActive);
    }
}