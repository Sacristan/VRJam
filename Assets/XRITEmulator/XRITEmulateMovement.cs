using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class XRITEmulateMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float upDownMoveSpeed = 0.1f;

    [SerializeField] private InputActionReference movementAction;
    [SerializeField] private InputActionReference upDownAction;

    private ContinuousMoveProvider _moveProvider;
    private Transform _verticalMoveTransform;

    private bool _verticalMoveActive;
    private float _defaultMoveSpeed;

    private void OnEnable()
    {
        InitMoveTransform();
        InitMoveProvider();

        movementAction.action.performed += ActivateMovement;
        movementAction.action.canceled += DeactivateMovement;

        upDownAction.action.performed += StartVerticalMovement;
        upDownAction.action.canceled += StopVerticalMovement;
    }

    private void OnDisable()
    {
        movementAction.action.performed -= ActivateMovement;
        movementAction.action.canceled -= DeactivateMovement;

        upDownAction.action.performed -= StartVerticalMovement;
        upDownAction.action.canceled -= StopVerticalMovement;
    }

    private void InitMoveTransform()
    {
        XROrigin xrOrigin = FindAnyObjectByType<XROrigin>();
        _verticalMoveTransform = xrOrigin.CameraFloorOffsetObject.transform;
    }

    private void InitMoveProvider()
    {
        _moveProvider = FindAnyObjectByType<ContinuousMoveProvider>(FindObjectsInactive.Include);
        _defaultMoveSpeed = _moveProvider.moveSpeed;
    }

    private void ActivateMovement(InputAction.CallbackContext obj)
    {
        _moveProvider.moveSpeed = _defaultMoveSpeed * movementSpeed;
    }

    private void DeactivateMovement(InputAction.CallbackContext obj)
    {
        _moveProvider.moveSpeed = _defaultMoveSpeed;
    }

    private void StartVerticalMovement(InputAction.CallbackContext context)
    {
        _verticalMoveActive = true;
    }

    private void StopVerticalMovement(InputAction.CallbackContext context)
    {
        _verticalMoveActive = false;
    }

    private void Update()
    {
        if (_verticalMoveActive)
        {
            DoVerticalMovement();
        }
    }

    private void DoVerticalMovement()
    {
        float upDownInputValue = upDownAction.action.ReadValue<float>();
        _verticalMoveTransform.Translate(Vector3.up * (upDownInputValue * Time.deltaTime), Space.World);
    }
}