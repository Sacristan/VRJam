using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public partial class FingersController : MonoBehaviour
{
    [SerializeField] private FingerDescriptor[] fingersDescriptors;

    public Finger[] Fingers { get; private set; }

    private readonly List<Collider> _allHandColliders = new();

    public void Init()
    {
        Fingers = new Finger[fingersDescriptors.Length];
        for (int i = 0, iSize = fingersDescriptors.Length; i < iSize; i++)
        {
            Fingers[i] = InitFinger(fingersDescriptors[i]);
        }

        CollectAllFingerColliders();
    }

    private Coroutine updateColliderRoutine = null;

    public void SetCollidersActive(bool active, float delay = 0f)
    {
        if (updateColliderRoutine != null) StopCoroutine(updateColliderRoutine);

        if (delay <= 0f) UpdateColliders();
        else
        {
            updateColliderRoutine = StartCoroutine(UpdateCollidersRoutine());
        }

        IEnumerator UpdateCollidersRoutine()
        {
            yield return new WaitForSeconds(delay);
            UpdateColliders();
            updateColliderRoutine = null;
        }

        void UpdateColliders()
        {
            SetCollidersActiveInternal(active);
        }
    }

    private void SetCollidersActiveInternal(bool active)
    {
        for (int i = 0, iSize = _allHandColliders.Count; i < iSize; i++)
        {
            Collider handCollider = _allHandColliders[i];
            if (handCollider == null) continue;

            handCollider.enabled = active;
        }
    }

    public void Transfer(FingersController otherHandFingers)
    {
        Fingers = otherHandFingers.Fingers;

        for (int i = 0, iSize = Fingers.Length; i < iSize; i++)
        {
            Finger finger = Fingers[i];
            FindDescriptor(finger.FingerType, out FingerDescriptor toDescriptor);
            TransferFinger(finger, toDescriptor);
        }

        CollectAllFingerColliders();
    }

    private void TransferFinger(Finger finger, FingerDescriptor toDescriptor)
    {
        Assert.IsNotNull(toDescriptor);

        Transform fingerTransform = finger.transform;
        fingerTransform.SetParent(toDescriptor.Parent);
        fingerTransform.ResetLocalTransform();
    }

    private Finger InitFinger(FingerDescriptor descriptor)
    {
        if (!FindExistingFinger(descriptor, out Finger finger))
        {
            GameObject fingerObj = new GameObject($"Finger-{descriptor.FingerType}");
            fingerObj.layer = XRPlayerLayers.Hand;

            Transform fingerTransform = fingerObj.transform;
            fingerTransform.SetParent(descriptor.Parent);
            fingerTransform.ResetLocalTransform();

            finger = fingerObj.AddComponent<Finger>();
            descriptor.CapsuleColliderDescriptor.CreateCollider(fingerObj);
        }

        return finger.Init(descriptor.FingerType);
    }

    private bool FindExistingFinger(FingerDescriptor descriptor, out Finger finger)
    {
        Finger[] fingers = descriptor.Parent.GetComponentsInChildren<Finger>();
        for (int i = 0, iSize = fingers.Length; i < iSize; i++)
        {
            if (fingers[i].FingerType == descriptor.FingerType)
            {
                finger = fingers[i];
                return true;
            }
        }

        finger = default;
        return false;
    }

    private bool FindDescriptor(
        Finger.Type fingerType, out FingerDescriptor targetDescriptor)
    {
        for (int i = 0, iSize = fingersDescriptors.Length; i < iSize; i++)
        {
            if (fingersDescriptors[i].FingerType == fingerType)
            {
                targetDescriptor = fingersDescriptors[i];
                return true;
            }
        }

        targetDescriptor = default;
        return false;
    }

    public bool FindByType(Finger.Type targetType, out Finger targetFinger)
    {
        Finger[] fingers = Fingers;
        for (int i = 0, iSize = fingers.Length; i < iSize; i++)
        {
            if (fingers[i].FingerType == targetType)
            {
                targetFinger = fingers[i];
                return true;
            }
        }

        targetFinger = default;
        return false;
    }

    public bool Contains(Finger finger)
    {
        return Array.IndexOf(Fingers, finger) >= 0;
    }

    public bool FindFingerWithCollider(Collider c, out Finger finger)
    {
        Finger[] fingers = Fingers;
        for (int i = 0, iSize = fingers.Length; i < iSize; i++)
        {
            if (fingers[i].Collider == c)
            {
                finger = fingers[i];
                return true;
            }
        }

        finger = default;
        return false;
    }

    public bool HasFingerCollider(Collider c)
    {
        return _allHandColliders.Contains(c);
    }

    private void CollectAllFingerColliders()
    {
        _allHandColliders.Clear();
        _allHandColliders.AddRange(
            gameObject.GetComponentsInChildren<Collider>()
        );
    }
}