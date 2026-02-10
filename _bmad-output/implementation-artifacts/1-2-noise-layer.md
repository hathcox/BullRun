# Story 1.2: Noise Layer

Status: ready-for-dev

## Story

As a player,
I want random variation on top of price trends,
so that prices are not perfectly predictable even when the trend is known.

## Acceptance Criteria

1. Random walk noise is applied over the base trend each frame
2. Noise amplitude scales with stock tier volatility (penny = very high, blue chip = low-med)
3. Noise creates readable chart patterns without overwhelming the trend
4. Noise parameters are configurable per tier via StockTierData
5. The combined trend+noise produces a line that looks like a real stock chart

## Tasks / Subtasks

- [ ] Task 1: Add noise parameters to StockTierData (AC: 2, 4)
  - [ ] Add `NoiseAmplitude` field to `StockTierConfig` (float, relative to price)
  - [ ] Add `NoiseFrequency` field (how often direction changes occur)
  - [ ] Populate values: Penny = very high amplitude, Blue Chip = low amplitude
  - [ ] File: `Scripts/Setup/Data/StockTierData.cs` (extend from Story 1.1)
- [ ] Task 2: Add noise fields to StockInstance (AC: 1)
  - [ ] Add `_noiseAmplitude`, `_noiseFrequency` fields set from tier config
  - [ ] Add `_noiseAccumulator` for smooth random walk (not pure Random.Range per frame)
  - [ ] File: `Scripts/Runtime/PriceEngine/StockInstance.cs` (extend from Story 1.1)
- [ ] Task 3: Implement noise in PriceGenerator pipeline (AC: 1, 3, 5)
  - [ ] Add noise step after trend: `price += Random.Range(-noise, noise)` scaled by tier
  - [ ] Use smoothed random walk, not pure white noise — avoids jittery appearance
  - [ ] Clamp price to tier's valid price range (never go negative)
  - [ ] File: `Scripts/Runtime/PriceEngine/PriceGenerator.cs` (extend from Story 1.1)
- [ ] Task 4: Verify combined output readability (AC: 3, 5)
  - [ ] Trend should remain visually apparent through the noise
  - [ ] Penny stocks should swing wildly; blue chips should be relatively smooth
  - [ ] No price going to zero or negative

## Dev Notes

### Architecture Compliance

- **Extends Story 1.1 files** — do NOT create new files, add noise layer to existing PriceGenerator and StockInstance
- **Pipeline order matters:** trend THEN noise. The architecture defines: `price += trendPerSecond * deltaTime; price += Random.Range(-noise, noise);`
- **Data in StockTierData** — noise amplitude/frequency are tier-level config, not per-stock random values
- **No new events needed** — PriceUpdatedEvent (from Story 1.1) already publishes after the full pipeline runs

### Implementation Guidance

```csharp
// In PriceGenerator.UpdatePrice():
// Step 1: Trend (Story 1.1)
stock.CurrentPrice += stock.TrendPerSecond * deltaTime;

// Step 2: Noise (THIS STORY)
float noiseRange = stock.CurrentPrice * stock.NoiseAmplitude * deltaTime;
stock.CurrentPrice += Random.Range(-noiseRange, noiseRange);

// Clamp to valid range
stock.CurrentPrice = Mathf.Max(stock.CurrentPrice, stock.TierConfig.MinPrice);
```

**Smoothing approach:** Consider Perlin noise or a random-walk accumulator instead of pure `Random.Range` for smoother chart appearance. Pure random per frame creates a jittery line; a random walk with momentum looks more like a real stock chart.

### Design Principle

Noise gives the chart "personality" between events. It should make prices unpredictable but not chaotic. A skilled player watching the chart should still be able to identify the underlying trend direction through the noise. Think "real stock chart on a 1-minute timeframe" — not "earthquake seismograph."

### Project Structure Notes

- Modifies: `Scripts/Runtime/PriceEngine/PriceGenerator.cs`
- Modifies: `Scripts/Runtime/PriceEngine/StockInstance.cs`
- Modifies: `Scripts/Setup/Data/StockTierData.cs`
- No new files needed

### References

- [Source: game-architecture.md#Price Engine] — `price += Random.Range(-noise, noise)` pipeline step
- [Source: game-architecture.md#Data Architecture] — Tier configs as static data
- [Source: bull-run-gdd-mvp.md#3.3] — "Noise Layer: Random walk noise on top of base trend. Amplitude scales with stock tier volatility."
- [Source: bull-run-gdd-mvp.md#3.2] — Tier volatility levels

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
