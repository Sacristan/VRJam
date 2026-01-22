using System;
using UnityEngine;

[Serializable]
public class CapsuleColliderDescriptor
{
    [SerializeField] public bool isTrigger;
    [SerializeField] public Vector3 center;
    [SerializeField] public float radius;
    [SerializeField] public float height;
    [SerializeField] public int direction;

    public void FillFromCollider(CapsuleCollider collider)
    {
        isTrigger = collider.isTrigger;
        center = collider.center;
        radius = collider.radius;
        height = collider.height;
        direction = collider.direction;
    }

    public CapsuleCollider CreateCollider(GameObject go)
    {
        CapsuleCollider collider = go.AddComponent<CapsuleCollider>();

        collider.isTrigger = isTrigger;
        collider.center = center;
        collider.radius = radius;
        collider.height = height;
        collider.direction = direction;

        return collider;
    }
}