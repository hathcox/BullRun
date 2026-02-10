# Story 6.4: Stock Pool Management

Status: ready-for-dev

## Story

As a player,
I want different stocks available per act with appropriate behavior,
so that each tier introduces new trading dynamics and the run feels fresh.

## Acceptance Criteria

1. Act 1: 3-4 penny stocks with wild swings and pump & dump patterns
2. Act 2: 3-4 low-value stocks with trend-based movement and reversals
3. Act 3: 2-3 mid-value stocks with sector correlation and steadier trends
4. Act 4: 2-3 blue chips with stable movement and rare high-impact events
5. Stocks are randomly selected from the tier pool each round (not the same stocks every time)
6. No duplicate stocks within a single round
7. Stock names and tickers are thematic and memorable

## Tasks / Subtasks

- [ ] Task 1: Verify StockPoolData completeness (AC: 1, 2, 3, 4, 7)
  - [ ] Story 1.5 created StockPoolData with pools per tier
  - [ ] Verify each tier has enough stocks (6-8) to allow variety across rounds
  - [ ] Verify stock names fit the Wall Street satire / synthwave aesthetic
  - [ ] Verify sector tags exist on Mid and Blue Chip stocks for Sector Rotation events
  - [ ] File: `Scripts/Setup/Data/StockPoolData.cs` (verify/extend)
- [ ] Task 2: Verify PriceGenerator stock selection (AC: 5, 6)
  - [ ] Story 1.5 created `SelectStocksForRound()` — verify it selects randomly from tier pool
  - [ ] Verify no duplicates within a round
  - [ ] Verify correct count per tier: 3-4 for Penny/Low, 2-3 for Mid/Blue
  - [ ] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (verify)
- [ ] Task 3: Ensure per-tier behavior differentiation (AC: 1, 2, 3, 4)
  - [ ] Penny stocks: high noise amplitude, minimal reversion, prone to wild swings
  - [ ] Low-value: moderate noise, moderate reversion, trend reversals visible
  - [ ] Mid-value: lower noise, faster reversion, stocks in same sector should trend similarly
  - [ ] Blue chips: low noise, fast reversion, very stable with occasional dramatic events
  - [ ] Verify StockTierData configs produce these behaviors
  - [ ] File: `Scripts/Setup/Data/StockTierData.cs` (verify/tune)
- [ ] Task 4: Add sector correlation for Mid-Value stocks (AC: 3)
  - [ ] When multiple mid-value stocks share a sector, apply a shared trend bias
  - [ ] Shared bias: stocks in the same sector have correlated trend directions (both bull or both bear)
  - [ ] Correlation applied during PriceGenerator.InitializeRound() for mid/blue tiers
  - [ ] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [ ] Task 5: Add F2 god mode for testing stock behavior (AC: 1-4)
  - [ ] F2 enables god mode: infinite cash, cannot fail margin call
  - [ ] Allows dev to focus on observing stock behavior without gameplay pressure
  - [ ] File: `Scripts/Editor/DebugManager.cs` (extend)

## Dev Notes

### Architecture Compliance

- **This story is primarily verification and sector correlation** — most infrastructure was built in Story 1.5
- **Data-driven:** All stock pools and tier parameters in `Scripts/Setup/Data/`
- **F2 god mode** per architecture debug tools table

### Sector Correlation Implementation

For mid-value and blue chip stocks, apply a sector-aware trend initialization:

```csharp
// During InitializeRound for Mid/Blue tiers:
// 1. Group stocks by sector
// 2. Pick a trend direction per sector (not per stock)
// 3. All stocks in a sector get the same base trend direction
// 4. Individual noise still varies per stock

var sectorGroups = stocks.GroupBy(s => s.Sector);
foreach (var group in sectorGroups)
{
    var sectorTrend = RandomTrendDirection(); // Bull, Bear, or Neutral
    foreach (var stock in group)
    {
        stock.Initialize(tier, startPrice, sectorTrend, trendStrength);
    }
}
```

This creates the "sector correlation" behavior the GDD describes for mid-value stocks. Players learn to watch for sector-wide movements.

### God Mode (F2) Design

```
F2 toggles god mode:
- Portfolio.Cash = float.MaxValue (or very large number)
- Margin call check always passes
- Visual indicator: "GOD MODE" text in corner
- Toggle off with F2 again
```

### Project Structure Notes

- Verifies: `Scripts/Setup/Data/StockPoolData.cs`
- Verifies: `Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- Verifies: `Scripts/Setup/Data/StockTierData.cs`
- Modifies: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (sector correlation)
- Modifies: `Scripts/Editor/DebugManager.cs` (F2 god mode)

### References

- [Source: bull-run-gdd-mvp.md#3.2] — Stock Behavior by Tier table
- [Source: bull-run-gdd-mvp.md#3.2] — "Mid-Value: Sector correlation, steadier trends"
- [Source: bull-run-gdd-mvp.md#2.1] — "Each Act corresponds to a market tier with increasing complexity"
- [Source: game-architecture.md#Debug Tools] — "F2: Infinite cash, can't fail margin call"

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
