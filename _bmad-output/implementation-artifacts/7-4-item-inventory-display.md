# Story 7.4: Item Inventory Display

Status: ready-for-dev

## Story

As a player,
I want to see my collected items during trading rounds,
so that I know what tools and perks are active.

## Acceptance Criteria

1. Bottom bar shows active Trading Tools with hotkey labels (Q/E/R)
2. Passive Perks visible in a compact list (name + rarity indicator)
3. Intel items show as informational badges with category indicator
4. Item display reflects the ordered list from RunContext.ActiveItems (Balatro-style ordering)
5. Inventory display updates when items are purchased (reactive to ShopItemPurchasedEvent or refreshed on TradingState entry)
6. Empty slots shown when no items of a given category are owned
7. Inventory panel created programmatically by UISetup during F5 (Setup-Oriented Generation)

## Tasks / Subtasks

- [ ] Task 1: Create ItemInventoryPanel MonoBehaviour (AC: 1, 2, 3, 4, 6)
  - [ ] Create `Scripts/Runtime/UI/ItemInventoryPanel.cs` (new)
  - [ ] Define Initialize method accepting RunContext and UI element references (same pattern as TradingHUD)
  - [ ] Three sections: Tool slots (left, up to 3), Intel badges (center), Perk list (right)
  - [ ] Tool slots display: item name, rarity-colored border, hotkey label (Q/E/R) — max 3 slots
  - [ ] Intel badges display: item name, rarity indicator, info icon — compact horizontal row
  - [ ] Perk list display: item name, rarity dot/indicator — compact vertical list
  - [ ] Empty state: show dimmed placeholder slots (e.g., "[Q] ---") when no tool in that slot
  - [ ] Implement Refresh() method that reads RunContext.ActiveItems, cross-references ShopItemDefinitions to get category/rarity/name, and populates UI
  - [ ] Store references to Text and Image components for each slot — avoid GetComponent calls during refresh
  - [ ] Items ordered by their position in ActiveItems list (first Tool found = Q slot, second = E, third = R)

- [ ] Task 2: Create ItemInventorySetup in UISetup (AC: 7)
  - [ ] Add `ExecuteItemInventoryPanel()` static method to `Scripts/Setup/UISetup.cs`
  - [ ] Create a new Canvas (sortingOrder 22, between HUD at 20 and timer at 25)
  - [ ] Bottom bar layout: anchored to bottom of screen, full width, ~50px height
  - [ ] Background panel with dark semi-transparent color matching existing HUD aesthetic (BarBackgroundColor)
  - [ ] HorizontalLayoutGroup with three sections: Tools | Intel | Perks
  - [ ] Tool section: 3 slots, each with hotkey label (Text) + item name (Text) + rarity border (Image)
  - [ ] Intel section: flexible width, horizontal flow of badge GameObjects
  - [ ] Perk section: flexible width, compact vertical list of name Text elements
  - [ ] Instantiate ItemInventoryPanel MonoBehaviour on the parent GameObject
  - [ ] Call Initialize() with all created UI element references
  - [ ] Follow existing UISetup patterns: CreatePanel, CreateLabel, CreateHUDSection helpers
  - [ ] Add debug log: `[Setup] ItemInventoryPanel created: bottom bar with tool/intel/perk sections`

- [ ] Task 3: Integrate ItemInventoryPanel into TradingState flow (AC: 5)
  - [ ] In TradingState.Enter(), locate ItemInventoryPanel (FindFirstObjectByType or static reference) and call Refresh()
  - [ ] Subscribe ItemInventoryPanel to ShopItemPurchasedEvent to handle mid-flow purchases (future-proofing, though shop and trading don't overlap currently)
  - [ ] Subscribe to RoundStartedEvent inside ItemInventoryPanel to auto-refresh when a new round begins
  - [ ] Show the inventory panel when TradingState is active, hide when not (set panel GameObject active/inactive)
  - [ ] Unsubscribe from events in OnDestroy (follow TradingHUD pattern)

- [ ] Task 4: Create ItemLookup utility for resolving item IDs to definitions (AC: 1, 2, 3, 4)
  - [ ] Create `Scripts/Runtime/Items/ItemLookup.cs` (new)
  - [ ] Static method: `ShopItemDef? GetItemById(string itemId)` — searches ShopItemDefinitions for matching Id
  - [ ] Static method: `List<ShopItemDef> GetItemsByCategory(List<string> itemIds, ItemCategory category)` — filters and returns ordered items of a category
  - [ ] Static method: `Color GetRarityColor(ItemRarity rarity)` — returns color for rarity indicator (Common=gray, Uncommon=green, Rare=blue, Legendary=gold)
  - [ ] Cache the lookup dictionary on first access for O(1) lookups (static Dictionary<string, ShopItemDef>)
  - [ ] This utility serves both ItemInventoryPanel and future Item systems (Epic 8)

- [ ] Task 5: Wire up panel visibility in game state flow (AC: 5, 7)
  - [ ] ItemInventoryPanel should be visible during TradingState (trading rounds)
  - [ ] Hidden during MarketOpenState, MarketCloseState, ShopState, RunSummaryState
  - [ ] Panel starts hidden (gameObject.SetActive(false) in Initialize)
  - [ ] Add Show() / Hide() methods on ItemInventoryPanel
  - [ ] TradingState.Enter() calls Show() + Refresh(); TradingState.Exit() calls Hide()
  - [ ] Alternative: subscribe to RoundStartedEvent for Show, TradingPhaseEndedEvent for Hide (EventBus-only, no direct state references)
  - [ ] Prefer the EventBus approach to maintain architecture compliance (no direct system references)

- [ ] Task 6: Define hotkey label constants and display formatting (AC: 1)
  - [ ] Define tool slot hotkey labels as constants: `{"Q", "E", "R"}` (matching GDD Section 6.3 input mapping)
  - [ ] Format tool slot display: `[Q] Item Name` or `[Q] ---` when empty
  - [ ] Hotkey text uses a distinct color (e.g., WarningYellow from TradingHUD) to stand out
  - [ ] Item name text uses ValueColor (white) with rarity-colored left border/accent
  - [ ] Intel badge format: item name with small info icon indicator
  - [ ] Perk list format: rarity dot + item name, compact single-line per perk
  - [ ] These are DISPLAY ONLY — actual hotkey input handling is Epic 8

- [ ] Task 7: Write tests (All AC)
  - [ ] `Tests/Runtime/UI/ItemInventoryPanelTests.cs` (new)
  - [ ] Test: empty ActiveItems list shows all empty/placeholder slots
  - [ ] Test: single Trading Tool appears in Q slot with correct name and hotkey
  - [ ] Test: multiple tools fill Q, E, R slots in order
  - [ ] Test: max 3 tools displayed even if more exist in ActiveItems
  - [ ] Test: Passive Perks listed with correct names and rarity indicators
  - [ ] Test: Intel items shown as badges with correct names
  - [ ] Test: mixed item types sort into correct sections (tools vs intel vs perks)
  - [ ] Test: Refresh() updates display when ActiveItems changes
  - [ ] `Tests/Runtime/Items/ItemLookupTests.cs` (new)
  - [ ] Test: GetItemById returns correct definition for valid ID
  - [ ] Test: GetItemById returns null for unknown ID
  - [ ] Test: GetItemsByCategory filters correctly
  - [ ] Test: GetRarityColor returns expected colors for each rarity tier

## Dev Notes

### Architecture Compliance

- **Setup-Oriented Generation:** ItemInventoryPanel created programmatically via `UISetup.ExecuteItemInventoryPanel()` during F5. No Inspector configuration, no prefabs.
- **uGUI Canvas:** All inventory UI built with uGUI (Text, Image, HorizontalLayoutGroup, VerticalLayoutGroup) — NOT UI Toolkit.
- **EventBus:** Panel subscribes to RoundStartedEvent and TradingPhaseEndedEvent for show/hide. No direct references to GameStateMachine or other states.
- **Static Data:** Item definitions read from `Scripts/Setup/Data/ShopItemDefinitions.cs` via ItemLookup utility. No ScriptableObjects.
- **No ScriptableObjects:** All item data accessed via `public static readonly` fields in data classes.
- **Single Scene:** Panel created once in the scene, shown/hidden as needed. Not destroyed between rounds.
- **RunContext as Source of Truth:** `RunContext.ActiveItems` (List<string>) is the authoritative ordered list of collected items. The panel reads from this list during Refresh().

### Display-Only Scope

This story is strictly about DISPLAYING items in the trading HUD. Critical scope boundaries:

- Trading Tool hotkeys (Q/E/R) are shown as labels but do NOT trigger any actions (Epic 8)
- Intel items show their name and category but do NOT reveal actual intel data (Epic 8)
- Passive Perks are listed but their effects are NOT applied (Epic 8)
- Item reordering (drag-to-rearrange) is NOT in scope — items display in RunContext.ActiveItems order
- No tooltip or detailed item description popup in this story

### Bottom Bar Layout

The bottom bar sits at the very bottom of the screen, anchored full-width. Layout:

```
+-------------------------------------------------------+
| [Q] Insider Tip  |  [E] ---  |  [R] ---  |  Bull Run Intel  |  Diamond Hands  |
|  TOOLS                       |  INTEL              |  PERKS         |
+-------------------------------------------------------+
```

- Left section (~40% width): Tool slots with hotkey labels, horizontal layout
- Center section (~30% width): Intel badges, horizontal flow
- Right section (~30% width): Perk names, compact vertical list

### Item Category Resolution

RunContext.ActiveItems stores item IDs as strings. To display items by category, the panel must:

1. Iterate RunContext.ActiveItems in order
2. Look up each ID in ShopItemDefinitions via ItemLookup
3. Partition into Tools, Intel, and Perks based on ItemCategory
4. Tools are assigned to Q/E/R slots based on order encountered (first Tool = Q, etc.)
5. Maintain the player's ordered list — this ordering matters for Balatro-style left-to-right processing in Epic 8

### Rarity Color Reference

From Story 7.1 shop UI conventions:
- Common: gray `(0.6f, 0.6f, 0.6f)` — `#999999`
- Uncommon: green `(0.2f, 0.8f, 0.2f)` — `#33CC33`
- Rare: blue `(0.3f, 0.5f, 1f)` — `#4D80FF`
- Legendary: gold `(1f, 0.85f, 0f)` — `#FFD900`

### Existing Patterns to Follow

The ItemInventoryPanel follows the exact same patterns as TradingHUD:
- MonoBehaviour with `Initialize()` method receiving RunContext + UI element references
- Private fields for all UI Text/Image components (no public serialized fields)
- `_dirty` flag pattern for efficient refresh (avoid per-frame updates)
- EventBus Subscribe in Initialize, Unsubscribe in OnDestroy
- Static utility methods for testability (e.g., formatting, color lookups)

### Canvas Sorting Order Reference

Existing sorting orders in UISetup:
- StockSidebar: 15
- PositionPanel: 15
- TradingHUD (top bar): 20
- **ItemInventoryPanel (bottom bar): 22** (new — between HUD and timer)
- RoundTimerUI: 25
- MarketOpenUI: 100
- RoundResultsUI: 105
- TierTransitionUI: 108
- RunSummaryUI: 110

### Game Flow Context

The inventory panel appears during:
```
MarketOpen → [TradingState] → MarketClose → MarginCall → Shop → MarketOpen → ...
                  ^
          Panel visible here
```

The panel refreshes on TradingState entry. If items were purchased in the preceding ShopState, the new items will appear in the bottom bar when trading begins.

### ShopItemDefinitions Dependency

This story assumes ShopItemDefinitions exists (created in 7.1) with the `ShopItemDef` struct containing at minimum:
```csharp
public struct ShopItemDef
{
    public string Id;
    public string Name;
    public string Description;
    public int Cost;
    public ItemRarity Rarity;
    public ItemCategory Category; // TradingTool, MarketIntel, PassivePerk
}
```

If ShopItemDefinitions does not yet exist in the codebase (it may still be in the modified-but-uncommitted changes from 7.1), the dev agent must verify its presence before implementing ItemLookup.

### Existing Code to Read Before Implementing

Before implementing, the dev agent MUST read:
- `Scripts/Runtime/UI/TradingHUD.cs` — primary pattern reference for HUD MonoBehaviour
- `Scripts/Setup/UISetup.cs` — how UI panels are generated; use CreatePanel/CreateLabel helpers
- `Scripts/Runtime/Core/RunContext.cs` — ActiveItems list (List<string>), ordered
- `Scripts/Runtime/Core/GameEvents.cs` — RoundStartedEvent, TradingPhaseEndedEvent for show/hide
- `Scripts/Runtime/Core/GameStates/TradingState.cs` — where panel visibility would integrate
- `Scripts/Runtime/Core/GameStates/ShopState.cs` — understand shop → trading flow
- `Scripts/Runtime/Core/EventBus.cs` — Subscribe/Unsubscribe/Publish patterns

### Project Structure Notes

- New file: `Scripts/Runtime/UI/ItemInventoryPanel.cs` — MonoBehaviour for inventory display
- New file: `Scripts/Runtime/Items/ItemLookup.cs` — static utility for resolving item IDs to definitions
- Modifies: `Scripts/Setup/UISetup.cs` — add `ExecuteItemInventoryPanel()` method
- New folder: `Scripts/Runtime/Items/` (if it does not already exist — also used by Epic 8)
- Tests: `Tests/Runtime/UI/ItemInventoryPanelTests.cs` (new)
- Tests: `Tests/Runtime/Items/ItemLookupTests.cs` (new)
- Does NOT modify: TradingState.cs (prefer EventBus subscription over direct state modification)
- Does NOT modify: RunContext.cs (ActiveItems already exists)
- Does NOT modify: GameEvents.cs (RoundStartedEvent and TradingPhaseEndedEvent already exist)

### References

- [Source: epics.md#Epic 7, Story 7.4] — "Item Inventory Display" acceptance criteria
- [Source: bull-run-gdd-mvp.md#6.1] — Trading Phase UI: "Bottom Bar: Active Trading Tools"
- [Source: bull-run-gdd-mvp.md#6.3] — Input Mapping: Q/E/R for tool slots
- [Source: bull-run-gdd-mvp.md#4] — Item categories: Trading Tools, Market Intel, Passive Perks
- [Source: game-architecture.md#Item System] — IUpgrade interface, ItemManager, left-to-right dispatch
- [Source: game-architecture.md#UI Architecture] — UI panels as MonoBehaviours, UISetup generation
- [Source: 7-1-shop-ui.md] — ShopItemDef struct, ItemRarity/ItemCategory enums, rarity colors
- [Source: 7-3-purchase-flow.md] — Items added to RunContext.ActiveItems on purchase

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
