using UnityEngine;
using UnityEngine.InputSystem;

public class XRITEmulator : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField] private XREmulatorConfig config;

    private XRITEmulateController[] _controllers;
    private XRITEmulateLook _lookEmulator;
    private XRITEmulateMovement _movementEmulator;

    public static XRITEmulator Instance { get; private set; }
    public XREmulatorConfig Config => config;
    public static bool IsReady => Instance != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CacheComponents();
    }

    private void CacheComponents()
    {
        _controllers = GetComponentsInChildren<XRITEmulateController>();
        _lookEmulator = GetComponentInChildren<XRITEmulateLook>();
        _movementEmulator = GetComponentInChildren<XRITEmulateMovement>();
    }

    private void OnEnable()
    {
        inputActionAsset?.Enable();
    }

    private void OnDisable()
    {
        inputActionAsset?.Disable();
    }

    public bool IsLookActive() => _lookEmulator != null && _lookEmulator.IsActive();
}