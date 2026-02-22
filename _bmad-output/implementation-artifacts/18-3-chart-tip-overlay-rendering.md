# Story 18.3: Chart Tip Overlay Rendering

Status: ready-for-dev

## Story

As a player,
I want purchased insider tips to appear as visual overlays directly on the trading chart -- price lines, shaded zones, event markers, and directional arrows,
so that tips are impossible to miss and directly inform my trading decisions in real time.

## Acceptance Criteria

1. New `TipOverlayRenderer` MonoBehaviour created in `Scripts/Runtime/Chart/TipOverlayRenderer.cs`, attached to ChartSystem by ChartSetup during F5 rebuild
2. **Horizontal Line Overlays** (Price Floor = cyan, Price Ceiling = amber): LineRenderers spanning full chart width at the price level, repositioned every `LateUpdate` as Y-axis rescales; labels "FLOOR ~$X.XX" / "CEILING ~$X.XX" at left edge on ChartCanvas
3. **Horizontal Band Overlay** (Price Forecast): Semi-transparent quad mesh spanning full chart width, centered on forecast price +/- BandHalfWidth, blue/purple at ~18% opacity, label "FORECAST ~$X.XX" at left edge; resizes every `LateUpdate`
4. **Vertical Marker Overlays** (Event Timing): Pool of up to 15 thin vertical LineRenderers from chart bottom to chart top, one per scheduled event at fuzzed normalized time, red at ~40% opacity, "!" label at top; X positions update only when round duration changes
5. **Time Zone Overlays** (Dip Marker = green, Peak Marker = amber/gold): Semi-transparent quad meshes covering full chart height over limited time width, labels "DIP ZONE" / "PEAK ZONE" at top; ~18% opacity; X positions static unless round duration changes
6. **Trend Reversal Marker**: Vertical LineRenderer at ReversalTime position, magenta at ~50% opacity, "R" label at top; only rendered when `ReversalTime >= 0`
7. **Direction Arrow Overlay** (Closing Direction): Text element on right edge of chart showing green "^" for closes higher or red "v" for closes lower, with label "CLOSING UP" / "CLOSING DOWN"; static position
8. All overlay labels use the existing terminal font at 11pt on ChartCanvas, matching overlay color at 80% opacity; labels do not overlap with Y-axis labels
9. **Overlay lifecycle**: All overlay GameObjects pre-created during ChartSetup and hidden (`SetActive(false)`); activated on `TipOverlaysActivatedEvent`; cleared on `RoundStartedEvent` (before new overlays arrive) and `ShopOpenedEvent`
10. No per-frame heap allocations in steady state -- all GameObjects, Materials, and mesh data pre-created and reused
11. Overlay sorting orders: quad meshes at -3 (behind gridlines at -2), horizontal tip lines at 0 (between gridlines and main line), vertical markers at 0, direction arrow at 6 (in front of trade markers at 5)
12. Tests verify: overlay coordinate math (price-to-Y, time-to-X), visibility lifecycle (hidden by default, shown after activation, hidden after round clear), edge cases (zero-length price range, no overlays, max 15 event markers)

## Tasks / Subtasks

- [ ] Task 1: Add overlay color constants to `ChartVisualConfig` (AC: 2, 3, 4, 5, 6, 7, 11)
  - [ ] Open `Assets/Scripts/Runtime/Chart/ChartVisualConfig.cs`
  - [ ] Add static readonly overlay color/sorting constants:
    ```csharp
    // --- Tip Overlay Constants ---
    public static readonly Color OverlayFloorColor = ColorPalette.WithAlpha(ColorPalette.Cyan, 0.6f);
    public static readonly Color OverlayCeilingColor = ColorPalette.WithAlpha(ColorPalette.Amber, 0.6f);
    public static readonly Color OverlayForecastColor = ColorPalette.WithAlpha(
        new Color(0.4f, 0.3f, 0.8f, 1f), 0.18f);  // Blue-purple at 18% opacity
    public static readonly Color OverlayEventMarkerColor = ColorPalette.WithAlpha(ColorPalette.Red, 0.4f);
    public static readonly Color OverlayDipZoneColor = ColorPalette.WithAlpha(ColorPalette.Green, 0.18f);
    public static readonly Color OverlayPeakZoneColor = ColorPalette.WithAlpha(ColorPalette.Amber, 0.18f);
    public static readonly Color OverlayReversalColor = ColorPalette.WithAlpha(ColorPalette.Magenta, 0.5f);

    public static readonly float OverlayLineWidth = 0.012f;       // Slightly thinner than break-even (0.015)
    public static readonly float OverlayVerticalLineWidth = 0.008f;
    public static readonly int OverlayQuadSortingOrder = -3;       // Behind gridlines (-2)
    public static readonly int OverlayLineSortingOrder = 0;        // Between gridlines and main line
    public static readonly int OverlayArrowSortingOrder = 6;       // In front of trade markers (5)
    public static readonly int MaxEventTimingMarkers = 15;
    public static readonly float OverlayLabelFontSize = 11f;
    public static readonly float OverlayLabelAlpha = 0.8f;
    ```
  - [ ] Note: `ColorPalette` does not have a `Magenta` constant; add one to `ColorPalette.cs` if missing:
    ```csharp
    public static readonly Color Magenta = new Color(180 / 255f, 80 / 255f, 200 / 255f, 1f);
    ```
  - [ ] File: `Assets/Scripts/Runtime/Chart/ChartVisualConfig.cs`
  - [ ] File: `Assets/Scripts/Setup/Data/ColorPalette.cs` (only if Magenta missing)

- [ ] Task 2: Create `TipOverlayRenderer` MonoBehaviour shell (AC: 1, 9, 10)
  - [ ] Create new file: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`
  - [ ] Class structure:
    ```csharp
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Renders TipOverlayData as visual chart overlays (lines, bands, zones, markers, arrows).
    /// Created by ChartSetup during F5 rebuild. Subscribes to TipOverlaysActivatedEvent.
    /// Updates horizontal overlay Y positions every LateUpdate as chart Y-axis rescales.
    /// </summary>
    public class TipOverlayRenderer : MonoBehaviour
    {
        // Chart bounds (world space) -- set during Initialize
        private float _chartLeft, _chartRight, _chartBottom, _chartTop;
        private ChartRenderer _chartRenderer;

        // Cached canvas reference for label coordinate conversion
        private Canvas _chartCanvas;
        private RectTransform _canvasRect;

        // -- Horizontal line overlays (Floor, Ceiling) --
        private LineRenderer _floorLine;
        private LineRenderer _ceilingLine;
        private Text _floorLabel;
        private Text _ceilingLabel;
        private float _floorPrice;
        private float _ceilingPrice;
        private bool _floorActive;
        private bool _ceilingActive;

        // -- Horizontal band overlay (Forecast) --
        private MeshFilter _forecastBandMeshFilter;
        private MeshRenderer _forecastBandRenderer;
        private Text _forecastLabel;
        private float _forecastCenter;
        private float _forecastHalfWidth;
        private bool _forecastActive;

        // -- Vertical marker overlays (Event Timing) --
        private LineRenderer[] _eventMarkerLines;
        private Text[] _eventMarkerLabels;
        private float[] _eventMarkerTimes;  // normalized 0-1
        private int _activeEventMarkerCount;

        // -- Time zone overlays (Dip, Peak) --
        private MeshFilter _dipZoneMeshFilter;
        private MeshRenderer _dipZoneRenderer;
        private Text _dipZoneLabel;
        private float _dipZoneCenter;
        private float _dipZoneHalfWidth;
        private bool _dipZoneActive;

        private MeshFilter _peakZoneMeshFilter;
        private MeshRenderer _peakZoneRenderer;
        private Text _peakZoneLabel;
        private float _peakZoneCenter;
        private float _peakZoneHalfWidth;
        private bool _peakZoneActive;

        // -- Trend Reversal marker --
        private LineRenderer _reversalLine;
        private Text _reversalLabel;
        private float _reversalTime;  // normalized 0-1, -1 = inactive
        private bool _reversalActive;

        // -- Direction arrow --
        private Text _directionArrowText;
        private Text _directionLabel;
        private int _directionSign;  // +1 or -1
        private bool _directionActive;

        // Cached round duration for X-axis recalculation
        private float _cachedRoundDuration;

        // Reusable mesh data (avoid per-frame allocation)
        private Mesh _forecastMesh;
        private Mesh _dipZoneMesh;
        private Mesh _peakZoneMesh;
        private readonly Vector3[] _quadVerts = new Vector3[4];
        private static readonly int[] QuadTriangles = { 0, 1, 2, 2, 3, 0 };
    }
    ```
  - [ ] Add `Initialize()` method that receives all pre-created GameObjects from ChartSetup
  - [ ] Add `OnEnable()` / `OnDisable()` for EventBus subscribe/unsubscribe:
    ```csharp
    private void OnEnable()
    {
        EventBus.Subscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ShopOpenedEvent>(OnShopOpened);
        EventBus.Subscribe<RoundTimerExtendedEvent>(OnTimerExtended);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<ShopOpenedEvent>(OnShopOpened);
        EventBus.Unsubscribe<RoundTimerExtendedEvent>(OnTimerExtended);
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 3: Implement overlay activation and clearing (AC: 9)
  - [ ] `OnTipOverlaysActivated(TipOverlaysActivatedEvent evt)`:
    - [ ] Call `ClearAllOverlays()` first to reset any stale state
    - [ ] Iterate `evt.Overlays`, for each `TipOverlayData`:
      ```csharp
      switch (overlay.Type)
      {
          case InsiderTipType.PriceFloor:
              _floorPrice = overlay.PriceLevel;
              _floorLabel.text = overlay.Label;
              _floorLine.gameObject.SetActive(true);
              _floorLabel.gameObject.SetActive(true);
              _floorActive = true;
              break;
          case InsiderTipType.PriceCeiling:
              _ceilingPrice = overlay.PriceLevel;
              _ceilingLabel.text = overlay.Label;
              _ceilingLine.gameObject.SetActive(true);
              _ceilingLabel.gameObject.SetActive(true);
              _ceilingActive = true;
              break;
          case InsiderTipType.PriceForecast:
              _forecastCenter = overlay.BandCenter;
              _forecastHalfWidth = overlay.BandHalfWidth;
              _forecastLabel.text = overlay.Label;
              _forecastBandMeshFilter.gameObject.SetActive(true);
              _forecastLabel.gameObject.SetActive(true);
              _forecastActive = true;
              break;
          case InsiderTipType.EventTiming:
              ActivateEventMarkers(overlay.TimeMarkers);
              break;
          case InsiderTipType.DipMarker:
              ActivateTimeZone(overlay, ref _dipZoneCenter, ref _dipZoneHalfWidth,
                  ref _dipZoneActive, _dipZoneMeshFilter, _dipZoneLabel);
              break;
          case InsiderTipType.PeakMarker:
              ActivateTimeZone(overlay, ref _peakZoneCenter, ref _peakZoneHalfWidth,
                  ref _peakZoneActive, _peakZoneMeshFilter, _peakZoneLabel);
              break;
          case InsiderTipType.TrendReversal:
              if (overlay.ReversalTime >= 0f)
              {
                  _reversalTime = overlay.ReversalTime;
                  _reversalLine.gameObject.SetActive(true);
                  _reversalLabel.text = overlay.Label;
                  _reversalLabel.gameObject.SetActive(true);
                  _reversalActive = true;
              }
              break;
          case InsiderTipType.ClosingDirection:
              _directionSign = overlay.DirectionSign;
              ActivateDirectionArrow(overlay);
              break;
      }
      ```
  - [ ] `OnRoundStarted(RoundStartedEvent evt)` -- call `ClearAllOverlays()` and cache `_cachedRoundDuration = evt.TimeLimit`
  - [ ] `OnShopOpened(ShopOpenedEvent evt)` -- call `ClearAllOverlays()`
  - [ ] `OnTimerExtended(RoundTimerExtendedEvent evt)` -- update `_cachedRoundDuration = evt.NewDuration` and reposition all X-based overlays
  - [ ] `ClearAllOverlays()` -- `SetActive(false)` every overlay GameObject, reset all `_xxxActive` flags to false
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 4: Implement `LateUpdate` repositioning for horizontal overlays (AC: 2, 3)
  - [ ] `LateUpdate()` method -- runs every frame to reposition Y-dependent overlays:
    ```csharp
    private void LateUpdate()
    {
        if (_chartRenderer == null || _chartRenderer.PointCount < 2) return;

        _chartRenderer.GetLivePriceRange(out float minPrice, out float maxPrice);
        float priceRange = maxPrice - minPrice;

        // Match ChartLineView's minimum range logic
        float center = (minPrice + maxPrice) * 0.5f;
        float minRange = center * 0.05f;
        if (minRange < 0.01f) minRange = 0.01f;
        if (priceRange < minRange)
        {
            minPrice = center - minRange * 0.5f;
            priceRange = minRange;
        }

        // Match ChartLineView's 10% Y-axis padding
        float chartHeight = _chartTop - _chartBottom;
        float padding = chartHeight * 0.1f;
        float paddedBottom = _chartBottom + padding;
        float paddedTop = _chartTop - padding;

        // Update horizontal line overlays
        if (_floorActive)
            UpdateHorizontalLine(_floorLine, _floorLabel, _floorPrice,
                minPrice, priceRange, paddedBottom, paddedTop);
        if (_ceilingActive)
            UpdateHorizontalLine(_ceilingLine, _ceilingLabel, _ceilingPrice,
                minPrice, priceRange, paddedBottom, paddedTop);

        // Update forecast band
        if (_forecastActive)
            UpdateForecastBand(minPrice, priceRange, paddedBottom, paddedTop);

        // Update time zones (only Y needs updating; X is static unless duration changed)
        if (_dipZoneActive)
            UpdateTimeZoneY(_dipZoneMeshFilter, _dipZoneMesh, _dipZoneCenter,
                _dipZoneHalfWidth, paddedBottom, paddedTop);
        if (_peakZoneActive)
            UpdateTimeZoneY(_peakZoneMeshFilter, _peakZoneMesh, _peakZoneCenter,
                _peakZoneHalfWidth, paddedBottom, paddedTop);

        // Update vertical overlay Y extents (event markers, reversal)
        if (_activeEventMarkerCount > 0)
            UpdateVerticalLineYExtents(_eventMarkerLines, _activeEventMarkerCount,
                paddedBottom, paddedTop);
        if (_reversalActive)
            UpdateSingleVerticalLineY(_reversalLine, paddedBottom, paddedTop);
    }
    ```
  - [ ] `UpdateHorizontalLine()`:
    ```csharp
    private void UpdateHorizontalLine(LineRenderer lr, Text label, float price,
        float minPrice, float priceRange, float paddedBottom, float paddedTop)
    {
        float y = PriceToWorldY(price, minPrice, priceRange, paddedBottom, paddedTop);
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(_chartLeft, y, 0f));
        lr.SetPosition(1, new Vector3(_chartRight, y, 0f));

        // Position label in canvas space at left edge
        PositionLabelAtWorldY(label, _chartLeft, y);
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 5: Implement coordinate transform helpers (AC: 2, 3, 4, 5, 6, 12)
  - [ ] Static methods for testability:
    ```csharp
    /// <summary>
    /// Converts a price value to a world-space Y coordinate.
    /// Uses the same formula as ChartLineView for consistency.
    /// Static for testability.
    /// </summary>
    public static float PriceToWorldY(float price, float minPrice, float priceRange,
        float paddedBottom, float paddedTop)
    {
        if (priceRange <= 0f) return (paddedBottom + paddedTop) * 0.5f;
        float normalized = (price - minPrice) / priceRange;
        return Mathf.Lerp(paddedBottom, paddedTop, normalized);
    }

    /// <summary>
    /// Converts a normalized time (0-1) to a world-space X coordinate.
    /// Static for testability.
    /// </summary>
    public static float NormalizedTimeToWorldX(float normalizedTime,
        float chartLeft, float chartRight)
    {
        return Mathf.Lerp(chartLeft, chartRight, normalizedTime);
    }

    /// <summary>
    /// Calculates the world-space X extents for a time zone.
    /// Returns (xLeft, xRight) clamped to chart bounds.
    /// Static for testability.
    /// </summary>
    public static (float xLeft, float xRight) TimeZoneToWorldX(
        float zoneCenter, float zoneHalfWidth, float chartLeft, float chartRight)
    {
        float left = Mathf.Lerp(chartLeft, chartRight,
            Mathf.Clamp01(zoneCenter - zoneHalfWidth));
        float right = Mathf.Lerp(chartLeft, chartRight,
            Mathf.Clamp01(zoneCenter + zoneHalfWidth));
        return (left, right);
    }
    ```
  - [ ] These MUST match ChartLineView's coordinate transform exactly:
    - `Time -> X: Mathf.Lerp(_chartLeft, _chartRight, normalizedTime)`
    - `Price -> Y: Mathf.Lerp(paddedBottom, paddedTop, (price - minPrice) / priceRange)`
    - Padded bounds: `paddedBottom = _chartBottom + chartHeight * 0.1f`, `paddedTop = _chartTop - chartHeight * 0.1f`
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 6: Implement quad mesh overlays (Forecast Band, Time Zones) (AC: 3, 5)
  - [ ] `UpdateForecastBand()` -- builds a quad mesh for the price forecast band:
    ```csharp
    private void UpdateForecastBand(float minPrice, float priceRange,
        float paddedBottom, float paddedTop)
    {
        float yTop = PriceToWorldY(_forecastCenter + _forecastHalfWidth,
            minPrice, priceRange, paddedBottom, paddedTop);
        float yBottom = PriceToWorldY(_forecastCenter - _forecastHalfWidth,
            minPrice, priceRange, paddedBottom, paddedTop);

        BuildQuadMesh(_forecastMesh, _chartLeft, _chartRight, yBottom, yTop);
        PositionLabelAtWorldY(_forecastLabel, _chartLeft, (yTop + yBottom) * 0.5f);
    }
    ```
  - [ ] `UpdateTimeZoneY()` -- rebuilds time zone quad Y extents:
    ```csharp
    private void UpdateTimeZoneY(MeshFilter mf, Mesh mesh, float zoneCenter,
        float zoneHalfWidth, float paddedBottom, float paddedTop)
    {
        var (xLeft, xRight) = TimeZoneToWorldX(zoneCenter, zoneHalfWidth,
            _chartLeft, _chartRight);
        BuildQuadMesh(mesh, xLeft, xRight, paddedBottom, paddedTop);
    }
    ```
  - [ ] `BuildQuadMesh()` -- reusable quad builder, no allocations:
    ```csharp
    private void BuildQuadMesh(Mesh mesh, float left, float right,
        float bottom, float top)
    {
        _quadVerts[0] = new Vector3(left, bottom, 0f);
        _quadVerts[1] = new Vector3(left, top, 0f);
        _quadVerts[2] = new Vector3(right, top, 0f);
        _quadVerts[3] = new Vector3(right, bottom, 0f);

        mesh.Clear();
        mesh.vertices = _quadVerts;
        mesh.triangles = QuadTriangles;
    }
    ```
  - [ ] Each quad mesh is created once in `Initialize()` with `mesh.MarkDynamic()` and reused
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 7: Implement vertical marker overlays and direction arrow (AC: 4, 6, 7)
  - [ ] `ActivateEventMarkers(float[] timeMarkers)`:
    ```csharp
    private void ActivateEventMarkers(float[] timeMarkers)
    {
        if (timeMarkers == null)
        {
            _activeEventMarkerCount = 0;
            return;
        }
        _activeEventMarkerCount = Mathf.Min(timeMarkers.Length,
            ChartVisualConfig.MaxEventTimingMarkers);

        for (int i = 0; i < _activeEventMarkerCount; i++)
        {
            _eventMarkerTimes[i] = timeMarkers[i];
            float x = NormalizedTimeToWorldX(timeMarkers[i], _chartLeft, _chartRight);

            _eventMarkerLines[i].positionCount = 2;
            _eventMarkerLines[i].SetPosition(0, new Vector3(x, _chartBottom, 0f));
            _eventMarkerLines[i].SetPosition(1, new Vector3(x, _chartTop, 0f));
            _eventMarkerLines[i].gameObject.SetActive(true);
            _eventMarkerLabels[i].gameObject.SetActive(true);

            // Position "!" label at top of marker
            PositionLabelAtWorldXY(_eventMarkerLabels[i], x, _chartTop);
        }

        // Hide unused markers
        for (int i = _activeEventMarkerCount;
             i < ChartVisualConfig.MaxEventTimingMarkers; i++)
        {
            _eventMarkerLines[i].gameObject.SetActive(false);
            _eventMarkerLabels[i].gameObject.SetActive(false);
        }
    }
    ```
  - [ ] `ActivateDirectionArrow(TipOverlayData overlay)`:
    ```csharp
    private void ActivateDirectionArrow(TipOverlayData overlay)
    {
        bool isUp = overlay.DirectionSign > 0;
        _directionArrowText.text = isUp ? "\u25B2" : "\u25BC";  // Triangle up / down
        _directionArrowText.color = isUp
            ? ColorPalette.WithAlpha(ColorPalette.Green, ChartVisualConfig.OverlayLabelAlpha)
            : ColorPalette.WithAlpha(ColorPalette.Red, ChartVisualConfig.OverlayLabelAlpha);
        _directionLabel.text = overlay.Label;  // "CLOSING UP" or "CLOSING DOWN"
        _directionLabel.color = _directionArrowText.color;
        _directionArrowText.gameObject.SetActive(true);
        _directionLabel.gameObject.SetActive(true);
        _directionActive = true;
    }
    ```
  - [ ] `UpdateVerticalLineYExtents()` and `UpdateSingleVerticalLineY()`:
    - These only update Y positions of the vertical lines (top/bottom of padded chart area)
    - Called in LateUpdate because padded bounds may shift
    ```csharp
    private void UpdateVerticalLineYExtents(LineRenderer[] lines, int count,
        float paddedBottom, float paddedTop)
    {
        for (int i = 0; i < count; i++)
        {
            if (!lines[i].gameObject.activeSelf) continue;
            float x = lines[i].GetPosition(0).x;  // X stays the same
            lines[i].SetPosition(0, new Vector3(x, paddedBottom, 0f));
            lines[i].SetPosition(1, new Vector3(x, paddedTop, 0f));
        }
    }
    ```
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 8: Implement label positioning helper (AC: 8)
  - [ ] Canvas-space label positioning via world-to-screen-to-canvas conversion:
    ```csharp
    /// <summary>
    /// Positions a UI Text label in canvas space at the given world Y coordinate,
    /// anchored to the left edge of the chart (for horizontal overlays).
    /// </summary>
    private void PositionLabelAtWorldY(Text label, float worldX, float worldY)
    {
        if (label == null || _canvasRect == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(new Vector3(worldX, worldY, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out Vector2 localPoint);

        var rt = label.GetComponent<RectTransform>();
        // Offset left labels inward so they don't clip chart edge
        rt.anchoredPosition = new Vector2(localPoint.x + 5f, localPoint.y);
    }

    /// <summary>
    /// Positions a UI Text label at an arbitrary world XY (for vertical marker tops).
    /// </summary>
    private void PositionLabelAtWorldXY(Text label, float worldX, float worldY)
    {
        if (label == null || _canvasRect == null) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(new Vector3(worldX, worldY, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out Vector2 localPoint);

        var rt = label.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(localPoint.x, localPoint.y + 10f);
    }
    ```
  - [ ] This follows the exact pattern from `ChartUI.UpdateCurrentPriceLabel()` (lines 109-148)
  - [ ] File: `Assets/Scripts/Runtime/Chart/TipOverlayRenderer.cs`

- [ ] Task 9: Create overlay GameObjects in ChartSetup (AC: 1, 9, 10, 11)
  - [ ] Open `Assets/Scripts/Setup/ChartSetup.cs`
  - [ ] After the existing `chartLineView.SetTradeVisuals(...)` call (line 123), add overlay creation:
    ```csharp
    // --- Tip Overlay GameObjects (Story 18.3) ---
    var tipOverlayParent = new GameObject("TipOverlays");
    tipOverlayParent.transform.SetParent(chartParent.transform);

    // Horizontal line overlays: Floor and Ceiling
    var floorLineGo = CreateLineRendererObject("TipFloorLine", tipOverlayParent.transform);
    ConfigureTipLine(floorLineGo, ChartVisualConfig.OverlayFloorColor,
        ChartVisualConfig.OverlayLineWidth, ChartVisualConfig.OverlayLineSortingOrder);
    floorLineGo.SetActive(false);

    var ceilingLineGo = CreateLineRendererObject("TipCeilingLine", tipOverlayParent.transform);
    ConfigureTipLine(ceilingLineGo, ChartVisualConfig.OverlayCeilingColor,
        ChartVisualConfig.OverlayLineWidth, ChartVisualConfig.OverlayLineSortingOrder);
    ceilingLineGo.SetActive(false);

    // Forecast band quad mesh
    var forecastBandGo = CreateQuadMeshObject("TipForecastBand", tipOverlayParent.transform,
        ChartVisualConfig.OverlayForecastColor, ChartVisualConfig.OverlayQuadSortingOrder);
    forecastBandGo.SetActive(false);

    // Time zone quad meshes: Dip and Peak
    var dipZoneGo = CreateQuadMeshObject("TipDipZone", tipOverlayParent.transform,
        ChartVisualConfig.OverlayDipZoneColor, ChartVisualConfig.OverlayQuadSortingOrder);
    dipZoneGo.SetActive(false);

    var peakZoneGo = CreateQuadMeshObject("TipPeakZone", tipOverlayParent.transform,
        ChartVisualConfig.OverlayPeakZoneColor, ChartVisualConfig.OverlayQuadSortingOrder);
    peakZoneGo.SetActive(false);

    // Reversal vertical line
    var reversalLineGo = CreateLineRendererObject("TipReversalLine", tipOverlayParent.transform);
    ConfigureTipLine(reversalLineGo, ChartVisualConfig.OverlayReversalColor,
        ChartVisualConfig.OverlayVerticalLineWidth, ChartVisualConfig.OverlayLineSortingOrder);
    reversalLineGo.SetActive(false);

    // Event timing marker pool (max 15)
    var eventMarkerParent = new GameObject("TipEventMarkers");
    eventMarkerParent.transform.SetParent(tipOverlayParent.transform);
    var eventMarkerLines = new LineRenderer[ChartVisualConfig.MaxEventTimingMarkers];
    for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
    {
        var markerGo = CreateLineRendererObject($"EventMarker_{i}",
            eventMarkerParent.transform);
        ConfigureTipLine(markerGo, ChartVisualConfig.OverlayEventMarkerColor,
            ChartVisualConfig.OverlayVerticalLineWidth,
            ChartVisualConfig.OverlayLineSortingOrder);
        markerGo.SetActive(false);
        eventMarkerLines[i] = markerGo.GetComponent<LineRenderer>();
    }
    ```
  - [ ] Create overlay labels on ChartCanvas (inside `CreateChartUI` or after it):
    ```csharp
    // Create tip overlay labels on ChartCanvas
    var floorLabelGo = CreateOverlayLabel("TipFloorLabel", canvasGo.transform,
        ChartVisualConfig.OverlayFloorColor);
    var ceilingLabelGo = CreateOverlayLabel("TipCeilingLabel", canvasGo.transform,
        ChartVisualConfig.OverlayCeilingColor);
    var forecastLabelGo = CreateOverlayLabel("TipForecastLabel", canvasGo.transform,
        ColorPalette.WithAlpha(new Color(0.4f, 0.3f, 0.8f, 1f),
            ChartVisualConfig.OverlayLabelAlpha));
    var dipZoneLabelGo = CreateOverlayLabel("TipDipZoneLabel", canvasGo.transform,
        ChartVisualConfig.OverlayDipZoneColor);
    var peakZoneLabelGo = CreateOverlayLabel("TipPeakZoneLabel", canvasGo.transform,
        ChartVisualConfig.OverlayPeakZoneColor);
    var reversalLabelGo = CreateOverlayLabel("TipReversalLabel", canvasGo.transform,
        ChartVisualConfig.OverlayReversalColor);

    // Event timing marker labels ("!" at top of each marker)
    var eventMarkerLabels = new Text[ChartVisualConfig.MaxEventTimingMarkers];
    for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
    {
        var labelGo = CreateOverlayLabel($"EventMarkerLabel_{i}",
            canvasGo.transform, ChartVisualConfig.OverlayEventMarkerColor);
        labelGo.GetComponent<Text>().text = "!";
        labelGo.SetActive(false);
        eventMarkerLabels[i] = labelGo.GetComponent<Text>();
    }

    // Direction arrow (right edge of chart)
    var arrowGo = CreateOverlayLabel("TipDirectionArrow", canvasGo.transform,
        ColorPalette.Green);
    var arrowText = arrowGo.GetComponent<Text>();
    arrowText.fontSize = 22;
    arrowText.alignment = TextAnchor.MiddleCenter;
    arrowGo.SetActive(false);

    var dirLabelGo = CreateOverlayLabel("TipDirectionLabel", canvasGo.transform,
        ColorPalette.Green);
    dirLabelGo.SetActive(false);
    ```
  - [ ] Add helper methods to ChartSetup:
    ```csharp
    private static void ConfigureTipLine(GameObject go, Color color,
        float width, int sortingOrder)
    {
        var lr = go.GetComponent<LineRenderer>();
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sortingOrder = sortingOrder;
        lr.alignment = LineAlignment.TransformZ;
    }

    private static GameObject CreateQuadMeshObject(string name, Transform parent,
        Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var mf = go.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        mesh.MarkDynamic();
        mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        mr.sharedMaterial = mat;
        mr.sortingOrder = sortingOrder;
        return go;
    }

    private static GameObject CreateOverlayLabel(string name, Transform parent,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 20f);
        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = (int)ChartVisualConfig.OverlayLabelFontSize;
        text.color = ColorPalette.WithAlpha(color, ChartVisualConfig.OverlayLabelAlpha);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        go.SetActive(false);
        return go;
    }
    ```
  - [ ] Wire TipOverlayRenderer with all created objects:
    ```csharp
    var tipOverlayRenderer = chartParent.AddComponent<TipOverlayRenderer>();
    tipOverlayRenderer.Initialize(
        chartRenderer, chartBounds, canvasGo.GetComponent<Canvas>(),
        floorLineGo.GetComponent<LineRenderer>(),
        ceilingLineGo.GetComponent<LineRenderer>(),
        forecastBandGo.GetComponent<MeshFilter>(),
        forecastBandGo.GetComponent<MeshRenderer>(),
        dipZoneGo.GetComponent<MeshFilter>(),
        dipZoneGo.GetComponent<MeshRenderer>(),
        peakZoneGo.GetComponent<MeshFilter>(),
        peakZoneGo.GetComponent<MeshRenderer>(),
        reversalLineGo.GetComponent<LineRenderer>(),
        eventMarkerLines,
        floorLabelGo.GetComponent<Text>(),
        ceilingLabelGo.GetComponent<Text>(),
        forecastLabelGo.GetComponent<Text>(),
        dipZoneLabelGo.GetComponent<Text>(),
        peakZoneLabelGo.GetComponent<Text>(),
        reversalLabelGo.GetComponent<Text>(),
        eventMarkerLabels,
        arrowText,
        dirLabelGo.GetComponent<Text>()
    );
    ```
  - [ ] File: `Assets/Scripts/Setup/ChartSetup.cs`

- [ ] Task 10: Write tests (AC: 12)
  - [ ] Create `Assets/Tests/Runtime/Chart/TipOverlayRendererTests.cs`
  - [ ] Test fixture setup:
    ```csharp
    using NUnit.Framework;
    using UnityEngine;

    namespace BullRun.Tests.Chart
    {
        [TestFixture]
        public class TipOverlayRendererTests
        {
            // Chart bounds matching typical test values
            private const float ChartLeft = -4f;
            private const float ChartRight = 4f;
            private const float ChartBottom = -2f;
            private const float ChartTop = 2f;

            // Padded bounds (10% padding on each side)
            private float PaddedBottom => ChartBottom + (ChartTop - ChartBottom) * 0.1f;
            private float PaddedTop => ChartTop - (ChartTop - ChartBottom) * 0.1f;
        }
    }
    ```
  - [ ] **Coordinate transform tests:**
    - [ ] `PriceToWorldY_MidPrice_ReturnsMidPaddedHeight` -- price at midpoint of range returns center of padded area
    - [ ] `PriceToWorldY_MinPrice_ReturnsPaddedBottom` -- price at min returns paddedBottom
    - [ ] `PriceToWorldY_MaxPrice_ReturnsPaddedTop` -- price at max returns paddedTop
    - [ ] `PriceToWorldY_ZeroPriceRange_ReturnsMidpoint` -- edge case where priceRange is 0
    - [ ] `NormalizedTimeToWorldX_Zero_ReturnsChartLeft`
    - [ ] `NormalizedTimeToWorldX_One_ReturnsChartRight`
    - [ ] `NormalizedTimeToWorldX_Half_ReturnsMidpoint`
    - [ ] `TimeZoneToWorldX_ClampedToChartBounds` -- zone extending beyond 0-1 is clamped
    - [ ] `TimeZoneToWorldX_FullWidth_SpansEntireChart` -- center 0.5 with halfWidth 0.5
  - [ ] **Overlay positioning tests (using static methods):**
    - [ ] `FloorLine_PriceAtMin_PositionedAtPaddedBottom`
    - [ ] `CeilingLine_PriceAtMax_PositionedAtPaddedTop`
    - [ ] `ForecastBand_CenteredOnPrice_SpansPlusMinusHalfWidth`
    - [ ] `EventMarker_AtNormalizedTime_XMatchesLerp`
  - [ ] **Edge case tests:**
    - [ ] `PriceToWorldY_PriceOutsideRange_ExtrapolatesCorrectly` -- price above max or below min
    - [ ] `TimeZoneToWorldX_ZeroHalfWidth_ReturnsPointWidth` -- degenerate zone
    - [ ] `EventMarkerCount_CappedAtMax15` -- if more than 15 markers passed, only 15 used
  - [ ] File: `Assets/Tests/Runtime/Chart/TipOverlayRendererTests.cs`

## Dev Notes

### Architecture Compliance

- **No ScriptableObjects:** All overlay configuration (colors, widths, sorting orders) lives as `public static readonly` in `ChartVisualConfig.cs`. No Inspector-configured values.
- **Assembly boundary:** `TipOverlayRenderer` is in `Scripts/Runtime/Chart/` (Runtime assembly). It references `TipOverlayData` from `Scripts/Runtime/Shop/StoreDataTypes.cs` (also Runtime). No editor references.
- **EventBus pattern:** Subscribes to `TipOverlaysActivatedEvent`, `RoundStartedEvent`, `ShopOpenedEvent`, `RoundTimerExtendedEvent`. Never references TipActivator, TradingState, or ShopState directly.
- **Setup-oriented creation:** All overlay GameObjects are created by `ChartSetup.Execute()` during F5 rebuild. `TipOverlayRenderer` receives references via its `Initialize()` method, following the same pattern as `ChartLineView.Initialize()` and `ChartLineView.SetTradeVisuals()`.
- **No .meta files:** One new `.cs` file (`TipOverlayRenderer.cs`) and one new test file. Unity auto-generates `.meta` files.

### Coordinate Transform Formulas (CRITICAL)

These MUST exactly match `ChartLineView.LateUpdate()` (lines 116-196):

```
// Padded bounds (10% padding):
float chartHeight = _chartTop - _chartBottom;
float padding = chartHeight * 0.1f;
float paddedBottom = _chartBottom + padding;
float paddedTop = _chartTop - padding;

// Price -> Y (world space):
float y = Mathf.Lerp(paddedBottom, paddedTop, (price - minPrice) / priceRange);

// Normalized time -> X (world space):
float x = Mathf.Lerp(_chartLeft, _chartRight, normalizedTime);

// Minimum price range enforcement:
float center = (minPrice + maxPrice) * 0.5f;
float minRange = center * 0.05f;
if (minRange < 0.01f) minRange = 0.01f;
if (priceRange < minRange)
{
    minPrice = center - minRange * 0.5f;
    priceRange = minRange;
}
```

### Overlay Sorting Order Stack

From back to front:

| Layer | sortingOrder | What |
|-------|-------------|------|
| Tip quad meshes (forecast band, dip/peak zones) | -3 | Farthest back -- transparent fills |
| Gridlines | -2 | Existing (from FIX-8) |
| Glow trail | -1 | Existing |
| Tip horizontal lines (floor, ceiling) | 0 | Between gridlines and main line |
| Tip vertical lines (event markers, reversal) | 0 | Same layer as horizontal tip lines |
| Main price line | 1 | Existing |
| Break-even line | 2 | Existing |
| Short position line | 2 | Existing |
| Trade markers (buy/sell dots) | 5 | Existing |
| Direction arrow | 6 | Frontmost overlay |
| ChartCanvas (all labels) | 10 | ScreenSpaceOverlay canvas (always on top) |

### Performance Requirements

- **No per-frame allocations:** All GameObjects, Meshes, Materials, and Text elements are pre-created during ChartSetup. Mesh vertex arrays use the reusable `_quadVerts` field. LineRenderer positions are set in-place. No `new` calls in `LateUpdate()`.
- **Conditional updates:** Only update active overlays (check `_xxxActive` flags before doing work).
- **X-axis optimization:** Time-based overlay X positions (event markers, time zones, reversal line) only update when `_cachedRoundDuration` changes (on `RoundTimerExtendedEvent`), not every frame. Only Y extents update per frame.
- **Mesh.MarkDynamic():** Called once at mesh creation for quad meshes that rebuild every frame.

### Canvas Label Positioning Pattern

Labels live on the existing `ChartCanvas` (ScreenSpaceOverlay, sortingOrder=10). To position a label at a world-space point, use the same world-to-screen-to-canvas pattern from `ChartUI.UpdateCurrentPriceLabel()`:

```csharp
Vector3 screenPos = cam.WorldToScreenPoint(new Vector3(worldX, worldY, 0f));
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    _canvasRect, screenPos, null, out Vector2 localPoint);
rt.anchoredPosition = localPoint;
```

### Label Placement Strategy

- **Horizontal overlay labels** (Floor, Ceiling, Forecast): Left edge of chart, at the overlay's Y position. Offset 5px right so text doesn't clip.
- **Time zone labels** (Dip Zone, Peak Zone): Top of chart, centered within the zone's X span.
- **Vertical marker labels** (Event "!", Reversal "R"): Top of chart, at the marker's X position. Offset 10px above so they clear the chart edge.
- **Direction arrow**: Right edge of chart, vertically centered. Arrow character above, text label below.

### ChartSetup Integration Point

The new overlay creation code goes in `ChartSetup.Execute()` between the existing trade visuals wiring (line 123) and the chart UI creation (line 126). Overlay labels must be created inside the `CreateChartUI()` method because they need the `canvasGo` reference. Alternative: create them after `CreateChartUI()` returns, using the canvas reference from the ChartUI component.

The recommended approach is to:
1. Create world-space overlay objects (LineRenderers, quad meshes) in `Execute()` after trade visuals
2. Create canvas-space labels in `CreateChartUI()` or after it returns
3. Wire everything to `TipOverlayRenderer` at the end of `Execute()`

### Quad Mesh Material

Each quad overlay (forecast band, dip zone, peak zone) needs its own Material instance because they have different colors. Use `Sprites/Default` shader (same as the existing line material). Color is set on the material, not per-vertex, because the entire quad is a single solid color.

### Missing ColorPalette.Magenta

The `ColorPalette` class does not currently define a `Magenta` color. Add it for the Trend Reversal overlay:
```csharp
public static readonly Color Magenta = new Color(180 / 255f, 80 / 255f, 200 / 255f, 1f);  // #b450c8
```

### Existing Code to Read Before Implementing

Read these files COMPLETELY before making any changes:

1. `Assets/Scripts/Runtime/Chart/ChartLineView.cs` -- LateUpdate coordinate transforms (lines 116-197), UpdateBreakEvenLine pattern (lines 293-312), Initialize/SetTradeVisuals patterns
2. `Assets/Scripts/Setup/ChartSetup.cs` -- CreateLineRendererObject (lines 196-218), CreateMeshObject (lines 173-194), full Execute() flow, CreateChartUI() label creation pattern
3. `Assets/Scripts/Runtime/Chart/ChartRenderer.cs` -- GetLivePriceRange (lines 60-75), RoundDuration, PointCount
4. `Assets/Scripts/Runtime/Chart/ChartUI.cs` -- UpdateCurrentPriceLabel world-to-canvas positioning (lines 109-148), CreateChartUI label creation pattern
5. `Assets/Scripts/Runtime/Chart/ChartVisualConfig.cs` -- existing config structure
6. `Assets/Scripts/Runtime/Chart/ChartMeshLine.cs` -- procedural mesh pattern (reference for quad mesh creation)
7. `Assets/Scripts/Setup/Data/ColorPalette.cs` -- available colors, WithAlpha utility
8. `Assets/Scripts/Runtime/Core/GameEvents.cs` -- existing event patterns, RoundStartedEvent, ShopOpenedEvent, RoundTimerExtendedEvent
9. `Assets/Scripts/Runtime/Shop/StoreDataTypes.cs` -- TipOverlayData struct, InsiderTipType enum
10. `Assets/Tests/Runtime/Chart/ChartRendererTests.cs` -- existing test patterns and conventions

### Initialize Method Signature

The `Initialize()` method takes many parameters because all GameObjects are pre-created in ChartSetup. This follows the existing pattern (ChartLineView.Initialize takes 6 params, then SetTradeVisuals adds 3 more). Consider grouping related objects into a data struct if the parameter count becomes unwieldy:

```csharp
public struct TipOverlayObjects
{
    public LineRenderer FloorLine;
    public LineRenderer CeilingLine;
    public MeshFilter ForecastBandMeshFilter;
    public MeshRenderer ForecastBandRenderer;
    public MeshFilter DipZoneMeshFilter;
    public MeshRenderer DipZoneRenderer;
    public MeshFilter PeakZoneMeshFilter;
    public MeshRenderer PeakZoneRenderer;
    public LineRenderer ReversalLine;
    public LineRenderer[] EventMarkerLines;
    public Text FloorLabel;
    public Text CeilingLabel;
    public Text ForecastLabel;
    public Text DipZoneLabel;
    public Text PeakZoneLabel;
    public Text ReversalLabel;
    public Text[] EventMarkerLabels;
    public Text DirectionArrowText;
    public Text DirectionLabel;
}
```

### Depends On

- Story 18.1 (Tip Data Model) -- `TipOverlayData` struct, `InsiderTipType` enum with new values
- Story 18.2 (Tip Generation & Activation) -- `TipOverlaysActivatedEvent`, `TipActivator` that produces overlay data at round start

### References

- [Source: _bmad-output/planning-artifacts/epic-18-insider-tips-overhaul.md#Story 18.3]
- [Source: _bmad-output/implementation-artifacts/18-1-tip-data-model-type-overhaul.md]
- [Source: _bmad-output/implementation-artifacts/18-2-tip-generation-round-start-activation.md]
- [Source: _bmad-output/project-context.md#Performance Rules]
- [Source: _bmad-output/project-context.md#EventBus Communication]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6

### Debug Log References

N/A -- rendering component; visual debugging via in-editor inspection of overlay GameObjects.

### Completion Notes List

### File List

### Change Log

- 2026-02-21: Story 18.3 created -- comprehensive implementation guide for chart tip overlay rendering
