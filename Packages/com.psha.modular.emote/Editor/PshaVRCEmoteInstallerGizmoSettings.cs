#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

[InitializeOnLoad]
public static class PshaVRCEmoteInstallerGizmoSettings
{
    static PshaVRCEmoteInstallerGizmoSettings()
    {
        DisableSceneIconFor<PshaVRCEmoteInstaller>();
    }

    private static void DisableSceneIconFor<T>()
    {
        var type = typeof(T);
        var editorAsm = typeof(Editor).Assembly;
        var annotationUtility = editorAsm.GetType("UnityEditor.AnnotationUtility");
        if (annotationUtility == null) return;

        var getAnnotations = annotationUtility.GetMethod(
            "GetAnnotations",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        var setIconEnabled = annotationUtility.GetMethod(
            "SetIconEnabled",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (getAnnotations == null || setIconEnabled == null) return;

        var annotations = (Array)getAnnotations.Invoke(null, null);
        if (annotations == null) return;

        foreach (var a in annotations)
        {
            var annotationType = a.GetType();
            int classID = (int)annotationType.GetField("classID").GetValue(a);
            string scriptClass = (string)annotationType.GetField("scriptClass").GetValue(a);

            if (scriptClass == type.Name)
            {
                setIconEnabled.Invoke(null, new object[] { classID, scriptClass, 0 });
            }
        }
    }
}
#endif
