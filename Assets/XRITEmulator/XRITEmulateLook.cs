using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRITEmulateLook : MonoBehaviour
{
    [SerializeField] private float horizontalRotationMultiplier = 0.2f;
    [SerializeField] private float verticalRotationMultiplier = 0.2f;
    [SerializeField] private bool captureCursor = true;

    [Header("Inputs")] [SerializeField] private InputActionReference lookAroundAction;

    private XROrigin _xrOrigin;
    private Transform BodyRotationTransform => _xrOrigin.transform;
    private Transform CameraRotationTransform => _xrOrigin.CameraFloorOffsetObject.transform;

    private int _lastRotationFrame;

    private bool _wasActive = false;

    // private void Start()
    // {
    //     XRPlayerBase.Instance.Locomotion.AddProviderState(this);
    // }

    private void OnEnable()
    {
        InitXROrigin();
        lookAroundAction.action.performed += DoRotation;
    }

    private void OnDisable()
    {
        lookAroundAction.action.performed -= DoRotation;

        if (_wasActive)
        {
            _wasActive = false;
            OnLookAroundStop();
        }
    }

    private void InitXROrigin()
    {
        _xrOrigin = FindAnyObjectByType<XROrigin>();
    }

    private void DoRotation(InputAction.CallbackContext context)
    {
        Vector2 inputValue = context.ReadValue<Vector2>();
        BodyRotationTransform.Rotate(Vector3.up, inputValue.x * horizontalRotationMultiplier);
        CameraRotationTransform.Rotate(
            Vector3.left * inputValue.y * verticalRotationMultiplier, Space.Self
        );
        _lastRotationFrame = Time.frameCount;
    }

    public bool IsActive()
    {
        //prolong active locomotion for 1 frame after the actual rotation
        return Time.frameCount <= _lastRotationFrame + 1;
    }

    private void LateUpdate()
    {
        bool isActive = IsActive();
        bool wasActive = _wasActive;
        _wasActive = isActive;

        if (isActive != wasActive)
        {
            if (isActive)
                OnLookAroundStart();
            else
                OnLookAroundStop();
        }
    }

    private void OnLookAroundStart()
    {
        if (captureCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void OnLookAroundStop()
    {
        if (captureCursor)
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
}