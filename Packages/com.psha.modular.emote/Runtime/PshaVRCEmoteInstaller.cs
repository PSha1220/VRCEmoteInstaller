using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDKBase;

[HelpURL("https://psha.booth.pm/")]
[AddComponentMenu("ME Psha VRC Emote Installer")]
public class PshaVRCEmoteInstaller : MonoBehaviour, IEditorOnly
{
    
    public enum EmoteControlType
    {
        None = 0,   
        Button = 1,
        Toggle = 2
    }


    // [Header("VRC Emote Settings")]
    [Tooltip("VRCEmote menu to customize. Leave empty to auto detect at build time.")]
    public VRCExpressionsMenu targetMenu;

    [Tooltip("Name shown in the VRCEmote menu")]
    public string emoteName = "[ME]New Emote";

    [Tooltip("VRCEmote slot number (1 to 8)")]
    [Range(1, 8)]
    public int slotIndex = 1;

    [Tooltip("VRCEmote Menu Icon")]
    public Texture2D menuIcon;

    [Tooltip("VRCEmote control type (None keeps the existing type)")]
    public EmoteControlType controlType = EmoteControlType.None;

[Tooltip("Force Write Defaults off for all states in ME layers.")]
    public bool meWriteDefaultsOff = true;


    [Tooltip("Replace the VRC Emote menu icon with the ME icon.")]
    public bool changeEmoteMenuIcon = true;
    
    
    

    [Tooltip("Avatar Action layer controller (leave empty to use the descriptor Action layer)")]
    public RuntimeAnimatorController actionLayer;

    [Tooltip("Avatar FX layer controller")]
    public RuntimeAnimatorController fxLayer;

    [Tooltip("ME Action template controller for merging")]
    public RuntimeAnimatorController actionMELayer;

    [Tooltip("ME FX template controller for merging")]
    public RuntimeAnimatorController fxMELayer;

    [Tooltip("State name that starts the emote branch in the Action layer")]
    public string startActionState;

    [Tooltip("State name that ends the emote branch in the Action layer")]
    public string endActionState;

    [Tooltip("StateMachine path or name to use as the Action merge scope (optional)")]
    public string actionMergeScope;

    [Tooltip("Whether to show the merge scope field in the inspector")]
    public bool showActionMergeScopeInInspector;

    
    

    [Tooltip("Whether to use the merge ME FX layer template")]
    public bool useMergeMEFxLayer;

    [Tooltip("Whether to use additional ME FX layers")]
    public bool useAdditionalMEFxLayers;

    [Tooltip("Additional ME FX layers to merge (up to 2 recommended)")]
    public RuntimeAnimatorController[] additionalMEFxLayers;


    [SerializeField, HideInInspector]
    private int valueInternal = 1;

    [SerializeField, HideInInspector]
    private string parameterNameInternal = "VRCEmote";

    private void OnValidate()
    {
        slotIndex = Mathf.Clamp(slotIndex, 1, 8);
        valueInternal = slotIndex;
        parameterNameInternal = "VRCEmote";
    }


public void ResetToDefaults()
{
    
    emoteName = "[ME]New Emote";
    slotIndex = 1;

#if UNITY_EDITOR
    
    var script = UnityEditor.MonoScript.FromMonoBehaviour(this);
    var defaultIcon = UnityEditor.AssetPreview.GetMiniThumbnail(script);
    menuIcon = defaultIcon;
#else
    
    menuIcon = null;
#endif

    controlType = EmoteControlType.None;


    
    changeEmoteMenuIcon = true;
    
    targetMenu = null;

    
    actionLayer = null;
    fxLayer = null;
    actionMELayer = null;
    fxMELayer = null;

    
    startActionState = string.Empty;
    endActionState = string.Empty;
    actionMergeScope = string.Empty;
    showActionMergeScopeInInspector = false;

    
    useMergeMEFxLayer = false;
    useAdditionalMEFxLayers = false;
    additionalMEFxLayers = null;


    
    OnValidate();
}
    public int Value => valueInternal;
    public string ParameterName => parameterNameInternal;
}
