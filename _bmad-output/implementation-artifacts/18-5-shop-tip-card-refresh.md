# Story 18.5: Shop Tip Card Refresh

Status: ready-for-dev

## Story

As a player,
I want the shop's insider tip cards to show exciting face-down teasers that spark curiosity, clear revealed text that tells me the tip is marked on my chart, and a small visual type badge so I can tell at a glance whether a tip provides a chart overlay or live data,
so that I make informed purchasing decisions and feel rewarded when a card flips to reveal actionable information.

## Acceptance Criteria

1. `FormatTipTypeName()` returns correct ALL CAPS display names for all 9 post-18.1 tip types; zero references to the 4 removed types (`OpeningPrice`, `VolatilityWarning`, `TrendDirection`, `EventForecast`)
2. `GetTipFaceDownHint()` returns engaging, curiosity-driven hint strings for all 9 types (e.g., "How low can it go?" instead of "Reveals the price floor"); zero references to removed types
3. Each tip card displays a small type badge Text element: `[CHART]` in `ColorPalette.Cyan` for chart overlay tips (PriceForecast, PriceFloor, PriceCeiling, DipMarker, PeakMarker, EventTiming, TrendReversal) and `[LIVE]` in `ColorPalette.Green` for live data tips (EventCount); ClosingDirection displays `[CALL]` in `ColorPalette.Amber`
4. `TipCardView` class extended with `public Text TypeBadgeText` field to hold the badge reference
5. `CreateTipCard()` creates the badge Text element between the name and description, with fontSize 10, bold, centered, preferredHeight 14
6. `InsiderTipGenerator.CalculateRevealedText()` uses updated description templates that reference chart overlays (e.g., "Sweet spot around ~$6.50 -- marked on chart") for all 9 types
7. `InsiderTipDefinitions.All` description templates updated to match the new revealed text format with "marked on chart" phrasing for overlay types
8. `AnimateTipFlip()` sets the type badge color to `ColorPalette.White` on reveal (midpoint swap) so it stays visible against the revealed card background
9. Tests exist for all 9 type names, all 9 face-down hints, badge text/color for each category, no duplicate hints, no duplicate names, and zero references to removed types in shop UI code

## Tasks / Subtasks

- [ ] Task 1: Update `FormatTipTypeName()` for 9 post-18.1 types (AC: 1)
  - [ ] Open `Assets/Scripts/Runtime/UI/ShopUI.cs`, method at lines 911-925
  - [ ] After Story 18.1 lands, the 4 removed type cases will already be deleted and 5 new ones added. Verify the switch matches this exact table:
    ```csharp
    public static string FormatTipTypeName(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceForecast:    return "PRICE FORECAST";
            case InsiderTipType.PriceFloor:       return "PRICE FLOOR";
            case InsiderTipType.PriceCeiling:     return "PRICE CEILING";
            case InsiderTipType.EventCount:       return "EVENT COUNT";
            case InsiderTipType.DipMarker:        return "DIP MARKER";
            case InsiderTipType.PeakMarker:       return "PEAK MARKER";
            case InsiderTipType.ClosingDirection:  return "CLOSING CALL";
            case InsiderTipType.EventTiming:      return "EVENT TIMING";
            case InsiderTipType.TrendReversal:    return "TREND REVERSAL";
            default:                              return "UNKNOWN";
        }
    }
    ```
  - [ ] Confirm zero references to `OpeningPrice`, `VolatilityWarning`, `TrendDirection`, `EventForecast` anywhere in `ShopUI.cs`
  - [ ] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`

- [ ] Task 2: Update `GetTipFaceDownHint()` with engaging teasers (AC: 2)
  - [ ] Open `Assets/Scripts/Runtime/UI/ShopUI.cs`, method at lines 931-945
  - [ ] Replace the bland functional hints with curiosity-driven teasers:
    ```csharp
    public static string GetTipFaceDownHint(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceForecast:    return "What's the sweet spot?";
            case InsiderTipType.PriceFloor:       return "How low can it go?";
            case InsiderTipType.PriceCeiling:     return "What's the top?";
            case InsiderTipType.EventCount:       return "How many surprises?";
            case InsiderTipType.DipMarker:        return "When's the best buy?";
            case InsiderTipType.PeakMarker:       return "When should you sell?";
            case InsiderTipType.ClosingDirection:  return "Up or down?";
            case InsiderTipType.EventTiming:      return "When do shakeups hit?";
            case InsiderTipType.TrendReversal:    return "When does it turn?";
            default:                              return "Unknown intel";
        }
    }
    ```
  - [ ] No references to removed types
  - [ ] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`

- [ ] Task 3: Add `TypeBadgeText` field to `TipCardView` and create badge helper (AC: 3, 4)
  - [ ] Open `Assets/Scripts/Runtime/UI/ShopUI.cs`, class at lines 184-195
  - [ ] Add field to `TipCardView`:
    ```csharp
    public class TipCardView
    {
        public GameObject Root;
        public Text NameText;
        public Text TypeBadgeText;      // NEW — [CHART], [LIVE], or [CALL] badge
        public Text DescriptionText;
        public Text CostText;
        public Button PurchaseButton;
        public Text ButtonText;
        public Image CardBackground;
        public CanvasGroup Group;
        public bool IsRevealed;
    }
    ```
  - [ ] Add a new public static helper method to determine badge text and color:
    ```csharp
    /// <summary>
    /// Returns the type badge label and color for a tip type.
    /// Chart overlay tips get [CHART] in Cyan, live data gets [LIVE] in Green,
    /// directional call gets [CALL] in Amber.
    /// </summary>
    public static (string label, Color color) GetTipTypeBadge(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceForecast:
            case InsiderTipType.PriceFloor:
            case InsiderTipType.PriceCeiling:
            case InsiderTipType.DipMarker:
            case InsiderTipType.PeakMarker:
            case InsiderTipType.EventTiming:
            case InsiderTipType.TrendReversal:
                return ("[CHART]", ColorPalette.Cyan);

            case InsiderTipType.EventCount:
                return ("[LIVE]", ColorPalette.Green);

            case InsiderTipType.ClosingDirection:
                return ("[CALL]", ColorPalette.Amber);

            default:
                return ("", ColorPalette.WhiteDim);
        }
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`

- [ ] Task 4: Update `CreateTipCard()` to build the type badge element (AC: 5)
  - [ ] Open `Assets/Scripts/Runtime/UI/ShopUI.cs`, method at lines 767-857
  - [ ] Insert the badge creation BETWEEN the Name section (ends ~line 803) and the Description section (starts ~line 806). The badge sits below the type name and above the hint text:
    ```csharp
    // Type badge — shows [CHART], [LIVE], or [CALL] indicator
    var badgeGo = new GameObject("TypeBadge");
    badgeGo.transform.SetParent(cardGo.transform, false);
    view.TypeBadgeText = badgeGo.AddComponent<Text>();
    view.TypeBadgeText.font = DefaultFont;
    var (badgeLabel, badgeColor) = GetTipTypeBadge(offering.Definition.Type);
    view.TypeBadgeText.text = badgeLabel;
    view.TypeBadgeText.fontSize = 10;
    view.TypeBadgeText.fontStyle = FontStyle.Bold;
    view.TypeBadgeText.color = badgeColor;
    view.TypeBadgeText.alignment = TextAnchor.MiddleCenter;
    view.TypeBadgeText.raycastTarget = false;
    var badgeLayout = badgeGo.AddComponent<LayoutElement>();
    badgeLayout.preferredHeight = 14f;
    ```
  - [ ] Increase `cardLayout.preferredHeight` from `100f` to `116f` to accommodate the new 14px badge row plus spacing
  - [ ] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`

- [ ] Task 5: Update `InsiderTipDefinitions` description templates and `InsiderTipGenerator.CalculateRevealedText()` (AC: 6, 7)
  - [ ] Open `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs`
  - [ ] After Story 18.1 lands, update the `DescriptionTemplate` strings for the new "marked on chart" format. The templates use `{0}` for dynamic values:
    ```csharp
    public static readonly InsiderTipDef[] All = new InsiderTipDef[]
    {
        new InsiderTipDef(InsiderTipType.PriceForecast,
            "Sweet spot around ~${0} \u2014 marked on chart",
            GameConfig.TipCostPriceForecast),
        new InsiderTipDef(InsiderTipType.PriceFloor,
            "Floor at ~${0} \u2014 marked on chart",
            GameConfig.TipCostPriceFloor),
        new InsiderTipDef(InsiderTipType.PriceCeiling,
            "Ceiling at ~${0} \u2014 marked on chart",
            GameConfig.TipCostPriceCeiling),
        new InsiderTipDef(InsiderTipType.EventCount,
            "Expect ~{0} disruptions \u2014 live countdown active",
            GameConfig.TipCostEventCount),
        new InsiderTipDef(InsiderTipType.DipMarker,
            "Best buy window marked on chart",
            GameConfig.TipCostDipMarker),
        new InsiderTipDef(InsiderTipType.PeakMarker,
            "Peak sell window marked on chart",
            GameConfig.TipCostPeakMarker),
        new InsiderTipDef(InsiderTipType.ClosingDirection,
            "Round closes {0}",
            GameConfig.TipCostClosingDirection),
        new InsiderTipDef(InsiderTipType.EventTiming,
            "Event timing marked on chart",
            GameConfig.TipCostEventTiming),
        new InsiderTipDef(InsiderTipType.TrendReversal,
            "Trend reversal point marked on chart",
            GameConfig.TipCostTrendReversal),
    };
    ```
  - [ ] Open `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs`
  - [ ] Verify `CalculateRevealedText()` switch cases match the template format. After 18.1 + 18.2, the price-based cases use `string.Format(def.DescriptionTemplate, FormatPrice(fuzzed))` which will now produce e.g., `"Sweet spot around ~$6.50 -- marked on chart"`. The non-parameterized templates (DipMarker, PeakMarker, EventTiming, TrendReversal) return `def.DescriptionTemplate` directly. ClosingDirection uses `string.Format(def.DescriptionTemplate, closesHigher ? "HIGHER" : "LOWER")` producing `"Round closes HIGHER"` or `"Round closes LOWER"`.
  - [ ] Files: `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs`, `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs`

- [ ] Task 6: Update `AnimateTipFlip()` to handle badge on reveal (AC: 8)
  - [ ] Open `Assets/Scripts/Runtime/UI/ShopUI.cs`, method at lines 1441-1506
  - [ ] In the midpoint swap block (after `card.IsRevealed = true`, around line 1460), add badge color update:
    ```csharp
    // Swap to revealed content at midpoint
    card.IsRevealed = true;
    card.PurchaseButton.interactable = false;
    card.ButtonText.text = "REVEALED";
    if (_tipOffering != null && cardIndex < _tipOffering.Length)
    {
        var offering = _tipOffering[cardIndex];
        card.NameText.text = FormatTipTypeName(offering.Definition.Type);
        card.DescriptionText.text = offering.RevealedText;
        card.DescriptionText.fontSize = 12;
        card.DescriptionText.fontStyle = FontStyle.Normal;
        card.DescriptionText.color = ColorPalette.White;

        // Update badge to white on revealed card for visibility
        if (card.TypeBadgeText != null)
            card.TypeBadgeText.color = ColorPalette.White;
    }
    card.CardBackground.color = TipCardRevealedColor;
    ```
  - [ ] Note: After Story 18.1 the field may be `offering.DisplayText` instead of `offering.RevealedText` -- follow the rename from 18.1
  - [ ] File: `Assets/Scripts/Runtime/UI/ShopUI.cs`

- [ ] Task 7: Update and add tests (AC: 9)
  - [ ] Open `Assets/Tests/Runtime/Shop/StoreVisualPolishTests.cs`
  - [ ] After Story 18.1 lands, tests for removed types will already be deleted and replaced. Verify/add the following tests for all 9 types:

  **FormatTipTypeName tests (9 type + 1 unknown = 10 tests):**
  ```csharp
  [Test]
  public void FormatTipTypeName_PriceForecast_ReturnsCorrectString()
  {
      Assert.AreEqual("PRICE FORECAST", ShopUI.FormatTipTypeName(InsiderTipType.PriceForecast));
  }

  [Test]
  public void FormatTipTypeName_PriceFloor_ReturnsCorrectString()
  {
      Assert.AreEqual("PRICE FLOOR", ShopUI.FormatTipTypeName(InsiderTipType.PriceFloor));
  }

  [Test]
  public void FormatTipTypeName_PriceCeiling_ReturnsCorrectString()
  {
      Assert.AreEqual("PRICE CEILING", ShopUI.FormatTipTypeName(InsiderTipType.PriceCeiling));
  }

  [Test]
  public void FormatTipTypeName_EventCount_ReturnsCorrectString()
  {
      Assert.AreEqual("EVENT COUNT", ShopUI.FormatTipTypeName(InsiderTipType.EventCount));
  }

  [Test]
  public void FormatTipTypeName_DipMarker_ReturnsCorrectString()
  {
      Assert.AreEqual("DIP MARKER", ShopUI.FormatTipTypeName(InsiderTipType.DipMarker));
  }

  [Test]
  public void FormatTipTypeName_PeakMarker_ReturnsCorrectString()
  {
      Assert.AreEqual("PEAK MARKER", ShopUI.FormatTipTypeName(InsiderTipType.PeakMarker));
  }

  [Test]
  public void FormatTipTypeName_ClosingDirection_ReturnsCorrectString()
  {
      Assert.AreEqual("CLOSING CALL", ShopUI.FormatTipTypeName(InsiderTipType.ClosingDirection));
  }

  [Test]
  public void FormatTipTypeName_EventTiming_ReturnsCorrectString()
  {
      Assert.AreEqual("EVENT TIMING", ShopUI.FormatTipTypeName(InsiderTipType.EventTiming));
  }

  [Test]
  public void FormatTipTypeName_TrendReversal_ReturnsCorrectString()
  {
      Assert.AreEqual("TREND REVERSAL", ShopUI.FormatTipTypeName(InsiderTipType.TrendReversal));
  }

  [Test]
  public void FormatTipTypeName_UnknownValue_ReturnsUnknown()
  {
      Assert.AreEqual("UNKNOWN", ShopUI.FormatTipTypeName((InsiderTipType)999));
  }
  ```

  **GetTipFaceDownHint tests (9 type + 1 unknown = 10 tests):**
  ```csharp
  [Test]
  public void GetTipFaceDownHint_PriceForecast_ReturnsEngagingHint()
  {
      Assert.AreEqual("What's the sweet spot?", ShopUI.GetTipFaceDownHint(InsiderTipType.PriceForecast));
  }

  [Test]
  public void GetTipFaceDownHint_PriceFloor_ReturnsEngagingHint()
  {
      Assert.AreEqual("How low can it go?", ShopUI.GetTipFaceDownHint(InsiderTipType.PriceFloor));
  }

  [Test]
  public void GetTipFaceDownHint_PriceCeiling_ReturnsEngagingHint()
  {
      Assert.AreEqual("What's the top?", ShopUI.GetTipFaceDownHint(InsiderTipType.PriceCeiling));
  }

  [Test]
  public void GetTipFaceDownHint_EventCount_ReturnsEngagingHint()
  {
      Assert.AreEqual("How many surprises?", ShopUI.GetTipFaceDownHint(InsiderTipType.EventCount));
  }

  [Test]
  public void GetTipFaceDownHint_DipMarker_ReturnsEngagingHint()
  {
      Assert.AreEqual("When's the best buy?", ShopUI.GetTipFaceDownHint(InsiderTipType.DipMarker));
  }

  [Test]
  public void GetTipFaceDownHint_PeakMarker_ReturnsEngagingHint()
  {
      Assert.AreEqual("When should you sell?", ShopUI.GetTipFaceDownHint(InsiderTipType.PeakMarker));
  }

  [Test]
  public void GetTipFaceDownHint_ClosingDirection_ReturnsEngagingHint()
  {
      Assert.AreEqual("Up or down?", ShopUI.GetTipFaceDownHint(InsiderTipType.ClosingDirection));
  }

  [Test]
  public void GetTipFaceDownHint_EventTiming_ReturnsEngagingHint()
  {
      Assert.AreEqual("When do shakeups hit?", ShopUI.GetTipFaceDownHint(InsiderTipType.EventTiming));
  }

  [Test]
  public void GetTipFaceDownHint_TrendReversal_ReturnsEngagingHint()
  {
      Assert.AreEqual("When does it turn?", ShopUI.GetTipFaceDownHint(InsiderTipType.TrendReversal));
  }

  [Test]
  public void GetTipFaceDownHint_UnknownValue_ReturnsDefault()
  {
      Assert.AreEqual("Unknown intel", ShopUI.GetTipFaceDownHint((InsiderTipType)999));
  }
  ```

  **GetTipTypeBadge tests (3 categories + default = 4 tests):**
  ```csharp
  [Test]
  public void GetTipTypeBadge_ChartOverlayTypes_ReturnChartInCyan()
  {
      var chartTypes = new[] {
          InsiderTipType.PriceForecast, InsiderTipType.PriceFloor,
          InsiderTipType.PriceCeiling, InsiderTipType.DipMarker,
          InsiderTipType.PeakMarker, InsiderTipType.EventTiming,
          InsiderTipType.TrendReversal
      };
      foreach (var t in chartTypes)
      {
          var (label, color) = ShopUI.GetTipTypeBadge(t);
          Assert.AreEqual("[CHART]", label, $"Expected [CHART] for {t}");
          Assert.AreEqual(ColorPalette.Cyan, color, $"Expected Cyan for {t}");
      }
  }

  [Test]
  public void GetTipTypeBadge_EventCount_ReturnsLiveInGreen()
  {
      var (label, color) = ShopUI.GetTipTypeBadge(InsiderTipType.EventCount);
      Assert.AreEqual("[LIVE]", label);
      Assert.AreEqual(ColorPalette.Green, color);
  }

  [Test]
  public void GetTipTypeBadge_ClosingDirection_ReturnsCallInAmber()
  {
      var (label, color) = ShopUI.GetTipTypeBadge(InsiderTipType.ClosingDirection);
      Assert.AreEqual("[CALL]", label);
      Assert.AreEqual(ColorPalette.Amber, color);
  }

  [Test]
  public void GetTipTypeBadge_UnknownType_ReturnsEmpty()
  {
      var (label, _) = ShopUI.GetTipTypeBadge((InsiderTipType)999);
      Assert.AreEqual("", label);
  }
  ```

  **No-duplicate validation tests (2 tests):**
  ```csharp
  [Test]
  public void FormatTipTypeName_AllNineTypes_NoDuplicateNames()
  {
      var names = new HashSet<string>();
      foreach (InsiderTipType t in System.Enum.GetValues(typeof(InsiderTipType)))
      {
          string name = ShopUI.FormatTipTypeName(t);
          Assert.IsTrue(names.Add(name), $"Duplicate display name: {name}");
      }
      Assert.AreEqual(9, names.Count, "Expected exactly 9 unique type names");
  }

  [Test]
  public void GetTipFaceDownHint_AllNineTypes_NoDuplicateHints()
  {
      var hints = new HashSet<string>();
      foreach (InsiderTipType t in System.Enum.GetValues(typeof(InsiderTipType)))
      {
          string hint = ShopUI.GetTipFaceDownHint(t);
          Assert.IsTrue(hints.Add(hint), $"Duplicate face-down hint: {hint}");
      }
      Assert.AreEqual(9, hints.Count, "Expected exactly 9 unique face-down hints");
  }
  ```

  **TipCardView field test (1 test):**
  ```csharp
  [Test]
  public void TipCardView_HasTypeBadgeTextField()
  {
      var view = new ShopUI.TipCardView();
      Assert.IsNull(view.TypeBadgeText, "TypeBadgeText should be null by default (set during creation)");
  }
  ```

  - [ ] Total new/updated tests: 10 (names) + 10 (hints) + 4 (badges) + 2 (no-dups) + 1 (view field) = 27 tests
  - [ ] File: `Assets/Tests/Runtime/Shop/StoreVisualPolishTests.cs`

## Dev Notes

### Architecture Compliance

- **No ScriptableObjects:** All tip data remains as `public static readonly` in `Scripts/Setup/Data/`. Description templates updated in `InsiderTipDefinitions.cs`, not in Inspector.
- **uGUI only:** The type badge is a standard `UnityEngine.UI.Text` element, added programmatically via `new GameObject() + AddComponent<Text>()`. No UI Toolkit.
- **Programmatic UI:** `CreateTipCard()` builds the badge in code, maintaining the pattern of zero Inspector configuration.
- **EventBus pattern preserved:** No new events. The flip animation reads from existing `_tipOffering` array -- no direct system references.
- **No .meta files:** Only modifying existing `.cs` files. No new files created.
- **Assembly boundary:** `GetTipTypeBadge()` lives in `ShopUI.cs` (Runtime assembly). It references `ColorPalette` from `Scripts/Setup/Data/` which is fine -- Setup references Runtime, and UI reads from data (one-way dependency, same as `FormatTipTypeName` using the Runtime enum).

### Complete Display Name Table

| InsiderTipType    | FormatTipTypeName() | Status |
|-------------------|---------------------|--------|
| PriceForecast     | PRICE FORECAST      | Kept (text unchanged) |
| PriceFloor        | PRICE FLOOR         | Kept (text unchanged) |
| PriceCeiling      | PRICE CEILING       | Kept (text unchanged) |
| EventCount        | EVENT COUNT         | Kept (text unchanged) |
| DipMarker         | DIP MARKER          | New in 18.1 |
| PeakMarker        | PEAK MARKER         | New in 18.1 |
| ClosingDirection  | CLOSING CALL        | New in 18.1 (note: display name differs from enum name) |
| EventTiming       | EVENT TIMING        | New in 18.1 |
| TrendReversal     | TREND REVERSAL      | New in 18.1 |
| ~~TrendDirection~~    | ~~TREND DIRECTION~~     | REMOVED in 18.1 |
| ~~EventForecast~~     | ~~EVENT FORECAST~~      | REMOVED in 18.1 |
| ~~VolatilityWarning~~ | ~~VOLATILITY WARNING~~  | REMOVED in 18.1 |
| ~~OpeningPrice~~      | ~~OPENING PRICE~~       | REMOVED in 18.1 |

### Complete Face-Down Hint Table

| InsiderTipType    | GetTipFaceDownHint()       | Old Hint (replaced)              |
|-------------------|---------------------------|----------------------------------|
| PriceForecast     | "What's the sweet spot?"  | "Predicts the average price"     |
| PriceFloor        | "How low can it go?"      | "Reveals the price floor"        |
| PriceCeiling      | "What's the top?"         | "Reveals the price ceiling"      |
| EventCount        | "How many surprises?"     | "Reveals how many events"        |
| DipMarker         | "When's the best buy?"    | N/A (new type)                   |
| PeakMarker        | "When should you sell?"   | N/A (new type)                   |
| ClosingDirection  | "Up or down?"             | N/A (new type)                   |
| EventTiming       | "When do shakeups hit?"   | N/A (new type)                   |
| TrendReversal     | "When does it turn?"      | N/A (new type)                   |

### Complete Badge Table

| InsiderTipType    | Badge   | Color              | Rationale |
|-------------------|---------|--------------------|-----------|
| PriceForecast     | [CHART] | ColorPalette.Cyan  | Shows price band overlay on chart |
| PriceFloor        | [CHART] | ColorPalette.Cyan  | Shows horizontal line on chart |
| PriceCeiling      | [CHART] | ColorPalette.Cyan  | Shows horizontal line on chart |
| DipMarker         | [CHART] | ColorPalette.Cyan  | Shows time zone on chart |
| PeakMarker        | [CHART] | ColorPalette.Cyan  | Shows time zone on chart |
| EventTiming       | [CHART] | ColorPalette.Cyan  | Shows vertical time markers on chart |
| TrendReversal     | [CHART] | ColorPalette.Cyan  | Shows reversal point marker on chart |
| EventCount        | [LIVE]  | ColorPalette.Green | Live countdown, not a chart overlay |
| ClosingDirection  | [CALL]  | ColorPalette.Amber | Binary directional call, not a chart overlay |

### Revealed Text Template Examples

After `CalculateRevealedText()` processes the templates:

| Type | Template in InsiderTipDefinitions | Example Revealed Text |
|------|-----------------------------------|-----------------------|
| PriceForecast | `"Sweet spot around ~${0} \u2014 marked on chart"` | "Sweet spot around ~$6.50 -- marked on chart" |
| PriceFloor | `"Floor at ~${0} \u2014 marked on chart"` | "Floor at ~$3.20 -- marked on chart" |
| PriceCeiling | `"Ceiling at ~${0} \u2014 marked on chart"` | "Ceiling at ~$9.80 -- marked on chart" |
| EventCount | `"Expect ~{0} disruptions \u2014 live countdown active"` | "Expect ~7 disruptions -- live countdown active" |
| DipMarker | `"Best buy window marked on chart"` | "Best buy window marked on chart" |
| PeakMarker | `"Peak sell window marked on chart"` | "Peak sell window marked on chart" |
| ClosingDirection | `"Round closes {0}"` | "Round closes HIGHER" / "Round closes LOWER" |
| EventTiming | `"Event timing marked on chart"` | "Event timing marked on chart" |
| TrendReversal | `"Trend reversal point marked on chart"` | "Trend reversal point marked on chart" |

### Card Layout (Top to Bottom)

After this story, each tip card in the VerticalLayoutGroup renders:
1. **Name** -- "PRICE FORECAST" (13px bold, white, 18px height)
2. **Type Badge** -- "[CHART]" (10px bold, cyan/green/amber, 14px height) **NEW**
3. **Description/Hint** -- "What's the sweet spot?" (11px normal, dim white, 18px height)
4. **Cost** -- star + "15" (13px, reputation gold, 18px height)
5. **Feedback** -- hidden until purchase (12px bold, white, 16px height)

Total card height increased from 100px to 116px to accommodate badge row.

### Existing Code to Read Before Implementing

Read these files COMPLETELY before making any changes:

1. `Assets/Scripts/Runtime/UI/ShopUI.cs` -- TipCardView class (lines 184-195), FormatTipTypeName (lines 911-925), GetTipFaceDownHint (lines 931-945), CreateTipCard (lines 767-857), AnimateTipFlip (lines 1441-1506)
2. `Assets/Scripts/Setup/Data/InsiderTipDefinitions.cs` -- All 8 current definitions with DescriptionTemplate strings (lines 25-51)
3. `Assets/Scripts/Runtime/Shop/InsiderTipGenerator.cs` -- CalculateRevealedText switch (lines 59-128), uses string.Format with DescriptionTemplate
4. `Assets/Scripts/Setup/Data/ColorPalette.cs` -- Cyan, Green, Amber, White, WhiteDim color constants
5. `Assets/Tests/Runtime/Shop/StoreVisualPolishTests.cs` -- Current FormatTipTypeName tests (lines 200-252), TipCardView field test (line 191)
6. `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` -- InsiderTipType enum (lines 40-50)

### Depends On

- **Story 18.1** (Tip Data Model & Type Overhaul) -- MUST be complete before this story. 18.1 adds the 5 new enum values, removes 4 old ones, updates GameConfig costs, and stubs the ShopUI switch cases. This story refines the display strings, adds the badge, and updates description templates.
- **Story 18.2** (Tip Generation & Round-Start Activation) -- Provides the actual generation logic for new types. This story updates the templates that 18.2's generation logic formats. Can be implemented in parallel with 18.2 if needed (templates are compatible with 18.2's string.Format calls).

### References

- [Source: _bmad-output/implementation-artifacts/18-1-tip-data-model-type-overhaul.md] -- Task 8 (ShopUI display strings), Task 5 (InsiderTipDefinitions)
- [Source: _bmad-output/implementation-artifacts/18-2-tip-generation-round-start-activation.md] -- Task 1 (CalculateRevealedText for new types)
- [Source: _bmad-output/project-context.md#UI Framework Rules] -- uGUI only, programmatic construction
- [Source: _bmad-output/project-context.md#Serialization & Data] -- No ScriptableObjects, static readonly data

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A -- UI text and visual changes, no complex runtime debugging expected. Verify badge colors visually in Play Mode.

### Completion Notes List

### File List

### Change Log

- 2026-02-21: Story 18.5 created -- shop tip card refresh with engaging teasers, type badges, updated revealed text templates, and 27 tests
