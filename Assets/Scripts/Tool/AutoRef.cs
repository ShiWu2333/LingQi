using UnityEngine;

public static class AutoRef
{
    public static T GetOrAdd<T>(this Component c) where T : Component
    {
        var result = c.GetComponent<T>();
        if (result == null)
            result = c.gameObject.AddComponent<T>();
        return result;
    }

    public static T GetInChildren<T>(this Component c) where T : Component
    {
        return c.GetComponentInChildren<T>();
    }

    public static T GetInParent<T>(this Component c) where T : Component
    {
        return c.GetComponentInParent<T>();
    }
}
