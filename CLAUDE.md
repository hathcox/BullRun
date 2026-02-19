# BullRun - Claude Code Project Instructions

## Running Tests

Unity EditMode tests via CLI (takes ~90s to load project + execute):

```bash
"D:/UnityHub/Editor/6000.3.8f1/Editor/Unity.exe" -runTests -batchmode -nographics -projectPath "E:/BullRun" -testPlatform EditMode -testResults "E:/BullRun/TestResults.xml" -logFile "E:/BullRun/unity-test.log"
```

- Run in background (`run_in_background: true`) — it takes time
- Exit code 0 = all tests passed
- Results in `TestResults.xml` (NUnit format) — parse header for pass/fail/skip counts
- Check log tail for `Test run completed. Exiting with code 0 (Ok)`

## Project Context

Read `_bmad-output/project-context.md` for full rules and patterns before implementing game code.

## Key Rules

- Unity 6.3 LTS, uGUI only (no UI Toolkit), single-scene architecture
- All game objects/UI built programmatically in code — never configured in Inspector
- No ScriptableObjects — all data as `public static readonly` in `Scripts/Setup/Data/`
- Systems communicate via EventBus — never reference each other directly
- `Scripts/Runtime/` must have zero `UnityEditor` references
- Never create, modify, or delete `.meta` files
