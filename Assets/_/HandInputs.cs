using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "NStuff/XR Player/Hand Inputs", fileName = "HandInputs")]
public class HandInputs : ScriptableObject
{
    [SerializeField] private InputActionReference triggerInputButton;
    [SerializeField] private InputActionReference triggerInputValue;
    [SerializeField] private InputActionReference gripInputButton;
    [SerializeField] private InputActionReference gripInputValue;
    [SerializeField] private InputActionReference thumbTouch;
    [SerializeField] private InputActionReference primaryButton;

    public InputActionReference TriggerInputButton => triggerInputButton;
    public bool TriggerPressed => triggerInputButton.action.IsPressed();
    public float TriggerValue => triggerInputValue.action.ReadValue<float>();

    public InputActionReference GripInputButton => gripInputButton;
    public bool GripPressed => gripInputButton.action.IsPressed();
    public float GripValue => gripInputValue.action.ReadValue<float>();

    public bool ThumbTouched => thumbTouch.action.IsPressed();
    public InputActionReference PrimaryButton => primaryButton;
}