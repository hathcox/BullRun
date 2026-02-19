---
project_name: 'BullRun'
user_name: 'Iggy'
date: '2026-02-10'
sections_completed:
  ['technology_stack', 'engine_rules', 'performance_rules', 'organization_rules', 'testing_rules', 'platform_rules', 'anti_patterns']
status: 'complete'
rule_count: 52
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing game code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- **Engine:** Unity 6.3 LTS (6000.3.4f1)
- **Render Pipeline:** URP 17.3.0 — 2D Renderer only, no 3D models
- **UI Framework:** uGUI 2.0.0 — all UI built programmatically via Canvas hierarchies (NOT UI Toolkit)
- **Input:** Unity Input System 1.17.0 — action maps, keyboard/mouse primary, full gamepad mapping
- **Testing:** Unity Test Framework 1.6.0
- **Serialization:** JsonUtility / System.Text.Json for save data
- **Save System:** Easy Save 3 (deferred installation) for meta-progression persistence
- **Build:** IL2CPP for release, PC (Steam) target
- **Post-Processing:** Bloom (neon), vignette (dramatic moments), chromatic aberration (crashes) via URP Volume

## Critical Implementation Rules

### Engine-Specific Rules

#### Setup-Oriented Generation Framework (CRITICAL — Novel Pattern)
- **ALL game objects, UIs, prefabs, and scene composition are defined in C# code** — never configured in the Inspector
- F5 triggers a full deterministic rebuild via `SetupPipeline.RunAll()`
- Setup classes are `static` with `public static void Execute()`, annotated with `[SetupClass(SetupPhase.X, order)]`
- Phases execute in order: `ClearGenerated (0)` → `Prefabs (10)` → `SceneComposition (20)` → `WireReferences (30)`
- Setup classes write ONLY to `Assets/_Generated/` — this folder is deleted and recreated on every F5
- Setup classes read ALL values from `Scripts/Setup/Data/` static classes — never from Inspector fields
- `Assets/_Imported/` is NEVER touched by F5 (art, audio, shaders, fonts)

#### Scene Architecture
- **Single-scene game** — one scene (`Assets/_Generated/Scenes/MainScene.unity`) rebuilt by F5
- No multi-scene loading, no additive scenes, no scene transitions

#### UI Framework Rules
- Use **uGUI (Canvas)** exclusively — do NOT use UI Toolkit (UXML/USS)
- All Canvas hierarchies created programmatically via `new GameObject()` + `AddComponent<Canvas>()`
- UI panels are MonoBehaviours created by `UISetup` during F5

#### Serialization & Data
- **No ScriptableObjects** — all balance data lives as `public static readonly` fields in C# classes under `Scripts/Setup/Data/`
- Save data uses `JsonUtility` or `System.Text.Json` to `Application.persistentDataPath`
- Single source of truth: code constants, not JSON config files, not Inspector values

#### Assembly & Compilation
- `Scripts/Runtime/` must have **zero** `UnityEditor` namespace references — must compile in player builds
- `Scripts/Setup/` may reference Runtime code
- `Scripts/Editor/` may reference everything
- Debug tools wrapped in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`

#### EventBus Communication
- Systems communicate via central typed `EventBus` — synchronous dispatch (same frame)
- Systems **never reference each other directly** — use EventBus or RunContext
- Exception: UI classes may read from runtime systems (one-way dependency)
- Event naming: `{Subject}{Verb}Event` (e.g., `TradeExecutedEvent`, `RoundStartedEvent`)
- Handler naming: `On{EventName}` (e.g., `OnTradeExecuted`)

#### Lifecycle
- Game state managed by flat `GameStateMachine` with `IGameState` interface (`Enter`, `Update`, `Exit`)
- States: `MetaHub → MarketOpen → Trading → MarketClose → Shop → (loop or) RunSummary`
- `RunContext` carries all run data (act, round, cash, portfolio, active items)

### Performance Rules

#### Frame Budget
- **Target: 60fps minimum** during trading phase (16.67ms per frame)
- Chart rendering, price updates, and UI refresh must ALL fit within this budget
- Profile chart rendering in early development — LineRenderer is prototype, upgrade to custom mesh if needed

#### Hot Path Rules
- Price engine updates every frame (trend + noise + event + reversion per stock, multiple stocks simultaneously)
- Chart renderer feeds new price points every frame — must be allocation-free in steady state
- Item dispatch iterates the full ordered item list on every relevant game event — keep `OnEvent` implementations lightweight

#### Memory Management
- Avoid per-frame heap allocations in core loop systems (PriceEngine, ChartRenderer, Trading)
- Use object pooling for frequently created/destroyed objects (chart line segments, UI elements, event visual effects)
- Cache component references — never call `GetComponent<T>()` in Update loops

#### Logging Performance
- `Debug.Log` calls are stripped from release builds via `#if UNITY_EDITOR || DEVELOPMENT_BUILD`
- `Debug.LogWarning` and `Debug.LogError` remain in release — use sparingly in hot paths
- Never log per-frame in release builds

#### Asset Loading
- Single-scene architecture means no runtime scene loading overhead
- All prefabs generated at edit-time (F5) — no runtime Instantiate from Resources
- Audio and art loaded from `_Imported/` — standard Unity asset pipeline

### Code Organization Rules

#### Folder Structure
- **`Assets/_Generated/`** — Disposable. Deleted and recreated on F5. Never reference by path from code.
- **`Assets/_Imported/`** — Protected. Art, audio, animations, shaders, fonts. Never touched by F5.
- **`Assets/Scripts/Runtime/`** — All game logic. Organized by system domain.
- **`Assets/Scripts/Setup/`** — Setup classes and static data definitions.
- **`Assets/Scripts/Editor/`** — Editor-only tools (F5, debug).
- **`Assets/Plugins/`** — Third-party assets (Easy Save 3, etc.)

#### System Organization
- Each major game system gets its own folder under `Scripts/Runtime/{SystemName}/`
- No orphan files outside system folders
- New system = new folder (e.g., `Scripts/Runtime/NewSystem/`)

| System | Location |
|---|---|
| Core (state, events) | `Scripts/Runtime/Core/` |
| Trading | `Scripts/Runtime/Trading/` |
| Price Engine | `Scripts/Runtime/PriceEngine/` |
| Market Events | `Scripts/Runtime/Events/` |
| Items/Upgrades | `Scripts/Runtime/Items/` (with `Tools/`, `Intel/`, `Perks/` subfolders) |
| Shop | `Scripts/Runtime/Shop/` |
| Meta-Progression | `Scripts/Runtime/Meta/` |
| Chart | `Scripts/Runtime/Chart/` |
| UI | `Scripts/Runtime/UI/` |
| Audio | `Scripts/Runtime/Audio/` |

#### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `TradeExecutor`, `PriceGenerator` |
| Interfaces | IPascalCase | `IUpgrade`, `IGameState` |
| Methods | PascalCase | `ExecuteTrade()`, `OnRoundStart()` |
| Private fields | _camelCase | `_currentPrice`, `_activeItems` |
| Public properties | PascalCase | `CurrentCash`, `IsMarginCalled` |
| Constants | PascalCase (static readonly) | `GameConfig.StartingCapital` |
| Events | {Subject}{Verb}Event | `TradeExecutedEvent` |
| Event handlers | On{EventName} | `OnTradeExecuted` |
| Sprites/Music/SFX | snake_case | `btn_buy`, `sfx_trade_execute` |
| Fonts | PascalCase | `MonoTerminal` |

#### Adding New Code — Where Things Go

| Adding... | Location | Rules |
|---|---|---|
| New system | `Scripts/Runtime/{SystemName}/` | Own folder, communicate via EventBus |
| New item | `Scripts/Runtime/Items/{Category}/{ItemName}.cs` | One file per item, implements `IUpgrade` |
| New event | `Scripts/Runtime/Core/GameEvents.cs` | Inherits `GameEvent`, `{Subject}{Verb}Event` naming |
| New data | `Scripts/Setup/Data/{DataClass}.cs` | `public static readonly` fields only |
| New setup class | `Scripts/Setup/{Name}Setup.cs` | `[SetupClass(phase, order)]` + `static void Execute()` |
| New UI panel | `Scripts/Runtime/UI/{PanelName}.cs` | MonoBehaviour, created by UISetup |
| New debug tool | `Scripts/Editor/DebugManager.cs` | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |

### Testing Rules

#### Test Framework
- Unity Test Framework 1.6.0 — supports both Edit Mode and Play Mode tests
- **Running Tests:** AI agents can and should run Unity EditMode tests via the CLI to verify changes:
  ```
  "D:/UnityHub/Editor/6000.3.8f1/Editor/Unity.exe" -runTests -batchmode -nographics -projectPath "E:/BullRun" -testPlatform EditMode -testResults "E:/BullRun/TestResults.xml" -logFile "E:/BullRun/unity-test.log"
  ```
  - Exit code 0 = all tests passed; results written to `TestResults.xml` (NUnit format)
  - Run in background — Unity takes ~90s to load the project and execute tests
  - Check the log tail for `Test run completed. Exiting with code 0 (Ok)` to confirm success
  - Parse `TestResults.xml` header for pass/fail/skip counts

#### Test Organization
- Edit Mode tests for pure logic (price calculations, trade execution, data validation, event dispatch)
- Play Mode tests for integration (state machine transitions, UI updates, F5 rebuild verification)
- Test files mirror source structure: `Tests/Runtime/{SystemName}/` and `Tests/Editor/`

#### What to Test
- **Price Engine:** Verify each layer (trend, noise, event spike, mean reversion) produces expected output ranges
- **Trading:** Buy/sell/short execution, portfolio P&L calculations, margin requirements
- **Event System:** Event scheduling, event effects on prices, tier-specific event availability
- **Items:** Each item's `OnEvent` behavior, item ordering effects, edge cases
- **Game State:** State transitions, RunContext data integrity across transitions
- **Data Classes:** Validate static data consistency (no missing tiers, no invalid ranges)

#### Testing Rules for Agents
- All new systems must include Edit Mode tests for core logic
- Pure C# classes (no MonoBehaviour dependency) are preferred for testability
- Test data access via static classes directly — no mocking needed for `Scripts/Setup/Data/`
- EventBus tests should verify subscribe/publish/unsubscribe lifecycle
- Never test Unity engine behavior (rendering, physics) — test YOUR logic only

### Platform & Build Rules

#### Target Platform
- **Primary:** PC (Steam)
- **Secondary consideration:** Steam Deck (controller support required)
- **No mobile, no console, no web** — PC-only simplifies all decisions

#### Input Handling
- Unity Input System 1.17.0 with Action Maps
- Keyboard/mouse is primary input method
- Full gamepad mapping required (Steam Deck compatibility)
- Input actions defined programmatically in `InputSetup.cs` during F5 — not via Inspector .inputactions asset
- Support rebinding for accessibility

#### Build Configuration
- **Development:** Mono scripting backend, `DEVELOPMENT_BUILD` define for debug tools
- **Release:** IL2CPP scripting backend, all `Debug.Log` stripped, no debug tools
- Debug tools (F1-F4) gated behind `#if UNITY_EDITOR || DEVELOPMENT_BUILD`

#### Resolution & Display
- 2D/UI-driven game — no 3D camera considerations
- uGUI CanvasScaler handles resolution scaling
- Design for 1920x1080 reference resolution

#### Steam Integration
- Deferred to later phase — no Steam SDK dependencies in core architecture
- Save data to `Application.persistentDataPath` (Steam Cloud compatible path)

### Critical Don't-Miss Rules

#### Anti-Patterns — NEVER Do These
- **NEVER configure anything in the Unity Inspector** — all values come from code. Inspector is read-only.
- **NEVER create ScriptableObjects** — all data lives as `public static readonly` in `Scripts/Setup/Data/`
- **NEVER use UI Toolkit (UXML/USS)** — uGUI Canvas only, built programmatically
- **NEVER reference `_Generated/` assets by file path** at runtime — get references via setup-time wiring
- **NEVER add `UnityEditor` references in `Scripts/Runtime/`** — these must compile in player builds
- **NEVER make systems reference each other directly** — use EventBus or RunContext
- **NEVER use `Resources.Load()` or `Addressables`** — assets are wired at F5 setup time
- **NEVER add multi-scene loading** — single-scene architecture, rebuilt by F5
- **NEVER use DI containers, service locators, or SO lookup** — direct static class access for data
- **NEVER create, modify, or delete `.meta` files** — Unity auto-generates `.meta` files for every asset. AI agents must not touch them under any circumstances. If a `.meta` file is missing, Unity will regenerate it on the next Editor refresh.

#### Common Gotchas
- The `_Generated/` folder is **deleted entirely** on F5 — any manual edits there are lost
- Items execute in **player-defined order** (left-to-right) — order-dependent logic is intentional, not a bug
- EventBus dispatch is **synchronous** (same frame) — handlers must not assume async
- Price engine is **gameplay-first, not simulation** — events create dramatic swings, realism is secondary
- The game has **no physics** — don't add Rigidbodies, Colliders, or physics-based solutions
- Chart rendering may swap from LineRenderer to custom mesh — code against an abstraction, not the implementation

#### Error Handling Pattern
- Try-catch at **system boundaries only** (trade execution, event firing, round transitions, shop purchases)
- Inner logic stays clean — no defensive try-catch everywhere
- **Critical errors** (save corruption, missing data): Log + return to MetaHub
- **Recoverable errors** (bad trade, event glitch): Log + skip the operation
- **NEVER show error dialogs to the player** — the game keeps running

#### Logging Convention
- Format: `[SystemName] Message with context`
- Use `Debug.Log` for info, `Debug.LogWarning` for unexpected-but-handled, `Debug.LogError` for failures
- Example: `Debug.Log("[PriceEngine] Event fired: EarningsBeat on ACME (+25%)")`

#### Data Access Pattern
- Access data directly: `GameConfig.StartingCapital`, `StockTierData.GetTier(StockTier.Penny)`
- No indirection layers, no factories, no abstractions over static data

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any game code
- Follow ALL rules exactly as documented
- When in doubt, prefer the more restrictive option
- Update this file if new patterns emerge

**For Humans:**

- Keep this file lean and focused on agent needs
- Update when technology stack changes
- Review quarterly for outdated rules
- Remove rules that become obvious over time

Last Updated: 2026-02-16
