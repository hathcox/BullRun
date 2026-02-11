# Story 1.5: Stock Tier Configuration

Status: done

## Story

As a developer,
I want stock behavior parameters fully defined per tier with named stock pools,
so that each market tier feels distinct and stocks are ready for round initialization.

## Acceptance Criteria

1. Penny tier: $0.10–$5, very high volatility, 3–4 stocks, wild swings and pump & dump patterns
2. Low-Value tier: $5–$50, high volatility, 3–4 stocks, trend-based with reversals
3. Mid-Value tier: $50–$500, medium volatility, 2–3 stocks, sector correlation and steadier trends
4. Blue Chip tier: $500–$5,000, low-med volatility, 2–3 stocks, stable with rare dramatic events
5. Each tier has a pool of named stocks with ticker symbols
6. All parameters defined as data, not hardcoded in logic
7. Stock pools are selectable per round/act

## Tasks / Subtasks

- [x] Task 1: Finalize StockTierData with complete definitions (AC: 1–4, 6)
  - [x] Added EventFrequencyModifier to StockTierConfig: Penny 1.5, Low 1.2, Mid 1.0, BlueChip 0.5
  - [x] All tier configs verified coherent with Stories 1.1–1.4
  - [x] File: `Scripts/Setup/Data/StockTierData.cs` (finalized)
- [x] Task 2: Create stock pool definitions (AC: 5, 7)
  - [x] Defined `StockDefinition` struct: TickerSymbol, DisplayName, Tier, Sector, FlavorText
  - [x] Defined `StockSector` enum for sector correlation infrastructure
  - [x] Penny pool: 8 stocks (MEME, YOLO, PUMP, FOMO, MOON, HODL, DOGE, RICK)
  - [x] Low-Value pool: 6 stocks (BREW, GEAR, BOLT, NEON, GRID, FLUX)
  - [x] Mid-Value pool: 5 stocks with sector tags (NOVA, VOLT, MDCR, TRDE, CHIP)
  - [x] Blue Chip pool: 5 stocks (APEX, TITN, OMNI, VALT, CRWN)
  - [x] File: `Scripts/Setup/Data/StockPoolData.cs` (new)
- [x] Task 3: Add stock selection logic to PriceGenerator (AC: 7)
  - [x] Method: `SelectStocksForRound(StockTier tier)` — picks random subset via Fisher-Yates shuffle
  - [x] No duplicate stocks within a round
  - [x] Removed hardcoded TickerPool, InitializeRound now uses named pools
  - [x] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extended)
- [x] Task 4: Verify tier differentiation (AC: 1–4)
  - [x] Tier parameters verified: volatility, noise, reversion, event frequency all scale correctly
  - [x] Sector tags defined on Mid-Value and Blue Chip stocks (correlation logic deferred to Epic 5)

## Dev Notes

### Architecture Compliance

- **New data file:** `Scripts/Setup/Data/StockPoolData.cs` — follows the pattern of one data class per concern
- **Extends existing:** StockTierData.cs should already have most parameters from Stories 1.1–1.4
- **Stock names are thematic** — the GDD establishes a Wolf of Wall Street / synthwave aesthetic. Stock names should be fun, memorable, and fit the tone (not real company names)
- **Data-driven:** All values as `public static readonly`. StockPoolData is a catalog; PriceGenerator selects from it at round start

### Sector Correlation Note

Mid-Value and Blue Chip tiers mention sector correlation in the GDD. For this story, define the sector tags on stocks. The actual correlation logic (sector events affecting multiple stocks in the same sector) is implemented in Epic 5 (Event System). Here, just tag stocks with sectors so that infrastructure is ready.

### Stock Pool Design Guidance

Stock names should:
- Be 3-5 letter ticker symbols (recognizable as stock tickers)
- Be fun/thematic (Wall Street satire, meme culture for penny stocks)
- Not reference real companies
- Have a display name for the sidebar (e.g., "MEME" → "MemeCoin Inc.")

### Project Structure Notes

- Modifies: `Scripts/Setup/Data/StockTierData.cs`
- Creates: `Scripts/Setup/Data/StockPoolData.cs`
- Modifies: `Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- No new runtime files — this is primarily a data story

### References

- [Source: bull-run-gdd-mvp.md#3.2] — Stock Behavior by Tier table (price ranges, volatility, stocks available, event frequency, behavior)
- [Source: game-architecture.md#Data Architecture] — Pure C# static data classes pattern
- [Source: game-architecture.md#Configuration] — `Scripts/Setup/Data/` location for all game data
- [Source: game-architecture.md#Consistency Rules] — "New data: Add to existing data class or create new in Scripts/Setup/Data/"
- [Source: bull-run-gdd-mvp.md#7.1] — Synthwave/Wall Street aesthetic for naming tone

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6

### Debug Log References
Updated InitializeRound debug log to include DisplayName

### Completion Notes List
- Added EventFrequencyModifier to StockTierConfig (penny=1.5x, blue chip=0.5x)
- Created StockPoolData.cs with StockDefinition struct, StockSector enum, 24 named stocks across 4 tiers
- Replaced hardcoded TickerPool with pool-based selection using Fisher-Yates partial shuffle
- All Mid-Value stocks have sector tags; sector correlation logic deferred to Epic 5
- Added 17 new tests: StockPoolDataTests (10), StockTierDataTests (2), PriceGeneratorTests (5)

### File List
- `Assets/Scripts/Setup/Data/StockTierData.cs` — added EventFrequencyModifier field + values
- `Assets/Scripts/Setup/Data/StockPoolData.cs` — NEW: StockDefinition, StockSector, 4 tier pools
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — SelectStocksForRound(), updated InitializeRound
- `Assets/Tests/Runtime/PriceEngine/StockPoolDataTests.cs` — NEW: 10 tests
- `Assets/Tests/Runtime/PriceEngine/StockTierDataTests.cs` — 2 new EventFrequencyModifier tests
- `Assets/Tests/Runtime/PriceEngine/PriceGeneratorTests.cs` — 5 new stock selection tests
