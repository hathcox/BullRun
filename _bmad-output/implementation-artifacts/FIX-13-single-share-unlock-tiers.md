# Story FIX-13: Single Share Start with Unlockable Quantity Tiers

Status: done

## Story

As a player,
I want to start each run with the ability to buy or sell only 1 share at a time, and unlock higher quantity tiers by spending Reputation in the shop on upgrades,
so that position sizing is a progression mechanic rather than a free choice.

## Problem Analysis

Currently `QuantitySelector` offers presets x5, x10, x15, x25 from Round 1 (FIX-3/FIX-6). All presets are always available. With the new $10 economy (FIX-14), trading 10+ shares of a penny stock is immediately affordable and breaks the intended tight early-game feel.

**Current QuantitySelector:**
- `PresetValues = { 5, 10, 15, 25 }` — static array, always available
- `PresetLabels = { "x5", "x10", "x15", "x25" }` — static labels
- `Preset` enum: `Five, Ten, Fifteen, TwentyFive`
- Default: `Preset.Ten` (x10) — resets each round via `RoundStartedEvent`
- Keyboard shortcuts 1-4 always active
- UISetup creates 4 buttons unconditionally

**Desired System:**
- Start with x1 only — no preset buttons visible
- Unlock tiers via Reputation shop purchases: x5 → x10 → x15 → x25
- Each unlock adds a new preset button to the UI
- Unlocks persist for the rest of the run
- Keyboard shortcuts only work for unlocked tiers
- Default resets to x1 (or lowest unlocked) each round

**Affected Code:**
- `Scripts/Runtime/UI/QuantitySelector.cs` — refactor from static presets to dynamic unlock-based system
- `Scripts/Setup/Data/GameConfig.cs` — quantity tier definitions, Rep costs, default = 1
- `Scripts/Setup/UISetup.cs` — dynamic preset button creation
- `Scripts/Runtime/Core/GameRunner.cs` — keyboard shortcut gating, shop integration for unlocks

## Acceptance Criteria

1. Game starts with quantity locked to **1 share** — single "x1" display, no preset buttons visible
2. Quantity tier upgrades available as shop items purchasable with Reputation:
   - Tier 0 (default): x1 — always available, no cost
   - Tier 1 unlock: x5 (Rep cost TBD)
   - Tier 2 unlock: x10 (Rep cost TBD)
   - Tier 3 unlock: x15 (Rep cost TBD)
   - Tier 4 unlock: x25 (Rep cost TBD)
3. Each unlock adds that preset button to the quantity selector UI dynamically
4. Unlocks persist for the rest of the run (not reset per-round)
5. Keyboard shortcuts (1-4) only respond for unlocked presets
6. When switching quantity, only unlocked presets are selectable
7. Round start resets to x1 (base) regardless of unlocks
8. MAX calculation still works — clamped to highest unlocked tier value or affordable shares, whichever is lower
9. Short mechanic (FIX-11) is unaffected — shorts always use `ShortBaseShares` from GameConfig
10. Current `QuantitySelector.PresetValues` and `PresetLabels` refactored to support dynamic unlock state

## Tasks / Subtasks

- [x] Task 1: Add quantity tier config to GameConfig (AC: 2)
  - [x] Add `DefaultTradeQuantity = 1` (change from 10 to 1)
  - [x] Add `QuantityTiers` array or struct: `{ value: 1, cost: 0 }, { value: 5, cost: TBD }, { value: 10, cost: TBD }, { value: 15, cost: TBD }, { value: 25, cost: TBD }`
  - [x] Rep costs: placeholder values to tune during playtesting (suggest: 0, 10, 20, 35, 50)
  - [x] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [x] Task 2: Add unlock tracking to QuantitySelector (AC: 1, 4, 10)
  - [x] Add `_unlockedTierIndex` (int) — starts at 0 (only x1 unlocked)
  - [x] Replace static `PresetValues`/`PresetLabels` usage with dynamic array based on unlocked tiers
  - [x] Add `UnlockTier(int tierIndex)` method — called when player buys upgrade in shop
  - [x] Add `GetUnlockedPresets()` — returns only the preset values the player has access to
  - [x] Add `HighestUnlockedQuantity` property — returns max quantity available
  - [x] Keep x1 as always-available base (not a button — just the default)
  - [x] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`

- [x] Task 3: Refactor QuantitySelector preset selection for unlock awareness (AC: 6, 7, 8)
  - [x] `SelectPreset()`: validate that selected preset is unlocked, reject if not
  - [x] `ResetToDefault()`: reset to x1 (not x10) on round start
  - [x] `GetCurrentQuantity()`: clamp to highest unlocked value if somehow above
  - [x] MAX calculation: `min(maxAffordable, highestUnlockedQuantity)`
  - [x] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`

- [x] Task 4: Refactor UISetup to create dynamic preset buttons (AC: 1, 3)
  - [x] On initial setup: create NO preset buttons (only x1 display text)
  - [x] Add `AddPresetButton(int tierIndex)` method — creates and positions a new button in the quantity row
  - [x] Buttons appear left-to-right as tiers are unlocked: [x5] then [x5][x10] then [x5][x10][x15] etc.
  - [x] Each button wired to `QuantitySelector.SelectPreset()` for its tier
  - [x] Active button highlight logic still works for visible buttons
  - [x] File: `Assets/Scripts/Setup/UISetup.cs`

- [x] Task 5: Add quantity upgrade as shop item (AC: 2, 3)
  - [x] Create a "Trade Volume" upgrade item type in the shop system
  - [x] When purchased: calls `QuantitySelector.UnlockTier(nextTierIndex)`
  - [x] Shop should show the NEXT tier as available (e.g., if x5 unlocked, show "Unlock x10" as purchasable)
  - [x] If all tiers unlocked, volume upgrade no longer appears in shop
  - [x] Cost in Reputation (from GameConfig.QuantityTiers)
  - [x] Integration with existing shop item generation system (ShopItemDef, item pool)
  - [x] Files: Shop item definition scripts, `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 6: Gate keyboard shortcuts to unlocked presets (AC: 5)
  - [x] In `GameRunner.HandleTradingInput()`: keys 1-4 only trigger if corresponding tier is unlocked
  - [x] Key 1 = x5 (tier 1), Key 2 = x10 (tier 2), Key 3 = x15 (tier 3), Key 4 = x25 (tier 4)
  - [x] If tier not unlocked, keypress ignored (no feedback needed)
  - [x] Note: x1 has no keyboard shortcut — it's the default, always selected on round start
  - [x] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [x] Task 7: Update quantity display for x1 default (AC: 1)
  - [x] When no presets unlocked: show "Qty: 1" text (same style as current quantity display)
  - [x] No preset buttons visible — clean, minimal look
  - [x] When first preset unlocked: "Qty: 1" still shown but now a [x5] button appears
  - [x] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`, `Assets/Scripts/Setup/UISetup.cs`

- [x] Task 8: Ensure short mechanic unaffected (AC: 9)
  - [x] Verify FIX-11 short path reads `GameConfig.ShortBaseShares` directly, NOT QuantitySelector
  - [x] Shorts always trade `ShortBaseShares` (1) regardless of unlocked quantity presets
  - [x] File: verification only — no changes expected

- [x] Task 9: Write tests (AC: 1-10)
  - [x] Test: Default quantity is 1 at run start (no unlocks)
  - [x] Test: UnlockTier(1) makes x5 available
  - [x] Test: UnlockTier progressive — x5 then x10 then x15 then x25
  - [x] Test: SelectPreset rejected for locked tiers
  - [x] Test: ResetToDefault goes to x1 (not x10)
  - [x] Test: GetCurrentQuantity clamped to highest unlocked
  - [x] Test: MAX calculation respects unlock cap
  - [x] Test: Unlocks persist across rounds (not reset on RoundStartedEvent)
  - [x] Test: Keyboard shortcut ignored for locked tier
  - [x] Test: Short trade quantity unaffected by QuantitySelector unlocks
  - [x] File: `Assets/Tests/Runtime/UI/QuantityUnlockTests.cs`

## Dev Notes

### Architecture Compliance
- **Setup-Oriented:** Preset buttons dynamically created by UISetup. No Inspector config.
- **GameConfig Constants:** Tier definitions, costs, default quantity all in GameConfig.
- **EventBus:** May need a `QuantityTierUnlockedEvent` for UI updates, or direct method call from shop is acceptable.

### Key Design Decisions
- **x1 is not a button:** The base quantity (1) is just the default display. Preset buttons only appear for x5+. This keeps the UI clean at game start.
- **Sequential unlocks:** Player must unlock tiers in order (x5 before x10). The shop shows only the NEXT available tier as a purchasable item. No skipping.
- **Round reset to x1:** Even with x25 unlocked, each round starts at x1. Player must actively choose higher quantity. This prevents accidental over-trading at round start.
- **Separate from short shares:** Short mechanic (FIX-11) has its own share count in GameConfig. Quantity unlocks only affect Buy/Sell. Clean separation.
- **Rep costs TBD:** Placeholder costs in GameConfig. Tuning during playtesting. Should feel achievable after 2-3 rounds of Rep earning (FIX-14).

### Dependencies
- **FIX-12 (Reputation currency):** Required — quantity unlocks cost Reputation. If FIX-12 not done, can temporarily use cash or stub.
- **FIX-14 (economy rebalance):** Provides Rep income to afford unlocks. Without it, player has 0 Rep and can't buy upgrades.
- **FIX-11 (short redesign):** Short shares are separate. No conflict, but verify during implementation.

### Edge Cases
- **All tiers unlocked:** "Trade Volume" upgrade no longer appears in shop. Shop generates other items instead.
- **Player at x25 preset, round resets to x1:** Player must re-select x25 via button or keyboard. Intentional friction.
- **MAX < selected preset:** GetCurrentQuantity already clamps to affordable. If player has x10 selected but can only afford 3 shares, trades 3.
- **No unlocks entire run:** Player trades x1 all game. Viable but slow — incentivizes spending Rep on volume.

## Dev Agent Record

### Implementation Plan
- Refactored QuantitySelector from static enum/array-based presets to dynamic tier-based unlock system
- Added QuantityTier struct and QuantityTiers config array to GameConfig with placeholder Rep costs (0, 10, 20, 35, 50)
- Changed DefaultTradeQuantity from 10 to 1
- Added QuantityTierUnlockedEvent to EventBus for decoupled shop-to-selector communication
- Added UnlockedQuantityTier tracking to RunContext (persists per-run, resets on new run)
- UISetup ExecuteTradePanel creates NO preset buttons at start; AddPresetButton dynamically adds them on unlock
- ShopState shows Trade Volume upgrade card when next tier is available; hides when all tiers unlocked
- ShopUI extended with upgrade card section (separate from 3 regular item cards)
- GameRunner HandleTradingInput gates keyboard shortcuts 1-4 to IsTierUnlocked checks

### Completion Notes
- All 9 tasks implemented and verified
- QuantitySelector fully refactored: removed Preset enum, PresetValues, PresetLabels; replaced with tier-index-based system
- Shop integration via EventBus pattern: ShopState publishes QuantityTierUnlockedEvent, QuantitySelector subscribes
- Upgrade card in shop UI is a compact horizontal bar below the 3 item cards
- Short mechanic verified unaffected: GameRunner.OpenShortPosition reads GameConfig.ShortBaseShares (line 317)
- 40+ tests in QuantityUnlockTests.cs covering all ACs
- Existing QuantitySelectorTests.cs updated to work with new API
- All old API references removed across entire codebase (verified by search)

### Debug Log
- No compilation errors detected after migration
- All old references to QuantitySelector.Preset, PresetValues, PresetLabels successfully removed

## File List

- `Assets/Scripts/Setup/Data/GameConfig.cs` — Modified: DefaultTradeQuantity 10→1, added QuantityTiers array, added QuantityTier struct
- `Assets/Scripts/Runtime/UI/QuantitySelector.cs` — Modified: Complete refactor from static presets to dynamic tier-based unlock system
- `Assets/Scripts/Setup/UISetup.cs` — Modified: ExecuteTradePanel starts with no preset buttons, AddPresetButton for dynamic creation, ExecuteShopUI adds upgrade card
- `Assets/Scripts/Runtime/Core/GameRunner.cs` — Modified: HandleTradingInput gates keys 1-4 to IsTierUnlocked
- `Assets/Scripts/Runtime/Core/GameEvents.cs` — Modified: Added QuantityTierUnlockedEvent
- `Assets/Scripts/Runtime/Core/RunContext.cs` — Modified: Added UnlockedQuantityTier field, reset in ResetForNewRun
- `Assets/Scripts/Runtime/Core/GameStates/ShopState.cs` — Modified: Added ShowQuantityUpgrade, OnUpgradePurchaseRequested for Trade Volume upgrades
- `Assets/Scripts/Runtime/UI/ShopUI.cs` — Modified: Added upgrade card support (SetUpgradeCard, ShowUpgrade, HideUpgrade, RefreshAfterUpgradePurchase)
- `Assets/Tests/Runtime/UI/QuantityUnlockTests.cs` — New: 40+ tests covering all ACs for the unlock system
- `Assets/Tests/Runtime/UI/QuantitySelectorTests.cs` — Modified: Updated to work with new API (Initialize(Text), SelectPresetByTier, SelectedQuantity)

## Senior Developer Review (AI)

**Reviewer:** Code Review Workflow | **Date:** 2026-02-14
**All ACs:** Implemented | **All Tasks [x]:** Verified genuine

### Findings Fixed (3)
1. **[HIGH] Multi-upgrade per shop visit** — After purchasing a tier upgrade, `ShowQuantityUpgrade(ctx)` now called to refresh the upgrade card with the next available tier. Previously stuck on "UNLOCKED" with no way to buy additional tiers in the same shop visit. (ShopState.cs)
2. **[MEDIUM] Dual state desync risk** — Added sequential validation check (`tierIndex != ctx.UnlockedQuantityTier + 1`) and moved RunContext update to after EventBus publish. Prevents RunContext and QuantitySelector from diverging. (ShopState.cs)
3. **[MEDIUM] Fragile cost parsing** — Added `_upgradeCost` field to ShopUI. `RefreshUpgradeAffordability()` now reads stored int instead of parsing cost from UI display text. (ShopUI.cs)

### Findings Noted (2 LOW — no fix needed)
4. **[LOW]** `GetUnlockedPresets()` allocates new array per call. Not in any hot path currently.
5. **[LOW]** `ShowQuantityUpgrade` doesn't independently null-check `ShopUIInstance`. Safe due to enclosing null check in caller.

## Change Log

- 2026-02-14: Story created — quantity as progression mechanic, x1 start, unlock tiers via Reputation
- 2026-02-14: Implementation complete — All 9 tasks done, full refactor of QuantitySelector, shop integration, tests written
- 2026-02-14: Code review complete — 1 HIGH + 2 MEDIUM fixed (multi-upgrade shop refresh, dual state desync, fragile cost parsing)
