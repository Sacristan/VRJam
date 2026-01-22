using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class XRITEmulatorInit : MonoBehaviour
{
#if UNITY_EDITOR
    private IEnumerator Start()
    {
        for (int i = 0; i < 5; i++)
        {
            if (XRGeneralSettings.Instance.Manager.isInitializationComplete) yield break;
            yield return null;
        }

        if (!XRGeneralSettings.Instance.Manager.isInitializationComplete) Init();
    }

    private static void Init()
    {
        var emulatorPrefab = UnityEditor.AssetDatabase
            .LoadAssetAtPath<XRITEmulator>("Assets/XRITEmulator/XRITEmulator.prefab");

        if (emulatorPrefab != null)
        {
            Instantiate(emulatorPrefab);
        }
        else
        {
            Debug.LogError($"{nameof(XRITEmulator)} prefab not found at specified path!");
        }
    }
#endif
}