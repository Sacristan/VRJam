using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TransformUtils
{
    public static void DestroyChildren(this Transform t)
    {
        foreach (Transform child in t)
        {
            Object.Destroy(child.gameObject);
        }
    }
    
    public static void DestroyChildrenImmediate(this Transform t)
    {
        foreach (Transform child in t)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    public static void ResetLocalTransform(this Transform t, bool resetScale = true)
    {
        t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        if (resetScale)
        {
            t.localScale = Vector3.one;
        }
    }


    ///https://discussions.unity.com/t/test-to-see-if-a-vector3-point-is-within-a-boxcollider/17385/4
    public static bool PointInOABB(Vector3 point, BoxCollider box)
    {
        point = box.transform.InverseTransformPoint(point) - box.center;

        Vector3 boxSize = box.size;
        float halfX = boxSize.x * 0.5f;
        float halfY = boxSize.y * 0.5f;
        float halfZ = boxSize.z * 0.5f;

        if (point.x < halfX
            && point.x > -halfX
            && point.y < halfY
            && point.y > -halfY
            && point.z < halfZ
            && point.z > -halfZ)
        {
            return true;
        }

        return false;
    }

    public static bool FindFirstChildNameContains(this Transform root, string namePart, out Transform targetChild)
    {
        foreach (Transform child in root)
        {
            if (child.name.Contains(namePart))
            {
                targetChild = child;
                return true;
            }
        }

        foreach (Transform child in root)
        {
            if (FindFirstChildNameContains(child, namePart, out targetChild))
            {
                return true;
            }
        }

        targetChild = default;
        return false;
    }

    public static Transform FindNearestChildInHierarchy(this Transform root, Vector3 targetPos,
        Func<Transform, bool> passFilter = null)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>();
        float minSqrDistance = float.MaxValue;
        Transform nearestChild = default;

        for (int i = 0, iSize = children.Length; i < iSize; i++)
        {
            Transform child = children[i];
            if (passFilter != null && !passFilter.Invoke(child))
            {
                continue;
            }

            float sqrDistToChild = Vector3.SqrMagnitude(child.position - targetPos);
            if (sqrDistToChild < minSqrDistance)
            {
                minSqrDistance = sqrDistToChild;
                nearestChild = child;
            }
        }

        return nearestChild;
    }

    public static T[] GetComponentsInDirectChildren<T>(this Transform parent) where T : Component
    {
        var result = new List<T>();
        foreach (Transform transform in parent)
        {
            if (transform.TryGetComponent<T>(out T component))
            {
                result.Add(component);
            }
        }
        return result.ToArray();
    }

    [CanBeNull]
    public static Transform FindChildRecursively(this Transform root, string childName)
    {
        Transform targetChild = root.Find(childName);
        if (targetChild != null) return targetChild;

        foreach (Transform child in root)
        {
            targetChild = FindChildRecursively(child, childName);
            if (targetChild != null)
            {
                return targetChild;
            }
        }

        return null;
    }

    [CanBeNull]
    public static T FindChildRecursively<T>(this Transform root, string childName) where T : Component
    {
        Transform targetChild = root.Find(childName);
        if (targetChild != null &&
            targetChild.TryGetComponent<T>(out var component))
        {
            return component;
        }

        foreach (Transform child in root)
        {
            T childComponent = FindChildRecursively<T>(child, childName);
            if (childComponent != null)
            {
                return childComponent;
            }
        }

        return null;
    }

    public static string[] GetFullPath(this Transform t)
    {
        List<string> path = new List<string>();

        Transform parent = t;
        while (parent != null)
        {
            path.Insert(0, parent.name);
            parent = parent.parent;
        }

        return path.ToArray();
    }

    public static string GetFullPathStr(this Transform t)
    {
        if (t == null)
        {
            return string.Empty;
        }

        StringBuilder pathBuilder = new StringBuilder(t.name);

        Transform parent = t.parent;
        while (parent != null)
        {
            pathBuilder.Insert(0, "/").Insert(0, parent.name);
            parent = parent.parent;
        }

        return pathBuilder.ToString();
    }

    public static void SetWorldScale(this Transform transform, Vector3 worldScale)
    {
        transform.localScale = Vector3.one;
        Vector3 lossyScale = transform.lossyScale;
        transform.localScale = new Vector3(
            worldScale.x / lossyScale.x,
            worldScale.y / lossyScale.y,
            worldScale.z / lossyScale.z);
    }

    public static Transform[] CollectChildren(this Transform transform)
    {
        int childCount = transform.childCount;
        if (childCount == 0)
            return Array.Empty<Transform>();

        var children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            children[i] = transform.GetChild(i);
        }
        return children;
    }

    public static void CollectChildrenRecursively(this Transform parent, List<Transform> result)
    {
        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            result.Add(child);
            CollectChildrenRecursively(child, result);
        }
    }

    public static void CollectChildrenRecursively(this Transform parent, List<Transform> result, Func<bool, Transform> condition)
    {
        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (!condition(child))
                continue;

            result.Add(child);
            CollectChildrenRecursively(child, result, condition);
        }
    }

    public static void ExecuteRecursively(this Transform target, Action<Transform> action)
    {
        action(target);
        
        int childCount = target.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = target.GetChild(i);
            ExecuteRecursively(child, action);
        }
    }

    public static void SetStaticRecursive(this Transform transform, bool isStatic)
    {
        transform.gameObject.isStatic = isStatic;
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).SetStaticRecursive(isStatic);
        }
    }

    public static Pose LerpPose(Pose pose0, Pose pose1, float t)
    {
        return new Pose(
            position: Vector3.Lerp(pose0.position, pose1.position, t),
            rotation: Quaternion.Slerp(pose0.rotation, pose1.rotation, t)
        );
    }

    /// https://forum.unity.com/threads/how-to-mirror-a-euler-angle-or-rotation.90650/
    public static Quaternion ReflectRotation(Quaternion source, Vector3 normal)
    {
        return Quaternion.LookRotation(Vector3.Reflect(source * Vector3.forward, normal), Vector3.Reflect(source * Vector3.up, normal));
    }
    
    /// <summary>
    /// Transforms rotation from local space to world space.
    /// Based on https://discussions.unity.com/t/what-is-the-rotation-equivalent-of-inversetransformpoint/45386/4
    /// </summary>
    public static Quaternion TransformRotation(this Transform transform, Quaternion localRotation) =>
        transform.rotation * localRotation;

    /// <summary>
    /// Transforms rotation from world space to local space.
    /// Based on https://discussions.unity.com/t/what-is-the-rotation-equivalent-of-inversetransformpoint/45386/4
    /// </summary>
    public static Quaternion InverseTransformRotation(this Transform transform, Quaternion worldRotation) =>
        Quaternion.Inverse(transform.rotation) * worldRotation;

    public static Pose TransformPose(this Transform transform, Pose localPose) =>
        localPose.GetTransformedBy(transform);

    public static Pose InverseTransformPose(this Transform transform, Pose worldPose) =>
        new(position: transform.InverseTransformPoint(worldPose.position),
            rotation: transform.InverseTransformRotation(worldPose.rotation));

    public static Pose GetWorldPose(this Transform transform) =>
        new(transform.position, transform.rotation);

    public static void SetWorldPose(this Transform transform, Pose pose) =>
        transform.SetPositionAndRotation(pose.position, pose.rotation);
    
    public static void SetWorldPose(this Transform transform, Transform other) =>
        transform.SetPositionAndRotation(other.position, other.rotation);

    public static Pose GetLocalPose(this Transform transform) =>
        new(transform.localPosition, transform.localRotation);

    public static void SetLocalPose(this Transform transform, Pose pose) =>
        transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
    
    public static void SetLocalPose(this Transform transform, Transform other) =>
        transform.SetLocalPositionAndRotation(other.position, other.rotation);
}