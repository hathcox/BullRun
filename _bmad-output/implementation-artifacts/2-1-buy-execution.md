# Story 2.1: Buy Execution

Status: ready-for-dev

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

- [ ] Task 1: Create Position data class (AC: 2, 3)
  - [ ] Fields: `StockId`, `Shares` (int), `AverageBuyPrice` (float), `IsLong` (bool, true for buys), `OpenTime`
  - [ ] Property: `UnrealizedPnL(float currentPrice)` — calculates current profit/loss
  - [ ] Property: `MarketValue(float currentPrice)` — current position value
  - [ ] File: `Scripts/Runtime/Trading/Position.cs`
- [ ] Task 2: Create Portfolio class (AC: 2, 3, 4)
  - [ ] Fields: `_positions` (Dictionary<string, Position>), `Cash` (float)
  - [ ] Method: `CanAfford(float cost)` — returns bool
  - [ ] Method: `OpenPosition(string stockId, int shares, float price)` — creates Position, deducts cash
  - [ ] Method: `GetPosition(string stockId)` — returns Position or null
  - [ ] Method: `GetAllPositions()` — returns all open positions
  - [ ] Property: `TotalValue(Func<string, float> priceProvider)` — cash + all position market values
  - [ ] Initialize with `GameConfig.StartingCapital`
  - [ ] File: `Scripts/Runtime/Trading/Portfolio.cs`
- [ ] Task 3: Create TradeExecutor (AC: 1, 4, 5)
  - [ ] Method: `ExecuteBuy(string stockId, int shares, float currentPrice, Portfolio portfolio)`
  - [ ] Validate: `portfolio.CanAfford(shares * currentPrice)` — if false, skip silently
  - [ ] On success: call `portfolio.OpenPosition()`, publish `TradeExecutedEvent`
  - [ ] Wrap in try-catch per architecture error handling pattern
  - [ ] File: `Scripts/Runtime/Trading/TradeExecutor.cs`
- [ ] Task 4: Define Trading events (AC: 5)
  - [ ] `TradeExecutedEvent`: StockId, Shares, Price, IsBuy (bool), IsShort (bool), TotalCost
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 5: Create RunContext with portfolio reference (AC: 2)
  - [ ] Fields: `CurrentAct`, `CurrentRound`, `Portfolio`, `ActiveItems` (ordered list)
  - [ ] RunContext is the central data carrier for run state
  - [ ] File: `Scripts/Runtime/Core/RunContext.cs`

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

### Debug Log References

### Completion Notes List

### File List
