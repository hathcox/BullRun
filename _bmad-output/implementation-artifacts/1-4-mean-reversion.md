# Story 1.4: Mean Reversion

Status: done

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

- [x] Task 1: Add reversion parameters to StockTierData (AC: 2, 5)
  - [x] Add `MeanReversionSpeed` to `StockTierConfig` (float, 0.0 = no reversion, 1.0 = instant snap-back)
  - [x] Populate: Penny ~0.05 (very slow), Low ~0.15, Mid ~0.3, Blue Chip ~0.6 (fast)
  - [x] File: `Scripts/Setup/Data/StockTierData.cs` (extend)
- [x] Task 2: Add trend line tracking to StockInstance (AC: 1)
  - [x] Add `TrendLinePrice` property — tracks where price "should be" based on trend alone
  - [x] Update trend line each frame via `UpdateTrendLine(deltaTime)`
  - [x] This gives a reference point for reversion to target
  - [x] File: `Scripts/Runtime/PriceEngine/StockInstance.cs` (extend)
- [x] Task 3: Implement reversion step in PriceGenerator pipeline (AC: 1, 4)
  - [x] Add as step 4 (final): `else price = Lerp(price, trendLine, reversionSpeed * deltaTime)`
  - [x] Only applies when no active events exist for the stock
  - [x] The `else` branch from the architecture's event check: `if (activeEvent) ... else reversion`
  - [x] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend)
- [x] Task 4: Validate complete pipeline integration (AC: 1, 3)
  - [x] Full pipeline now runs: trend → noise → event check → reversion
  - [x] Pipeline structure matches architecture spec
  - [x] Tier-specific reversion speeds: Penny 0.05, Low 0.15, Mid 0.30, Blue Chip 0.60

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
Claude Opus 4.6

### Debug Log References
N/A — no new debug logging added (reversion is a quiet pipeline step)

### Completion Notes List
- Added `MeanReversionSpeed` field to `StockTierConfig` struct with per-tier values
- Added `TrendLinePrice` property and `UpdateTrendLine()` method to `StockInstance`
- Modified `PriceGenerator.UpdatePrice()` to track active events via boolean and apply `Mathf.Lerp` reversion when no events active
- Pipeline is now complete: trend → noise → event OR reversion → clamp

### File List
- `Assets/Scripts/Setup/Data/StockTierData.cs` — added MeanReversionSpeed to struct + constructor + all 4 tier configs
- `Assets/Scripts/Runtime/PriceEngine/StockInstance.cs` — added TrendLinePrice, UpdateTrendLine(), initialized in Initialize()
- `Assets/Scripts/Runtime/PriceEngine/PriceGenerator.cs` — added trend line update call, restructured event/reversion as mutual exclusive branches
