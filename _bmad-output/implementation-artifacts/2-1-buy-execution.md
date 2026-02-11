# Story 2.1: Buy Execution

Status: review

## Story

As a player,
I want to click BUY to purchase shares at the current market price using my cash,
so that I can take a long position on a stock.

## Acceptance Criteria

1. Clicking BUY purchases shares of the currently selected stock at the current market price
2. Cost = share price x quantity, deducted immediately from available cash
3. Shares are held until explicitly sold or auto-liquidated at market close
4. Cannot buy if insufficient cash — order is silently rejected (no error dialog)
5. Trade execution publishes a `TradeExecutedEvent` via EventBus
6. Buy action is mapped to Left Click on BUY button / Spacebar / RT (controller)

## Tasks / Subtasks

- [x] Task 1: Create Position data class (AC: 2, 3)
  - [x] Fields: `StockId`, `Shares` (int), `AverageBuyPrice` (float), `IsLong` (bool, true for buys), `OpenTime`
  - [x] Property: `UnrealizedPnL(float currentPrice)` — calculates current profit/loss
  - [x] Property: `MarketValue(float currentPrice)` — current position value
  - [x] File: `Scripts/Runtime/Trading/Position.cs`
- [x] Task 2: Create Portfolio class (AC: 2, 3, 4)
  - [x] Fields: `_positions` (Dictionary<string, Position>), `Cash` (float)
  - [x] Method: `CanAfford(float cost)` — returns bool
  - [x] Method: `OpenPosition(string stockId, int shares, float price)` — creates Position, deducts cash
  - [x] Method: `GetPosition(string stockId)` — returns Position or null
  - [x] Method: `GetAllPositions()` — returns all open positions
  - [x] Property: `TotalValue(Func<string, float> priceProvider)` — cash + all position market values
  - [x] Initialize with `GameConfig.StartingCapital`
  - [x] File: `Scripts/Runtime/Trading/Portfolio.cs`
- [x] Task 3: Create TradeExecutor (AC: 1, 4, 5)
  - [x] Method: `ExecuteBuy(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [x] Validate: `portfolio.CanAfford(shares * currentPrice)` — if false, skip silently
  - [x] On success: call `portfolio.OpenPosition()`, publish `TradeExecutedEvent`
  - [x] Wrap in try-catch per architecture error handling pattern
  - [x] File: `Scripts/Runtime/Trading/TradeExecutor.cs`
- [x] Task 4: Define Trading events (AC: 5)
  - [x] `TradeExecutedEvent`: StockId, Shares, Price, IsBuy (bool), IsShort (bool), TotalCost
  - [x] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [x] Task 5: Create RunContext with portfolio reference (AC: 2)
  - [x] Fields: `CurrentAct`, `CurrentRound`, `Portfolio`, `ActiveItems` (ordered list)
  - [x] RunContext is the central data carrier for run state
  - [x] File: `Scripts/Runtime/Core/RunContext.cs`

## Dev Notes

### Architecture Compliance

- **Location:** All trading code in `Scripts/Runtime/Trading/`
- **Key files per architecture:** `TradeExecutor.cs`, `Portfolio.cs`, `Position.cs`
- **Error handling:** Try-catch at system boundary (TradeExecutor.ExecuteBuy). Inner logic stays clean. Never show error dialogs — game keeps running.
- **EventBus:** Publish `TradeExecutedEvent` after successful buy. Other systems (UI, items, audio) will subscribe.
- **RunContext:** Carries portfolio reference. Created per architecture: "RunContext carries: current act, current round, cash, portfolio, active items"
- **Logging:** `[Trading]` tag prefix. Example: `Debug.Log("[Trading] BUY executed: 10 shares of MEME at $2.50")`

### Architecture Error Handling Pattern

```csharp
public void ExecuteBuy(string stockId, int shares, float currentPrice, Portfolio portfolio)
{
    try
    {
        float cost = shares * currentPrice;
        if (!portfolio.CanAfford(cost))
        {
            Debug.Log($"[Trading] Buy rejected: insufficient cash for {shares}x {stockId} at ${currentPrice}");
            return;
        }
        var position = portfolio.OpenPosition(stockId, shares, currentPrice);
        EventBus.Publish(new TradeExecutedEvent(stockId, shares, currentPrice, isBuy: true, isShort: false));
    }
    catch (Exception e)
    {
        Debug.LogError($"[Trading] Trade failed: {e.Message}");
        // Recover: skip trade, player keeps their cash
    }
}
```

### Quantity Handling

The GDD mentions adjusting quantity via scroll wheel / arrow keys / D-Pad. For this story, implement a default quantity (e.g., buy max affordable or a fixed lot size). Quantity UI adjustment is a UI story (Epic 3/10). TradeExecutor should accept quantity as a parameter — it doesn't decide how many shares to buy.

### Input Mapping Note

Input binding (Spacebar, mouse click, RT) is not part of this story. This story creates the execution logic. Input wiring comes when the Trading HUD (Epic 3, Story 3.2) connects UI buttons to TradeExecutor. For now, TradeExecutor exposes public methods callable from any source.

### Project Structure Notes

- Creates: `Scripts/Runtime/Trading/Position.cs`
- Creates: `Scripts/Runtime/Trading/Portfolio.cs`
- Creates: `Scripts/Runtime/Trading/TradeExecutor.cs`
- Creates: `Scripts/Runtime/Core/RunContext.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs` (add TradeExecutedEvent)

### References

- [Source: game-architecture.md#Project Structure] — Trading/ folder: TradeExecutor, Portfolio, Position
- [Source: game-architecture.md#Error Handling] — Try-catch at system boundaries, example with ExecuteTrade
- [Source: game-architecture.md#Event System] — `TradeExecutedEvent` publishing pattern
- [Source: game-architecture.md#Game State Machine] — RunContext carries cash, portfolio, active items
- [Source: bull-run-gdd-mvp.md#3.1] — "BUY: Purchase shares at current market price. Costs cash equal to (share price x quantity)."
- [Source: bull-run-gdd-mvp.md#6.3] — Input mapping: Left Click / Spacebar / RT

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

- `[Trading] Buy rejected: insufficient cash for {shares}x {stockId} at ${currentPrice}` — TradeExecutor.cs
- `[Trading] BUY executed: {shares} shares of {stockId} at ${currentPrice}` — TradeExecutor.cs
- `[Trading] Trade failed: {e.Message}` — TradeExecutor.cs (error recovery)
- `[Trading] Position opened: {shares}x {stockId} at ${price}` — Portfolio.cs
- `[Trading] Position averaged: {stockId} now {totalShares} shares at ${avgPrice}` — Portfolio.cs

### Completion Notes List

- Task 1: Created `Position` class with StockId, Shares, AverageBuyPrice, IsLong, OpenTime fields. Includes UnrealizedPnL and MarketValue methods. 10 unit tests.
- Task 2: Created `Portfolio` class with Dictionary-based position tracking, Cash management, CanAfford, OpenPosition (with averaging for existing positions), GetPosition, GetAllPositions, TotalValue. Constructor accepts starting cash. 13 unit tests.
- Task 3: Created `TradeExecutor` with ExecuteBuy method. Try-catch at system boundary, silent rejection on insufficient cash, publishes TradeExecutedEvent on success. Returns bool for caller convenience. 8 unit tests.
- Task 4: Added `TradeExecutedEvent` struct to GameEvents.cs with StockId, Shares, Price, IsBuy, IsShort, TotalCost fields. 3 unit tests for event structure and EventBus integration.
- Task 5: Created `RunContext` as central run state carrier with CurrentAct, CurrentRound, Portfolio, ActiveItems (List<string>). 5 unit tests.

### Change Log

- 2026-02-10: Implemented all 5 tasks for Story 2.1 Buy Execution. Created Trading system foundation (Position, Portfolio, TradeExecutor), added TradeExecutedEvent to GameEvents, created RunContext. 39 total unit tests.

### File List

- `Assets/Scripts/Runtime/Trading/Position.cs` (new)
- `Assets/Scripts/Runtime/Trading/Portfolio.cs` (new)
- `Assets/Scripts/Runtime/Trading/TradeExecutor.cs` (new)
- `Assets/Scripts/Runtime/Core/RunContext.cs` (new)
- `Assets/Scripts/Runtime/Core/GameEvents.cs` (modified — added TradeExecutedEvent)
- `Assets/Tests/Runtime/Trading/PositionTests.cs` (new)
- `Assets/Tests/Runtime/Trading/PortfolioTests.cs` (new)
- `Assets/Tests/Runtime/Trading/TradeExecutorTests.cs` (new)
- `Assets/Tests/Runtime/Trading/TradeExecutedEventTests.cs` (new)
- `Assets/Tests/Runtime/Core/RunContextTests.cs` (new)
