using System;
using System.Reflection;
using UnityEngine;

public static class TLabComponentUtility
{
    /// <summary>
    /// Copying component properties
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static T CopyFrom<T>(this T self, T other) where T : Component
    {
        Type type = typeof(T);

        FieldInfo[] fields = type.GetFields();
        foreach (var field in fields)
        {
            if (field.IsStatic) continue;
            field.SetValue(self, field.GetValue(other));
        }

        PropertyInfo[] props = type.GetProperties();
        foreach (var prop in props)
        {
            if (!prop.CanWrite || !prop.CanRead || prop.Name == "name") continue;
            prop.SetValue(self, prop.GetValue(other));
        }

        return self as T;
    }

    /// <summary>
    /// Copy argument properties to the added component
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="self"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static T AddComponent<T>(this GameObject self, T other) where T : Component
    {
        return self.AddComponent<T>().CopyFrom(other);
    }
}