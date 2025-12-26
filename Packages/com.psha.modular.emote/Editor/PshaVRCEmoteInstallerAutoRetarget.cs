#if UNITY_EDITOR && (VRC_SDK_VRCSDK3 || VRC_SDK_VRCSDK3_AVATARS)
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using UnityEditor.SceneManagement;

internal static class PshaVRCEmoteInstallerAutoRetarget
{
    private static bool _queued;

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.delayCall += Process;
    }

    private static void OnHierarchyChanged()
    {
        if (_queued) return;
        _queued = true;
        EditorApplication.delayCall += Process;
    }

    public static void ClearNotOnAvatarOverrides(PshaVRCEmoteInstaller installer)
    {
        if (installer == null) return;

        if (PrefabStageUtility.GetPrefabStage(installer.gameObject) != null)
            return;

        var so = new SerializedObject(installer);
        ClearNotOnAvatarOverridesSO(so);

        if (so.ApplyModifiedPropertiesWithoutUndo())
            EditorUtility.SetDirty(installer);
    }

    private static void Process()
    {
        _queued = false;

        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        var installers = Resources.FindObjectsOfTypeAll<PshaVRCEmoteInstaller>();
        if (installers == null) return;

        for (int i = 0; i < installers.Length; i++)
        {
            var inst = installers[i];
            if (inst == null) continue;

            if (PrefabStageUtility.GetPrefabStage(inst.gameObject) != null)
                continue;

            if (EditorUtility.IsPersistent(inst)) continue;
            if (!inst.gameObject.scene.IsValid()) continue;
            if (!inst.gameObject.activeInHierarchy) continue;

            if (PrefabUtility.IsPartOfPrefabAsset(inst.gameObject)) continue;

            var desc = inst.GetComponentInParent<VRCAvatarDescriptor>(true);
            int currentDescId = desc != null ? desc.GetInstanceID() : 0;

            var so = new SerializedObject(inst);
            var cachedProp = so.FindProperty("_cachedAvatarDescriptorInstanceId");
            if (cachedProp == null) continue;

            int cachedId = cachedProp.intValue;
            if (cachedId == currentDescId) continue;

            Undo.RecordObject(inst, "Auto Retarget Psha VRC Emote Installer");

            cachedProp.intValue = currentDescId;

            ClearNotOnAvatarOverridesSO(so);

            if (desc != null)
            {
                AutoConfigureActionMergeScopeTracking(so, desc);
            }

            if (so.ApplyModifiedProperties())
                EditorUtility.SetDirty(inst);
        }
    }


    private const string kDefaultActionScope = "Action";
    private const string kDefaultActionStartState = "Prepare Standing";
    private const string kDefaultActionEndState = "BlendOut Stand";
    private const string kVrcEmoteParam = "VRCEmote";

    private static void AutoConfigureActionMergeScopeTracking(SerializedObject so, VRCAvatarDescriptor desc)
    {
        if (so == null || desc == null) return;

        var showProp = so.FindProperty("showActionMergeScopeInInspector");
        var scopeProp = so.FindProperty("actionMergeScope");
        if (showProp == null || scopeProp == null) return;

        AnimatorController actionController = null;
        bool actionIsDefault = true;

        var layers = desc.baseAnimationLayers;
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.type != VRCAvatarDescriptor.AnimLayerType.Action) continue;

            actionIsDefault = layer.isDefault;
            actionController = layer.animatorController as AnimatorController;
            break;
        }

        if (actionIsDefault || actionController == null)
        {
            showProp.boolValue = false;
            scopeProp.stringValue = string.Empty;
            return;
        }

        var startProp = so.FindProperty("startActionState");
        var endProp = so.FindProperty("endActionState");

        string startState = startProp != null ? startProp.stringValue : null;
        string endState = endProp != null ? endProp.stringValue : null;

        if (string.IsNullOrEmpty(startState)) startState = kDefaultActionStartState;
        if (string.IsNullOrEmpty(endState)) endState = kDefaultActionEndState;

        if (string.IsNullOrEmpty(startState) || string.IsNullOrEmpty(endState)) return;

        if (TryDetectEmoteMergeScope(actionController, startState, endState, out var mergeScopeName))
        {
            if (string.Equals(mergeScopeName, kDefaultActionScope, System.StringComparison.Ordinal))
            {
                showProp.boolValue = false;
                scopeProp.stringValue = string.Empty;
            }
            else if (!string.IsNullOrEmpty(mergeScopeName))
            {
                showProp.boolValue = true;
                scopeProp.stringValue = mergeScopeName;
            }
        }
    }

    private static bool TryDetectEmoteMergeScope(AnimatorController controller, string startStateName, string endStateName, out string scopeName)
    {
        scopeName = null;
        if (controller == null) return false;

        var layers = controller.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            var root = layers[i].stateMachine;
            if (root == null) continue;

            foreach (var sm in EnumerateStateMachines(root))
            {
                if (!StateMachineContainsStateNames(sm, startStateName, endStateName)) continue;
                if (!StateMachineTreeHasVrcEmoteEqualsCondition(sm)) continue;

                scopeName = sm.name;
                return !string.IsNullOrEmpty(scopeName);
            }
        }

        return false;
    }

    private static IEnumerable<AnimatorStateMachine> EnumerateStateMachines(AnimatorStateMachine root)
    {
        if (root == null) yield break;

        var stack = new Stack<AnimatorStateMachine>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var sm = stack.Pop();
            yield return sm;

            var children = sm.stateMachines;
            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i].stateMachine;
                if (child != null) stack.Push(child);
            }
        }
    }

    private static bool StateMachineContainsStateNames(AnimatorStateMachine root, string startStateName, string endStateName)
    {
        bool foundStart = false;
        bool foundEnd = false;

        foreach (var sm in EnumerateStateMachines(root))
        {
            var states = sm.states;
            for (int i = 0; i < states.Length; i++)
            {
                var st = states[i].state;
                if (st == null) continue;

                if (!foundStart && st.name == startStateName) foundStart = true;
                if (!foundEnd && st.name == endStateName) foundEnd = true;

                if (foundStart && foundEnd) return true;
            }
        }

        return false;
    }

    private static bool StateMachineTreeHasVrcEmoteEqualsCondition(AnimatorStateMachine root)
    {
        foreach (var sm in EnumerateStateMachines(root))
        {
            if (TransitionsHaveVrcEmote(sm.anyStateTransitions)) return true;
            if (TransitionsHaveVrcEmote(sm.entryTransitions)) return true;

            var states = sm.states;
            for (int i = 0; i < states.Length; i++)
            {
                var st = states[i].state;
                if (st == null) continue;

                if (TransitionsHaveVrcEmote(st.transitions)) return true;
            }

            var childSms = sm.stateMachines;
            for (int i = 0; i < childSms.Length; i++)
            {
                var child = childSms[i].stateMachine;
                if (child == null) continue;

                var smTransitions = sm.GetStateMachineTransitions(child);
                if (TransitionsHaveVrcEmote(smTransitions)) return true;
            }
        }

        return false;
    }

    private static bool TransitionsHaveVrcEmote(AnimatorTransitionBase[] transitions)
    {
        if (transitions == null) return false;

        for (int i = 0; i < transitions.Length; i++)
        {
            var t = transitions[i];
            if (t == null) continue;

            var conditions = t.conditions;
            for (int c = 0; c < conditions.Length; c++)
            {
                var cond = conditions[c];

                if (cond.parameter != kVrcEmoteParam) continue;
                if (cond.mode != AnimatorConditionMode.Equals) continue;
                if (cond.threshold < 1f || cond.threshold > 8f) continue;

                return true;
            }
        }

        return false;
    }

    private static void ClearNotOnAvatarOverridesSO(SerializedObject so)
    {
        ClearGuidRef(so.FindProperty("targetMenuAsset"));
        ClearGuidRef(so.FindProperty("actionLayerAsset"));
        ClearGuidRef(so.FindProperty("fxLayerAsset"));

        var pathProp = so.FindProperty("targetMenuPath");
        if (pathProp != null && pathProp.isArray)
        {
            pathProp.arraySize = 0;
        }
    }

    private static void ClearGuidRef(SerializedProperty refProp)
    {
        if (refProp == null) return;

        var guidProp = refProp.FindPropertyRelative("guid");
        if (guidProp != null) guidProp.stringValue = string.Empty;

        var localIdProp = refProp.FindPropertyRelative("localId");
        if (localIdProp != null) localIdProp.longValue = 0;

        var nameHintProp = refProp.FindPropertyRelative("nameHint");
        if (nameHintProp != null) nameHintProp.stringValue = string.Empty;
    }
}
#endif