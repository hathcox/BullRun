using System;

/// <summary>
/// Marks a static class for auto-discovery by SetupPipeline.
/// The class must have a public static void Execute() method.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SetupClassAttribute : Attribute
{
    public SetupPhase Phase { get; }
    public int Order { get; }

    public SetupClassAttribute(SetupPhase phase, int order = 0)
    {
        Phase = phase;
        Order = order;
    }
}
