# Story 4.3: Auto-Liquidation

Status: ready-for-dev

## Story

As a player,
I want all positions automatically liquidated when the timer expires,
so that rounds resolve cleanly and I see my final result.

## Acceptance Criteria

1. When the trading timer hits 0, all open long positions are sold at current market price
2. All open short positions are covered at current market price
3. Final P&L for the round is calculated after all liquidation
4. Clear visual/audio signal that market is closing (distinct from normal trading)
5. Player cannot execute trades during the liquidation process
6. After liquidation completes, transition to margin call check

## Tasks / Subtasks

- [ ] Task 1: Create MarketCloseState (AC: 1, 2, 5, 6)
  - [ ] On Enter: disable trade execution (set a flag on TradeExecutor or RunContext)
  - [ ] Call `Portfolio.LiquidateAllPositions()` (from Story 2.5) with current prices
  - [ ] Capture total round P&L from liquidation result
  - [ ] Publish `MarketClosedEvent` with round results
  - [ ] Brief pause (1-2 seconds) for the player to absorb the result
  - [ ] Then transition to margin call check logic (Story 4.4)
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- [ ] Task 2: Create MarketCloseUI feedback (AC: 4)
  - [ ] "MARKET CLOSED" text overlay — large, dramatic, brief
  - [ ] Flash or screen effect to punctuate the moment
  - [ ] Show final round profit prominently: "+$650" green or "-$120" red
  - [ ] File: `Scripts/Runtime/UI/MarketCloseUI.cs`
- [ ] Task 3: Add trade lockout mechanism (AC: 5)
  - [ ] Add `IsTradeEnabled` flag to TradeExecutor or RunContext
  - [ ] MarketCloseState sets this to false on Enter
  - [ ] TradingState sets this to true on Enter
  - [ ] All Execute methods in TradeExecutor check this flag first
  - [ ] File: `Scripts/Runtime/Trading/TradeExecutor.cs` (extend)
- [ ] Task 4: Define MarketClosedEvent (AC: 3)
  - [ ] `MarketClosedEvent`: RoundNumber, RoundProfit, FinalCash, PositionsLiquidated (count)
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 5: Connect TradingState → MarketCloseState transition (AC: 1)
  - [ ] Ensure TradingState transitions to MarketCloseState when timer expires
  - [ ] TradingState.Exit() should stop PriceGenerator updates
  - [ ] File: `Scripts/Runtime/Core/GameStates/TradingState.cs` (extend from Story 4.1)

## Dev Notes

### Architecture Compliance

- **State flow:** `Trading → **MarketClose** → (margin call check) → Shop or RunSummary`
- **Location:** `Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- **Uses existing:** `Portfolio.LiquidateAllPositions()` from Story 2.5
- **EventBus:** Publishes `MarketClosedEvent` for UI and analytics

### Liquidation Order

Liquidate all positions atomically:
1. Close all long positions (sell at current price)
2. Close all short positions (cover at current price)
3. Sum total realized P&L
4. Portfolio now has zero positions and all value as cash

The order doesn't matter for gameplay (all at the same final price), but process longs before shorts for cleaner logging.

### The Dramatic Moment

Market close is a key emotional beat. The player has been frantically trading for 60 seconds, and now everything resolves. The UI should make this moment feel decisive:
- Brief freeze/pause effect
- Numbers animate to final values
- Clear green/red color indicating profit or loss
- Then transition to the margin call check (did I make it?)

### Trade Lockout

The lockout must be immediate and absolute — no race condition where a trade executes during liquidation. Set the flag before starting liquidation.

### Project Structure Notes

- Creates: `Scripts/Runtime/Core/GameStates/MarketCloseState.cs`
- Creates: `Scripts/Runtime/UI/MarketCloseUI.cs`
- Modifies: `Scripts/Runtime/Trading/TradeExecutor.cs` (add IsTradeEnabled flag)
- Modifies: `Scripts/Runtime/Core/GameStates/TradingState.cs` (transition to MarketClose)
- Modifies: `Scripts/Runtime/Core/GameEvents.cs`

### References

- [Source: game-architecture.md#Game State Machine] — State flow includes MarketClose
- [Source: bull-run-gdd-mvp.md#2.2] — "Phase 3: Market Close & Draft Shop — When timer expires, all positions are automatically liquidated at current market price"
- [Source: bull-run-gdd-mvp.md#2.2] — "The round's profit or loss is calculated"
- [Source: bull-run-gdd-mvp.md#6.2] — Juice for dramatic moments

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
