using UnityEngine;
using UnityEngine.InputSystem;

public class XRITEmulator : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActionAsset;
    
    private void OnEnable()
    {
        inputActionAsset.Enable();
    }

    private void OnDisable()
    {
        inputActionAsset.Disable();
    }
}
