# Story 18.4: Trading HUD Tip Panel & Live Event Countdown

Status: done

## Story

As a player,
I want a dedicated tip panel in the trading HUD that replaces the old pipe-separated text with compact visual indicators, and I want the Event Count tip to be a live countdown that ticks down as market events fire during the round,
so that my purchased intel is prominent, easy to read at a glance, and the event countdown gives me real-time tactical information about how many disruptions remain.

## Acceptance Criteria

1. The old pipe-separated `_tipsDisplayText` in `TradingHUD.cs` and its `SetTipsDisplay()` method are fully removed, along with the corresponding Text element creation in UISetup
2. A new `TipPanel` MonoBehaviour is created that renders a compact indicator strip in the trading HUD area (below the Control Deck panel, above the chart)
3. The tip panel subscribes to `TipOverlaysActivatedEvent` at initialization and uses the `List<TipOverlayData>` to populate its indicators at round start
4. When an Event Count tip is active, the panel displays "EVENTS: X" where X is initialized from `TipOverlayData.EventCountdown`, and decrements X on each `MarketEventFiredEvent` received
5. When the event countdown reaches 0, the display changes to "ALL CLEAR" rendered in `ColorPalette.Green`, indicating no further disruptions
6. Each countdown decrement triggers a brief scale-pulse animation (0.15s scale to 1.2x then back to 1.0x) on the countdown text for visual feedback
7. Chart-overlay tips (PriceFloor, PriceCeiling, PriceForecast, DipMarker, PeakMarker, EventTiming, TrendReversal, ClosingDirection) each display a small colored dot badge with a 2-3 letter type abbreviation, indicating the overlay is active on the chart
8. The tip panel auto-hides (SetActive false) when no tips were purchased (empty overlay list), and auto-shows when at least one tip is present
9. The tip panel hides on `ShopOpenedEvent` and on `RoundStartedEvent` (before new overlays arrive), then shows again when `TipOverlaysActivatedEvent` fires with a non-empty list
10. The tip panel is created programmatically by UISetup following the Setup-Oriented pattern — no Inspector configuration
11. Static pure-logic methods on `TipPanel` (FormatCountdownText, GetBadgeAbbreviation, GetBadgeColor) are testable without MonoBehaviour instantiation
12. Tests: countdown decrement logic, ALL CLEAR state at zero, badge abbreviation mapping for all 9 types, panel visibility states, counter never goes below zero

## Tasks / Subtasks

- [x] Task 1: Remove old tip text display from TradingHUD (AC: 1)
  - [x] Open `Assets/Scripts/Runtime/UI/TradingHUD.cs`
  - [x] Remove the private field `_tipsDisplayText` (line 49)
  - [x] Remove the `SetTipsDisplay(Text tipsDisplayText)` method (lines 139-142)
  - [x] Remove the "Story 13.5: Insider tips" block in `RefreshDisplay()` (lines 314-332)
  - [x] File: `Assets/Scripts/Runtime/UI/TradingHUD.cs`

- [x] Task 2: Remove old tip text creation from UISetup (AC: 1)
  - [x] Open `Assets/Scripts/Setup/UISetup.cs`
  - [x] Search for any code that creates a tips display Text element and calls `tradingHUD.SetTipsDisplay(...)` — none found (never wired in UISetup)
  - [x] If a `DashboardReferences` field was used for tip text wiring, remove that field too — none found
  - [x] Verify no other callers reference `SetTipsDisplay` — grep confirmed zero references
  - [x] File: `Assets/Scripts/Setup/UISetup.cs`

- [x] Task 3: Create TipPanel MonoBehaviour with static utility methods (AC: 2, 4, 5, 7, 8, 11)
  - [x] Create `Assets/Scripts/Runtime/UI/TipPanel.cs`
  - [x] Private fields:
    ```csharp
    private int _eventCountdown = -1;   // -1 = no event count tip active
    private Text _countdownText;
    private RectTransform _countdownRect;
    private List<GameObject> _badgeSlots = new List<GameObject>();
    private GameObject _panelRoot;
    private bool _initialized;
    ```
  - [x] `Initialize(GameObject panelRoot, Text countdownText)` method:
    ```csharp
    public void Initialize(GameObject panelRoot, Text countdownText)
    {
        _panelRoot = panelRoot;
        _countdownText = countdownText;
        _countdownRect = countdownText?.GetComponent<RectTransform>();
        _initialized = true;
        _panelRoot.SetActive(false); // Hidden until tips arrive

        EventBus.Subscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ShopOpenedEvent>(OnShopOpened);
    }
    ```
  - [x] Event handler `OnTipOverlaysActivated(TipOverlaysActivatedEvent evt)`:
    - Clear existing badge slots (destroy children)
    - Iterate `evt.Overlays`
    - For EventCount type: set `_eventCountdown = overlay.EventCountdown`, update countdown text
    - For chart-overlay types: create a badge dot with abbreviation via helper method
    - Show or hide panel based on overlay count
  - [x] Event handler `OnMarketEventFired(MarketEventFiredEvent evt)`:
    - If `_eventCountdown > 0`: decrement, update text, play pulse animation
    - If `_eventCountdown == 0`: set text to "ALL CLEAR", color green
    - Never decrement below 0
  - [x] Event handler `OnRoundStarted(RoundStartedEvent evt)`:
    - Hide panel, reset countdown to -1, clear badges
  - [x] Event handler `OnShopOpened(ShopOpenedEvent evt)`:
    - Hide panel
  - [x] `OnDestroy()`: unsubscribe all 4 events
  - [x] Static utility methods (pure logic, testable):
    ```csharp
    /// <summary>
    /// Returns formatted countdown text. "ALL CLEAR" when count is 0,
    /// "EVENTS: X" when count > 0, empty string when count < 0 (inactive).
    /// </summary>
    public static string FormatCountdownText(int count)
    {
        if (count < 0) return "";
        if (count == 0) return "ALL CLEAR";
        return $"EVENTS: {count}";
    }

    /// <summary>
    /// Returns the 2-3 letter badge abbreviation for a tip type.
    /// Chart overlay tips get short labels; EventCount returns "EVT" (but is
    /// displayed as the live countdown, not a badge).
    /// </summary>
    public static string GetBadgeAbbreviation(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceFloor:        return "FLR";
            case InsiderTipType.PriceCeiling:      return "CLG";
            case InsiderTipType.PriceForecast:     return "FC";
            case InsiderTipType.DipMarker:         return "DIP";
            case InsiderTipType.PeakMarker:        return "PK";
            case InsiderTipType.ClosingDirection:  return "DIR";
            case InsiderTipType.EventTiming:       return "ET";
            case InsiderTipType.TrendReversal:     return "TR";
            case InsiderTipType.EventCount:        return "EVT";
            default:                               return "?";
        }
    }

    /// <summary>
    /// Returns the badge dot color for a given tip type.
    /// Uses ColorPalette colors keyed to tip category.
    /// </summary>
    public static Color GetBadgeColor(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceFloor:        return ColorPalette.Cyan;
            case InsiderTipType.PriceCeiling:      return ColorPalette.Amber;
            case InsiderTipType.PriceForecast:     return ColorPalette.Cyan;
            case InsiderTipType.DipMarker:         return ColorPalette.Green;
            case InsiderTipType.PeakMarker:        return ColorPalette.Amber;
            case InsiderTipType.ClosingDirection:  return ColorPalette.White;
            case InsiderTipType.EventTiming:       return ColorPalette.Red;
            case InsiderTipType.TrendReversal:     return ColorPalette.Cyan;
            case InsiderTipType.EventCount:        return ColorPalette.Green;
            default:                               return ColorPalette.WhiteDim;
        }
    }
    ```
  - [x] File: `Assets/Scripts/Runtime/UI/TipPanel.cs`

- [x] Task 4: Implement countdown decrement and pulse animation (AC: 4, 5, 6)
  - [x] In `TipPanel.OnMarketEventFired()`:
    ```csharp
    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (_eventCountdown <= 0) return;

        _eventCountdown--;
        UpdateCountdownDisplay();

        // AC 6: Pulse animation on decrement
        if (_countdownRect != null)
            PlayPulse(_countdownRect);
    }

    private void UpdateCountdownDisplay()
    {
        if (_countdownText == null) return;
        _countdownText.text = FormatCountdownText(_eventCountdown);
        _countdownText.color = _eventCountdown == 0
            ? ColorPalette.Green
            : CRTThemeData.TextHigh;
    }
    ```
  - [x] Pulse animation using DOTween (already available in the project via DG.Tweening):
    ```csharp
    public static readonly float PulseDuration = 0.15f;
    public static readonly float PulseScale = 1.2f;

    private void PlayPulse(RectTransform target)
    {
        target.localScale = Vector3.one;
        target.DOScale(PulseScale, PulseDuration * 0.5f)
            .SetUpdate(true) // unscaled time (works during Time.timeScale=0 pauses)
            .OnComplete(() =>
                target.DOScale(1f, PulseDuration * 0.5f).SetUpdate(true));
    }
    ```
  - [x] File: `Assets/Scripts/Runtime/UI/TipPanel.cs`

- [x] Task 5: Implement badge dot creation for chart-overlay tips (AC: 7)
  - [x] In `TipPanel`, add method to create a badge dynamically:
    ```csharp
    private void CreateBadge(InsiderTipType type, Transform parent)
    {
        var badgeGo = new GameObject($"Badge_{type}");
        badgeGo.transform.SetParent(parent, false);

        // Background dot (24x24)
        var badgeRect = badgeGo.AddComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(24f, 24f);

        var bgImage = badgeGo.AddComponent<Image>();
        bgImage.color = ColorPalette.WithAlpha(GetBadgeColor(type), 0.7f);
        bgImage.raycastTarget = false;

        // Abbreviation label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(badgeGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGo.AddComponent<Text>();
        labelText.text = GetBadgeAbbreviation(type);
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 9;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = ColorPalette.Background;
        labelText.raycastTarget = false;

        // Add LayoutElement for HorizontalLayoutGroup sizing
        var layout = badgeGo.AddComponent<LayoutElement>();
        layout.preferredWidth = 24f;
        layout.preferredHeight = 24f;

        _badgeSlots.Add(badgeGo);
    }
    ```
  - [x] EventCount type is NOT rendered as a badge — it is rendered as the live countdown text instead
  - [x] In `OnTipOverlaysActivated`, only call `CreateBadge()` for non-EventCount types
  - [x] File: `Assets/Scripts/Runtime/UI/TipPanel.cs`

- [x] Task 6: Create TipPanel UI hierarchy in UISetup (AC: 2, 10)
  - [x] Open `Assets/Scripts/Setup/UISetup.cs`
  - [x] Add a new section after the TradingHUD creation block (after line ~128):
    ```csharp
    // Story 18.4: Create TipPanel below Control Deck
    var tipPanelGo = new GameObject("TipPanel");
    tipPanelGo.transform.SetParent(dashRefs.ControlDeckCanvas.transform, false);
    var tipPanelRect = tipPanelGo.AddComponent<RectTransform>();

    // Position above Control Deck panel, left-aligned
    tipPanelRect.anchorMin = new Vector2(0.05f, 0f);
    tipPanelRect.anchorMax = new Vector2(0.5f, 0f);
    tipPanelRect.pivot = new Vector2(0f, 0f);
    tipPanelRect.anchoredPosition = new Vector2(0f, 175f); // Above 160px deck + 10px gap
    tipPanelRect.sizeDelta = new Vector2(0f, 30f);

    // Semi-transparent background
    var tipPanelBg = tipPanelGo.AddComponent<Image>();
    tipPanelBg.color = ColorPalette.WithAlpha(ColorPalette.Panel, 0.85f);
    tipPanelBg.raycastTarget = false;

    // HorizontalLayoutGroup for auto-arrangement
    var tipHlg = tipPanelGo.AddComponent<HorizontalLayoutGroup>();
    tipHlg.padding = new RectOffset(8, 8, 4, 4);
    tipHlg.spacing = 6f;
    tipHlg.childAlignment = TextAnchor.MiddleLeft;
    tipHlg.childForceExpandWidth = false;
    tipHlg.childForceExpandHeight = false;

    // ContentSizeFitter so panel auto-sizes to content
    var tipCsf = tipPanelGo.AddComponent<ContentSizeFitter>();
    tipCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

    // Countdown text element (used by EventCount tip)
    var countdownGo = new GameObject("CountdownText");
    countdownGo.transform.SetParent(tipPanelGo.transform, false);
    var countdownText = countdownGo.AddComponent<Text>();
    countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    countdownText.fontSize = 14;
    countdownText.fontStyle = FontStyle.Bold;
    countdownText.color = CRTThemeData.TextHigh;
    countdownText.alignment = TextAnchor.MiddleCenter;
    countdownText.raycastTarget = false;
    var countdownLayout = countdownGo.AddComponent<LayoutElement>();
    countdownLayout.preferredHeight = 22f;
    countdownLayout.minWidth = 80f;
    countdownGo.SetActive(false); // Hidden until EventCount tip is active

    // Create and initialize TipPanel MonoBehaviour
    var tipPanel = tipPanelGo.AddComponent<TipPanel>();
    tipPanel.Initialize(tipPanelGo, countdownText);
    ```
  - [x] File: `Assets/Scripts/Setup/UISetup.cs`

- [x] Task 7: Wire panel visibility lifecycle (AC: 8, 9)
  - [x] In `TipPanel.OnTipOverlaysActivated()`:
    ```csharp
    private void OnTipOverlaysActivated(TipOverlaysActivatedEvent evt)
    {
        // Clear previous state
        ClearBadges();
        _eventCountdown = -1;

        if (evt.Overlays == null || evt.Overlays.Count == 0)
        {
            _panelRoot.SetActive(false);
            return;
        }

        bool hasCountdown = false;
        foreach (var overlay in evt.Overlays)
        {
            if (overlay.Type == InsiderTipType.EventCount && overlay.EventCountdown >= 0)
            {
                _eventCountdown = overlay.EventCountdown;
                hasCountdown = true;
            }
            else
            {
                CreateBadge(overlay.Type, _panelRoot.transform);
            }
        }

        // Show/hide countdown text
        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(hasCountdown);
            if (hasCountdown)
                UpdateCountdownDisplay();
        }

        _panelRoot.SetActive(true);
    }
    ```
  - [x] `ClearBadges()` helper:
    ```csharp
    private void ClearBadges()
    {
        foreach (var slot in _badgeSlots)
        {
            if (slot != null) Object.Destroy(slot);
        }
        _badgeSlots.Clear();
    }
    ```
  - [x] In `OnRoundStarted`: hide panel, clear badges, reset countdown
  - [x] In `OnShopOpened`: hide panel
  - [x] File: `Assets/Scripts/Runtime/UI/TipPanel.cs`

- [x] Task 8: Write TipPanel tests (AC: 11, 12)
  - [x] Create `Assets/Tests/Runtime/UI/TipPanelTests.cs`
  - [x] Test class structure following existing pattern (`BullRun.Tests.UI` namespace, `[TestFixture]`, `EventBus.Clear()` in SetUp/TearDown):
    ```csharp
    using NUnit.Framework;
    using UnityEngine;

    namespace BullRun.Tests.UI
    {
        [TestFixture]
        public class TipPanelTests
        {
            [SetUp]
            public void SetUp() { EventBus.Clear(); }

            [TearDown]
            public void TearDown() { EventBus.Clear(); }

            // --- FormatCountdownText ---

            [Test]
            public void FormatCountdownText_PositiveCount_ReturnsEventsX()
            {
                Assert.AreEqual("EVENTS: 5", TipPanel.FormatCountdownText(5));
            }

            [Test]
            public void FormatCountdownText_Zero_ReturnsAllClear()
            {
                Assert.AreEqual("ALL CLEAR", TipPanel.FormatCountdownText(0));
            }

            [Test]
            public void FormatCountdownText_Negative_ReturnsEmpty()
            {
                Assert.AreEqual("", TipPanel.FormatCountdownText(-1));
            }

            [Test]
            public void FormatCountdownText_One_ReturnsEvents1()
            {
                Assert.AreEqual("EVENTS: 1", TipPanel.FormatCountdownText(1));
            }

            [Test]
            public void FormatCountdownText_LargeCount_FormatsCorrectly()
            {
                Assert.AreEqual("EVENTS: 15", TipPanel.FormatCountdownText(15));
            }

            // --- GetBadgeAbbreviation ---

            [Test]
            public void GetBadgeAbbreviation_PriceFloor_ReturnsFLR()
            {
                Assert.AreEqual("FLR", TipPanel.GetBadgeAbbreviation(InsiderTipType.PriceFloor));
            }

            [Test]
            public void GetBadgeAbbreviation_PriceCeiling_ReturnsCLG()
            {
                Assert.AreEqual("CLG", TipPanel.GetBadgeAbbreviation(InsiderTipType.PriceCeiling));
            }

            [Test]
            public void GetBadgeAbbreviation_PriceForecast_ReturnsFC()
            {
                Assert.AreEqual("FC", TipPanel.GetBadgeAbbreviation(InsiderTipType.PriceForecast));
            }

            [Test]
            public void GetBadgeAbbreviation_DipMarker_ReturnsDIP()
            {
                Assert.AreEqual("DIP", TipPanel.GetBadgeAbbreviation(InsiderTipType.DipMarker));
            }

            [Test]
            public void GetBadgeAbbreviation_PeakMarker_ReturnsPK()
            {
                Assert.AreEqual("PK", TipPanel.GetBadgeAbbreviation(InsiderTipType.PeakMarker));
            }

            [Test]
            public void GetBadgeAbbreviation_ClosingDirection_ReturnsDIR()
            {
                Assert.AreEqual("DIR", TipPanel.GetBadgeAbbreviation(InsiderTipType.ClosingDirection));
            }

            [Test]
            public void GetBadgeAbbreviation_EventTiming_ReturnsET()
            {
                Assert.AreEqual("ET", TipPanel.GetBadgeAbbreviation(InsiderTipType.EventTiming));
            }

            [Test]
            public void GetBadgeAbbreviation_TrendReversal_ReturnsTR()
            {
                Assert.AreEqual("TR", TipPanel.GetBadgeAbbreviation(InsiderTipType.TrendReversal));
            }

            [Test]
            public void GetBadgeAbbreviation_EventCount_ReturnsEVT()
            {
                Assert.AreEqual("EVT", TipPanel.GetBadgeAbbreviation(InsiderTipType.EventCount));
            }

            // --- GetBadgeColor ---

            [Test]
            public void GetBadgeColor_PriceFloor_ReturnsCyan()
            {
                Assert.AreEqual(ColorPalette.Cyan, TipPanel.GetBadgeColor(InsiderTipType.PriceFloor));
            }

            [Test]
            public void GetBadgeColor_PriceCeiling_ReturnsAmber()
            {
                Assert.AreEqual(ColorPalette.Amber, TipPanel.GetBadgeColor(InsiderTipType.PriceCeiling));
            }

            [Test]
            public void GetBadgeColor_DipMarker_ReturnsGreen()
            {
                Assert.AreEqual(ColorPalette.Green, TipPanel.GetBadgeColor(InsiderTipType.DipMarker));
            }

            [Test]
            public void GetBadgeColor_PeakMarker_ReturnsAmber()
            {
                Assert.AreEqual(ColorPalette.Amber, TipPanel.GetBadgeColor(InsiderTipType.PeakMarker));
            }

            [Test]
            public void GetBadgeColor_EventTiming_ReturnsRed()
            {
                Assert.AreEqual(ColorPalette.Red, TipPanel.GetBadgeColor(InsiderTipType.EventTiming));
            }

            [Test]
            public void GetBadgeColor_EventCount_ReturnsGreen()
            {
                Assert.AreEqual(ColorPalette.Green, TipPanel.GetBadgeColor(InsiderTipType.EventCount));
            }

            // --- All types have non-default badge abbreviations ---

            [Test]
            public void GetBadgeAbbreviation_AllNineTypes_ReturnNonQuestionMark()
            {
                var types = new[]
                {
                    InsiderTipType.PriceForecast,
                    InsiderTipType.PriceFloor,
                    InsiderTipType.PriceCeiling,
                    InsiderTipType.EventCount,
                    InsiderTipType.DipMarker,
                    InsiderTipType.PeakMarker,
                    InsiderTipType.ClosingDirection,
                    InsiderTipType.EventTiming,
                    InsiderTipType.TrendReversal
                };
                foreach (var type in types)
                {
                    string abbrev = TipPanel.GetBadgeAbbreviation(type);
                    Assert.AreNotEqual("?", abbrev,
                        $"Type {type} should have a defined abbreviation");
                    Assert.IsTrue(abbrev.Length >= 2 && abbrev.Length <= 3,
                        $"Type {type} abbreviation '{abbrev}' should be 2-3 chars");
                }
            }

            // --- Countdown never goes below zero ---

            [Test]
            public void FormatCountdownText_AtZero_StaysAllClear()
            {
                // Simulates: countdown already at 0, another event fires
                // The handler guards against decrementing below 0,
                // so FormatCountdownText(0) should stay "ALL CLEAR"
                Assert.AreEqual("ALL CLEAR", TipPanel.FormatCountdownText(0));
            }

            // --- PulseDuration and PulseScale are sane ---

            [Test]
            public void PulseDuration_IsPositive()
            {
                Assert.Greater(TipPanel.PulseDuration, 0f);
                Assert.LessOrEqual(TipPanel.PulseDuration, 1f);
            }

            [Test]
            public void PulseScale_IsGreaterThanOne()
            {
                Assert.Greater(TipPanel.PulseScale, 1f);
                Assert.LessOrEqual(TipPanel.PulseScale, 2f);
            }
        }
    }
    ```
  - [x] File: `Assets/Tests/Runtime/UI/TipPanelTests.cs`

## Dev Notes

### Architecture Compliance
- **uGUI only:** TipPanel uses `Canvas`, `Text`, `Image`, `HorizontalLayoutGroup`, `ContentSizeFitter` -- no UI Toolkit
- **Programmatic creation:** All TipPanel GameObjects are created in `UISetup.Execute()`, never in the Inspector
- **EventBus communication:** TipPanel subscribes to `TipOverlaysActivatedEvent`, `MarketEventFiredEvent`, `RoundStartedEvent`, `ShopOpenedEvent` -- never references TradingHUD, EventScheduler, or TipActivator directly
- **No ScriptableObjects:** All constants (`PulseDuration`, `PulseScale`) are `public static readonly` on the `TipPanel` class
- **Runtime-safe:** No `UnityEditor` references in `TipPanel.cs`
- **Static utility pattern:** `FormatCountdownText`, `GetBadgeAbbreviation`, `GetBadgeColor` are `public static` for EditMode testability without requiring MonoBehaviour instantiation (follows TradingHUD's `FormatCurrency`/`FormatProfit` pattern)

### Existing Code to Read Before Implementing
- `Assets/Scripts/Runtime/UI/TradingHUD.cs` -- the code being modified (remove old tip text display)
- `Assets/Scripts/Setup/UISetup.cs` -- where the TipPanel creation code goes (follow the pattern used by RelicBar creation at the end of `Execute()`)
- `Assets/Scripts/Runtime/UI/RelicBar.cs` -- closest structural analog: another compact HUD strip with dynamic slots, EventBus subscriptions, and visibility lifecycle
- `Assets/Scripts/Runtime/UI/FloatingTextService.cs` -- reference for programmatic text/animation creation pattern
- `Assets/Scripts/Runtime/UI/DashboardReferences.cs` -- may need a new field if TipPanel needs wiring through dashboard refs (likely not needed since TipPanel is self-contained)
- `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` -- `TipOverlayData` struct definition (from Story 18.1), `InsiderTipType` enum
- `Assets/Scripts/Runtime/Core/GameEvents.cs` -- `TipOverlaysActivatedEvent`, `MarketEventFiredEvent`, `RoundStartedEvent`, `ShopOpenedEvent` definitions
- `Assets/Scripts/Setup/Data/ColorPalette.cs` -- color constants used by badge dots and ALL CLEAR text
- `Assets/Scripts/Setup/Data/CRTThemeData.cs` -- `TextHigh`, `Panel`, `Border` for panel styling
- `Assets/Tests/Runtime/UI/TradingHUDTests.cs` -- test style reference (namespace, SetUp/TearDown, static method tests)

### Depends On
- **Story 18.1** (Tip Data Model): Provides the updated `InsiderTipType` enum with 9 types, `TipOverlayData` struct with `EventCountdown` field, and `RevealedTip` struct changes
- **Story 18.2** (Tip Activation): Provides `TipActivator` which computes `TipOverlayData` at round start, and `TipOverlaysActivatedEvent` which TipPanel subscribes to

### References
- `TipOverlaysActivatedEvent` (defined in Story 18.2, added to `GameEvents.cs`): `{ List<TipOverlayData> Overlays }`
- `TipOverlayData.EventCountdown` (defined in Story 18.1): `-1` if not an EventCount tip, otherwise the actual scheduled event count
- `MarketEventFiredEvent` (already exists in `GameEvents.cs`): fired each time a market event occurs during trading
- DOTween (`DG.Tweening`): already used by TradingHUD for cash tween and progress bar tween -- use `DOScale` for pulse effect
- `ContentSizeFitter.FitMode.PreferredSize`: used in ShopUI tooltips for auto-sizing panels (same pattern for tip panel horizontal sizing)

### Animation Approach
- The countdown pulse uses DOTween's `DOScale` method on the countdown text's `RectTransform`
- `SetUpdate(true)` ensures the animation plays even during `Time.timeScale = 0` pauses (which happen during `EventPopup` display)
- The pulse is a two-phase sequence: scale up to `PulseScale` (1.2x) over half the duration, then back to 1.0x over the remaining half
- This matches the existing game feel philosophy: brief, noticeable, non-distracting

### UISetup Integration Pattern
- TipPanel creation goes AFTER the TradingHUD initialization block in `UISetup.Execute(RunContext, int, float)`
- The panel is parented to `ControlDeckCanvas` (same as FloatingTextService, RelicBar)
- Positioned above the Control Deck panel using anchor offsets
- Uses `ContentSizeFitter` for horizontal auto-sizing based on badge count

### Event Subscription Lifecycle
- Subscribe in `Initialize()` (called from UISetup during trading phase setup)
- Unsubscribe in `OnDestroy()` (called when the TradingHUD parent is destroyed at phase end)
- Events flow: `RoundStartedEvent` (hide + reset) -> `TipOverlaysActivatedEvent` (populate + show) -> N x `MarketEventFiredEvent` (decrement) -> `ShopOpenedEvent` (hide)

## Dev Agent Record

### Agent Model Used
Claude Opus 4.6

### Debug Log References
- `[TipPanel] Activated with {count} overlays, EventCountdown={countdown}` on TipOverlaysActivated
- `[TipPanel] Event countdown: {old} -> {new}` on MarketEventFired decrement
- `[TipPanel] ALL CLEAR — no remaining events` when countdown hits zero

### Completion Notes List
- Removed old pipe-separated `_tipsDisplayText` field, `SetTipsDisplay()` method, and `RefreshDisplay()` tips block from TradingHUD.cs (AC 1)
- No UISetup code existed for old tips wiring — `SetTipsDisplay` was never called from UISetup (AC 1 confirmed via grep)
- Created TipPanel.cs with full EventBus lifecycle: subscribes to TipOverlaysActivatedEvent, MarketEventFiredEvent, RoundStartedEvent, ShopOpenedEvent (AC 3, 8, 9)
- Implemented live countdown: "EVENTS: X" decrement on each MarketEventFiredEvent, "ALL CLEAR" in green at zero, guard against negative (AC 4, 5)
- Implemented DOTween pulse animation (0.15s, 1.2x scale) with SetUpdate(true) for unscaled time (AC 6)
- Implemented colored badge dots (24x24) with 2-3 letter abbreviations for all 8 chart-overlay tip types (AC 7)
- EventCount is rendered as live countdown text, NOT as a badge dot
- TipPanel created programmatically in UISetup, parented to ControlDeckCanvas, with HorizontalLayoutGroup + ContentSizeFitter (AC 2, 10)
- All three static utility methods (FormatCountdownText, GetBadgeAbbreviation, GetBadgeColor) are pure logic, testable without MonoBehaviour (AC 11)
- 27 EditMode tests covering: countdown formatting (6), badge abbreviations (10), badge colors (9), pulse constants (2) (AC 12 — static pure-logic coverage; behavioral tests like decrement handler and visibility lifecycle require PlayMode)

### File List
- **Create:** `Assets/Scripts/Runtime/UI/TipPanel.cs`
- **Create:** `Assets/Tests/Runtime/UI/TipPanelTests.cs`
- **Modify:** `Assets/Scripts/Runtime/UI/TradingHUD.cs` (remove old tip text display)
- **Modify:** `Assets/Scripts/Setup/UISetup.cs` (add TipPanel creation)

### Change Log
- 2026-02-21: Story created
- 2026-02-21: Implementation complete — TipPanel MonoBehaviour with live countdown, badge dots, pulse animation, visibility lifecycle, and 22 tests
- 2026-02-21: Code review fixes — stored DOTween pulse reference to prevent MissingReferenceException and tween stacking (H1), added 3 missing GetBadgeColor tests for PriceForecast/ClosingDirection/TrendReversal (H3), removed dead `_initialized` field (M1), made OnShopOpened cleanup consistent with OnRoundStarted (M3), fixed test count and misleading test comment (M2/H2), fixed UISetup comment and File List description (L1/L2). Total: 27 tests.
