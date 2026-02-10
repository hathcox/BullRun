# Story 2.4: Portfolio Tracking

Status: ready-for-dev

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

- [ ] Task 1: Add real-time valuation to Portfolio (AC: 1, 2, 3)
  - [ ] Method: `GetTotalValue(Func<string, float> getCurrentPrice)` — cash + all position values
  - [ ] For longs: value = shares * currentPrice
  - [ ] For shorts: value = marginHeld + unrealizedPnL (can be negative)
  - [ ] Method: `GetTotalUnrealizedPnL(Func<string, float> getCurrentPrice)` — sum of all position P&Ls
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 2: Add round profit tracking (AC: 4)
  - [ ] Field: `_roundStartValue` — snapshot of total value at round start
  - [ ] Method: `StartRound(float startingValue)` — captures baseline
  - [ ] Property: `RoundProfit(Func<string, float> getCurrentPrice)` — current total value minus round start value
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 3: Subscribe Portfolio to PriceUpdatedEvent (AC: 5)
  - [ ] Portfolio (or a PortfolioUpdater helper) subscribes to `PriceUpdatedEvent`
  - [ ] On price update, cached valuation is refreshed for the affected stock
  - [ ] This avoids recalculating all positions every frame — only update changed stocks
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)
- [ ] Task 4: Expose read accessors for UI consumption (AC: 6)
  - [ ] Property: `Cash` (float)
  - [ ] Property: `PositionCount` (int)
  - [ ] Method: `GetAllPositions()` — returns IReadOnlyList<Position>
  - [ ] Method: `GetPosition(string stockId)` — returns Position or null
  - [ ] Method: `GetPositionPnL(string stockId, float currentPrice)` — returns float
  - [ ] Method: `HasPosition(string stockId)` — returns bool
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs` (extend)

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

### Debug Log References

### Completion Notes List

### File List
