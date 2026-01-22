using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRITEmulateLook : MonoBehaviour
{
    [SerializeField] private InputActionReference lookAroundAction;

    private XROrigin _xrOrigin;
    private Transform _bodyTransform;
    private Transform _cameraTransform;

    private float _currentVerticalRotation;
    private int _lastRotationFrame;
    private bool _wasActive;

    float Sensitivity => XRITEmulator.Instance.Config.sensitivity;
    float VerticalAngleTresholdTreshold => XRITEmulator.Instance.Config.verticalAngleTreshold;
    bool ShowCursor => XRITEmulator.Instance.Config.showCursor;
    
    private void OnEnable()
    {
        InitializeXROrigin();

        if (lookAroundAction?.action != null)
            lookAroundAction.action.performed += OnLookPerformed;
    }

    private void OnDisable()
    {
        if (lookAroundAction?.action != null)
            lookAroundAction.action.performed -= OnLookPerformed;

        if (_wasActive)
        {
            _wasActive = false;
            ReleaseCursor();
        }
    }

    private void InitializeXROrigin()
    {
        _xrOrigin = FindAnyObjectByType<XROrigin>();
        if (_xrOrigin != null)
        {
            _bodyTransform = _xrOrigin.transform;
            _cameraTransform = _xrOrigin.CameraFloorOffsetObject.transform;
            _currentVerticalRotation = _cameraTransform.localEulerAngles.x;

            // Normalize to -180 to 180 range
            if (_currentVerticalRotation > 180f)
                _currentVerticalRotation -= 360f;
        }
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        if (_bodyTransform == null || _cameraTransform == null) return;

        Vector2 input = ctx.ReadValue<Vector2>();

        // Horizontal rotation (body)
        _bodyTransform.Rotate(Vector3.up, input.x * Sensitivity, Space.World);

        // Vertical rotation (camera) with clamping
        float verticalDelta = -input.y * Sensitivity;

        _currentVerticalRotation = Mathf.Clamp(
            _currentVerticalRotation + verticalDelta,
            -VerticalAngleTresholdTreshold,
            VerticalAngleTresholdTreshold
        );

        _cameraTransform.localRotation = Quaternion.Euler(_currentVerticalRotation, 0f, 0f);
        _lastRotationFrame = Time.frameCount;
    }

    public bool IsActive() => Time.frameCount <= _lastRotationFrame + 1;

    private void LateUpdate()
    {
        bool isActive = IsActive();

        if (isActive != _wasActive)
        {
            _wasActive = isActive;

            if (isActive)
                CaptureCursor();
            else
                ReleaseCursor();
        }
    }

    private void CaptureCursor()
    {
        if (ShowCursor)
            Cursor.lockState = CursorLockMode.Locked;
    }

    private void ReleaseCursor()
    {
        if (ShowCursor)
            Cursor.lockState = CursorLockMode.None;
    }
}