using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class TransformUtils
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static void DestroyAllChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
        }
    }


    public static void DumpObject(object obj)
    {
        if (obj == null)
            return;

        var type = obj.GetType();

        Logger.Log($"Inspecting {type.FullName}");

        var flags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        foreach (var field in type.GetFields(flags))
        {
            object value;
            try
            {
                value = field.GetValue(obj);
            }
            catch
            {
                value = "<unreadable>";
            }

            Logger.Log($"  FIELD {field.FieldType.Name} {field.Name} = {value}");
        }

        foreach (var prop in type.GetProperties(flags))
        {
            if (!prop.CanRead)
                continue;

            object value;
            try
            {
                value = prop.GetValue(obj);
            }
            catch
            {
                value = "<unreadable>";
            }

            Logger.Log($"  PROP {prop.PropertyType.Name} {prop.Name} = {value}");
        }
    }

    public static void LogHierarchy(Transform root, string indent = "")
    {
        Debug.Log($"{indent}{root.name}");

        for (int i = 0; i < root.childCount; i++)
        {
            LogHierarchy(root.GetChild(i), indent + "  ");
        }
    }

}