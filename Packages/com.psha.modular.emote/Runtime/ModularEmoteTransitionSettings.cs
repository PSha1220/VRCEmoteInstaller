using UnityEngine;

public class ModularEmoteTransitionSettings : StateMachineBehaviour
{

    [Tooltip("Transition interruption source.")]
    public TransitionInterruptionSource interruptionSource = TransitionInterruptionSource.None;

    public enum TransitionInterruptionSource
    {
        None,
        Source,
        Destination,
        SourceThenDestination,
        DestinationThenSource
    }

    [System.Serializable]
public class Condition
{
    public enum ParameterType
    {
        Bool,
        Int,
        Float,
        Trigger,
    }

    public enum IntComparison
    {
        Greater,
        Equal,
        Less,
        NotEqual,
    }

    public enum FloatComparison
    {
        Greater,
        Equal,
        Less,
        NotEqual,
    }

    [Tooltip("Animator parameter name used for the condition.")]
    public string parameter;

    [Tooltip("Parameter type (Bool, Int, Float, Trigger).")]
    public ParameterType type = ParameterType.Bool;

    [Header("Bool Setting")]
    [Tooltip("Value used when the type is Bool.")]
    public bool boolValue;

    [Header("Int Setting")]
    [Tooltip("Comparison used when the type is Int.")]
    public IntComparison intComparison = IntComparison.Equal;

    [Tooltip("Integer value used when the type is Int.")]
    public int intValue;

    [Header("Float Setting")]
    [Tooltip("Comparison used when the type is Float.")]
    public FloatComparison floatComparison = FloatComparison.Equal;

    [Tooltip("Float value used when the type is Float.")]
    public float floatValue;
}

    [Header("Transition Settings")]
    [Tooltip("Whether to use Exit Time.")]
    public bool transitionHasExitTime = false;

    [Tooltip("Exit Time value used when Has Exit Time is enabled.")]
    [Range(0f, 1f)]
    public float transitionExitTime = 0f;

    [Tooltip("Transition duration in seconds.")]
    public float transitionDuration = 0.1f;

    [Tooltip("Transition offset (0 to 1).")]
    [Range(0f, 1f)]
    public float transitionOffset = 0f;

    [Tooltip("Whether duration is fixed time (seconds) or normalized.")]
    public bool useFixedDuration = true;

    [Tooltip("Whether interruptions are evaluated in order.")]
    public bool orderedInterruption = false;

    [Header("Additional Conditions")]
    [Tooltip(
        "Additional conditions applied to the StartState to template entry transition.\n" +
        "The VRCEmote equals slot index condition is added automatically by the pass.\n" +
        "Use this for extra filters such as IsSeated or custom mode parameters."
    )]
    public Condition[] conditions = System.Array.Empty<Condition>();
}
