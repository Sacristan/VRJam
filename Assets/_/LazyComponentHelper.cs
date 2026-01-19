using UnityEngine;

public static class LazyComponentHelper
{
    public static T GetLazyComponent<T>(this GameObject go, ref T component) where T : Object
    {
        if (component == null) component = go.GetComponent<T>();
        return component;
    }

    public static T[] GetLazyComponents<T>(this GameObject go, ref T[] components)
        where T : Object
    {
        if (components == null) components = go.GetComponents<T>();
        return components;
    }

    public static T GetLazyComponentInChildren<T>(this GameObject go, ref T component, bool includeInactive = false)
        where T : Object
    {
        if (component == null) component = go.GetComponentInChildren<T>(includeInactive);
        return component;
    }

    public static T[] GetLazyComponentsInChildren<T>(this GameObject go, ref T[] components,
        bool includeInactive = false)
        where T : Object
    {
        if (components == null) components = go.GetComponentsInChildren<T>(includeInactive);
        return components;
    }

    public static T GetLazyComponentInParent<T>(this GameObject go, ref T component, bool includeInactive = false)
        where T : Object
    {
        if (component == null) component = go.GetComponentInParent<T>(includeInactive);
        return component;
    }

    public static T[] GetLazyComponentsInParent<T>(this GameObject go, ref T[] components, bool includeInactive = false)
        where T : Object
    {
        if (components == null) components = go.GetComponentsInParent<T>(includeInactive);
        return components;
    }

    public static T FindLazyComponent<T>(ref T component, bool includeInactive = false) where T : Object
    {
        if (component == null)
        {
            component = Object.FindFirstObjectByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude
            );
        }
        return component;
    }

    public static T[] FindLazyComponents<T>(ref T[] components, bool includeInactive = false) where T : Object
    {
        if (components == null)
        {
            components = Object.FindObjectsByType<T>(
                includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None
            );
        }
        return components;
    }

    public static T GetLazyInstance<T>(ref T so) where T : ScriptableObject
    {
        if (so == null) so = ScriptableObject.CreateInstance<T>();
        return so;
    }
}