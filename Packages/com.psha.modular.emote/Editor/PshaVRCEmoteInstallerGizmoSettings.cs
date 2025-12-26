#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

[InitializeOnLoad]
public static class PshaVRCEmoteInstallerGizmoSettings
{
    private static int _tries;

    static PshaVRCEmoteInstallerGizmoSettings()
    {
        EditorApplication.delayCall += TryApply;
    }

    private static void TryApply()
    {
        if (DisableSceneIconFor<PshaVRCEmoteInstaller>()) return;


        if (_tries++ < 30)
            EditorApplication.delayCall += TryApply;
    }

    private static bool DisableSceneIconFor<T>()
    {
        var type = typeof(T);
        var editorAsm = typeof(Editor).Assembly;
        var annotationUtility = editorAsm.GetType("UnityEditor.AnnotationUtility");
        if (annotationUtility == null) return false;

        var getAnnotations = annotationUtility.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
        var setIconEnabled = annotationUtility.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);
        if (getAnnotations == null || setIconEnabled == null) return false;

        var annotations = (Array)getAnnotations.Invoke(null, null);
        if (annotations == null) return false;

        var didAny = false;

        foreach (var a in annotations)
        {
            var annotationType = a.GetType();
            int classID = (int)annotationType.GetField("classID")?.GetValue(a);
            string scriptClass = (string)annotationType.GetField("scriptClass")?.GetValue(a);

            if (scriptClass == type.Name)
            {
                setIconEnabled.Invoke(null, new object[] { classID, scriptClass, 0 });
                didAny = true;
            }
        }

        return didAny;
    }
}
#endif
