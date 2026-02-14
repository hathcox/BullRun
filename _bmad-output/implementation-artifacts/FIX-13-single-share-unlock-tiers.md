# Story FIX-13: Single Share Start with Unlockable Quantity Tiers

Status: ready-for-dev

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

- [ ] Task 1: Add quantity tier config to GameConfig (AC: 2)
  - [ ] Add `DefaultTradeQuantity = 1` (change from 10 to 1)
  - [ ] Add `QuantityTiers` array or struct: `{ value: 1, cost: 0 }, { value: 5, cost: TBD }, { value: 10, cost: TBD }, { value: 15, cost: TBD }, { value: 25, cost: TBD }`
  - [ ] Rep costs: placeholder values to tune during playtesting (suggest: 0, 10, 20, 35, 50)
  - [ ] File: `Assets/Scripts/Setup/Data/GameConfig.cs`

- [ ] Task 2: Add unlock tracking to QuantitySelector (AC: 1, 4, 10)
  - [ ] Add `_unlockedTierIndex` (int) — starts at 0 (only x1 unlocked)
  - [ ] Replace static `PresetValues`/`PresetLabels` usage with dynamic array based on unlocked tiers
  - [ ] Add `UnlockTier(int tierIndex)` method — called when player buys upgrade in shop
  - [ ] Add `GetUnlockedPresets()` — returns only the preset values the player has access to
  - [ ] Add `HighestUnlockedQuantity` property — returns max quantity available
  - [ ] Keep x1 as always-available base (not a button — just the default)
  - [ ] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`

- [ ] Task 3: Refactor QuantitySelector preset selection for unlock awareness (AC: 6, 7, 8)
  - [ ] `SelectPreset()`: validate that selected preset is unlocked, reject if not
  - [ ] `ResetToDefault()`: reset to x1 (not x10) on round start
  - [ ] `GetCurrentQuantity()`: clamp to highest unlocked value if somehow above
  - [ ] MAX calculation: `min(maxAffordable, highestUnlockedQuantity)`
  - [ ] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`

- [ ] Task 4: Refactor UISetup to create dynamic preset buttons (AC: 1, 3)
  - [ ] On initial setup: create NO preset buttons (only x1 display text)
  - [ ] Add `AddPresetButton(int tierIndex)` method — creates and positions a new button in the quantity row
  - [ ] Buttons appear left-to-right as tiers are unlocked: [x5] then [x5][x10] then [x5][x10][x15] etc.
  - [ ] Each button wired to `QuantitySelector.SelectPreset()` for its tier
  - [ ] Active button highlight logic still works for visible buttons
  - [ ] File: `Assets/Scripts/Setup/UISetup.cs`

- [ ] Task 5: Add quantity upgrade as shop item (AC: 2, 3)
  - [ ] Create a "Trade Volume" upgrade item type in the shop system
  - [ ] When purchased: calls `QuantitySelector.UnlockTier(nextTierIndex)`
  - [ ] Shop should show the NEXT tier as available (e.g., if x5 unlocked, show "Unlock x10" as purchasable)
  - [ ] If all tiers unlocked, volume upgrade no longer appears in shop
  - [ ] Cost in Reputation (from GameConfig.QuantityTiers)
  - [ ] Integration with existing shop item generation system (ShopItemDef, item pool)
  - [ ] Files: Shop item definition scripts, `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 6: Gate keyboard shortcuts to unlocked presets (AC: 5)
  - [ ] In `GameRunner.HandleTradingInput()`: keys 1-4 only trigger if corresponding tier is unlocked
  - [ ] Key 1 = x5 (tier 1), Key 2 = x10 (tier 2), Key 3 = x15 (tier 3), Key 4 = x25 (tier 4)
  - [ ] If tier not unlocked, keypress ignored (no feedback needed)
  - [ ] Note: x1 has no keyboard shortcut — it's the default, always selected on round start
  - [ ] File: `Assets/Scripts/Runtime/Core/GameRunner.cs`

- [ ] Task 7: Update quantity display for x1 default (AC: 1)
  - [ ] When no presets unlocked: show "Qty: 1" text (same style as current quantity display)
  - [ ] No preset buttons visible — clean, minimal look
  - [ ] When first preset unlocked: "Qty: 1" still shown but now a [x5] button appears
  - [ ] File: `Assets/Scripts/Runtime/UI/QuantitySelector.cs`, `Assets/Scripts/Setup/UISetup.cs`

- [ ] Task 8: Ensure short mechanic unaffected (AC: 9)
  - [ ] Verify FIX-11 short path reads `GameConfig.ShortBaseShares` directly, NOT QuantitySelector
  - [ ] Shorts always trade `ShortBaseShares` (1) regardless of unlocked quantity presets
  - [ ] File: verification only — no changes expected

- [ ] Task 9: Write tests (AC: 1-10)
  - [ ] Test: Default quantity is 1 at run start (no unlocks)
  - [ ] Test: UnlockTier(1) makes x5 available
  - [ ] Test: UnlockTier progressive — x5 then x10 then x15 then x25
  - [ ] Test: SelectPreset rejected for locked tiers
  - [ ] Test: ResetToDefault goes to x1 (not x10)
  - [ ] Test: GetCurrentQuantity clamped to highest unlocked
  - [ ] Test: MAX calculation respects unlock cap
  - [ ] Test: Unlocks persist across rounds (not reset on RoundStartedEvent)
  - [ ] Test: Keyboard shortcut ignored for locked tier
  - [ ] Test: Short trade quantity unaffected by QuantitySelector unlocks
  - [ ] File: `Assets/Tests/Runtime/UI/QuantityUnlockTests.cs`

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
_To be filled during implementation_

### Completion Notes
_To be filled after implementation_

### Debug Log
_To be filled during implementation_

## File List

_To be filled during implementation_

## Change Log

- 2026-02-14: Story created — quantity as progression mechanic, x1 start, unlock tiers via Reputation
