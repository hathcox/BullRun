# Story 5.4: Global Market Events

Status: done

## Story

As a player,
I want rare market-wide events like crashes, bull runs, flash crashes, and short squeezes,
so that dramatic moments shake up the entire round and force rapid adaptation.

## Acceptance Criteria

1. Market Crash: all stocks drop 30-70% sharply and simultaneously — rare, high-impact global event
2. Bull Run: all stocks rise 25-60% steadily for the event duration — rare global event
3. Flash Crash: single stock drops 25-50% then recovers to near original price — creates a V-shape buy opportunity
4. Short Squeeze: shorted stock spikes 40-100% violently — punishes open short positions
5. Global events (MarketCrash, BullRun) affect ALL active stocks with `TargetStockId = null`
6. Short Squeeze prefers targeting a stock the player is currently shorting (portfolio-aware)
7. MarketCrash, BullRun, and ShortSqueeze are available across all tiers; FlashCrash is Low/Mid only per GDD — all weighted as rare (rarity 0.1-0.2)
8. Headlines for all four events added to EventHeadlineData

## Tasks / Subtasks

- [x] Task 1: Implement Market Crash as global event (AC: 1, 5)
  - [x] Target: ALL active stocks (`TargetStockId = null`, `IsGlobalEvent = true`)
  - [x] EventEffects already handles global events — applies effect to all stocks in `GetActiveEventsForStock()`
  - [x] Verify that price Lerp applies the same percentage drop to each stock (not absolute amount)
  - [x] Config already exists: `EventDefinitions.MarketCrash` (-30 to -70%, 8s, AllTiers, rarity 0.1)
  - [x] Should work with existing single-phase EventEffects — verify end-to-end

- [x] Task 2: Implement Bull Run as global event (AC: 2, 5)
  - [x] Target: ALL active stocks (global)
  - [x] Config already exists: `EventDefinitions.BullRunEvent` (+25-60%, 8s, AllTiers, rarity 0.1)
  - [x] Same implementation path as MarketCrash but positive effect
  - [x] Verify end-to-end with EventScheduler

- [x] Task 3: Implement Flash Crash as multi-phase event (AC: 3)
  - [x] Target: single stock
  - [x] Phase 1 (Crash): price drops 25-50% rapidly over first ~40% of duration
  - [x] Phase 2 (Recovery): price recovers toward original price over remaining ~60% of duration
  - [x] Creates V-shape on chart — buy opportunity for quick players who recognize the pattern
  - [x] Uses multi-phase MarketEvent system from Story 5-3
  - [x] Config already exists: `EventDefinitions.FlashCrash` (-25 to -50%, 4s, Low/Mid, rarity 0.2)
  - [x] Note: 4s total is short — Phase 1 ~1.5s crash, Phase 2 ~2.5s recovery

- [x] Task 4: Implement Short Squeeze with portfolio-aware targeting (AC: 4, 6)
  - [x] Target selection: check `RunContext.Portfolio` for open short positions
  - [x] If player has shorts: target the stock with the largest short position (maximum pain)
  - [x] If player has no shorts: target a random active stock (squeeze still happens, just less punishing)
  - [x] Effect: violent price spike 40-100% over 4s
  - [x] Config already exists: `EventDefinitions.ShortSqueeze` (+40-100%, 4s, AllTiers, rarity 0.2)
  - [x] EventScheduler needs access to `RunContext` for portfolio inspection

- [x] Task 5: Wire RunContext into EventScheduler for portfolio-aware targeting (AC: 6)
  - [x] Add `RunContext` parameter to `EventScheduler` constructor or `InitializeRound()`
  - [x] EventScheduler reads `RunContext.Portfolio.GetPositions()` — one-way read, never modifies
  - [x] Only used by Short Squeeze targeting — other events ignore portfolio
  - [x] File: `Scripts/Runtime/Events/EventScheduler.cs` (extend from Story 5-1)

- [x] Task 6: Add global/special event headlines to EventHeadlineData (AC: 8)
  - [x] MarketCrash headlines: `"MARKET CRASH — panic selling across all sectors!"`, `"Markets in freefall — investors flee"`, `"Breaking: massive selloff hits every stock"`
  - [x] BullRun headlines: `"BULL RUN — markets surge across the board!"`, `"Euphoria grips Wall Street — everything's up"`, `"Green everywhere — biggest rally this year"`
  - [x] FlashCrash headlines: `"Flash crash hits {ticker} — price in freefall!"`, `"Algorithm glitch tanks {ticker}"`, `"{ticker} plunges on mysterious volume spike"`
  - [x] ShortSqueeze headlines: `"SHORT SQUEEZE on {ticker} — shorts getting crushed!"`, `"Bears trapped as {ticker} rockets upward"`, `"{ticker} skyrockets — margin calls for shorts"`
  - [x] File: `Scripts/Setup/Data/EventHeadlineData.cs` (extend from Stories 5-2, 5-3)

- [x] Task 7: Add rare event cap to EventScheduler (AC: 7)
  - [x] Maximum 1 rare event (rarity <= 0.2) per round to prevent overwhelming the player
  - [x] After a rare event is scheduled in a round, exclude rare events from remaining slots
  - [x] This prevents double Market Crash or Market Crash + Short Squeeze in same round
  - [x] File: `Scripts/Runtime/Events/EventScheduler.cs` (extend)

- [x] Task 8: Write comprehensive tests (AC: 1-8)
  - [x] Test: MarketCrash affects all active stocks (global targeting verified)
  - [x] Test: BullRun affects all active stocks with positive effect
  - [x] Test: FlashCrash creates V-shape pattern (price drops then recovers near original)
  - [x] Test: ShortSqueeze targets shorted stock when player has shorts
  - [x] Test: ShortSqueeze targets random stock when player has no shorts
  - [x] Test: Portfolio-aware targeting reads but never modifies portfolio
  - [x] Test: Maximum 1 rare event per round enforced
  - [x] Test: All 4 event types have headlines in EventHeadlineData
  - [x] File: `Tests/Runtime/Events/EventSchedulerTests.cs` (extend from Story 5-1)
  - [x] File: `Tests/Runtime/Events/EventEffectsTests.cs` (extend)

## Dev Notes

### Architecture Compliance

- **RunContext access is read-only** — EventScheduler reads portfolio to make targeting decisions, never modifies it. Price changes from events naturally flow through PriceGenerator → Portfolio P&L.
- **Global events already supported** — `EventEffects.GetActiveEventsForStock()` already returns events where `IsGlobalEvent == true`. `ApplyEventEffect()` applies to each stock. No changes to the effect pipeline needed.
- **Multi-phase from Story 5-3** — Flash Crash reuses the multi-phase MarketEvent support built for PumpAndDump
- **Rarity is config data** — rarity values in EventDefinitions.cs, rare event cap logic in EventScheduler

### What Already Exists (DO NOT recreate)

| Component | File | Status |
|-----------|------|--------|
| `MarketCrash` config | `EventDefinitions.cs` | Complete — -30-70%, 8s, AllTiers, rarity 0.1 |
| `BullRunEvent` config | `EventDefinitions.cs` | Complete — +25-60%, 8s, AllTiers, rarity 0.1 |
| `FlashCrash` config | `EventDefinitions.cs` | Complete — -25-50%, 4s, Low/Mid, rarity 0.2 |
| `ShortSqueeze` config | `EventDefinitions.cs` | Complete — +40-100%, 4s, AllTiers, rarity 0.2 |
| Global event handling | `EventEffects.cs` | Complete — `IsGlobalEvent` check in `GetActiveEventsForStock()` |
| Multi-phase support | `MarketEvent.cs` (Story 5-3) | Prerequisite — used by FlashCrash |
| `Portfolio.GetPositions()` | `Portfolio.cs` | Complete — can iterate open positions |
| `Position.IsShort` | `Position.cs` | Complete — identifies short positions |

### Market Crash — The "Oh Shit" Moment

All stocks tank simultaneously. It's devastating for long positions but a massive windfall for shorts. This teaches players that diversifying with short positions is defensive, not just speculative.

### Short Squeeze — Portfolio-Aware Targeting

The portfolio-aware targeting is a deliberate design choice. It makes the Short Squeeze feel *personal* — the game "fights back" against dominant strategies. Implementation:

```csharp
int? SelectShortSqueezeTarget(RunContext ctx, List<StockInstance> activeStocks)
{
    // Find player's largest short position
    var shortPositions = ctx.Portfolio.GetAllPositions()
        .Where(p => p.IsShort)
        .OrderByDescending(p => Math.Abs(p.MarketValue(/* current price */)))
        .ToList();

    if (shortPositions.Count > 0)
    {
        // Target the largest short — maximum pain
        return shortPositions[0].StockId; // Note: may need int↔string mapping
    }

    // No shorts open — target random stock
    return activeStocks[Random.Range(0, activeStocks.Count)].StockId;
}
```

**Note:** `Position.StockId` is currently `string` while `StockInstance.StockId` is `int`. This mismatch (noted in GameEvents.cs) may need a registry or the StockId types need to be reconciled. Address pragmatically — either convert or create a simple lookup.

### Flash Crash — V-Shape Using Multi-Phase

```
Flash Crash Timeline (4s total):
  $100 ────╲         Phase 0: crash to -40% (~$60) over 1.5s
            ╲
             ╱───    Phase 1: recover to ~$95 over 2.5s
            ╱
```

Reuses the multi-phase system from Story 5-3. Two phases:
- Phase 0: `TargetPricePercent = -0.40`, `PhaseDuration = 1.5s`
- Phase 1: `TargetPricePercent = +0.90` (relative to crash bottom, recovering ~90% of drop), `PhaseDuration = 2.5s`

### Rare Event Cap

```csharp
// In EventScheduler.InitializeRound():
bool rareEventScheduledThisRound = false;

// In SelectEventType():
var available = EventDefinitions.GetEventsForTier(tier);
if (rareEventScheduledThisRound)
{
    // Exclude rare events (rarity <= 0.2)
    available = available.Where(e => e.Rarity > 0.2f).ToList();
}

// After scheduling a rare event:
if (selectedConfig.Rarity <= 0.2f)
    rareEventScheduledThisRound = true;
```

### StockId Type Mismatch

`Position.StockId` is `string` (ticker symbol like "MEME") while `StockInstance.StockId` is `int`. For Short Squeeze targeting:
- Option A: Look up by ticker symbol — `activeStocks.FirstOrDefault(s => s.TickerSymbol == position.StockId)`
- Option B: Add a `Dictionary<string, int>` registry to EventScheduler during InitializeRound
- Prefer Option A for simplicity — it's called once per Short Squeeze, not per-frame

### Previous Story Learnings

- Multi-phase system (Story 5-3) handles the FlashCrash V-shape pattern
- EventEffects global event handling is already tested — just verify with actual MarketCrash/BullRun
- Portfolio access pattern: read-only, same frame, no async

### Project Structure Notes

- Modified: `Assets/Scripts/Runtime/Events/EventScheduler.cs` (RunContext access, rare event cap, Short Squeeze targeting)
- Modified: `Assets/Scripts/Setup/Data/EventHeadlineData.cs` (add 4 event type headline arrays)
- Modified: `Assets/Tests/Runtime/Events/EventSchedulerTests.cs` (extend)
- Modified: `Assets/Tests/Runtime/Events/EventEffectsTests.cs` (extend)
- No new files — builds on Stories 5-1, 5-2, 5-3 infrastructure

### References

- [Source: epics.md#5.4] — "Market Crash: all stocks drop sharply. Bull Run: all stocks rise. Flash Crash: drop then recover. Short Squeeze: shorted stock spikes violently."
- [Source: bull-run-gdd-mvp.md#3.4] — Event table: MarketCrash (screen shake + alarm, all rare), BullRun (green tint, all rare), Flash Crash (rapid red flash, Low/Mid), Short Squeeze (warning on positions, all)
- [Source: EventDefinitions.cs] — All 4 event configs with rarity 0.1-0.2
- [Source: Portfolio.cs] — GetAllPositions(), Position.IsShort for targeting
- [Source: GameEvents.cs] — Note about StockId string vs int mismatch

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

### Completion Notes List

- Tasks 1-2: MarketCrash and BullRun already worked end-to-end via existing global event infrastructure (FireEvent sets null TargetStockId, EventEffects.GetActiveEventsForStock returns global events for all stocks, ApplyEventEffect applies percentage-based Lerp). Verified with new tests confirming same percentage drop applies to all stocks.
- Task 3: FlashCrash implemented as 2-phase event in EventScheduler.FireEvent(). Phase 0 (40% of 4s = 1.6s): crash by rolled effect. Phase 1 (60% = 2.4s): recovery to ~95% of original price. Recovery target calculated as `0.95 / (1 + crashEffect) - 1` to ensure V-shape trajectory.
- Task 4: Short Squeeze targeting implemented via SelectShortSqueezeTarget() method. Iterates portfolio positions to find largest short (by shares), then matches to active stock by both TickerSymbol and StockId.ToString() to handle the string/int type mismatch (Option A from Dev Notes).
- Task 5: Added `_runContext` field and `SetRunContext()` method to EventScheduler. Read-only access — only used by Short Squeeze targeting logic.
- Task 6: Expanded headline arrays from 1 to 4 templates each for MarketCrash, BullRun, FlashCrash, and ShortSqueeze. All follow existing {ticker} substitution pattern.
- Task 7: Added `_rareEventScheduledThisRound` flag to EventScheduler, reset in InitializeRound(). SelectEventType() filters out events with rarity <= 0.2 after first rare event fires. Falls back to full list if all tier events are rare.
- Task 8: Added 14 new tests — FlashCrash multi-phase (2), Short Squeeze targeting (4), rare event cap (1), MarketCrash/BullRun end-to-end (2), headline counts (1), global event price application (2), BullRun price increase (2 in EventEffectsTests).

### Change Log

- 2026-02-13: Implemented all 8 tasks — global events, FlashCrash V-shape, Short Squeeze portfolio-aware targeting, rare event cap, headlines, tests.
- 2026-02-13: Code review — 2 fixes applied (2 MEDIUM). 3 LOW items noted as action items. AC 7 corrected for FlashCrash tier scope.

### Senior Developer Review (AI)

**Reviewer:** Iggy (via Claude Opus 4.6)
**Date:** 2026-02-13
**Outcome:** Changes Requested → Fixed

**Fixes Applied:**
1. [MEDIUM] Fixed Short Squeeze tests to use ticker symbols instead of integer-as-string IDs — now exercises the primary `TickerSymbol` matching path used in production (`EventSchedulerTests.cs`)
2. [MEDIUM] Corrected AC 7 — FlashCrash is Low/Mid only per GDD, not AllTiers. Config was correct, AC wording was imprecise.

**Remaining Action Items (LOW):**
- [ ] [AI-Review][LOW] `foreach` over portfolio positions in `SelectShortSqueezeTarget` allocates enumerator [`EventScheduler.cs:280`]
- [ ] [AI-Review][LOW] Rare event cap only tested for MidValue tier — consider adding Penny/BlueChip tier tests [`EventSchedulerTests.cs:966`]
- [ ] [AI-Review][LOW] Story Change Log section was outside Dev Agent Record (reformatted during review)

### File List

- Modified: `Assets/Scripts/Runtime/Events/EventScheduler.cs` — FlashCrash multi-phase, RunContext wiring, Short Squeeze targeting, rare event cap
- Modified: `Assets/Scripts/Setup/Data/EventHeadlineData.cs` — Expanded headlines for MarketCrash, BullRun, FlashCrash, ShortSqueeze (4 each)
- Modified: `Assets/Tests/Runtime/Events/EventSchedulerTests.cs` — 12 new tests for Story 5-4, Short Squeeze tests fixed to use ticker symbols
- Modified: `Assets/Tests/Runtime/Events/EventEffectsTests.cs` — 2 new tests for global event price application
