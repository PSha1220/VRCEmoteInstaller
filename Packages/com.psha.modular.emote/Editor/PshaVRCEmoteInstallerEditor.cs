#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;


[CustomEditor(typeof(PshaVRCEmoteInstaller))]
public class PshaVRCEmoteInstallerEditor : Editor
{

    // Keep labels Editor Language and Use System Language (Auto) unchanged
    private static readonly Dictionary<string, Dictionary<string, string>> s_langMaps
        = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> s_langTriedLoad
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeLang(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return "en-us";
        lang = lang.Trim().ToLowerInvariant();

        if (lang == "ko") return "ko-kr";
        if (lang == "ja") return "ja-jp";
        if (lang == "en") return "en-us";

        if (lang == "zh") return "zh-hans";
        if (lang == "zh-cn") return "zh-hans";
        if (lang == "zh-sg") return "zh-hans";
        if (lang == "zh-hans") return "zh-hans";

        if (lang == "zh-tw") return "zh-hant";
        if (lang == "zh-hk") return "zh-hant";
        if (lang == "zh-mo") return "zh-hant";
        if (lang == "zh-hant") return "zh-hant";

        return lang;
    }

    private static Dictionary<string, string> EnsureLangLoaded(string lang)
    {
        lang = NormalizeLang(lang);

        if (s_langMaps.TryGetValue(lang, out var cached))
            return cached;

        if (s_langTriedLoad.Contains(lang))
            return null;

        s_langTriedLoad.Add(lang);

        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        try
        {
            string[] candidates =
       {
    lang,
    lang == "zh-cn" || lang == "zh" ? "zh-hans" : null,
    lang == "zh-tw" || lang == "zh-hk" ? "zh-hant" : null
};

            string chosen = null;
            string[] guids = null;

            for (int c = 0; c < candidates.Length; c++)
            {
                var code = candidates[c];
                if (string.IsNullOrEmpty(code)) continue;

                guids = AssetDatabase.FindAssets($"PshaVRCEmote.{code} t:TextAsset");
                if (guids != null && guids.Length > 0)
                {
                    chosen = code;
                    break;
                }
            }
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta != null && !string.IsNullOrEmpty(ta.text))
                {
                    foreach (var kv in ParseFlatJsonStringMap(ta.text))
                        if (!string.IsNullOrEmpty(kv.Key))
                            map[kv.Key] = kv.Value ?? string.Empty;
                }
            }
        }
        catch
        {
        }

        s_langMaps[lang] = map;
        return map;
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseFlatJsonStringMap(string json)
    {
        if (string.IsNullOrEmpty(json)) yield break;

        if (json.Length > 0 && json[0] == '\uFEFF') json = json.Substring(1);

        int i = 0;
        while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        if (i < json.Length && json[i] == '{') i++;

        while (i < json.Length)
        {
            SkipWs(json, ref i);
            if (i >= json.Length) yield break;
            if (json[i] == '}') yield break;

            string key = ReadJsonString(json, ref i);
            SkipWs(json, ref i);

            if (i < json.Length && json[i] == ':') i++;
            SkipWs(json, ref i);

            string value = ReadJsonString(json, ref i);
            yield return new KeyValuePair<string, string>(key, value);

            SkipWs(json, ref i);
            if (i < json.Length && json[i] == ',') i++;
        }

        static void SkipWs(string s, ref int p)
        {
            while (p < s.Length && char.IsWhiteSpace(s[p])) p++;
        }

        static string ReadJsonString(string s, ref int p)
        {
            SkipWs(s, ref p);
            if (p >= s.Length) return string.Empty;

            if (s[p] != '"')
            {
                int start = p;
                while (p < s.Length && s[p] != ',' && s[p] != '}' && !char.IsWhiteSpace(s[p])) p++;
                return s.Substring(start, p - start);
            }

            p++;
            var sb = new StringBuilder();
            while (p < s.Length)
            {
                char c = s[p++];
                if (c == '"') break;

                if (c == '\\' && p < s.Length)
                {
                    char esc = s[p++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (p + 4 <= s.Length)
                            {
                                string hex = s.Substring(p, 4);
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                    sb.Append((char)code);
                                p += 4;
                            }
                            break;
                        default:
                            sb.Append(esc);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }


    private static string Tr(string key, string fallback)
    {
        // Keep these keys unlocalized
        if (key == "psha.btn_setup" || key == "psha.dialog_setup_title")
            return fallback;

        var lang = NormalizeLang(GetEffectiveLanguageCode());

        // Always query the selected language json first
        var map = EnsureLangLoaded(lang);
        if (map != null && map.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
            return v;

        return fallback ?? string.Empty;
    }

    private static string Trf(string key, string fallback, params object[] args)
    {
        string fmt = Tr(key, fallback);
        try { return string.Format(fmt, args); }
        catch { return fmt; }
    }

    private static GUIContent GC(string labelKey, string labelFallback, string ttKey = null, string ttFallback = null)
    {
        if (!string.IsNullOrEmpty(ttKey))
            return new GUIContent(Tr(labelKey, labelFallback), Tr(ttKey, ttFallback ?? string.Empty));
        return new GUIContent(Tr(labelKey, labelFallback));
    }



    SerializedProperty _emoteNameProp;
    SerializedProperty _slotIndexProp;
    SerializedProperty _menuIconProp;
    SerializedProperty _controlTypeProp;

    SerializedProperty _targetMenuProp;

    SerializedProperty _actionLayerProp;
    SerializedProperty _fxLayerProp;


    SerializedProperty _MEactionProp;
    SerializedProperty _MEfxMotionProp;


    SerializedProperty _startActionStateProp;
    SerializedProperty _endActionStateProp;

    SerializedProperty _showActionMergeScopeProp;
    SerializedProperty _actionMergeScopeProp;

    SerializedProperty _meWriteDefaultsOffProp;

    SerializedProperty _useMergeMEFxProp;
    SerializedProperty _useAdditionalMEFxLayersProp;
    SerializedProperty _additionalMEFxLayersProp;


    SerializedProperty _changeEmoteMenuIconProp;
    static bool s_devFoldout = false;
    static bool s_previewFoldout = true;
    static bool s_advancedFoldout = false;

    void OnEnable()
    {
        _emoteNameProp = serializedObject.FindProperty("emoteName");
        _slotIndexProp = serializedObject.FindProperty("slotIndex");
        _menuIconProp = serializedObject.FindProperty("menuIcon");
        _controlTypeProp = serializedObject.FindProperty("controlType");

        _targetMenuProp = serializedObject.FindProperty("targetMenu");

        _actionLayerProp = serializedObject.FindProperty("actionLayer");
        _fxLayerProp = serializedObject.FindProperty("fxLayer");


        _MEactionProp = serializedObject.FindProperty("actionMELayer");
        _MEfxMotionProp = serializedObject.FindProperty("fxMELayer");

        _startActionStateProp = serializedObject.FindProperty("startActionState");
        _endActionStateProp = serializedObject.FindProperty("endActionState");

        _showActionMergeScopeProp = serializedObject.FindProperty("showActionMergeScopeInInspector");
        _actionMergeScopeProp = serializedObject.FindProperty("actionMergeScope");

        _meWriteDefaultsOffProp = serializedObject.FindProperty("meWriteDefaultsOff");

        _useMergeMEFxProp = serializedObject.FindProperty("useMergeMEFxLayer");
        _useAdditionalMEFxLayersProp = serializedObject.FindProperty("useAdditionalMEFxLayers");
        _additionalMEFxLayersProp = serializedObject.FindProperty("additionalMEFxLayers");


        _changeEmoteMenuIconProp = serializedObject.FindProperty("changeEmoteMenuIcon");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var mgr = (PshaVRCEmoteInstaller)target;


        DrawPlacementGuidance(mgr);


        if (_useMergeMEFxProp.boolValue)
        {
            DrawEmoteNameMismatchWarning(mgr);
            DrawSharedFxLayerWarning(mgr);
        }

        EditorGUILayout.LabelField(Tr("psha.content", "Content"), EditorStyles.boldLabel);
        EditorGUILayout.Space(2);


        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.BeginHorizontal();


            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField(Tr("psha.name", "Name"));

                string rawName = mgr.emoteName;
                string displayName = string.IsNullOrEmpty(rawName)
                    ? Tr("psha.no_name", "(No Name)")
                    : rawName.Replace("<br>", "\n");

                var nameStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true,
                    fontStyle = FontStyle.Bold
                };
                nameStyle.fontSize = EditorStyles.label.fontSize + 2;

                EditorGUILayout.LabelField(displayName, nameStyle);
            }


            GUILayout.FlexibleSpace();

            Rect previewRect = GUILayoutUtility.GetRect(
                80, 80,
                GUILayout.Width(80),
                GUILayout.Height(80)
            );

            if (mgr.menuIcon != null)
            {
                EditorGUI.DrawTextureTransparent(previewRect, mgr.menuIcon, ScaleMode.ScaleToFit);
            }
            else
            {
                var centered = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                EditorGUI.LabelField(previewRect, Tr("psha.no_icon", "No Icon"), centered);
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(Tr("psha.target_slot", "Target Slot No. (1 to 8)"));

            EditorGUI.indentLevel++;
            int slot = _slotIndexProp.intValue;
            slot = EditorGUILayout.IntSlider(slot, 1, 8);
            EditorGUI.indentLevel--;
            _slotIndexProp.intValue = slot;
        }

        EditorGUILayout.Space();


        s_devFoldout = EditorGUILayout.Foldout(s_devFoldout, Tr("psha.dev_options", "Developer Options"));
        if (s_devFoldout)
        {

            EditorGUI.indentLevel++;

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(Tr("psha.vrc_emote_settings", "VRC Emote Settings"), EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(_targetMenuProp, GC("psha.target_menu", "Target VRC Emote Menu", "psha.tt.target_menu", "VRCEmote menu to customize. Leave empty to auto detect at build time."));

                EditorGUILayout.Space(4);



                EditorGUILayout.LabelField(Tr("psha.menu_settings", "Menu Settings"), EditorStyles.miniBoldLabel);

                EditorGUILayout.PropertyField(_emoteNameProp, GC("psha.emote_name", "Emote Name", "psha.tt.emote_name", "Name displayed in the VRC Emote menu."));
                EditorGUILayout.PropertyField(_menuIconProp, GC("psha.menu_icon", "Menu Icon", "psha.tt.menu_icon", "Icon displayed in the VRC Emote menu."));
                EditorGUILayout.PropertyField(_controlTypeProp, GC("psha.type", "Type", "psha.tt.type", "Control type. None keeps the existing type."));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField(Tr("psha.value", "Value"), mgr.Value);
                    EditorGUILayout.TextField(Tr("psha.parameter", "Parameter"), $"{mgr.ParameterName}, Int");
                }

                EditorGUILayout.Space(4);


                EditorGUILayout.LabelField(Tr("psha.avatar_layer_settings", "Avatar Layer Settings"), EditorStyles.miniBoldLabel);

                EditorGUILayout.PropertyField(_actionLayerProp, GC("psha.avatar_action_layer", "Avatar Action Layer", "psha.tt.avatar_action_layer", "Avatar Action layer controller. If empty, uses the descriptor default."));
                EditorGUILayout.PropertyField(_fxLayerProp, GC("psha.avatar_fx_layer", "Avatar FX Layer", "psha.tt.avatar_fx_layer", "Avatar FX layer controller."));

                EditorGUILayout.Space(2);


                EditorGUILayout.LabelField(Tr("psha.me_merge_settings", "ME Layer Merge Settings"), EditorStyles.miniBoldLabel);

                EditorGUILayout.PropertyField(_MEactionProp, GC("psha.me_action_layer", "ME Action Layer", "psha.tt.me_action_layer", "ME Action template controller to merge."));


                if (_useMergeMEFxProp.boolValue)
                {
                    EditorGUILayout.PropertyField(_MEfxMotionProp, GC("psha.me_fx_layer", "ME FX Layer", "psha.tt.me_fx_layer", "ME FX template controller to merge."));
                }

                EditorGUILayout.Space(4);


                EditorGUILayout.LabelField(Tr("psha.state_settings", "State Settings"), EditorStyles.miniBoldLabel);

                EditorGUILayout.PropertyField(
                    _startActionStateProp,
                    GC("psha.start_action_state", "Start Action State", "psha.tt.start_action_state", "State name where the emote branch starts in the Action layer.")
                );
                EditorGUILayout.PropertyField(
                    _endActionStateProp,
                    GC("psha.end_action_state", "End Action State", "psha.tt.end_action_state", "State name where the emote branch ends in the Action layer.")
                );


                if (_showActionMergeScopeProp.boolValue)
                {
                    EditorGUILayout.PropertyField(
                        _actionMergeScopeProp,
                        GC("psha.action_sub_sm_root", "Action SM Root", "psha.tt.action_sub_sm_root", "Sub state machine name/path used as the merge scope.")
                    );


                    if (string.IsNullOrEmpty(_actionMergeScopeProp.stringValue))
                    {
                        EditorGUILayout.HelpBox(
                            Tr("psha.action_sub_sm_root_empty_warn", "Action Sub StateMachine Root is empty. Click 'Setup VRC Emote' or enter the Action Sub StateMachine Root."),
                            MessageType.Warning
                        );
                        EditorGUILayout.Space(4);
                    }
                }


                EditorGUILayout.PropertyField(
                    _meWriteDefaultsOffProp,
                    GC("psha.me_write_defaults_off", "ME Write Default OFF", "psha.tt.me_write_defaults_off", "Force Write Defaults OFF for states inside ME layers.")
                );

                EditorGUILayout.Space(8);


                s_advancedFoldout = EditorGUILayout.Foldout(
                    s_advancedFoldout,
                    Tr("psha.advanced_options", "Advanced Options"),
                    true
                );

                if (s_advancedFoldout)
                {

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(
                        _showActionMergeScopeProp,
                        GC("psha.track_action_sub_sm", "Action SM Root Tracking", "psha.tt.track_action_sub_sm", "Automatically track merge scope by Start/End and VRCEmote conditions.")
                    );
                    if (EditorGUI.EndChangeCheck())
                    {

                        if (!_showActionMergeScopeProp.boolValue)
                        {
                            _actionMergeScopeProp.stringValue = string.Empty;
                        }
                    }

                    EditorGUILayout.Space(4);


                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(
                        _useMergeMEFxProp,
                        GC("psha.use_merge_me_fx", "Use Merge ME FX", "psha.tt.use_merge_me_fx", "Use ME FX merge template.")
                    );
                    if (EditorGUI.EndChangeCheck())
                    {

                        if (!_useMergeMEFxProp.boolValue)
                        {
                            _MEfxMotionProp.objectReferenceValue = null;

                            _useAdditionalMEFxLayersProp.boolValue = false;

                            if (_additionalMEFxLayersProp != null && _additionalMEFxLayersProp.isArray)
                            {
                                for (int i = 0; i < _additionalMEFxLayersProp.arraySize; i++)
                                {
                                    var el = _additionalMEFxLayersProp.GetArrayElementAtIndex(i);
                                    el.objectReferenceValue = null;
                                }
                                _additionalMEFxLayersProp.arraySize = 0;
                            }
                        }
                    }


                    if (_useMergeMEFxProp.boolValue)
                    {
                        EditorGUI.indentLevel++;


                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(
                            _useAdditionalMEFxLayersProp,
                            GC("psha.additional_me_fx", "+ Additional ME FX", "psha.tt.additional_me_fx", "Merge additional ME FX layers (up to 2).")
                        );
                        if (EditorGUI.EndChangeCheck())
                        {

                            if (!_useAdditionalMEFxLayersProp.boolValue)
                            {
                                if (_additionalMEFxLayersProp != null && _additionalMEFxLayersProp.isArray)
                                {
                                    for (int i = 0; i < _additionalMEFxLayersProp.arraySize; i++)
                                    {
                                        var el = _additionalMEFxLayersProp.GetArrayElementAtIndex(i);
                                        el.objectReferenceValue = null;
                                    }
                                    _additionalMEFxLayersProp.arraySize = 0;
                                }
                            }
                        }

                        if (_useAdditionalMEFxLayersProp.boolValue)
                        {
                            EditorGUI.indentLevel++;


                            EditorGUILayout.HelpBox(
                                Tr(
                                    "psha.additional_fx_perf_info",
                                    "Using multiple FX layers can negatively impact avatar animation performance and optimization.\n" +
                                    "Use additional ME FX layers only when necessary."
                                ),
                                MessageType.Info
                            );


                            int size = _additionalMEFxLayersProp.arraySize;
                            int newSize = EditorGUILayout.IntSlider(GC("psha.layer_count", "Layer Count", "psha.tt.layer_count", "Additional ME FX layer count."), size, 0, 2);
                            if (newSize != size)
                            {

                                if (newSize < size)
                                {
                                    for (int j = newSize; j < size; j++)
                                    {
                                        var el = _additionalMEFxLayersProp.GetArrayElementAtIndex(j);
                                        el.objectReferenceValue = null;
                                    }
                                }

                                _additionalMEFxLayersProp.arraySize = newSize;
                            }

                            for (int i = 0; i < _additionalMEFxLayersProp.arraySize; i++)
                            {
                                var element = _additionalMEFxLayersProp.GetArrayElementAtIndex(i);
                                EditorGUILayout.PropertyField(
                                    element,
                                    new GUIContent(Trf("psha.me_fx_layer_n", " + ME FX Layer {0}", i + 1), Tr("psha.tt.me_fx_layer", "Additional ME FX template controller."))
                                );
                            }

                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }


                    if (_changeEmoteMenuIconProp != null)
                    {
                        EditorGUILayout.PropertyField(
                            _changeEmoteMenuIconProp,
                            GC("psha.replace_me_menu_icon", "Replace ME Menu Icon", "psha.replace_me_menu_icon_tt", "Replace the VRC Emote menu icon with the ME icon.")
                        );
                    }

                    EditorGUILayout.Space(4);
                }


                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);



            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Tr("psha.btn_setup", "Setup VRC Emote")))
            {
                SetupVRCEmote(mgr);
            }


            if (GUILayout.Button(Tr("psha.btn_default", "Default"), GUILayout.Width(70)))
            {

                Undo.RecordObjects(targets, Tr("psha.undo_reset", "Reset Psha VRC Emote Installer"));

                foreach (var t in targets)
                {
                    if (t is PshaVRCEmoteInstaller inst)
                    {
                        inst.ResetToDefaults();
                        EditorUtility.SetDirty(inst);
                    }
                }


                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.Space();


        s_previewFoldout = EditorGUILayout.Foldout(s_previewFoldout, Tr("psha.preview_foldout", "Show Applied Changes (Preview)"));
        if (s_previewFoldout)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawCombinedPreview(mgr);
            }
        }
        DrawEditorLanguageSection();
        serializedObject.ApplyModifiedProperties();
    }





    private void DrawPlacementGuidance(PshaVRCEmoteInstaller mgr)
    {
        if (mgr == null) return;


        var descriptor = mgr.GetComponentInParent<VRCAvatarDescriptor>();
        if (descriptor == null)
        {
            EditorGUILayout.HelpBox(
                Tr(
                    "psha.warn_need_descriptor",
                    "For this component to work correctly, it must be placed under the avatar GameObject that has a VRCAvatarDescriptor.\n" +
                    "Move this object under the avatar root (the GameObject with the descriptor)."
                ),
                MessageType.Warning
            );
            EditorGUILayout.Space(4);
        }
    }






    private void DrawEmoteNameMismatchWarning(PshaVRCEmoteInstaller mgr)
    {
        if (mgr == null) return;


        if (!mgr.useMergeMEFxLayer) return;

        var emoteName = mgr.emoteName;
        var objName = mgr.gameObject != null ? mgr.gameObject.name : null;


        if (string.IsNullOrEmpty(emoteName)) return;

        if (!string.Equals(emoteName, objName, System.StringComparison.Ordinal))
        {
            EditorGUILayout.HelpBox(
                Tr(
                    "psha.warn_name_mismatch",
                    "The emote name does not match the GameObject name.\n" +
                    "Results may differ when using ME FX layers.\n" +
                    "Click 'Setup VRC Emote' or make the names match."
                ),
                MessageType.Warning
            );
            EditorGUILayout.Space(4);
        }
    }






    private void DrawSharedFxLayerWarning(PshaVRCEmoteInstaller mgr)
    {
        if (mgr == null) return;


        if (!mgr.useMergeMEFxLayer || mgr.fxMELayer == null) return;


        var descriptor = mgr.GetComponentInParent<VRCAvatarDescriptor>();
        if (descriptor == null) return;


        var all = descriptor.GetComponentsInChildren<PshaVRCEmoteInstaller>(true);
        PshaVRCEmoteInstaller duplicate = null;

        foreach (var other in all)
        {
            if (other == null) continue;
            if (other == mgr) continue;
            if (!other.useMergeMEFxLayer) continue;
            if (other.fxMELayer == null) continue;

            if (other.fxMELayer == mgr.fxMELayer)
            {
                duplicate = other;
                break;
            }
        }

        if (duplicate != null)
        {
            string objName = duplicate.gameObject != null
                ? duplicate.gameObject.name
                : Tr("psha.no_gameobject", "(No GameObject)");

            EditorGUILayout.HelpBox(
                Trf(
                    "psha.warn_shared_fx",
                    "There are multiple ME emotes sharing the same ME FX layer.\n" +
                    "Results may differ when using ME FX layers.\n" +
                    "Duplicate ME FX layer object name: {0}",
                    objName
                ),
                MessageType.Warning
            );
            EditorGUILayout.Space(4);
        }
    }





    private void DrawCombinedPreview(PshaVRCEmoteInstaller current)
    {

        var descriptor = current.GetComponentInParent<VRCAvatarDescriptor>();
        if (descriptor == null)
        {
            EditorGUILayout.HelpBox(Tr("psha.no_descriptor_short", "Could not find a parent VRCAvatarDescriptor."), MessageType.Info);
            return;
        }

        var rootMenu = descriptor.expressionsMenu;
        if (rootMenu == null)
        {
            EditorGUILayout.HelpBox(Tr("psha.menu_empty", "The avatar Expressions Menu is empty."), MessageType.Info);
            return;
        }


        var explicitMenu = _targetMenuProp.objectReferenceValue as VRCExpressionsMenu;
        var autoEmoteMenu = FindEmoteMenu(rootMenu);

        VRCExpressionsMenu previewMenu = explicitMenu ?? autoEmoteMenu;
        if (previewMenu == null)
        {
            if (explicitMenu == null)
            {
                EditorGUILayout.HelpBox(
                    Tr("psha.preview_no_menu", "Target VRC emote menu is empty, and the emote menu could not be auto detected on the avatar."),
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    Tr("psha.preview_target_no_controls", "The target VRC emote menu has no controls."),
                    MessageType.Warning
                );
            }
            return;
        }

        bool previewIsAuto = (explicitMenu == null);

        if (previewIsAuto)
        {
            EditorGUILayout.HelpBox(
                Trf("psha.preview_auto_detected", "Auto detected emote menu for preview: {0}", previewMenu.name),
                MessageType.None
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                Trf("psha.preview_menu", "Preview menu: {0}", previewMenu.name),
                MessageType.None
            );
        }

        if (previewMenu.controls == null || previewMenu.controls.Count == 0)
        {
            EditorGUILayout.HelpBox(Tr("psha.preview_menu_no_controls", "The preview menu has no controls."), MessageType.Warning);
            return;
        }



        var finalList = CollectFinalSlotWinners(descriptor);
        if (finalList.Count == 0)
        {
            EditorGUILayout.HelpBox(
                Tr("psha.preview_no_installers", "There are no active PshaVRCEmoteInstaller components, so there is nothing to preview."),
                MessageType.Info
            );
            return;
        }

        var affectingManagers = new List<PshaVRCEmoteInstaller>();
        foreach (var m in finalList)
        {
            if (m == null) continue;

            var targetMenu = m.targetMenu != null ? m.targetMenu : autoEmoteMenu;
            if (targetMenu == previewMenu)
            {
                affectingManagers.Add(m);
            }
        }

        int controlCount = previewMenu.controls.Count;


        var beforeNames = new string[controlCount];
        var beforeTypes = new VRCExpressionsMenu.Control.ControlType[controlCount];
        var afterNames = new string[controlCount];
        var afterTypes = new VRCExpressionsMenu.Control.ControlType[controlCount];
        var finalManager = new PshaVRCEmoteInstaller[controlCount];
        var changed = new bool[controlCount];


        for (int i = 0; i < controlCount; i++)
        {
            var ctrl = previewMenu.controls[i];
            beforeNames[i] = ctrl != null ? ctrl.name : "";
            beforeTypes[i] = ctrl != null ? ctrl.type : VRCExpressionsMenu.Control.ControlType.Button;

            afterNames[i] = beforeNames[i];
            afterTypes[i] = beforeTypes[i];
            finalManager[i] = null;
            changed[i] = false;
        }


        for (int i = affectingManagers.Count - 1; i >= 0; i--)
        {
            var m = affectingManagers[i];
            if (m == null) continue;
            if (controlCount == 0) break;

            int slot = Mathf.Clamp(m.slotIndex, 1, 8);
            int idx = FindEmoteControlIndex(previewMenu, slot);
            if (idx < 0) continue;

            bool didChange = false;


            if (!string.IsNullOrEmpty(m.emoteName))
            {
                var raw = m.emoteName;
                var processed = raw.Replace("<br>", " ");
                afterNames[idx] = processed;
                didChange = true;
            }


            if (m.controlType != PshaVRCEmoteInstaller.EmoteControlType.None)
            {
                afterTypes[idx] = (m.controlType == PshaVRCEmoteInstaller.EmoteControlType.Toggle)
                    ? VRCExpressionsMenu.Control.ControlType.Toggle
                    : VRCExpressionsMenu.Control.ControlType.Button;
                didChange = true;
            }

            if (didChange)
            {
                finalManager[idx] = m;
                changed[idx] = true;
            }
        }



        float noWidth = 25f;
        float beforeNameWidth = 60f;
        float afterNameWidth = 110f;
        float typeWidth = 60f;


        var nameColStyle = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            wordWrap = false,
            clipping = TextClipping.Clip
        };

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(Tr("psha.preview_no", "No."), GUILayout.Width(noWidth));
        EditorGUILayout.LabelField(Tr("psha.preview_name_before", "Name →→"), GUILayout.Width(beforeNameWidth));
        EditorGUILayout.LabelField(Tr("psha.preview_name_after", "Name (After)"), GUILayout.Width(afterNameWidth));
        EditorGUILayout.LabelField(Tr("psha.preview_type_before", "Type →→"), GUILayout.Width(typeWidth));
        EditorGUILayout.LabelField(Tr("psha.preview_type_after", "Type"), GUILayout.Width(typeWidth));
        EditorGUILayout.EndHorizontal();


        for (int i = 0; i < controlCount; i++)
        {
            var ctrl = previewMenu.controls[i];
            if (ctrl == null) continue;

            int no = i + 1;

            string beforeName = beforeNames[i];
            string beforeType = beforeTypes[i].ToString();


            string afterNameStr = changed[i] ? afterNames[i] : "-";
            string afterTypeStr = changed[i] ? afterTypes[i].ToString() : "-";

            bool ownedByCurrent = (finalManager[i] == current);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(no.ToString(), GUILayout.Width(noWidth));
            EditorGUILayout.LabelField(beforeName, nameColStyle, GUILayout.Width(beforeNameWidth));

            if (ownedByCurrent && changed[i])
            {
                var boldName = new GUIStyle(nameColStyle)
                {
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField(afterNameStr, boldName, GUILayout.Width(afterNameWidth));
            }
            else
            {
                EditorGUILayout.LabelField(afterNameStr, nameColStyle, GUILayout.Width(afterNameWidth));
            }

            EditorGUILayout.LabelField(beforeType, GUILayout.Width(typeWidth));

            if (ownedByCurrent && changed[i])
            {
                var bold = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField(afterTypeStr, bold, GUILayout.Width(typeWidth));
            }
            else
            {
                EditorGUILayout.LabelField(afterTypeStr, GUILayout.Width(typeWidth));
            }

            EditorGUILayout.EndHorizontal();
        }
    }


    private void SetupVRCEmote(PshaVRCEmoteInstaller mgr)
    {
        if (mgr == null) return;

        var descriptor = mgr.GetComponentInParent<VRCAvatarDescriptor>();
        if (descriptor == null)
        {
            EditorUtility.DisplayDialog(
                Tr("psha.dialog_setup_title", "Setup VRC Emote"),
                Tr("psha.dialog_no_descriptor", "Could not find a parent VRCAvatarDescriptor.\nMake sure this object is placed under the avatar root."),
                Tr("psha.ok", "OK")
            );
            return;
        }


        var layers = descriptor.baseAnimationLayers;

        AnimatorController actionController = null;
        bool actionIsDefault = false;

        if (layers != null && layers.Length > 0)
        {
            foreach (var layer in layers)
            {
                switch (layer.type)
                {
                    case VRCAvatarDescriptor.AnimLayerType.Action:

                        _actionLayerProp.objectReferenceValue = layer.animatorController;
                        actionIsDefault = layer.isDefault;
                        actionController = layer.animatorController as AnimatorController;
                        break;

                    case VRCAvatarDescriptor.AnimLayerType.FX:

                        if (layer.animatorController != null)
                        {
                            _fxLayerProp.objectReferenceValue = layer.animatorController;
                        }
                        break;
                }
            }
        }


        string startStateName = null;
        string endStateName = null;

        if (actionController != null)
        {

            TryDetectEmoteStartEnd(actionController, out startStateName, out endStateName);
        }



        if (actionIsDefault)
        {
            if (string.IsNullOrEmpty(startStateName))
                startStateName = "Prepare Standing";

            if (string.IsNullOrEmpty(endStateName))
                endStateName = "BlendOut Stand";
        }




        if (_showActionMergeScopeProp.boolValue &&
            actionController != null &&
            !string.IsNullOrEmpty(startStateName) &&
            !string.IsNullOrEmpty(endStateName))
        {
            string mergeScopeName;
            if (TryDetectEmoteMergeScope(
                    actionController,
                    startStateName,
                    endStateName,
                    out mergeScopeName))
            {

                _actionMergeScopeProp.stringValue = mergeScopeName;
            }
        }




        _startActionStateProp.stringValue =
            !string.IsNullOrEmpty(startStateName) ? startStateName : "(Inspection failed)";

        _endActionStateProp.stringValue =
            !string.IsNullOrEmpty(endStateName) ? endStateName : "(Inspection failed)";


        var rootMenu = descriptor.expressionsMenu;
        if (rootMenu == null)
        {
            EditorUtility.DisplayDialog(
                Tr("psha.dialog_setup_title", "Setup VRC Emote"),
                Tr("psha.dialog_menu_not_assigned", "The avatar Expressions Menu is not assigned."),
                Tr("psha.ok", "OK")
            );
            return;
        }

        var emoteMenu = FindEmoteMenu(rootMenu);
        if (emoteMenu != null)
        {
            _targetMenuProp.objectReferenceValue = emoteMenu;
        }
        else
        {
            EditorUtility.DisplayDialog(
               "Setup VRC Emote",
               Tr(
                   "psha.dialog_failed_auto_detect_menu",
                   "Failed to auto detect the emote menu.\nPlease assign the target VRC emote menu manually."
               ),
               Tr("psha.ok", "OK")
           );
        }



        if (!string.IsNullOrEmpty(mgr.emoteName) && mgr.gameObject != null)
        {
            if (mgr.gameObject.name != mgr.emoteName)
            {
                Undo.RecordObject(mgr.gameObject, "Rename Emote Object");
                mgr.gameObject.name = mgr.emoteName;
                EditorUtility.SetDirty(mgr.gameObject);
            }
        }
    }




    private static VRCExpressionsMenu FindEmoteMenu(VRCExpressionsMenu root)
    {
        if (root == null) return null;

        var visited = new HashSet<VRCExpressionsMenu>();
        var queue = new Queue<VRCExpressionsMenu>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var menu = queue.Dequeue();
            if (menu == null || visited.Contains(menu)) continue;
            visited.Add(menu);

            if (IsVRCEmoteMenu(menu))
                return menu;

            if (menu.controls == null) continue;

            foreach (var ctrl in menu.controls)
            {
                if (ctrl?.subMenu != null)
                    queue.Enqueue(ctrl.subMenu);
            }
        }

        return null;
    }

    private static bool IsVRCEmoteMenu(VRCExpressionsMenu menu)
    {
        if (menu.controls == null || menu.controls.Count == 0) return false;

        int emoteCount = 0;
        foreach (var ctrl in menu.controls)
        {
            if (ctrl == null || ctrl.parameter == null) continue;

            if (ctrl.parameter.name == "VRCEmote" &&
                (ctrl.type == VRCExpressionsMenu.Control.ControlType.Button ||
                 ctrl.type == VRCExpressionsMenu.Control.ControlType.Toggle))
            {
                emoteCount++;
            }
        }

        return emoteCount >= 4;
    }




    private class EmoteStateInfo
    {
        public int OutgoingStandingEmoteTransitions;
        public int IncomingStandingEmoteTransitions;
    }











    private static void TryDetectEmoteStartEnd(
        AnimatorController controller,
        out string startStateName,
        out string endStateName)
    {
        startStateName = null;
        endStateName = null;

        if (controller == null) return;

        var map = new Dictionary<AnimatorState, EmoteStateInfo>();

        foreach (var layer in controller.layers)
        {
            if (layer.stateMachine == null) continue;
            CollectStandingEmoteStateInfo(layer.stateMachine, map);
        }

        if (map.Count == 0) return;

        AnimatorState bestStart = null;
        int bestStartCount = 0;
        AnimatorState bestEnd = null;
        int bestEndCount = 0;

        foreach (var kvp in map)
        {
            var state = kvp.Key;
            var info = kvp.Value;
            if (state == null) continue;


            if (IsSitLikeName(state.name))
                continue;

            if (info.OutgoingStandingEmoteTransitions > bestStartCount)
            {
                bestStartCount = info.OutgoingStandingEmoteTransitions;
                bestStart = state;
            }

            if (info.IncomingStandingEmoteTransitions > bestEndCount)
            {
                bestEndCount = info.IncomingStandingEmoteTransitions;
                bestEnd = state;
            }
        }

        if (bestStart != null && bestStartCount > 0)
            startStateName = bestStart.name;

        if (bestEnd != null && bestEndCount > 0)
            endStateName = bestEnd.name;
    }


    private static void CollectStandingEmoteStateInfo(
        AnimatorStateMachine sm,
        Dictionary<AnimatorState, EmoteStateInfo> map)
    {
        if (sm == null) return;


        foreach (var child in sm.states)
        {
            var state = child.state;
            if (state == null) continue;

            if (!map.TryGetValue(state, out var info))
            {
                info = new EmoteStateInfo();
                map[state] = info;
            }

            bool fromSit = IsSitLikeName(state.name);


            foreach (var t in state.transitions)
            {
                ProcessEmoteTransition(state, t, fromSit, map);
            }
        }


        foreach (var any in sm.anyStateTransitions)
        {
            ProcessEmoteTransition(null, any, false, map);
        }


        foreach (var sub in sm.stateMachines)
        {
            CollectStandingEmoteStateInfo(sub.stateMachine, map);
        }
    }






    private static void ProcessEmoteTransition(
        AnimatorState fromState,
        AnimatorTransitionBase t,
        bool fromSit,
        Dictionary<AnimatorState, EmoteStateInfo> map)
    {
        if (t == null) return;

        var conditions = t.conditions;
        if (conditions == null || conditions.Length == 0) return;

        bool hasVrcEmote = false;
        bool hasStandingSlot = false;
        bool isSitTransition = false;

        for (int i = 0; i < conditions.Length; i++)
        {
            var cond = conditions[i];
            if (cond.parameter != "VRCEmote") continue;

            hasVrcEmote = true;

            if (cond.mode == AnimatorConditionMode.Equals)
            {
                int value = Mathf.RoundToInt(cond.threshold);

                if (value >= 1 && value <= 8)
                {
                    hasStandingSlot = true;
                }
                else if (value >= 9)
                {

                    isSitTransition = true;
                }
            }
        }


        if (!hasVrcEmote) return;


        if (isSitTransition) return;




        if (fromState != null && !fromSit && hasStandingSlot)
        {
            if (!map.TryGetValue(fromState, out var fromInfo))
            {
                fromInfo = new EmoteStateInfo();
                map[fromState] = fromInfo;
            }

            fromInfo.OutgoingStandingEmoteTransitions++;
        }




        var dest = t.destinationState;
        if (dest != null && !IsSitLikeName(dest.name))
        {
            if (!map.TryGetValue(dest, out var destInfo))
            {
                destInfo = new EmoteStateInfo();
                map[dest] = destInfo;
            }

            destInfo.IncomingStandingEmoteTransitions++;
        }
    }













    private static bool IsSitLikeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        var lower = name.ToLowerInvariant();


        if (lower.Contains(" sit") || lower.Contains("sit ")
            || lower.Contains("_sit") || lower.Contains("sit_"))
        {
            return true;
        }

        return false;
    }








    private static bool TryDetectEmoteMergeScope(
        AnimatorController controller,
        string startStateName,
        string endStateName,
        out string scopeName)
    {
        scopeName = null;

        if (controller == null ||
            string.IsNullOrEmpty(startStateName) ||
            string.IsNullOrEmpty(endStateName))
        {
            return false;
        }

        var candidates = new List<AnimatorStateMachine>();

        foreach (var layer in controller.layers)
        {
            if (layer.stateMachine == null) continue;
            CollectCandidateStateMachinesByStates(
                layer.stateMachine,
                startStateName,
                endStateName,
                candidates);
        }

        if (candidates.Count == 0)
            return false;


        foreach (var sm in candidates)
        {
            if (ContainsVRCEmoteEqualsInRange(sm, 1, 8))
            {
                scopeName = sm.name;
                return true;
            }
        }

        return false;
    }





    private static void CollectCandidateStateMachinesByStates(
        AnimatorStateMachine sm,
        string startStateName,
        string endStateName,
        List<AnimatorStateMachine> result)
    {
        if (sm == null) return;

        bool hasStart = false;
        bool hasEnd = false;

        foreach (var child in sm.states)
        {
            var state = child.state;
            if (state == null) continue;

            if (state.name == startStateName) hasStart = true;
            if (state.name == endStateName) hasEnd = true;
        }

        if (hasStart && hasEnd)
        {
            result.Add(sm);
        }


        foreach (var sub in sm.stateMachines)
        {
            CollectCandidateStateMachinesByStates(
                sub.stateMachine,
                startStateName,
                endStateName,
                result);
        }
    }






    private static bool ContainsVRCEmoteEqualsInRange(
        AnimatorStateMachine sm,
        int minSlot,
        int maxSlot)
    {
        if (sm == null) return false;


        foreach (var child in sm.states)
        {
            var state = child.state;
            if (state == null) continue;

            foreach (var t in state.transitions)
            {
                if (TransitionHasVRCEmoteEqualsInRange(t, minSlot, maxSlot))
                    return true;
            }
        }


        foreach (var t in sm.anyStateTransitions)
        {
            if (TransitionHasVRCEmoteEqualsInRange(t, minSlot, maxSlot))
                return true;
        }


        foreach (var t in sm.entryTransitions)
        {
            if (TransitionHasVRCEmoteEqualsInRange(t, minSlot, maxSlot))
                return true;
        }


        foreach (var sub in sm.stateMachines)
        {
            var subSm = sub.stateMachine;
            if (subSm == null) continue;

            var transitions = sm.GetStateMachineTransitions(subSm);
            foreach (var t in transitions)
            {
                if (TransitionHasVRCEmoteEqualsInRange(t, minSlot, maxSlot))
                    return true;
            }
        }


        foreach (var sub in sm.stateMachines)
        {
            if (ContainsVRCEmoteEqualsInRange(sub.stateMachine, minSlot, maxSlot))
                return true;
        }

        return false;
    }




    private static bool TransitionHasVRCEmoteEqualsInRange(
        AnimatorTransitionBase t,
        int minSlot,
        int maxSlot)
    {
        if (t == null) return false;

        var conditions = t.conditions;
        if (conditions == null || conditions.Length == 0) return false;

        for (int i = 0; i < conditions.Length; i++)
        {
            var cond = conditions[i];
            if (cond.parameter != "VRCEmote") continue;
            if (cond.mode != AnimatorConditionMode.Equals) continue;

            int value = Mathf.RoundToInt(cond.threshold);
            if (value >= minSlot && value <= maxSlot)
            {
                return true;
            }
        }

        return false;
    }








    private const string EmoteParamName = "VRCEmote";

    private static List<PshaVRCEmoteInstaller> CollectFinalSlotWinners(VRCAvatarDescriptor descriptor)
    {
        var all = descriptor != null
            ? descriptor.GetComponentsInChildren<PshaVRCEmoteInstaller>(true)
            : Array.Empty<PshaVRCEmoteInstaller>();

        var list = new List<PshaVRCEmoteInstaller>(all);
        list.RemoveAll(m => m == null || !m.gameObject.activeInHierarchy);

        list.Sort((a, b) => CompareHierarchyOrder(a.transform, b.transform));

        var used = new bool[9];
        var finalList = new List<PshaVRCEmoteInstaller>();
        foreach (var m in list)
        {
            int slot = Mathf.Clamp(m.slotIndex, 1, 8);
            if (used[slot]) continue;
            used[slot] = true;
            finalList.Add(m);
        }

        return finalList;
    }

    private static int FindEmoteControlIndex(VRCExpressionsMenu menu, int slot)
    {
        if (menu?.controls == null) return -1;

        for (int i = 0; i < menu.controls.Count; i++)
        {
            var c = menu.controls[i];
            if (c?.parameter == null) continue;

            if (c.parameter.name == EmoteParamName && Mathf.RoundToInt(c.value) == slot)
                return i;
        }

        int idx = slot - 1;
        if (0 <= idx && idx < menu.controls.Count) return idx;

        return -1;
    }


    private static int CompareHierarchyOrder(Transform a, Transform b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        var pathA = BuildSiblingIndexPath(a);
        var pathB = BuildSiblingIndexPath(b);

        int min = System.Math.Min(pathA.Count, pathB.Count);
        for (int i = 0; i < min; i++)
        {
            int cmp = pathA[i].CompareTo(pathB[i]);
            if (cmp != 0) return cmp;
        }

        int lenCmp = pathA.Count.CompareTo(pathB.Count);
        if (lenCmp != 0) return lenCmp;

        return a.GetInstanceID().CompareTo(b.GetInstanceID());
    }

    private static List<int> BuildSiblingIndexPath(Transform t)
    {
        var path = new List<int>(8);
        while (t != null)
        {
            path.Add(t.GetSiblingIndex());
            t = t.parent;
        }
        path.Reverse();
        return path;
    }




    private const string PrefKey_UseSystemLanguage = "PshaVRCEmote.EditorLanguage.UseSystem";
    private const string PrefKey_LanguageCode = "PshaVRCEmote.EditorLanguage.Code";

    private const string LegacyPrefKey_UseSystemLanguage = "PshaVRCEmote.EditorLang.UseSystem";
    private const string LegacyPrefKey_LanguageCode = "PshaVRCEmote.EditorLang.Code";

    private static bool s_prefsMigrated = false;

    private static void EnsurePrefsMigrated()
    {
        if (s_prefsMigrated) return;
        s_prefsMigrated = true;

        if (!UnityEditor.EditorPrefs.HasKey(PrefKey_UseSystemLanguage) && UnityEditor.EditorPrefs.HasKey(LegacyPrefKey_UseSystemLanguage))
        {
            UnityEditor.EditorPrefs.SetBool(PrefKey_UseSystemLanguage, UnityEditor.EditorPrefs.GetBool(LegacyPrefKey_UseSystemLanguage, true));
        }

        if (!UnityEditor.EditorPrefs.HasKey(PrefKey_LanguageCode) && UnityEditor.EditorPrefs.HasKey(LegacyPrefKey_LanguageCode))
        {
            UnityEditor.EditorPrefs.SetString(PrefKey_LanguageCode, UnityEditor.EditorPrefs.GetString(LegacyPrefKey_LanguageCode, "en-us"));
        }
    }

    private struct LangItem
    {
        public string Code;
        public string Display;
        public LangItem(string code, string display) { Code = code; Display = display; }
    }

    private static readonly LangItem[] s_langItems =
    {
    new LangItem("en-us",  "English (United States)"),
    new LangItem("ja-jp",  "日本語 (日本)"),
    new LangItem("ko-kr",  "한국어 (Korea)"),
    new LangItem("zh-hans","中文（简体）"),
    new LangItem("zh-hant","中文（繁體）"),
};

    private static bool GetUseSystemLanguage()
    {
        EnsurePrefsMigrated();
        return UnityEditor.EditorPrefs.GetBool(PrefKey_UseSystemLanguage, true);
    }

    private static void SetUseSystemLanguage(bool v)
    {
        EnsurePrefsMigrated();
        UnityEditor.EditorPrefs.SetBool(PrefKey_UseSystemLanguage, v);
    }

    private static string GetSavedLanguageCode()
    {
        EnsurePrefsMigrated();
        return UnityEditor.EditorPrefs.GetString(PrefKey_LanguageCode, "en-us");
    }

    private static void SetSavedLanguageCode(string code)
    {
        EnsurePrefsMigrated();
        UnityEditor.EditorPrefs.SetString(PrefKey_LanguageCode, code ?? "en-us");
    }

    private static string GetSystemLanguageCode()
    {

        switch (UnityEngine.Application.systemLanguage)
        {
            case UnityEngine.SystemLanguage.Japanese: return "ja-jp";
            case UnityEngine.SystemLanguage.Korean: return "ko-kr";
            case UnityEngine.SystemLanguage.ChineseSimplified: return "zh-hans";
            case UnityEngine.SystemLanguage.ChineseTraditional: return "zh-hant";
            default: return "en-us";
        }
    }

    private static string GetEffectiveLanguageCode()
    {
        return GetUseSystemLanguage() ? GetSystemLanguageCode() : GetSavedLanguageCode();
    }

    private static string CodeToDisplay(string code)
    {
        for (int i = 0; i < s_langItems.Length; i++)
            if (string.Equals(s_langItems[i].Code, code, StringComparison.OrdinalIgnoreCase))
                return s_langItems[i].Display;


        return code ?? "en-us";
    }

    private static void DrawEditorLanguageSection()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        using (new EditorGUILayout.VerticalScope("box"))
        {

            Rect row = EditorGUILayout.GetControlRect();
            Rect labelRect = row;
            labelRect.width = EditorGUIUtility.labelWidth;

            Rect buttonRect = row;
            buttonRect.xMin = labelRect.xMax;

            EditorGUI.LabelField(labelRect, "Editor Language");

            string systemCode = GetSystemLanguageCode();
            bool useSystem = GetUseSystemLanguage();

            string savedCode = GetSavedLanguageCode();
            bool sameAsSystem = string.Equals(savedCode, systemCode, StringComparison.OrdinalIgnoreCase);

            bool autoChecked = useSystem || sameAsSystem;

            string effectiveCode = useSystem ? systemCode : savedCode;
            string effectiveDisplay = CodeToDisplay(effectiveCode);


            if (GUI.Button(buttonRect, effectiveDisplay, EditorStyles.popup))
            {
                var menu = new GenericMenu();


                for (int i = 0; i < s_langItems.Length; i++)
                {
                    var item = s_langItems[i];
                    bool on = string.Equals(item.Code, effectiveCode, StringComparison.OrdinalIgnoreCase);

                    menu.AddItem(new GUIContent(item.Display), on, () =>
                    {
                        SetSavedLanguageCode(item.Code);



                        bool sameAsSystem = string.Equals(item.Code, systemCode, StringComparison.OrdinalIgnoreCase);
                        SetUseSystemLanguage(sameAsSystem);
                    });
                }


                menu.AddSeparator("");


                menu.AddItem(new GUIContent("Use System Language (Auto)"), autoChecked, () =>
                {
                    bool now = GetUseSystemLanguage();
                    SetUseSystemLanguage(!now);
                });


                menu.DropDown(buttonRect);
            }
        }
    }
}
#endif
