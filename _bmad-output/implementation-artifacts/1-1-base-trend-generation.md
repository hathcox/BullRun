# Story 1.1: Base Trend Generation

Status: done

## Story

As a player,
I want stock prices to have an underlying directional bias (bullish, bearish, or neutral) each round,
so that price movement feels intentional and readable.

## Acceptance Criteria

1. Each stock receives a hidden trend direction (bullish, bearish, neutral) at round start
2. Trend creates a visible slope on the price line over the round duration
3. Trend strength varies by stock tier (penny = volatile, blue chip = steady)
4. Trend direction is not exposed to the player (hidden state for debug overlay later)
5. Multiple stocks can have independent trends in the same round
6. Price updates occur every frame, producing a smooth line

## Tasks / Subtasks

- [x] Task 1: Create StockTierData static data class (AC: 3)
  - [x] Define `StockTier` enum: `Penny`, `LowValue`, `MidValue`, `BlueChip`
  - [x] Define `StockTierConfig` struct: price range (min/max), base volatility, trend strength range, stock count per round
  - [x] Populate tier configs per GDD Section 3.2 table
  - [x] File: `Scripts/Setup/Data/StockTierData.cs`
- [x] Task 2: Create GameConfig with core constants (AC: 6)
  - [x] Define `StartingCapital` ($1000), `RoundDurationSeconds` (60f), `PriceUpdateRate` (per-frame)
  - [x] File: `Scripts/Setup/Data/GameConfig.cs`
- [x] Task 3: Create StockInstance runtime class (AC: 1, 2, 5)
  - [x] Fields: `StockId`, `TickerSymbol`, `CurrentPrice`, `TrendDirection` (enum: Bull/Bear/Neutral), `TrendPerSecond`, `TierConfig` reference
  - [x] Method: `Initialize(tier, startingPrice, trendDirection, trendStrength)` — sets up stock for a round
  - [x] File: `Scripts/Runtime/PriceEngine/StockInstance.cs`
- [x] Task 4: Create PriceGenerator pipeline (AC: 1, 2, 6)
  - [x] Method: `UpdatePrice(StockInstance stock, float deltaTime)` — applies trend: `stock.CurrentPrice += stock.TrendPerSecond * deltaTime`
  - [x] Method: `InitializeRound(int act, int round)` — creates stock instances with random trend directions and tier-appropriate strengths
  - [x] Publish `PriceUpdatedEvent` via EventBus after each price update
  - [x] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- [x] Task 5: Define PriceEngine events (AC: 6)
  - [x] `PriceUpdatedEvent`: StockId, NewPrice, PreviousPrice, DeltaTime
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 6: Create EventBus if not already existing (AC: 5)
  - [x] Typed publish/subscribe pattern per architecture doc
  - [x] File: `Scripts/Runtime/Core/EventBus.cs`

## Dev Notes

### Architecture Compliance

- **Location:** All PriceEngine code in `Scripts/Runtime/PriceEngine/`
- **Data:** All tier configs and constants in `Scripts/Setup/Data/` as `public static readonly` fields
- **No ScriptableObjects** — pure C# data classes only
- **No Inspector interaction** — all values from code constants
- **EventBus communication** — PriceGenerator publishes events, never directly calls other systems
- **Logging:** Use `Debug.Log("[PriceEngine] ...")` format

### Price Generation Formula (from Architecture)

```csharp
// Story 1.1 implements ONLY the trend layer:
price += trendPerSecond * deltaTime;
// Noise (Story 1.2), Event spikes (Story 1.3), and Mean reversion (Story 1.4) added later
```

### Design Principle

This is an **arcade game**, not a stock simulator. Trend should create a readable directional bias that players can observe and trade against. Keep it simple — the dramatic gameplay comes from events (Story 1.3), not from trend complexity.

### Stock Tier Reference (GDD Section 3.2)

| Tier | Price Range | Volatility | Stocks/Round |
|------|------------|------------|-------------|
| Penny | $0.10–$5 | Very High | 3–4 |
| Low-Value | $5–$50 | High | 3–4 |
| Mid-Value | $50–$500 | Medium | 2–3 |
| Blue Chip | $500–$5,000 | Low–Med | 2–3 |

### Naming Conventions

- Classes: PascalCase (`PriceGenerator`, `StockInstance`)
- Private fields: _camelCase (`_currentPrice`, `_trendPerSecond`)
- Public properties: PascalCase (`CurrentPrice`, `TrendDirection`)
- Events: `{Subject}{Verb}Event` (`PriceUpdatedEvent`)
- Logging: `[PriceEngine]` tag prefix

### Project Structure Notes

- `Scripts/Runtime/PriceEngine/PriceGenerator.cs` — pipeline orchestrator
- `Scripts/Runtime/PriceEngine/StockInstance.cs` — per-stock runtime state
- `Scripts/Setup/Data/StockTierData.cs` — tier definitions
- `Scripts/Setup/Data/GameConfig.cs` — master constants
- `Scripts/Runtime/Core/EventBus.cs` — central event bus
- `Scripts/Runtime/Core/GameEvents.cs` — all event type definitions

### References

- [Source: game-architecture.md#Price Engine] — Pipeline chain pattern
- [Source: game-architecture.md#Data Architecture] — Pure C# static data classes
- [Source: game-architecture.md#Event System] — EventBus pattern
- [Source: game-architecture.md#Naming Conventions] — All naming rules
- [Source: bull-run-gdd-mvp.md#3.2] — Stock tier behavior table
- [Source: bull-run-gdd-mvp.md#3.3] — Price generation system design

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6

### Change Log
- 2026-02-10: Story 1-1 implemented — Base trend generation pipeline with StockTierData, GameConfig, StockInstance, PriceGenerator, EventBus, and GameEvents. 45 unit tests across all components.

### Debug Log References

### Completion Notes List
- Task 1: Created StockTierData.cs with StockTier enum (Penny/LowValue/MidValue/BlueChip), StockTierConfig readonly struct, and static tier configurations matching GDD Section 3.2. Created StockTierDataTests.cs with 10 unit tests covering price ranges, volatility ordering, stock counts, and trend strength ranges.
- Task 2: Created GameConfig.cs with StartingCapital ($1000), RoundDurationSeconds (60f), PriceUpdateRate (0 = per-frame). Created GameConfigTests.cs with 5 unit tests.
- Task 3: Created StockInstance.cs with TrendDirection enum, all required fields (StockId, TickerSymbol, CurrentPrice, TrendDirection, TrendPerSecond, TierConfig), and Initialize method that converts trend strength to per-second price change. Created StockInstanceTests.cs with 10 unit tests.
- Task 4: Created PriceGenerator.cs with UpdatePrice (trend formula: price += trendPerSecond * deltaTime), InitializeRound (creates stocks per tier with random trends), and PriceUpdatedEvent publishing. Created PriceGeneratorTests.cs with 11 unit tests.
- Task 5: Created GameEvents.cs with PriceUpdatedEvent struct (StockId, NewPrice, PreviousPrice, DeltaTime). Created GameEventsTests.cs with 2 unit tests.
- Task 6: Created EventBus.cs with typed publish/subscribe/unsubscribe/clear pattern, synchronous dispatch. Created EventBusTests.cs with 7 unit tests.

### File List
- Assets/Scripts/Setup/Data/StockTierData.cs (new)
- Assets/Scripts/BullRun.asmdef (new)
- Assets/Tests/Runtime/BullRun.Tests.EditMode.asmdef (new)
- Assets/Tests/Runtime/PriceEngine/StockTierDataTests.cs (new)
- Assets/Scripts/Setup/Data/GameConfig.cs (new)
- Assets/Tests/Runtime/PriceEngine/GameConfigTests.cs (new)
- Assets/Scripts/Runtime/PriceEngine/StockInstance.cs (new)
- Assets/Tests/Runtime/PriceEngine/StockInstanceTests.cs (new)
- Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs (new)
- Assets/Tests/Runtime/PriceEngine/PriceGeneratorTests.cs (new)
- Assets/Scripts/Runtime/Core/GameEvents.cs (new)
- Assets/Tests/Runtime/Core/GameEventsTests.cs (new)
- Assets/Scripts/Runtime/Core/EventBus.cs (new)
- Assets/Tests/Runtime/Core/EventBusTests.cs (new)
