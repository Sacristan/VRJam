using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class XRITEmulateMovement : MonoBehaviour
{
    [SerializeField] private InputActionReference movementAction;
    [SerializeField] private InputActionReference upDownAction;

    private ContinuousMoveProvider _moveProvider;
    private Transform _xrOriginTransform;

    private float _defaultMoveSpeed;
    private bool _verticalMoveActive;
    private bool _isInitialized;

    float MovementSpeed => XRITEmulator.Instance.Config.movementSpeed;
    float AdjustHeightSpeed => XRITEmulator.Instance.Config.adjustHeightSpeed;

    private void OnEnable()
    {
        if (!_isInitialized)
        {
            InitializeComponents();
            _isInitialized = true;
        }

        SubscribeToInputs();
    }

    private void OnDisable()
    {
        UnsubscribeFromInputs();
    }

    private void Update()
    {
        if (_verticalMoveActive) ProcessVerticalMovement();
    }

    private void InitializeComponents()
    {
        var xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
        {
            _xrOriginTransform = xrOrigin.transform;
        }

        _moveProvider = FindAnyObjectByType<ContinuousMoveProvider>(FindObjectsInactive.Include);
        if (_moveProvider != null)
        {
            _defaultMoveSpeed = _moveProvider.moveSpeed;
        }
    }

    private void SubscribeToInputs()
    {
        if (movementAction?.action != null)
        {
            movementAction.action.performed += OnMovementPerformed;
            movementAction.action.canceled += OnMovementCanceled;
        }

        if (upDownAction?.action != null)
        {
            upDownAction.action.performed += OnVerticalMoveStarted;
            upDownAction.action.canceled += OnVerticalMoveStopped;
        }
    }

    private void UnsubscribeFromInputs()
    {
        if (movementAction?.action != null)
        {
            movementAction.action.performed -= OnMovementPerformed;
            movementAction.action.canceled -= OnMovementCanceled;
        }

        if (upDownAction?.action != null)
        {
            upDownAction.action.performed -= OnVerticalMoveStarted;
            upDownAction.action.canceled -= OnVerticalMoveStopped;
        }
    }

    private void OnMovementPerformed(InputAction.CallbackContext ctx)
    {
        if (_moveProvider != null)
            _moveProvider.moveSpeed = _defaultMoveSpeed * MovementSpeed;
    }

    private void OnMovementCanceled(InputAction.CallbackContext ctx)
    {
        if (_moveProvider != null)
            _moveProvider.moveSpeed = _defaultMoveSpeed;
    }

    private void OnVerticalMoveStarted(InputAction.CallbackContext ctx)
    {
        _verticalMoveActive = true;
    }

    private void OnVerticalMoveStopped(InputAction.CallbackContext ctx)
    {
        _verticalMoveActive = false;
    }


    private void ProcessVerticalMovement()
    {
        if (_xrOriginTransform == null || upDownAction?.action == null) return;

        float inputValue = upDownAction.action.ReadValue<float>();
        Vector3 movement = Vector3.up * (inputValue * AdjustHeightSpeed * Time.deltaTime);
        _xrOriginTransform.Translate(movement, Space.World);
    }
}