# Story 0.1: Setup Pipeline Infrastructure

Status: done

## Story

As a developer,
I want the SetupPipeline infrastructure and F5 hotkey to exist,
so that pressing F5 in the Unity Editor rebuilds the entire game scene from code via the Setup-Oriented Generation Framework.

## Acceptance Criteria

1. `SetupPhase` enum exists with phases: ClearGenerated (0), Prefabs (10), SceneComposition (20), WireReferences (30)
2. `SetupClassAttribute` exists for annotating setup classes with phase and order
3. `SetupPipeline.RunAll()` discovers all `[SetupClass]` types via reflection, sorts by phase then order, and executes their `Execute()` methods
4. Pipeline deletes `Assets/_Generated/` and recreates it before executing setup classes
5. F5 hotkey triggers `SetupPipeline.RunAll()` in the Unity Editor via `EditorApplication.globalEventHandler`
6. All existing setup classes (UISetup, ChartSetup, DebugSetup) have their `[SetupClass]` attributes uncommented and are discovered by the pipeline
7. Console logs each executed setup class name and phase, plus a completion message
8. `AssetDatabase.Refresh()` is called after all setup classes execute

## Tasks / Subtasks

- [x] Task 1: Create SetupPhase enum and SetupClassAttribute (AC: 1, 2)
  - [x] File: `Scripts/Setup/SetupPhase.cs` (new)
  - [x] File: `Scripts/Setup/SetupClassAttribute.cs` (new)
- [x] Task 2: Create SetupPipeline executor (AC: 3, 4, 7, 8)
  - [x] Reflection-based discovery of [SetupClass] types
  - [x] Sort by Phase then Order
  - [x] Delete and recreate Assets/_Generated/
  - [x] Execute each class's static Execute() method
  - [x] Log each execution and final completion
  - [x] File: `Scripts/Editor/SetupPipeline.cs` (new)
- [x] Task 3: Create F5 hotkey binding (AC: 5)
  - [x] [InitializeOnLoad] static class
  - [x] Bind F5 KeyDown to SetupPipeline.RunAll()
  - [x] Also added MenuItem at BullRun/F5 Rebuild as fallback
  - [x] File: `Scripts/Editor/SetupPipeline.cs` (same file)
- [x] Task 4: Uncomment [SetupClass] attributes on existing setup classes (AC: 6)
  - [x] UISetup.cs — [SetupClass(SetupPhase.SceneComposition, 50)] + added parameterless Execute()
  - [x] ChartSetup.cs — [SetupClass(SetupPhase.SceneComposition, 40)]
  - [x] DebugSetup.cs — [SetupClass(SetupPhase.SceneComposition, 90)]
- [x] Task 5: Create SceneSetup for base scene (camera, canvas)
  - [x] File: `Scripts/Setup/SceneSetup.cs` (new)
  - [x] Creates MainCamera (orthographic, 2D), GameCanvas, saves to _Generated/Scenes/

## Dev Notes

### Architecture Compliance
- Per architecture doc: all setup classes are `static` with `public static void Execute()`
- Pipeline lives in `Scripts/Editor/` so it's editor-only
- SetupPhase and SetupClassAttribute live in `Scripts/Setup/` so runtime and editor code can reference them
- F5 rebuild deletes `Assets/_Generated/` completely — never touches `Assets/_Imported/`

### Important
- UISetup.Execute() takes parameters (RunContext, int, float) — it will NOT match the parameterless Execute() signature. The pipeline only calls parameterless Execute(). UISetup's individual methods (ExecuteSidebar, ExecuteMarketOpenUI, etc.) are called by GameStates at runtime, not by the pipeline. A new parameterless Execute() wrapper is needed, or the pipeline should skip it until a SceneSetup orchestrator exists.
- ChartSetup.Execute() is parameterless — will work directly
- DebugSetup.Execute() is parameterless — will work directly
- A SceneSetup class (per the architecture doc) should create the base scene (camera, canvas) as the first SceneComposition step

### Dependencies
- None — this is foundational infrastructure that everything else depends on
