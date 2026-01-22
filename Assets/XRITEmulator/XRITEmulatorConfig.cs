using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Scriptable object for configuring emulator settings.
/// </summary>
[CreateAssetMenu(fileName = "XREmulatorConfig", menuName = "XR/Emulator Config")]
public class XREmulatorConfig : ScriptableObject
{
    [Header("Controller Settings")] public Vector3 leftControllerPosition = new(-0.15f, -0.15f, 0.3f);
    public Vector3 leftControllerRotation = new(0f, -10f, 0f);

    public Vector3 rightControllerPosition = new(0.15f, -0.15f, 0.3f);
    public Vector3 rightControllerRotation = new(0f, 10f, 0f);

    [Header("Movement Settings")] public float movementSpeed = 2f;
    [FormerlySerializedAs("verticalMoveSpeed")] public float adjustHeightSpeed = 0.1f;

    public float sensitivity = 0.2f;
    public bool showCursor = true;
    public float verticalAngleTreshold = 80f;

    public Vector3 GetControllerPosition(InteractorHandedness interactorHandedness)
    {
        switch (interactorHandedness)
        {
            case InteractorHandedness.Right:
                return rightControllerPosition;
            case InteractorHandedness.Left:
                return leftControllerPosition;
            default:
                return Vector3.zero;
        }
    }

    public Quaternion GetControllerRotation(InteractorHandedness interactorHandedness)
    {
        switch (interactorHandedness)
        {
            case InteractorHandedness.Right:
                return Quaternion.Euler(rightControllerRotation);
            case InteractorHandedness.Left:
                return Quaternion.Euler(leftControllerRotation);
            default:
                return Quaternion.identity;
        }
    }
}