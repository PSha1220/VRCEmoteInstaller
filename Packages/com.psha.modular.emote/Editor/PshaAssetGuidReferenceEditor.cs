#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

[CustomPropertyDrawer(typeof(PshaAssetGuidReference))]
public sealed class PshaAssetGuidReferenceDrawer : PropertyDrawer
{

    private static bool IsDescriptorLockedField(string fieldName)
    {
        return fieldName == "targetMenuAsset"
            || fieldName == "actionLayerAsset"
            || fieldName == "fxLayerAsset";
    }

    private static void DrawDescriptorLockedField(Rect fieldRect, Type objType, UnityEngine.Object avatarDescObj, string fieldName)
    {
#if VRC_SDK_VRCSDK3
        var avatarDesc = avatarDescObj as VRCAvatarDescriptor;
        UnityEngine.Object displayObj = null;
        string overlay = string.Empty;

        if (avatarDesc != null)
        {
            if (fieldName == "targetMenuAsset" && objType == typeof(VRCExpressionsMenu))
            {
                displayObj = GetBestEmoteMenuForDisplay(avatarDesc) as UnityEngine.Object;
                if (displayObj == null) overlay = "None";
            }
            else if (objType == typeof(RuntimeAnimatorController))
            {
                bool isDefault;
                if (fieldName == "actionLayerAsset")
                    displayObj = GetDescriptorLayerController(avatarDesc, VRCAvatarDescriptor.AnimLayerType.Action, out isDefault);
                else
                    displayObj = GetDescriptorLayerController(avatarDesc, VRCAvatarDescriptor.AnimLayerType.FX, out isDefault);

                if (isDefault) overlay = "Default";
                else if (displayObj == null) overlay = "None";
            }
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.ObjectField(fieldRect, displayObj, objType, false);
        }

        if (!string.IsNullOrEmpty(overlay))
        {
            DrawRightOverlayText(fieldRect, overlay);
        }
#else
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.ObjectField(fieldRect, null, objType, false);
        }
#endif
    }

#if VRC_SDK_VRCSDK3
    private static RuntimeAnimatorController GetDescriptorLayerController(VRCAvatarDescriptor avatarDesc, VRCAvatarDescriptor.AnimLayerType type, out bool isDefault)
    {
        isDefault = true;
        if (avatarDesc == null) return null;

        var layers = avatarDesc.baseAnimationLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].type != type) continue;
            isDefault = layers[i].isDefault;
            return layers[i].animatorController;
        }

        return null;
    }

    private static VRCExpressionsMenu GetBestEmoteMenuForDisplay(VRCAvatarDescriptor avatarDesc)
    {
        if (avatarDesc == null) return null;
        var root = avatarDesc.expressionsMenu;
        if (root == null) return null;

        var found = FindFirstSubMenuByName(root, "emote");
        return found ?? root;
    }

    private static VRCExpressionsMenu FindFirstSubMenuByName(VRCExpressionsMenu root, string keywordLower)
    {
        if (root == null) return null;

        var visited = new HashSet<VRCExpressionsMenu>();
        var q = new Queue<VRCExpressionsMenu>();
        visited.Add(root);
        q.Enqueue(root);

        while (q.Count > 0)
        {
            var m = q.Dequeue();
            if (m == null) continue;

            var controls = m.controls;
            if (controls == null) continue;

            for (int i = 0; i < controls.Count; i++)
            {
                var c = controls[i];
                if (c.type != VRCExpressionsMenu.Control.ControlType.SubMenu) continue;

                var sm = c.subMenu;
                if (sm == null) continue;

                var n = sm.name ?? string.Empty;
                if (n.IndexOf(keywordLower, StringComparison.OrdinalIgnoreCase) >= 0)
                    return sm;

                if (visited.Add(sm))
                    q.Enqueue(sm);
            }
        }

        return null;
    }
#endif

    private static void DrawRightOverlayText(Rect fieldRect, string text)
    {
        var r = fieldRect;
        r.xMin += kTextPadLeft;
        r.xMax -= kTextPadRight;

        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight,
            clipping = TextClipping.Clip
        };

        var prev = GUI.color;
        GUI.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        GUI.Label(r, text, style);
        GUI.color = prev;
    }

private static void DrawNotOnAvatar(Rect position, GUIContent label)
{
    var valueRect = EditorGUI.PrefixLabel(position, label);
    using (new EditorGUI.DisabledScope(true))
    {
        EditorGUI.TextField(valueRect, "(Not on avatar)");
    }
}


    // Layout tuning




    private const float kFieldInsetX = 0f;
    private const float kFieldInsetY = 0f;

    // Keep the overlay mask inside the field border
    private const float kMaskInnerInset = 1f;

    // Overlay text padding
    private const float kTextPadLeft = 3f;
    private const float kTextPadRight = 2f;



    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null)
        {
            EditorGUI.LabelField(position, label.text, "Missing property");
            return;
        }
        var fieldName = fieldInfo != null ? fieldInfo.Name : string.Empty;
Rect rowRect = position;

        int prevIndent = EditorGUI.indentLevel;

        var typeAttr = fieldInfo?.GetCustomAttribute<PshaAssetGuidReferenceTypeAttribute>();
        var objType = typeAttr?.AssetType ?? typeof(UnityEngine.Object);
        var allowScene = typeAttr?.AllowSceneObjects ?? false;

        var guidProp = property.FindPropertyRelative("guid");
        var lidProp = property.FindPropertyRelative("localId");
        var hintProp = property.FindPropertyRelative("nameHint");

        string guid = guidProp?.stringValue ?? string.Empty;
        long lid = lidProp?.longValue ?? 0;
        string hint = hintProp?.stringValue ?? string.Empty;

        UnityEngine.Object resolved = PshaAssetGuidReference.Resolve(guid, lid, objType);

        // Avatar context
        bool underAvatar = TryGetAvatarDescriptor(property.serializedObject.targetObject, out var avatarDesc);

        EditorGUI.BeginProperty(rowRect, label, property);


        Rect valueRect = EditorGUI.PrefixLabel(rowRect, label);

        // Draw the object field without inheriting indentation (prevents double-indent inside valueRect)
        EditorGUI.indentLevel = 0;


        Rect fieldRect = InsetCentered(valueRect, kFieldInsetX, kFieldInsetY);

        bool isDescriptorLocked = IsDescriptorLockedField(fieldName);
        if (isDescriptorLocked)
        {
            if (!underAvatar)
            {
                DrawNotOnAvatar(position, label);
                EditorGUI.EndProperty();
                EditorGUI.indentLevel = prevIndent;
                return;
            }

            DrawDescriptorLockedField(fieldRect, objType, avatarDesc, fieldName);

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = prevIndent;
            return;
        }


        // Outside avatar: show hint only

        if (!underAvatar)
        {
            var prev = GUI.color;
            GUI.color = new Color(0.45f, 0.45f, 0.45f, 1f);

            var style = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft
            };

            EditorGUI.LabelField(fieldRect, string.IsNullOrEmpty(hint) ? "(Not on avatar)" : hint, style);

            GUI.color = prev;

            EditorGUI.EndProperty();
            EditorGUI.indentLevel = prevIndent;
            return;
        }

        // Under avatar: validate selection
        bool invalidForThisAvatar = false;

#if VRC_SDK_VRCSDK3
        // Menu must be within the avatar menu tree
        if (avatarDesc != null && objType == typeof(VRCExpressionsMenu) && resolved is VRCExpressionsMenu menu)
        {
            var root = avatarDesc.expressionsMenu;


            if (root == null)
            {
                invalidForThisAvatar = true;
            }
            else if (!MenuTreeContains(root, menu))
            {
                invalidForThisAvatar = true;
            }
        }

        // Action and FX controllers must match the avatar descriptor layers
        if (!invalidForThisAvatar
            && avatarDesc != null
            && !string.IsNullOrEmpty(guid)
            && typeof(RuntimeAnimatorController).IsAssignableFrom(objType))
        {
            string currentFieldName = fieldName;

            if (currentFieldName == "actionLayerAsset")
            {
                invalidForThisAvatar = !MatchesDescriptorLayerControllerGuid(
                    avatarDesc, VRCAvatarDescriptor.AnimLayerType.Action, guid, lid);
            }
            else if (currentFieldName == "fxLayerAsset")
            {
                invalidForThisAvatar = !MatchesDescriptorLayerControllerGuid(
                    avatarDesc, VRCAvatarDescriptor.AnimLayerType.FX, guid, lid);
            }
        }
#endif

        bool missing = !string.IsNullOrEmpty(guid) && resolved == null;
        bool hintMismatch = (resolved != null && !string.IsNullOrEmpty(hint) && resolved.name != hint);

        bool showRedHint = invalidForThisAvatar || missing || hintMismatch;


        EditorGUI.BeginChangeCheck();
        var picked = EditorGUI.ObjectField(fieldRect, GUIContent.none, resolved, objType, allowScene);
        if (EditorGUI.EndChangeCheck())
        {
            PshaAssetGuidReference.SetToSerializedProperty(property, picked);
            property.serializedObject.ApplyModifiedProperties();
        }

        // Draw a red overlay hint without covering the field border
        if (showRedHint && Event.current.type == EventType.Repaint)
        {
            // Picker button width is based on field height
            float pickerButtonWidth = Mathf.Ceil(fieldRect.height);

            // Mask only the text area
            Rect maskRect = fieldRect;
            maskRect.xMax -= pickerButtonWidth;


            maskRect = InsetCentered(maskRect, kMaskInnerInset, kMaskInnerInset);

            // Redraw the field style to cover the text

            var fieldStyle = EditorStyles.objectField;
            fieldStyle.Draw(maskRect, GUIContent.none, false, false, false, false);

            // Overlay hint text
            Rect textRect = maskRect;
            textRect.xMin += kTextPadLeft;
            textRect.xMax -= kTextPadRight;

            string text = string.IsNullOrEmpty(hint)
                ? (missing ? "Missing" : "(Invalid)")
                : hint;

            var prev = GUI.color;
            GUI.color = new Color(1f, 0.3f, 0.3f, 1f);

            var style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            GUI.Label(textRect, text, style);
            GUI.color = prev;
        }

        EditorGUI.EndProperty();
        EditorGUI.indentLevel = prevIndent;
    }

    private static Rect InsetCentered(Rect r, float insetX, float insetY)
    {
        r.x += insetX;
        r.y += insetY;
        r.width = Mathf.Max(0f, r.width - insetX * 2f);
        r.height = Mathf.Max(0f, r.height - insetY * 2f);
        return r;
    }

#if VRC_SDK_VRCSDK3
    private static bool TryGetAvatarDescriptor(UnityEngine.Object target, out VRCAvatarDescriptor desc)
    {
        desc = null;

        if (target is Component c)
        {
            var t = c.transform;
            while (t != null)
            {
                if (t.TryGetComponent(out VRCAvatarDescriptor found))
                {
                    desc = found;
                    return true;
                }
                t = t.parent;
            }
        }
        else if (target is GameObject go)
        {
            var t = go.transform;
            while (t != null)
            {
                if (t.TryGetComponent(out VRCAvatarDescriptor found))
                {
                    desc = found;
                    return true;
                }
                t = t.parent;
            }
        }

        return false;
    }

    private static bool MenuTreeContains(VRCExpressionsMenu root, VRCExpressionsMenu target)
    {
        if (root == null || target == null) return false;
        if (ReferenceEquals(root, target)) return true;

        var visited = new HashSet<VRCExpressionsMenu>();
        var stack = new Stack<VRCExpressionsMenu>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur == null) continue;
            if (!visited.Add(cur)) continue;

            if (ReferenceEquals(cur, target)) return true;

            var controls = cur.controls;
            if (controls == null) continue;

            for (int i = 0; i < controls.Count; i++)
            {
                var sub = controls[i]?.subMenu;
                if (sub != null) stack.Push(sub);
            }
        }

        return false;
    }

    private static bool MatchesDescriptorLayerControllerGuid(
        VRCAvatarDescriptor desc,
        VRCAvatarDescriptor.AnimLayerType layerType,
        string targetGuid,
        long targetLocalId)
    {
        // If descriptor is missing, skip validation
        if (desc == null) return true;
        if (string.IsNullOrEmpty(targetGuid)) return true;

        var layers = desc.baseAnimationLayers;
        if (layers == null) return false;

        VRCAvatarDescriptor.CustomAnimLayer found = default;
        bool hasFound = false;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].type == layerType)
            {
                found = layers[i];
                hasFound = true;
                break;
            }
        }

        if (!hasFound) return false;



        if (found.isDefault) return false;

        var controller = found.animatorController;
        if (controller == null) return false;

        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(controller, out var guid, out long lid))
            return false;

        if (guid != targetGuid)
            return false;

        // LocalId may be zero for main assets, so guid match is enough
        if (targetLocalId == 0) return true;

        return lid == targetLocalId;
    }
#else
    private static bool TryGetAvatarDescriptor(UnityEngine.Object target, out UnityEngine.Object desc)
    {
        desc = null;
        return false;
    }
#endif
}
#endif
