# Story 1.5: Stock Tier Configuration

Status: ready-for-dev

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

- [ ] Task 1: Finalize StockTierData with complete definitions (AC: 1–4, 6)
  - [ ] Ensure all tier configs have: price range, volatility, trend strength, noise amplitude, reversion speed, event frequency modifier, stock count per round
  - [ ] Verify values from Stories 1.1–1.4 are coherent and balanced
  - [ ] File: `Scripts/Setup/Data/StockTierData.cs` (finalize)
- [ ] Task 2: Create stock pool definitions (AC: 5, 7)
  - [ ] Define `StockDefinition` struct: TickerSymbol, DisplayName, Tier, Sector (optional), FlavorText
  - [ ] Create Penny pool: ~6-8 stocks (e.g., MEME, YOLO, PUMP, FOMO, MOON, HODL, DOGE, RICK)
  - [ ] Create Low-Value pool: ~6-8 stocks (e.g., BREW, GEAR, BOLT, NEON, GRID, FLUX)
  - [ ] Create Mid-Value pool: ~4-6 stocks with sector tags (e.g., TECH/Nova Systems, ENRG/Volt Power, HLTH/MedCore)
  - [ ] Create Blue Chip pool: ~4-6 stocks (e.g., APEX, TITAN, OMNI, VAULT, CROWN)
  - [ ] File: `Scripts/Setup/Data/StockPoolData.cs` (new)
- [ ] Task 3: Add stock selection logic to PriceGenerator (AC: 7)
  - [ ] Method: `SelectStocksForRound(StockTier tier, int count)` — picks random subset from tier's pool
  - [ ] Ensure no duplicate stocks within a round
  - [ ] Return list of initialized StockInstance objects
  - [ ] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [ ] Task 4: Verify tier differentiation (AC: 1–4)
  - [ ] Penny stocks should produce wild, chaotic charts
  - [ ] Blue chips should produce smooth, steady charts with subtle movement
  - [ ] Mid-value should show sector correlation (when multiple mid-value stocks share a sector, they should trend similarly)

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

### Debug Log References

### Completion Notes List

### File List
