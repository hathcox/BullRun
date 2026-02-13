# Story 7.1: Shop UI

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a player,
I want a draft shop screen after each successful round presenting one item per category,
so that I can upgrade my build.

## Acceptance Criteria

1. Three item slots displayed: Trading Tool, Market Intel, Passive Perk
2. Each item shows: name, description, cost, rarity indicator
3. Clear purchase button per item
4. Display current cash available prominently
5. Shop screen appears after successful round (margin call passed) and before next round starts
6. Player can see which items they can/cannot afford (visual distinction)

## Tasks / Subtasks

- [ ] Task 1: Create ShopState game state (AC: 5)
  - [ ] Implement `IGameState` with Enter/Update/Exit
  - [ ] Enter: generate shop items, show ShopUI overlay
  - [ ] Exit: hide ShopUI overlay, transition to MarketOpenState for next round
  - [ ] Wire into GameStateMachine — ShopState comes after MarketCloseState (when margin call passes and not final round)
  - [ ] File: `Scripts/Runtime/Core/GameStates/ShopState.cs` (new)
- [ ] Task 2: Create ShopUI panel (AC: 1, 2, 3, 4, 6)
  - [ ] Three item card slots arranged horizontally, one per category
  - [ ] Each card displays: category label, item name, description text, cost, rarity badge (Common/Uncommon/Rare/Legendary with color coding)
  - [ ] Purchase button per card — enabled when player can afford, disabled/grayed when insufficient cash
  - [ ] Cash display at top showing current available cash (updates after each purchase)
  - [ ] Rarity color coding: Common=white/gray, Uncommon=green, Rare=blue, Legendary=gold
  - [ ] File: `Scripts/Runtime/UI/ShopUI.cs` (new)
- [ ] Task 3: Create ShopGenerator for item selection (AC: 1, 2)
  - [ ] Select one item per category from available item pool
  - [ ] Rarity-weighted random selection (Common most frequent, Legendary rarest)
  - [ ] Use ShopItemDefinitions data for item pool
  - [ ] Return three items (one Trading Tool, one Market Intel, one Passive Perk)
  - [ ] File: `Scripts/Runtime/Shop/ShopGenerator.cs` (new)
- [ ] Task 4: Define ShopItemDefinitions data (AC: 2)
  - [ ] Static data class with all shop items from GDD Section 4
  - [ ] Each item definition: Id, Name, Description, Cost, Rarity, Category
  - [ ] Include all 10 Trading Tools, 10 Market Intel, 10 Passive Perks from GDD
  - [ ] Rarity weights for selection probability
  - [ ] File: `Scripts/Setup/Data/ShopItemDefinitions.cs` (new)
- [ ] Task 5: Define shop-related events (AC: 5)
  - [ ] `ShopOpenedEvent`: round number, available items
  - [ ] `ShopItemPurchasedEvent`: item id, cost, remaining cash
  - [ ] `ShopClosedEvent`: items purchased count, cash remaining
  - [ ] Add to `Scripts/Runtime/Core/GameEvents.cs`
- [ ] Task 6: Wire ShopState into existing game flow (AC: 5)
  - [ ] Update MarginCallState: when margin call passes AND not final round → transition to ShopState (instead of directly to MarketOpenState)
  - [ ] When final round (Round 8) passes → continue to RunSummaryState (victory path, already implemented in 6.5)
  - [ ] ShopState exit → transition to MarketOpenState for next round
  - [ ] File: `Scripts/Runtime/Core/GameStates/MarginCallState.cs` (modify)
- [ ] Task 7: Create ShopSetup for F5 generation (AC: 1)
  - [ ] Generate ShopUI overlay in the Canvas hierarchy
  - [ ] Three card panels with text, button, and rarity badge components
  - [ ] Cash display text at top
  - [ ] Register in SceneComposition phase
  - [ ] File: `Scripts/Setup/ShopSetup.cs` (new) OR extend `Scripts/Setup/UISetup.cs`
- [ ] Task 8: Write tests (All AC)
  - [ ] ShopGenerator: verify one item per category, rarity weighting, no duplicates
  - [ ] ShopState: verify Enter shows UI, Exit hides UI, transitions correctly
  - [ ] ShopItemDefinitions: verify all 30 items defined, valid costs, valid rarities
  - [ ] Purchase flow: verify cash deduction, insufficient cash rejection
  - [ ] Files: `Tests/Runtime/Shop/ShopGeneratorTests.cs`, `Tests/Runtime/Core/GameStates/ShopStateTests.cs`

## Dev Notes

### Architecture Compliance

- **Setup-Oriented Generation:** ShopUI created programmatically via UISetup/ShopSetup during F5. No Inspector configuration.
- **uGUI Canvas:** All shop UI built with uGUI — NOT UI Toolkit. Canvas hierarchy created in code.
- **EventBus:** Shop events published for future systems (audio cues in Epic 11, item activation in Epic 8)
- **Static Data:** All item definitions in `Scripts/Setup/Data/ShopItemDefinitions.cs` as `public static readonly` — no ScriptableObjects
- **GameStateMachine:** ShopState implements `IGameState` with Enter/Update/Exit pattern
- **No direct system references:** ShopState communicates via EventBus and RunContext only

### Game Flow with Shop

The current flow is: `MarketOpen → Trading → MarketClose → MarginCallCheck → (fail: RunSummary) / (pass + final round: RunSummary victory) / (pass + not final: ShopState → MarketOpen)`

This story inserts ShopState between MarginCallState success and next round's MarketOpenState. The shop is where the player spends accumulated cash on upgrades, creating the core capital tension (spend on items vs. keep cash for trading).

### Shop Timer

The GDD specifies 15-20 seconds for shop time. For this story, implement a basic timer display. The timer should count down and auto-close the shop when it expires (transitioning to next round).

### Item Effects NOT in Scope

This story creates the shop UI and purchase flow. Actual item EFFECTS (what items do during trading) are Epic 8 stories. For now, items are purchased and tracked in RunContext.ActiveItems but have no gameplay effect yet.

### Items Data Structure

Items should be defined with enough data for the shop to display them:

```csharp
public struct ShopItemDef
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;
    public ItemRarity Rarity;
    public ItemCategory Category;
}

public enum ItemRarity { Common, Uncommon, Rare, Legendary }
public enum ItemCategory { TradingTool, MarketIntel, PassivePerk }
```

### Rarity Selection Weights

From GDD design principles — rarity controls frequency:
- Common: ~50% chance
- Uncommon: ~30% chance
- Rare: ~15% chance
- Legendary: ~5% chance

### Capital Tension (Critical Design)

From GDD Section 4: "Every dollar spent on upgrades is a dollar not available for trading in the next round." The shop UI must make this tension visible — show current cash prominently, and make it clear that buying items reduces trading capital.

### Previous Story Intelligence (6.5 Win State)

Key learnings from the most recent completed story:
- RunContext is the single source of truth for run data — extended with new properties as needed
- GameEvents.cs holds all event definitions — add new events there
- MarginCallState handles the pass/fail logic — this is where shop transition hooks in
- Victory path already established: MarginCallState → (ShopState if not final) → RunSummaryState (if final + win)
- Count-up animations used for victory stats — similar polish could apply to shop item costs
- Code review emphasized computed properties over manual tracking (e.g., ItemsCollected as computed property)

### Git Intelligence

Recent commits show:
- Pattern of extending existing state machine flow (MarginCallState, RunSummaryState)
- RunContext is frequently extended with new properties
- GameEvents.cs grows with new event structs
- All work follows the Setup-Oriented Generation Framework

### Existing Code to Understand

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/Core/GameStateMachine.cs` — understand state transition API
- `Scripts/Runtime/Core/GameStates/MarginCallState.cs` — where shop transition hooks in
- `Scripts/Runtime/Core/GameStates/MarketCloseState.cs` — flow before shop
- `Scripts/Runtime/Core/RunContext.cs` — where purchased items are tracked
- `Scripts/Runtime/Core/GameEvents.cs` — event definition patterns
- `Scripts/Setup/UISetup.cs` — how UI panels are generated in F5
- `Scripts/Setup/Data/GameConfig.cs` — where shop timer duration should be defined

### Project Structure Notes

- New folder: `Scripts/Runtime/Shop/` for ShopGenerator (and later ShopTransaction in 7.3)
- New file: `Scripts/Runtime/Core/GameStates/ShopState.cs`
- New file: `Scripts/Runtime/UI/ShopUI.cs`
- New file: `Scripts/Setup/Data/ShopItemDefinitions.cs`
- Modifies: `Scripts/Runtime/Core/GameEvents.cs` (add shop events)
- Modifies: `Scripts/Runtime/Core/GameStates/MarginCallState.cs` (add shop transition)
- Modifies: `Scripts/Setup/UISetup.cs` or new `Scripts/Setup/ShopSetup.cs` (shop UI generation)
- Tests: `Tests/Runtime/Shop/`, `Tests/Runtime/Core/GameStates/ShopStateTests.cs`

### References

- [Source: bull-run-gdd-mvp.md#4] — "Draft Shop System" — full shop design including categories, items, costs
- [Source: bull-run-gdd-mvp.md#4.1] — Trading Tools: 10 items with costs and rarities
- [Source: bull-run-gdd-mvp.md#4.2] — Market Intel: 10 items with costs and rarities
- [Source: bull-run-gdd-mvp.md#4.3] — Passive Perks: 10 items with costs and rarities
- [Source: bull-run-gdd-mvp.md#2.2] — "Phase 3: Market Close & Draft Shop (15-20 seconds)"
- [Source: epics.md#Epic 7] — Story 7.1 acceptance criteria
- [Source: game-architecture.md#Shop System] — ShopGenerator, ShopTransaction location
- [Source: game-architecture.md#Data Architecture] — ShopItemDefinitions.cs pattern
- [Source: game-architecture.md#Game State Machine] — State flow: MetaHub → MarketOpen → Trading → MarketClose → Shop → loop
- [Source: project-context.md#Lifecycle] — ShopState in the state machine flow

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
