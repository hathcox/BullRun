# Story 2.4: Portfolio Tracking

Status: done

## Story

As a player,
I want real-time tracking of all my positions with P&L calculations,
so that I know my current financial state at every moment during trading.

## Acceptance Criteria

1. Portfolio tracks all open positions (both long and short) simultaneously
2. Unrealized P&L is recalculated every frame as prices update
3. Total portfolio value = cash + sum of all position market values (longs + shorts)
4. Round profit is tracked separately: total portfolio value change since round start
5. Portfolio subscribes to PriceUpdatedEvent to stay current
6. Portfolio exposes all data needed by the UI (Epic 3) via public accessors

## Tasks / Subtasks

- [x] Task 1: Add real-time valuation to Portfolio (AC: 1, 2, 3)
  - [x] Method: `GetTotalValue(Func<string, float> getCurrentPrice)` — cash + all position values
  - [x] For longs: value = shares * currentPrice
  - [x] For shorts: value = marginHeld + unrealizedPnL (can be negative)
  - [x] Method: `GetTotalUnrealizedPnL(Func<string, float> getCurrentPrice)` — sum of all position P&Ls
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 2: Add round profit tracking (AC: 4)
  - [x] Field: `_roundStartValue` — snapshot of total value at round start
  - [x] Method: `StartRound(float startingValue)` — captures baseline
  - [x] Property: `RoundProfit(Func<string, float> getCurrentPrice)` — current total value minus round start value
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 3: Subscribe Portfolio to PriceUpdatedEvent (AC: 5)
  - [x] Portfolio (or a PortfolioUpdater helper) subscribes to `PriceUpdatedEvent`
  - [x] On price update, cached valuation is refreshed for the affected stock
  - [x] This avoids recalculating all positions every frame — only update changed stocks
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [x] Task 4: Expose read accessors for UI consumption (AC: 6)
  - [x] Property: `Cash` (float)
  - [x] Property: `PositionCount` (int)
  - [x] Method: `GetAllPositions()` — returns IReadOnlyList<Position>
  - [x] Method: `GetPosition(string stockId)` — returns Position or null
  - [x] Method: `GetPositionPnL(string stockId, float currentPrice)` — returns float
  - [x] Method: `HasPosition(string stockId)` — returns bool
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)

## Dev Notes

### Architecture Compliance

- **EventBus subscription:** Portfolio subscribes to `PriceUpdatedEvent` to stay current — follows the "systems communicate through EventBus" rule
- **One-way dependency:** UI will read from Portfolio (allowed per architecture: "UI classes read from runtime systems"). Portfolio never references UI.
- **RunContext:** Portfolio is accessed via `RunContext.Portfolio` — the central data carrier
- **No direct system references:** Portfolio doesn't call PriceEngine directly. It gets price updates via EventBus.

### Valuation Strategy

Two approaches for real-time valuation:
1. **Event-driven** (preferred): Cache per-stock prices on PriceUpdatedEvent, recalculate totals on demand
2. **Poll-based**: Recalculate everything every frame via a price lookup function

The event-driven approach is more efficient since not all stocks change every frame. Cache the latest price per stock and recompute totals only when requested by UI.

```csharp
private readonly Dictionary<string, float> _latestPrices = new();

private void OnPriceUpdated(PriceUpdatedEvent e)
{
    _latestPrices[e.StockId] = e.NewPrice;
}

public float GetTotalValue()
{
    float total = Cash;
    foreach (var pos in _positions.Values)
    {
        if (_latestPrices.TryGetValue(pos.StockId, out float price))
            total += pos.IsShort ? pos.MarginHeld + pos.UnrealizedPnL(price) : pos.Shares * price;
    }
    return total;
}
```

### Round Profit Tracking

Round profit is a key UI metric (displayed in the top bar HUD). It drives the margin call check (Epic 4). The calculation is simple: `currentTotalValue - roundStartValue`. The round start snapshot is taken when the Trading phase begins.

### Project Structure Notes

- Modifies: `Scripts/Runtime/Trading/Portfolio.cs`
- No new files — this story enriches the Portfolio class with real-time tracking capabilities

### References

- [Source: game-architecture.md#Event System] — Subscribe pattern: `EventBus.Subscribe<PriceUpdatedEvent>(handler)`
- [Source: game-architecture.md#Architectural Boundaries] — "UI classes read from runtime systems (one-way dependency)"
- [Source: game-architecture.md#Game State Machine] — RunContext carries portfolio
- [Source: bull-run-gdd-mvp.md#6.1] — "Top Bar: Cash available | Total Portfolio Value (with % change) | Current Round Profit | Margin Call Target"
- [Source: bull-run-gdd-mvp.md#6.1] — "Right Sidebar: Current positions. Each held stock shows: shares held, average buy price, current P&L"

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6

### Debug Log References
No issues encountered during implementation.

### Completion Notes List
- Task 1: Renamed `TotalValue` to `GetTotalValue` with correct short position valuation (marginHeld + unrealizedPnL instead of raw market value). Added `GetTotalUnrealizedPnL` method summing P&L across all positions.
- Task 2: Added `_roundStartValue` field, `StartRound(float)` to capture baseline, and `GetRoundProfit(Func)` returning current total value minus baseline.
- Task 3: Added event-driven price caching via `SubscribeToPriceUpdates()`/`UnsubscribeFromPriceUpdates()`. Portfolio caches latest prices from `PriceUpdatedEvent` (int StockId converted to string). Added no-arg overloads for `GetTotalValue()`, `GetTotalUnrealizedPnL()`, and `GetRoundProfit()` that use cached prices.
- Task 4: Added `PositionCount` property, `GetPositionPnL(string, float)` method, `HasPosition(string)` method. Changed `GetAllPositions()` return type from `List<Position>` to `IReadOnlyList<Position>`. `Cash` and `GetPosition` already existed.

### Change Log
- 2026-02-10: Implemented story 2-4 — real-time portfolio valuation, round profit tracking, EventBus price caching, and UI read accessors
- 2026-02-10: Code review fixes — GetAllPositions returns IReadOnlyCollection (no allocation), added debug warning for uncached prices, documented int/string StockId bridge, UnsubscribeFromPriceUpdates clears cache, LiquidateAllPositions clears price cache

### File List
- Assets/Scripts/Runtime/Trading/Portfolio.cs (modified)
- Assets/Tests/Runtime/Trading/PortfolioTests.cs (modified)
