using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class PshaAssetGuidReferenceTypeAttribute : PropertyAttribute
{
    public Type AssetType { get; }
    public bool AllowSceneObjects { get; }

    public PshaAssetGuidReferenceTypeAttribute(Type assetType, bool allowSceneObjects = false)
    {
        AssetType = assetType ?? typeof(UnityEngine.Object);
        AllowSceneObjects = allowSceneObjects;
    }
}

[Serializable]
public struct PshaAssetGuidReference
{
    [SerializeField] internal string guid;
    [SerializeField] internal long localId;
    [SerializeField] internal string nameHint;

    public string Guid => guid;
    public long LocalId => localId;
    public string NameHint => nameHint;

    public bool IsSet => !string.IsNullOrEmpty(guid);

    public void Clear()
    {
        guid = string.Empty;
        localId = 0;
        nameHint = string.Empty;
    }

#if UNITY_EDITOR
    public T Get<T>(UnityEngine.Object owner = null) where T : UnityEngine.Object => Resolve<T>();

    public T Resolve<T>() where T : UnityEngine.Object
        => Resolve(guid, localId, typeof(T)) as T;

    public UnityEngine.Object Resolve(Type typeHint)
        => Resolve(guid, localId, typeHint);

    public static UnityEngine.Object Resolve(string guid, long localId, Type typeHint)
    {
        if (string.IsNullOrEmpty(guid)) return null;

        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;

        typeHint ??= typeof(UnityEngine.Object);

        if (localId == 0)
            return UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeHint);

        var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in all)
        {
            if (a == null) continue;
            if (!typeHint.IsAssignableFrom(a.GetType())) continue;

            if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out var g2, out long lid2))
            {
                if (g2 == guid && lid2 == localId) return a;
            }
        }

        return UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeHint);
    }

    public static void SetToSerializedProperty(UnityEditor.SerializedProperty refProp, UnityEngine.Object obj)
    {
        if (refProp == null) return;

        var guidProp = refProp.FindPropertyRelative("guid");
        var lidProp = refProp.FindPropertyRelative("localId");
        var hintProp = refProp.FindPropertyRelative("nameHint");
        if (guidProp == null || lidProp == null || hintProp == null) return;

        if (obj == null)
        {
            guidProp.stringValue = string.Empty;
            lidProp.longValue = 0;
            hintProp.stringValue = string.Empty;
            return;
        }

        if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var g, out long lid))
        {
            guidProp.stringValue = g;
            lidProp.longValue = lid;
            hintProp.stringValue = obj.name;
        }
        else
        {
            guidProp.stringValue = string.Empty;
            lidProp.longValue = 0;
            hintProp.stringValue = obj.name;
        }
    }
#endif
}
