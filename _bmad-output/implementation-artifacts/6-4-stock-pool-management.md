# Story 6.4: Stock Pool Management

Status: done

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

- [x] Task 1: Verify StockPoolData completeness (AC: 1, 2, 3, 4, 7)
  - [x] Story 1.5 created StockPoolData with pools per tier
  - [x] Verify each tier has enough stocks (6-8) to allow variety across rounds
  - [x] Verify stock names fit the Wall Street satire / synthwave aesthetic
  - [x] Verify sector tags exist on Mid and Blue Chip stocks for Sector Rotation events
  - [x] File: `Scripts/Setup/Data/StockPoolData.cs` (verify/extend)
- [x] Task 2: Verify PriceGenerator stock selection (AC: 5, 6)
  - [x] Story 1.5 created `SelectStocksForRound()` — verify it selects randomly from tier pool
  - [x] Verify no duplicates within a round
  - [x] Verify correct count per tier: 3-4 for Penny/Low, 2-3 for Mid/Blue
  - [x] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (verify)
- [x] Task 3: Ensure per-tier behavior differentiation (AC: 1, 2, 3, 4)
  - [x] Penny stocks: high noise amplitude, minimal reversion, prone to wild swings
  - [x] Low-value: moderate noise, moderate reversion, trend reversals visible
  - [x] Mid-value: lower noise, faster reversion, stocks in same sector should trend similarly
  - [x] Blue chips: low noise, fast reversion, very stable with occasional dramatic events
  - [x] Verify StockTierData configs produce these behaviors
  - [x] File: `Scripts/Setup/Data/StockTierData.cs` (verify/tune)
- [x] Task 4: Add sector correlation for Mid-Value stocks (AC: 3)
  - [x] When multiple mid-value stocks share a sector, apply a shared trend bias
  - [x] Shared bias: stocks in the same sector have correlated trend directions (both bull or both bear)
  - [x] Correlation applied during PriceGenerator.InitializeRound() for mid/blue tiers
  - [x] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [x] Task 5: Add F2 god mode for testing stock behavior (AC: 1-4)
  - [x] F2 enables god mode: infinite cash, cannot fail margin call
  - [x] Allows dev to focus on observing stock behavior without gameplay pressure
  - [x] File: `Scripts/Editor/DebugManager.cs` (extend)

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

Claude Opus 4.6

### Debug Log References

N/A — No blocking issues encountered.

### Completion Notes List

- **Task 1:** Verified StockPoolData pools. MidValue had 5 stocks (below 6 minimum for variety), added SOLR (Solar Flare Energy, Energy sector) and GENX (GenX Biotech, Health sector). BlueChip had 5 stocks, added FRGE (Forge Dynamics, Industrial sector). Final counts: Penny 8, LowValue 6, MidValue 7, BlueChip 6. All names/tickers fit Wall Street satire theme. All Mid/Blue stocks have sector tags. New tests: `AllPools_HaveAtLeast6Stocks_ForRoundVariety`, `BlueChipStocks_AllHaveSectorTags`.
- **Task 2:** Verified SelectStocksForRound() uses Fisher-Yates partial shuffle for random, duplicate-free selection. Count ranges correct per tier config (3-4 for Penny/Low, 2-3 for Mid/Blue). Added `SelectStocksForRound_ProducesVariety_AcrossMultipleRounds` test.
- **Task 3:** Verified StockTierData configs produce correct behavior differentiation. Noise amplitude decreases Penny(0.12) → LowValue(0.08) → MidValue(0.05) → BlueChip(0.025). Mean reversion increases 0.30 → 0.35 → 0.40 → 0.50. Existing tests already cover ordering assertions. No changes needed.
- **Task 4:** Implemented sector correlation in PriceGenerator.InitializeRound(). For MidValue and BlueChip tiers, stocks sharing a sector now receive the same trend direction (Bull/Bear/Neutral). Penny and LowValue tiers retain independent per-stock trends. Added 3 new tests: sector correlation for Mid, sector correlation for Blue, and no-correlation for Penny.
- **Task 5:** Implemented F2 god mode in DebugManager. Toggle sets cash to $999M and keeps it topped up each frame. MarginCallState auto-passes margin check when god mode active. Gold "GOD MODE" text indicator in top-left corner. Added Portfolio.SetCash() internal method for debug access.

### File List

- `Assets/Scripts/Setup/Data/StockPoolData.cs` — Added 3 new stocks (SOLR, GENX for MidValue; FRGE for BlueChip)
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — Added sector correlation logic in InitializeRound() for Mid/Blue tiers
- `Assets/Scripts/Editor/DebugManager.cs` — Implemented F2 god mode (toggle, cash, indicator)
- `Assets/Scripts/Runtime/Trading/Portfolio.cs` — Added SetCash() internal method for debug tools
- `Assets/Scripts/Runtime/Core/GameStates/MarginCallState.cs` — Added god mode bypass for margin call check
- `Assets/Tests/Runtime/PriceEngine/StockPoolDataTests.cs` — Added AllPools_HaveAtLeast6Stocks_ForRoundVariety, BlueChipStocks_AllHaveSectorTags; removed outdated PennyPool/BlueChipPool individual tests
- `Assets/Tests/Runtime/PriceEngine/PriceGeneratorTests.cs` — Added sector correlation tests (Mid, Blue, no-correlation Penny), variety test

## Senior Developer Review (AI)

**Reviewer:** Claude Opus 4.6 (code-review workflow)
**Date:** 2026-02-12
**Outcome:** Approve (after fixes)

### Action Items

- [x] [AI-Review][HIGH] Cache GUIStyle in DrawGodModeIndicator() — was allocating new GUIStyle every OnGUI call [DebugManager.cs:211-219]
- [x] [AI-Review][HIGH] Fix god mode bypass falsifying RoundCompletedEvent.RoundProfit — now bypasses comparison without mutating event data [MarginCallState.cs:34]
- [x] [AI-Review][MED] Sector correlation tests could silently pass with 0 testable rounds — added Assert.Greater(testableRounds, 0) [PriceGeneratorTests.cs:601,641]
- [x] [AI-Review][MED] Variety test only covered Penny tier — expanded to test all tiers [PriceGeneratorTests.cs:534]
- [x] [AI-Review][MED] LowValue tier missing no-correlation test — added InitializeRound_LowValueStocks_NoSectorCorrelation [PriceGeneratorTests.cs]
- [x] [AI-Review][LOW] DebugManager docstring said "F2 reserved" despite being implemented — updated [DebugManager.cs:8-9]

**Total:** 2 High, 3 Medium, 1 Low — all 6 fixed.

**Notes:** L2 (god mode not restoring cash on toggle off) left as-is — acceptable for a debug tool. Devs understand F2 is a test aid, not a gameplay feature.

## Change Log

- 2026-02-12: Story 6.4 implemented — stock pool verification, sector correlation for Mid/Blue tiers, F2 god mode debug tool
- 2026-02-12: Code review fixes — cached GUIStyle, fixed event data integrity in god mode bypass, strengthened test assertions, added LowValue no-correlation test
