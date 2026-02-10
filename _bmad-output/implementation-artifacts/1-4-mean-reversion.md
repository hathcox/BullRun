# Story 1.4: Mean Reversion

Status: ready-for-dev

## Story

As a player,
I want prices to gradually return toward the trend line after event spikes,
so that patient play is rewarded and temporary dislocations are identifiable.

## Acceptance Criteria

1. After an event spike ends, prices gradually revert toward the base trend line
2. Reversion speed varies by stock tier (penny = slow/none, blue chip = fast)
3. Reversion creates identifiable patterns — players learn to spot post-spike recovery opportunities
4. Reversion does not fight an active event (only applies when no event is active)
5. Reversion speed parameters are configurable per tier

## Tasks / Subtasks

- [ ] Task 1: Add reversion parameters to StockTierData (AC: 2, 5)
  - [ ] Add `MeanReversionSpeed` to `StockTierConfig` (float, 0.0 = no reversion, 1.0 = instant snap-back)
  - [ ] Populate: Penny ~0.05 (very slow), Low ~0.15, Mid ~0.3, Blue Chip ~0.6 (fast)
  - [ ] File: `Scripts/Setup/Data/StockTierData.cs` (extend)
- [ ] Task 2: Add trend line tracking to StockInstance (AC: 1)
  - [ ] Add `_trendLinePrice` field — tracks where price "should be" based on trend alone
  - [ ] Update trend line each frame: `_trendLinePrice += TrendPerSecond * deltaTime`
  - [ ] This gives a reference point for reversion to target
  - [ ] File: `Scripts/Runtime/PriceEngine/StockInstance.cs` (extend)
- [ ] Task 3: Implement reversion step in PriceGenerator pipeline (AC: 1, 4)
  - [ ] Add as step 4 (final): `else price = Lerp(price, trendLine, reversionSpeed * deltaTime)`
  - [ ] Only applies when stock has no active event (`!stock.HasActiveEvent`)
  - [ ] The `else` branch from the architecture's event check: `if (activeEvent) ... else reversion`
  - [ ] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [ ] Task 4: Validate complete pipeline integration (AC: 1, 3)
  - [ ] Full pipeline now runs: trend → noise → event check → reversion
  - [ ] Verify reversion produces visible "return to trend" after event spikes
  - [ ] Verify penny stocks barely revert while blue chips snap back quickly

## Dev Notes

### Architecture Compliance

- **Pipeline position:** Mean reversion is step 4 of 4 (final): trend → noise → events → **reversion**
- **Architecture formula:** `else price = Lerp(price, trendLine, reversionSpeed)`
- **Conditional:** Reversion is the `else` branch — it only fires when no event is active on this stock
- **Data in StockTierData** — reversion speed is tier-level config

### Complete Pipeline After This Story

```csharp
public void UpdatePrice(StockInstance stock, float deltaTime)
{
    // Step 1: Base trend (Story 1.1)
    stock.UpdateTrendLine(deltaTime);
    stock.CurrentPrice += stock.TrendPerSecond * deltaTime;

    // Step 2: Noise (Story 1.2)
    float noiseRange = stock.CurrentPrice * stock.NoiseAmplitude * deltaTime;
    stock.CurrentPrice += Random.Range(-noiseRange, noiseRange);

    // Step 3 & 4: Event spike OR Mean reversion (Story 1.3 & 1.4)
    if (stock.HasActiveEvent)
    {
        stock.CurrentPrice = Mathf.Lerp(stock.CurrentPrice, stock.EventTargetPrice, stock.EventForce * deltaTime);
    }
    else
    {
        stock.CurrentPrice = Mathf.Lerp(stock.CurrentPrice, stock.TrendLinePrice, stock.ReversionSpeed * deltaTime);
    }

    // Clamp
    stock.CurrentPrice = Mathf.Max(stock.CurrentPrice, stock.TierConfig.MinPrice);

    EventBus.Publish(new PriceUpdatedEvent(stock.StockId, stock.CurrentPrice, previousPrice, deltaTime));
}
```

### Design Principle

Mean reversion rewards patient players who can identify temporary dislocations from the trend. After a Flash Crash or Earnings Miss, a blue chip stock will visibly "recover" toward where it would have been — creating a buy-the-dip opportunity. Penny stocks barely revert, making their wild swings permanent and unpredictable. This tier difference teaches players that different stocks require different strategies.

### Project Structure Notes

- Modifies: `Scripts/Setup/Data/StockTierData.cs`
- Modifies: `Scripts/Runtime/PriceEngine/StockInstance.cs`
- Modifies: `Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- No new files needed — completes the 4-layer pipeline

### References

- [Source: game-architecture.md#Price Engine] — `else price = Lerp(price, trendLine, reversionSpeed)` pipeline step
- [Source: bull-run-gdd-mvp.md#3.3] — "Mean Reversion: After event spikes, prices gradually revert toward the trend line. Speed of reversion varies by tier."
- [Source: bull-run-gdd-mvp.md#3.3] — "Penny stocks revert slowly or not at all; blue chips revert quickly."

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
