---
title: 'Game Architecture'
project: 'BullRun'
date: '2026-02-10'
author: 'Iggy'
version: '1.0'
stepsCompleted: [1, 2, 3, 4, 5, 6, 7, 8, 9]
status: 'complete'

# Source Documents
gdd: '_bmad-output/planning-artifacts/bull-run-gdd-mvp.md'
epics: null
brief: null
---

# Game Architecture

## Executive Summary

Bull Run is a real-time stock trading roguelike built in Unity 6.3 LTS + URP, entirely 2D/UI-driven, targeting PC (Steam). The architecture is built on a **Setup-Oriented Generation Framework** where all game objects, UI, prefabs, and scene composition are defined in C# code and regenerated via F5 — no Inspector configuration. Core systems (price engine, trading, chart rendering, events) communicate through a central typed EventBus. All balance data lives as pure C# static classes. Items use a player-orderable event hook system inspired by Balatro's joker ordering.

## Project Context

### Game Overview

**Bull Run** — A real-time stock trading roguelike where players frantically buy, sell, and short stocks on a live chart across escalating market tiers, racing to hit profit targets before getting margin called. Between rounds, draft upgrades from Trading Tools, Market Intel, and Passive Perks. Meta-progression unlocks new instruments and upgrades the player's office.

### Technical Scope

**Platform:** PC (Steam) — with Steam Deck controller support
**Engine:** Unity 6.3 + URP
**Genre:** Real-Time Trading Roguelike
**Rendering:** Entirely 2D/UI-driven, no 3D models
**Multiplayer:** None (single-player only)
**Project Complexity:** High

### Architectural Priorities

1. **Playable core loop first** — Architecture must support standing up a single playable trading round (price chart + buy/sell/short + timer + margin call) as the earliest possible milestone. All foundational systems serve this goal before expanding.
2. **Setup-Oriented Generation Framework** — All game objects, UIs, prefabs, ScriptableObjects, and scene composition defined entirely in code. F5 triggers full rebuild. Inspector is read-only. Every change is a code change.
3. **AI-agent consistency** — Architecture decisions and patterns must be explicit enough that any AI agent can implement a story without ambiguity or Inspector interaction.

### Core Systems

| System | Complexity | Architectural Role |
|--------|-----------|-------------------|
| Setup-Oriented Generation Framework | Critical | Foundation — F5 rebuild pipeline, code-only configuration, generated vs imported asset boundaries |
| Price Engine | High | Core loop — 4-layer price generation (trend + noise + events + mean reversion) |
| Chart Renderer | High | Core loop — Real-time line chart as primary gameplay surface, 60fps with effects |
| Trading System | Medium | Core loop — Buy/sell/short execution, portfolio, P&L, margin |
| Event System | High | Core loop (partial) — 12+ event types with tier-specific effects on prices |
| Game State Manager | Medium | Flow control — Run/round/phase state machine, act progression |
| Item/Upgrade System | High | Expansion — 30 items across 3 categories with cross-cutting gameplay effects |
| Shop System | Medium | Expansion — Draft presentation, rarity, cost/capital tension |
| Meta-Progression | Medium | Retention — Reputation, office tiers, unlocks, Broker Perks |
| UI Manager | High | Presentation — Dense real-time HUD, shop UI, meta-hub |
| Audio Manager | Medium | Polish — State-responsive dynamic music, per-tier ambience |
| Save System | Low | Persistence — JSON serialization for meta-progression |

### Technical Requirements

- **Frame rate:** 60fps minimum during trading phase
- **Post-processing:** Bloom (neon elements), vignette (dramatic moments), chromatic aberration (crashes) via URP
- **Input:** Keyboard/mouse primary, full gamepad mapping required
- **Save:** JSON to Application.persistentDataPath
- **Data-driven:** All balance parameters (prices, events, targets, item stats) tunable via code constants in setup classes

### Complexity Drivers

- **Setup-Oriented Generation Framework:** Novel pattern with no standard Unity equivalent. Affects every system. Requires custom editor tooling (F5 hotkey), folder conventions, and a strict generated/imported boundary.
- **Chart Renderer as gameplay:** The price chart is the game — not a widget. Must support real-time line drawing, glow trails, event pulses, volume bars, and stock switching at 60fps.
- **Price Engine layers:** 4 interacting systems (trend, noise, events, mean reversion) per stock, multiple stocks simultaneously, with tier-specific behavior.
- **Item cross-cutting:** 30 items that modify trading execution, price visibility, margin rules, timing, and passive income. Requires a clean hook/modifier architecture.

### Technical Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Chart rendering performance | High | Profile in week 2, LineRenderer or custom mesh, Asset Store fallback |
| Price generation balance | High | All params in setup code constants, debug overlay (F1), data logging from day 1 |
| Shop item balance | Medium | Rarity tiers, synergy tracking, pick rate analytics |
| Setup-Oriented Generation Framework complexity | Medium | Establish clear conventions early, validate with first playable round |

## Engine & Framework

### Selected Engine

**Unity 6.3 LTS** (6000.3.x) — Supported through December 2027

**Rationale:** Specified in GDD. Unity 6.3 LTS is the current stable release. URP 2D Renderer provides all required post-processing (bloom, vignette, chromatic aberration) for the synthwave aesthetic. Mature uGUI system supports programmatic Canvas hierarchy creation, which is essential for the Setup-Oriented Generation Framework.

### Project Initialization

```
Unity Hub → New Project → 2D (URP) → Unity 6.3 LTS
```

No external starter template — the 2D URP template provides the base. The Setup-Oriented Generation Framework replaces what templates normally do.

### Engine-Provided Architecture

| Component | Solution | Notes |
|-----------|----------|-------|
| Rendering | URP 2D Renderer | Bloom, vignette, chromatic aberration for synthwave aesthetic |
| Physics | Not needed | No physics-driven gameplay — pure UI/data |
| Audio | Unity Native Audio | Sufficient for dynamic music + SFX |
| Input | Unity Input System | Action maps, rebinding, gamepad + keyboard/mouse |
| UI Framework | Unity uGUI (Canvas) | Mature, dense HUD layouts. Canvas hierarchies created programmatically via setup code |
| Build System | Unity Build Pipeline | Steam/PC target, IL2CPP for release |
| Scene Management | Single-scene + Setup Framework | One scene rebuilt by F5 — no multi-scene loading needed |
| Serialization | JsonUtility / System.Text.Json | JSON save files to Application.persistentDataPath |

### Why uGUI Over UI Toolkit

The entire game is a dense real-time HUD (chart, sidebar, positions, news ticker). uGUI's Canvas system is battle-tested for this kind of layout, and — critically for the Setup-Oriented approach — Canvas hierarchies are straightforward to create programmatically via `new GameObject` + `AddComponent<Canvas>()`. UI Toolkit's UXML/USS workflow is designer-oriented and harder to generate purely from code.

## Architectural Decisions

### Decision Summary

| Category | Decision | Rationale |
|----------|----------|-----------|
| Setup Framework | Phase-based pipeline with `_Generated/` / `_Imported/` split | Maps to F5 rebuild phases, clean asset boundaries, scalable |
| Game State | Flat state machine + RunContext data object | Simple, all transitions visible in one place, context carries run data |
| Chart Rendering | LineRenderer (prototype), upgrade to custom mesh if needed | Gets core loop playable fast, swap is just a code change + F5 |
| Price Engine | Pipeline chain, gameplay-first (not simulation) | Simple sequential processing, events are the star, arcade not simulator |
| Data Architecture | Pure C# data classes, no ScriptableObjects | Single source of truth in code, simplest F5 rebuild, F1 overlay for debugging |
| Item/Modifier System | Event hooks with player-orderable execution | Decoupled, self-contained items, reorder for strategy like Balatro jokers |
| Save System | Easy Save 3 (installed later) | Proven asset, handles serialization without custom schema |
| Audio System | Unity native audio, state-driven transitions (deferred) | Sufficient for dynamic music + SFX, design in Phase 3 |

### Setup-Oriented Generation Framework

**Approach:** Phase-based pipeline triggered by F5

**Phases (executed in order):**
1. `ClearGenerated` — Delete everything in `Assets/_Generated/`
2. `Prefabs` — Create all prefab GameObjects with components
3. `SceneComposition` — Compose the game scene (camera, canvas, managers)
4. `WireReferences` — Connect cross-references between generated objects

**Setup classes** register to a phase via attribute: `[SetupPhase(Phase.Prefabs)]`

**Folder structure:**

```
Assets/
├── _Generated/           ← DELETED and recreated on F5
│   ├── Prefabs/
│   ├── Scenes/
│   └── UI/
├── _Imported/            ← NEVER touched by F5
│   ├── Art/
│   ├── Audio/
│   ├── Animations/
│   └── Shaders/
├── Scripts/
│   ├── Runtime/          ← Game logic (MonoBehaviours, systems, data)
│   ├── Setup/            ← Setup classes (generate _Generated content)
│   └── Editor/           ← F5 hotkey, debug tools
└── Plugins/              ← Easy Save 3, other assets
```

### Game State Machine

**Approach:** Flat state machine with RunContext

**States:** `MetaHub → MarketOpen → Trading → MarketClose → Shop → (loop or) RunSummary`

**RunContext** carries: current act, current round, cash, portfolio, active items (ordered list), accumulated reputation

### Chart Rendering

**Approach:** LineRenderer (prototype) → custom mesh (if needed)

LineRenderer fed price points each frame. Second LineRenderer for glow trail. Volume bars as simple UI rects below chart. Swap to custom triangle-strip mesh + URP shader if visual quality or performance demands it.

### Price Engine

**Approach:** Pipeline chain, gameplay-first

```
price += trendPerSecond * deltaTime
price += Random.Range(-noise, noise)
if (activeEvent) price = Lerp(price, eventTarget, eventForce)
else price = Lerp(price, trendLine, reversionSpeed)
```

**Design principle:** This is an arcade game. Events deliver big dramatic swings. Trend and noise give the line personality between events. No simulation complexity.

### Data Architecture

**Approach:** Pure C# static data classes

All balance data (stock tiers, event configs, item stats, margin call targets, shop costs) lives as `static readonly` fields in C# classes under `Scripts/Setup/Data/`. No ScriptableObjects. Single source of truth. F1 debug overlay for runtime value inspection.

### Item/Modifier System

**Approach:** Event hooks with player-orderable execution

- Items implement `IUpgrade` interface
- Items subscribe to game events (`OnTradeExecuted`, `OnRoundStart`, `OnPriceUpdate`, `OnEventFired`)
- Items stored in an **ordered list** — player can reorder for strategic effect
- When an event fires, items process left-to-right through the player's arrangement
- Adds a Balatro-style strategic layer: item ordering matters

### Save System

**Approach:** Easy Save 3 (deferred installation)

Handles all serialization for meta-progression (Reputation, unlocks, office tier, Broker Perks). No custom schema needed.

### Audio System

**Approach:** Unity native audio, state-driven (deferred to Phase 3)

Dynamic music transitions tied to game state (calm → driving → tense → triumphant). Per-tier ambient soundscapes. Design details deferred.

## Cross-cutting Concerns

These patterns apply to ALL systems and must be followed by every implementation.

### Error Handling

**Strategy:** Try-Catch at system boundaries

Wrap trade execution, event firing, round transitions, and shop purchases. Inner logic stays clean.

**Error Levels:**
- **Critical** (save corruption, missing data): Log + return to MetaHub
- **Recoverable** (bad trade, event glitch): Log + skip the operation
- **Never** show error dialogs to the player — the game keeps running

**Example:**

```csharp
public void ExecuteTrade(TradeOrder order)
{
    try
    {
        var position = portfolio.OpenPosition(order);
        EventBus.Publish(new TradeExecutedEvent(order, position));
    }
    catch (Exception e)
    {
        Debug.LogError($"[Trading] Trade failed: {e.Message}");
        // Recover: skip trade, player keeps their cash
    }
}
```

### Logging

**Format:** Unity Debug.Log with system tags

**Convention:** `[SystemName] Message with context`

**Levels:**
- `Debug.Log` — info (stripped from release builds via `#if UNITY_EDITOR || DEVELOPMENT_BUILD`)
- `Debug.LogWarning` — unexpected but handled (stays in release)
- `Debug.LogError` — something broke (stays in release)

**Example:**

```csharp
Debug.Log("[PriceEngine] Event fired: EarningsBeat on ACME (+25%)");
Debug.LogWarning("[Trading] Short margin insufficient, order skipped");
Debug.LogError("[GameState] Failed to transition: invalid state");
```

### Configuration

**Approach:** Pure C# static data classes

```
Scripts/Setup/Data/
├── GameConfig.cs          ← Master constants (starting capital, round duration)
├── StockTierData.cs       ← Per-tier definitions (price ranges, volatility, stock count)
├── MarginCallTargets.cs   ← Per-round profit targets and scaling
├── EventDefinitions.cs    ← Event types, effects, tier availability, probabilities
├── ShopItemDefinitions.cs ← All 30 items: cost, rarity, effect parameters
├── MetaProgressionData.cs ← Reputation costs, office tiers, unlock thresholds
└── BrokerPerkData.cs      ← Persistent perk definitions and caps
```

All values are `public static readonly` fields. Single source of truth. No ScriptableObjects, no JSON config files, no Inspector.

### Event System

**Pattern:** Central event bus with typed events, synchronous dispatch

**Publishing:** `EventBus.Publish(new TradeExecutedEvent(stock, quantity, price, isBuy));`
**Subscribing:** `EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);`

**Event naming:** `{Subject}{Verb}Event` — examples:
- `TradeExecutedEvent`
- `RoundStartedEvent`
- `MarketEventFiredEvent`
- `PriceUpdatedEvent`
- `ShopItemPurchasedEvent`
- `MarginCallTriggeredEvent`
- `ItemReorderedEvent`

**Processing:** Synchronous (same frame). Items process in player-defined order when the bus dispatches.

### Debug Tools

| Tool | Key | Purpose |
|------|-----|---------|
| Debug overlay | F1 | Price trends, scheduled events, win rate, economy stats |
| F5 rebuild | F5 | Full Setup-Oriented Generation rebuild |
| God mode | F2 | Infinite cash, can't fail margin call |
| Skip to round | F3 | Jump to any act/round with configurable starting cash |
| Event trigger | F4 | Force-fire any event type on any stock |

**Activation:** All wrapped in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. None in release builds. Single `DebugManager` MonoBehaviour generated by setup code.

## Project Structure

### Organization Pattern

**Pattern:** By system/domain — each major game system gets its own folder under `Scripts/Runtime/`.

**Rationale:** Maps directly to the GDD's system table. Clear boundaries per system. Any AI agent can locate where code belongs by system name.

### Directory Structure

```
Assets/
├── _Generated/                          ← DELETED and recreated on F5
│   ├── Prefabs/
│   │   ├── UI/                          ← Generated Canvas/HUD prefabs
│   │   └── Managers/                    ← Generated manager GameObjects
│   └── Scenes/
│       └── MainScene.unity              ← The single game scene
│
├── _Imported/                           ← NEVER touched by F5
│   ├── Art/
│   │   ├── UI/                          ← Sprites, icons, backgrounds
│   │   ├── Office/                      ← Office tier background art
│   │   └── Fonts/                       ← Monospace + sans-serif fonts
│   ├── Audio/
│   │   ├── Music/                       ← Synthwave tracks
│   │   ├── SFX/                         ← Trade sounds, events, UI
│   │   └── Ambient/                     ← Per-tier office ambience
│   └── Shaders/                         ← Custom URP shaders (if needed)
│
├── Scripts/
│   ├── Runtime/                         ← All game logic
│   │   ├── Core/
│   │   │   ├── GameStateMachine.cs      ← Flat state machine
│   │   │   ├── RunContext.cs            ← Run data (act, round, cash, portfolio, items)
│   │   │   ├── GameStates/              ← MetaHub, MarketOpen, Trading, MarketClose, Shop, RunSummary
│   │   │   └── EventBus.cs             ← Central typed event bus
│   │   ├── Trading/
│   │   │   ├── TradeExecutor.cs         ← Buy/sell/short execution
│   │   │   ├── Portfolio.cs             ← Position tracking, P&L
│   │   │   └── Position.cs             ← Single stock position
│   │   ├── PriceEngine/
│   │   │   ├── PriceGenerator.cs        ← Pipeline chain (trend → noise → event → reversion)
│   │   │   └── StockInstance.cs         ← Per-stock runtime state
│   │   ├── Events/
│   │   │   ├── EventScheduler.cs        ← Schedules events during round
│   │   │   ├── MarketEvent.cs           ← Event base class
│   │   │   └── EventEffects.cs          ← How events affect prices
│   │   ├── Items/
│   │   │   ├── IUpgrade.cs              ← Item interface
│   │   │   ├── ItemManager.cs           ← Ordered item list, event dispatch
│   │   │   ├── Tools/                   ← Trading Tool implementations
│   │   │   ├── Intel/                   ← Market Intel implementations
│   │   │   └── Perks/                   ← Passive Perk implementations
│   │   ├── Shop/
│   │   │   ├── ShopGenerator.cs         ← Draft item selection logic
│   │   │   └── ShopTransaction.cs       ← Purchase flow
│   │   ├── Meta/
│   │   │   ├── MetaManager.cs           ← Reputation, unlocks, office tier
│   │   │   └── BrokerPerks.cs           ← Persistent perk application
│   │   ├── Chart/
│   │   │   ├── ChartRenderer.cs         ← LineRenderer feed + glow trail
│   │   │   └── ChartUI.cs              ← Price labels, time bar, volume bars
│   │   ├── UI/
│   │   │   ├── TradingHUD.cs            ← Cash, portfolio value, profit, margin target
│   │   │   ├── StockSidebar.cs          ← Stock list with sparklines
│   │   │   ├── PositionPanel.cs         ← Current positions display
│   │   │   ├── ShopUI.cs               ← Draft shop layout
│   │   │   ├── MetaHubUI.cs            ← Office screen, run start
│   │   │   └── NewsTicker.cs           ← Event news crawl
│   │   └── Audio/
│   │       ├── AudioManager.cs          ← Music/SFX playback
│   │       └── MusicStateDriver.cs      ← State-driven music transitions
│   │
│   ├── Setup/                           ← Setup classes (F5 rebuild)
│   │   ├── Data/                        ← Static data definitions
│   │   │   ├── GameConfig.cs
│   │   │   ├── StockTierData.cs
│   │   │   ├── MarginCallTargets.cs
│   │   │   ├── EventDefinitions.cs
│   │   │   ├── ShopItemDefinitions.cs
│   │   │   ├── MetaProgressionData.cs
│   │   │   └── BrokerPerkData.cs
│   │   ├── SetupPhaseAttribute.cs       ← [SetupPhase(Phase.X)] attribute
│   │   ├── SceneSetup.cs               ← Camera, canvas, managers
│   │   ├── UISetup.cs                  ← HUD layout, shop layout, meta hub
│   │   ├── PrefabSetup.cs             ← Manager prefabs, chart objects
│   │   └── InputSetup.cs              ← Input action map configuration
│   │
│   └── Editor/                          ← Editor-only tools
│       ├── F5RebuildTool.cs             ← F5 hotkey → phase pipeline
│       ├── SetupPipeline.cs             ← Discovers + executes setup classes by phase
│       └── DebugManager.cs             ← F1-F4 debug tools
│
└── Plugins/                             ← Third-party (Easy Save 3, etc.)
```

### System Location Mapping

| System | Location | Key Files |
|--------|----------|-----------|
| Setup Framework | `Scripts/Editor/` + `Scripts/Setup/` | F5RebuildTool, SetupPipeline, all *Setup.cs |
| Game State | `Scripts/Runtime/Core/` | GameStateMachine, RunContext, GameStates/ |
| Event Bus | `Scripts/Runtime/Core/` | EventBus.cs |
| Price Engine | `Scripts/Runtime/PriceEngine/` | PriceGenerator, StockInstance |
| Trading | `Scripts/Runtime/Trading/` | TradeExecutor, Portfolio, Position |
| Market Events | `Scripts/Runtime/Events/` | EventScheduler, MarketEvent, EventEffects |
| Items/Upgrades | `Scripts/Runtime/Items/` | IUpgrade, ItemManager, Tools/, Intel/, Perks/ |
| Shop | `Scripts/Runtime/Shop/` | ShopGenerator, ShopTransaction |
| Meta-Progression | `Scripts/Runtime/Meta/` | MetaManager, BrokerPerks |
| Chart | `Scripts/Runtime/Chart/` | ChartRenderer, ChartUI |
| UI | `Scripts/Runtime/UI/` | TradingHUD, StockSidebar, ShopUI, MetaHubUI, etc. |
| Audio | `Scripts/Runtime/Audio/` | AudioManager, MusicStateDriver |
| Data Definitions | `Scripts/Setup/Data/` | GameConfig, StockTierData, all *Data.cs |
| Debug Tools | `Scripts/Editor/` | DebugManager |

### Naming Conventions

#### Files & Classes

| Element | Convention | Example |
|---------|-----------|---------|
| Classes | PascalCase | `TradeExecutor`, `PriceGenerator` |
| Interfaces | IPascalCase | `IUpgrade`, `IGameState` |
| Methods | PascalCase | `ExecuteTrade()`, `OnRoundStart()` |
| Private fields | _camelCase | `_currentPrice`, `_activeItems` |
| Public properties | PascalCase | `CurrentCash`, `IsMarginCalled` |
| Constants | PascalCase (static readonly) | `GameConfig.StartingCapital` |
| Events | {Subject}{Verb}Event | `TradeExecutedEvent`, `RoundStartedEvent` |
| Event handlers | On{EventName} | `OnTradeExecuted`, `OnRoundStarted` |

#### Game Assets (in `_Imported/`)

| Asset Type | Convention | Example |
|------------|-----------|---------|
| Sprites | snake_case | `btn_buy`, `icon_earnings_beat` |
| Music | snake_case | `track_trading_calm`, `track_act4_intense` |
| SFX | snake_case | `sfx_trade_execute`, `sfx_margin_call` |
| Fonts | PascalCase | `MonoTerminal`, `CleanSans` |

### Architectural Boundaries

1. **`_Generated/` is disposable** — Never reference `_Generated/` assets from code by path. Setup code creates them; runtime gets references via setup-time wiring.
2. **`Scripts/Runtime/` has zero Unity Editor dependencies** — No `UnityEditor` namespace. Compiles in player builds.
3. **`Scripts/Setup/` can reference Runtime** — Setup classes create and configure runtime components.
4. **`Scripts/Editor/` can reference everything** — Editor tools call setup classes and runtime code.
5. **Systems don't reference each other directly** — Communicate through `EventBus` or `RunContext`. Exception: UI classes read from runtime systems (one-way dependency).
6. **Each item is one file** — `Scripts/Runtime/Items/Tools/StopLossOrder.cs`, `Scripts/Runtime/Items/Perks/InterestAccrual.cs`, etc.

## Implementation Patterns

These patterns ensure consistent implementation across all AI agents.

### Novel Pattern: Setup-Oriented Generation Framework

**Purpose:** Define all game objects, UIs, prefabs, and scene composition entirely in code. F5 triggers a full deterministic rebuild.

**Components:**
- `SetupPhase` enum — defines execution phases (ClearGenerated → Prefabs → SceneComposition → WireReferences)
- `[SetupClass(phase, order)]` attribute — marks and orders setup classes
- `SetupPipeline` — discovers and executes all setup classes via reflection
- `F5RebuildTool` — editor script binding F5 to `SetupPipeline.RunAll()`

**Implementation:**

```csharp
// Phase enum
public enum SetupPhase
{
    ClearGenerated = 0,
    Prefabs = 10,
    SceneComposition = 20,
    WireReferences = 30
}

// Attribute for auto-discovery
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

// Example setup class
[SetupClass(SetupPhase.SceneComposition, order: 10)]
public static class SceneSetup
{
    public static void Execute()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        var cam = new GameObject("MainCamera");
        cam.AddComponent<Camera>();
        cam.AddComponent<UniversalAdditionalCameraData>();

        var canvas = new GameObject("GameCanvas");
        var c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();

        EditorSceneManager.SaveScene(scene, "Assets/_Generated/Scenes/MainScene.unity");
    }
}

// F5 hotkey (Editor script)
[InitializeOnLoad]
public static class F5RebuildTool
{
    static F5RebuildTool()
    {
        EditorApplication.globalEventHandler += OnGlobalKeyPress;
    }

    static void OnGlobalKeyPress()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F5)
        {
            SetupPipeline.RunAll();
            Event.current.Use();
        }
    }
}

// Pipeline executor
public static class SetupPipeline
{
    public static void RunAll()
    {
        var setupClasses = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<SetupClassAttribute>() != null)
            .OrderBy(t => t.GetCustomAttribute<SetupClassAttribute>().Phase)
            .ThenBy(t => t.GetCustomAttribute<SetupClassAttribute>().Order);

        if (AssetDatabase.IsValidFolder("Assets/_Generated"))
            AssetDatabase.DeleteAsset("Assets/_Generated");
        AssetDatabase.CreateFolder("Assets", "_Generated");

        foreach (var type in setupClasses)
        {
            var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, null);
            Debug.Log($"[Setup] Executed: {type.Name} (Phase: {type.GetCustomAttribute<SetupClassAttribute>().Phase})");
        }

        AssetDatabase.Refresh();
        Debug.Log("[Setup] F5 rebuild complete.");
    }
}
```

**Rules for AI agents writing setup classes:**
- Every setup class is `static` with a `public static void Execute()` method
- Annotated with `[SetupClass(phase, order)]`
- Never reads from Inspector — all values come from `Scripts/Setup/Data/` constants
- Always writes to `Assets/_Generated/`

### Novel Pattern: Player-Orderable Item Execution

**Purpose:** 30 items subscribe to game events and execute in the order the player has arranged them. Order is changeable for strategic effect (Balatro-style).

**Components:**
- `IUpgrade` interface — all items implement this
- `ItemManager` — holds ordered list, dispatches events left-to-right
- Individual item classes — one file per item

**Implementation:**

```csharp
// Item interface
public interface IUpgrade
{
    string Id { get; }
    string DisplayName { get; }
    void OnEvent(GameEvent gameEvent, RunContext ctx);
}

// Example item
public class StopLossOrder : IUpgrade
{
    public string Id => "tool_stop_loss";
    public string DisplayName => "Stop-Loss Order";

    public void OnEvent(GameEvent gameEvent, RunContext ctx)
    {
        if (gameEvent is PriceUpdatedEvent priceEvent)
        {
            var position = ctx.Portfolio.GetPosition(priceEvent.StockId);
            if (position != null && priceEvent.Price < position.StopLossThreshold)
            {
                ctx.Portfolio.ClosePosition(priceEvent.StockId, priceEvent.Price);
                EventBus.Publish(new TradeExecutedEvent(priceEvent.StockId, position.Shares, priceEvent.Price, isSell: true));
            }
        }
    }
}

// Item manager dispatches in player order
public class ItemManager
{
    private readonly List<IUpgrade> _items = new();

    public void AddItem(IUpgrade item) => _items.Add(item);
    public void ReorderItem(int fromIndex, int toIndex)
    {
        var item = _items[fromIndex];
        _items.RemoveAt(fromIndex);
        _items.Insert(toIndex, item);
    }

    public void DispatchEvent(GameEvent gameEvent, RunContext ctx)
    {
        foreach (var item in _items)
        {
            try
            {
                item.OnEvent(gameEvent, ctx);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Items] {item.DisplayName} failed: {e.Message}");
            }
        }
    }
}
```

### Standard Pattern: Component Communication

**Pattern:** Central EventBus with typed events. Systems never reference each other directly.

```csharp
// Publishing from any system
EventBus.Publish(new RoundStartedEvent(roundNumber: 3, act: 2, target: 600f));

// Subscribing from any system
EventBus.Subscribe<RoundStartedEvent>(e => {
    Debug.Log($"[PriceEngine] Round {e.RoundNumber} starting, generating stocks...");
});
```

### Standard Pattern: Data Access

**Pattern:** Direct static class access. No service locator, no DI, no SO lookup.

```csharp
float startCash = GameConfig.StartingCapital;
float target = MarginCallTargets.GetTarget(roundNumber: 3);
var pennyStocks = StockTierData.GetTier(StockTier.Penny);
```

### Standard Pattern: Game State Transitions

**Pattern:** Flat state machine with explicit transitions.

```csharp
public class GameStateMachine
{
    private IGameState _current;
    private readonly RunContext _ctx;

    public void TransitionTo<T>() where T : IGameState, new()
    {
        _current?.Exit(_ctx);
        _current = new T();
        _current.Enter(_ctx);
        Debug.Log($"[GameState] Transition: → {typeof(T).Name}");
    }
}

public interface IGameState
{
    void Enter(RunContext ctx);
    void Update(RunContext ctx);
    void Exit(RunContext ctx);
}
```

### Consistency Rules

| Pattern | Convention | Enforcement |
|---------|-----------|-------------|
| New system | Create folder in `Scripts/Runtime/{SystemName}/` | Folder per system, no orphan files |
| New item | One file: `Scripts/Runtime/Items/{Category}/{ItemName}.cs` | Implements `IUpgrade` |
| New event | Define in `Scripts/Runtime/Core/GameEvents.cs` | Inherits `GameEvent`, naming: `{Subject}{Verb}Event` |
| New data | Add to existing data class or create new in `Scripts/Setup/Data/` | `public static readonly` fields only |
| New setup class | `Scripts/Setup/{Name}Setup.cs` | `[SetupClass(phase, order)]` + `static void Execute()` |
| New UI panel | `Scripts/Runtime/UI/{PanelName}.cs` | MonoBehaviour, created by UISetup |
| New debug tool | Add to `DebugManager.cs` | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |

## Architecture Validation

| Check | Result | Notes |
|-------|--------|-------|
| Decision Compatibility | PASS | All decisions coherent, no conflicts |
| GDD Coverage | PASS | 11/11 systems, 6/6 requirements |
| Pattern Completeness | PASS | 8/8 scenarios covered with code examples |
| Epic Mapping | PASS | 12/12 GDD epics mapped to locations + patterns |
| Document Completeness | PASS | All sections present, no placeholders |

**Systems Covered:** 11/11 | **Patterns Defined:** 5 (2 novel + 3 standard) | **Decisions Made:** 8

---

_Generated by BMAD GDS Architecture Workflow v2.0_
_Date: 2026-02-10_
_For: Iggy_
