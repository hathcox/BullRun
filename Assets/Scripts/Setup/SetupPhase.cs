/// <summary>
/// Defines the execution phases for the Setup-Oriented Generation Framework.
/// Setup classes are sorted by phase (then by order within phase) during F5 rebuild.
/// </summary>
public enum SetupPhase
{
    ClearGenerated = 0,
    Prefabs = 10,
    SceneComposition = 20,
    WireReferences = 30
}
