using System;
using UnityEngine;

[Serializable]
public class FingerDescriptor
{
    [SerializeField] private Finger.Type fingerType;
    [SerializeField] private Transform parent;
    [SerializeField] private CapsuleColliderDescriptor capsuleColliderDescriptor = new();

    public Finger.Type FingerType
    {
        get => fingerType;
        set => fingerType = value;
    }
    public Transform Parent
    {
        get => parent;
        set => parent = value;
    }
    public CapsuleColliderDescriptor CapsuleColliderDescriptor => capsuleColliderDescriptor;
}