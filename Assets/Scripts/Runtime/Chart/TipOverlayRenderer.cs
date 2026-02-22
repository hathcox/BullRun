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
    private Text _forecastLabel;
    private float _forecastCenter;
    private float _forecastHalfWidth;
    private bool _forecastActive;

    // -- Vertical marker overlays (Event Timing) --
    private LineRenderer[] _eventMarkerLines;
    private Text[] _eventMarkerLabels;
    private float[] _eventMarkerTimes;
    private int _activeEventMarkerCount;

    // -- Time zone overlays (Dip, Peak) --
    private MeshFilter _dipZoneMeshFilter;
    private Text _dipZoneLabel;
    private float _dipZoneCenter;
    private float _dipZoneHalfWidth;
    private bool _dipZoneActive;

    private MeshFilter _peakZoneMeshFilter;
    private Text _peakZoneLabel;
    private float _peakZoneCenter;
    private float _peakZoneHalfWidth;
    private bool _peakZoneActive;

    // -- Trend Reversal marker --
    private LineRenderer _reversalLine;
    private Text _reversalLabel;
    private float _reversalTime;
    private bool _reversalActive;

    // -- Direction arrow --
    private Text _directionArrowText;
    private Text _directionLabel;
    private bool _directionActive;

    // Cached camera reference (avoid per-frame Camera.main lookups)
    private Camera _cachedCamera;
    private bool _subscribed;

    // Reusable mesh data (avoid per-frame allocation)
    private Mesh _forecastMesh;
    private Mesh _dipZoneMesh;
    private Mesh _peakZoneMesh;
    private readonly List<Vector3> _quadVerts = new List<Vector3>(4) { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };
    private static readonly int[] QuadTriangles = { 0, 1, 2, 2, 3, 0 };

    public void Initialize(
        ChartRenderer chartRenderer, Rect chartBounds, RectTransform canvasRect,
        LineRenderer floorLine, LineRenderer ceilingLine,
        MeshFilter forecastBandMeshFilter,
        MeshFilter dipZoneMeshFilter,
        MeshFilter peakZoneMeshFilter,
        LineRenderer reversalLine,
        LineRenderer[] eventMarkerLines,
        Text floorLabel, Text ceilingLabel, Text forecastLabel,
        Text dipZoneLabel, Text peakZoneLabel, Text reversalLabel,
        Text[] eventMarkerLabels,
        Text directionArrowText, Text directionLabel)
    {
        _chartRenderer = chartRenderer;
        _chartLeft = chartBounds.xMin;
        _chartRight = chartBounds.xMax;
        _chartBottom = chartBounds.yMin;
        _chartTop = chartBounds.yMax;
        _canvasRect = canvasRect;

        _floorLine = floorLine;
        _ceilingLine = ceilingLine;
        _forecastBandMeshFilter = forecastBandMeshFilter;
        _dipZoneMeshFilter = dipZoneMeshFilter;
        _peakZoneMeshFilter = peakZoneMeshFilter;
        _reversalLine = reversalLine;
        _eventMarkerLines = eventMarkerLines;

        _floorLabel = floorLabel;
        _ceilingLabel = ceilingLabel;
        _forecastLabel = forecastLabel;
        _dipZoneLabel = dipZoneLabel;
        _peakZoneLabel = peakZoneLabel;
        _reversalLabel = reversalLabel;
        _eventMarkerLabels = eventMarkerLabels;
        _directionArrowText = directionArrowText;
        _directionLabel = directionLabel;

        _eventMarkerTimes = new float[ChartVisualConfig.MaxEventTimingMarkers];

        // Pre-create meshes for quad overlays (set triangles once, only update vertices per frame)
        var zeroVerts = new Vector3[4];
        _forecastMesh = new Mesh();
        _forecastMesh.MarkDynamic();
        _forecastMesh.vertices = zeroVerts;
        _forecastMesh.triangles = QuadTriangles;
        _forecastBandMeshFilter.mesh = _forecastMesh;

        _dipZoneMesh = new Mesh();
        _dipZoneMesh.MarkDynamic();
        _dipZoneMesh.vertices = zeroVerts;
        _dipZoneMesh.triangles = QuadTriangles;
        _dipZoneMeshFilter.mesh = _dipZoneMesh;

        _peakZoneMesh = new Mesh();
        _peakZoneMesh.MarkDynamic();
        _peakZoneMesh.vertices = zeroVerts;
        _peakZoneMesh.triangles = QuadTriangles;
        _peakZoneMeshFilter.mesh = _peakZoneMesh;

        _cachedCamera = Camera.main;

        EnsureSubscribed();
    }

    private void EnsureSubscribed()
    {
        if (_subscribed) return;
        _subscribed = true;
        EventBus.Subscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ShopOpenedEvent>(OnShopOpened);
        // RoundTimerExtendedEvent not needed: overlay positions use normalized times (0-1)
        // independent of round duration, so timer extensions don't affect overlay placement.
    }

    private void OnEnable()
    {
        EnsureSubscribed();
    }

    private void OnDisable()
    {
        if (!_subscribed) return;
        _subscribed = false;
        EventBus.Unsubscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<ShopOpenedEvent>(OnShopOpened);
    }

    // --- Event Handlers (Task 3) ---

    private void OnTipOverlaysActivated(TipOverlaysActivatedEvent evt)
    {
        ClearAllOverlays();

        if (evt.Overlays == null) return;

        for (int i = 0; i < evt.Overlays.Count; i++)
        {
            var overlay = evt.Overlays[i];
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
                        PositionVerticalLine(_reversalLine, _reversalTime);
                    }
                    break;
                case InsiderTipType.ClosingDirection:
                    ActivateDirectionArrow(overlay);
                    break;
            }
        }
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        ClearAllOverlays();
    }

    private void OnShopOpened(ShopOpenedEvent evt)
    {
        ClearAllOverlays();
    }

    private void ClearAllOverlays()
    {
        if (_floorLine != null) _floorLine.gameObject.SetActive(false);
        if (_ceilingLine != null) _ceilingLine.gameObject.SetActive(false);
        if (_forecastBandMeshFilter != null) _forecastBandMeshFilter.gameObject.SetActive(false);
        if (_dipZoneMeshFilter != null) _dipZoneMeshFilter.gameObject.SetActive(false);
        if (_peakZoneMeshFilter != null) _peakZoneMeshFilter.gameObject.SetActive(false);
        if (_reversalLine != null) _reversalLine.gameObject.SetActive(false);
        if (_directionArrowText != null) _directionArrowText.gameObject.SetActive(false);
        if (_directionLabel != null) _directionLabel.gameObject.SetActive(false);

        if (_floorLabel != null) _floorLabel.gameObject.SetActive(false);
        if (_ceilingLabel != null) _ceilingLabel.gameObject.SetActive(false);
        if (_forecastLabel != null) _forecastLabel.gameObject.SetActive(false);
        if (_dipZoneLabel != null) _dipZoneLabel.gameObject.SetActive(false);
        if (_peakZoneLabel != null) _peakZoneLabel.gameObject.SetActive(false);
        if (_reversalLabel != null) _reversalLabel.gameObject.SetActive(false);

        if (_eventMarkerLines != null)
        {
            for (int i = 0; i < ChartVisualConfig.MaxEventTimingMarkers; i++)
            {
                if (_eventMarkerLines[i] != null) _eventMarkerLines[i].gameObject.SetActive(false);
                if (_eventMarkerLabels != null && _eventMarkerLabels[i] != null)
                    _eventMarkerLabels[i].gameObject.SetActive(false);
            }
        }

        _floorActive = false;
        _ceilingActive = false;
        _forecastActive = false;
        _dipZoneActive = false;
        _peakZoneActive = false;
        _reversalActive = false;
        _directionActive = false;
        _activeEventMarkerCount = 0;
    }

    // --- LateUpdate Repositioning (Task 4) ---

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

        if (_floorActive)
            UpdateHorizontalLine(_floorLine, _floorLabel, _floorPrice,
                minPrice, priceRange, paddedBottom, paddedTop);
        if (_ceilingActive)
            UpdateHorizontalLine(_ceilingLine, _ceilingLabel, _ceilingPrice,
                minPrice, priceRange, paddedBottom, paddedTop);
        if (_forecastActive)
            UpdateForecastBand(minPrice, priceRange, paddedBottom, paddedTop);
        if (_dipZoneActive)
            UpdateTimeZoneY(_dipZoneMeshFilter, _dipZoneMesh, _dipZoneCenter,
                _dipZoneHalfWidth, paddedBottom, paddedTop);
        if (_peakZoneActive)
            UpdateTimeZoneY(_peakZoneMeshFilter, _peakZoneMesh, _peakZoneCenter,
                _peakZoneHalfWidth, paddedBottom, paddedTop);
        if (_activeEventMarkerCount > 0)
            UpdateVerticalLineYExtents(_eventMarkerLines, _activeEventMarkerCount,
                paddedBottom, paddedTop);
        if (_reversalActive)
            UpdateSingleVerticalLineY(_reversalLine, paddedBottom, paddedTop);
    }

    private void UpdateHorizontalLine(LineRenderer lr, Text label, float price,
        float minPrice, float priceRange, float paddedBottom, float paddedTop)
    {
        float y = PriceToWorldY(price, minPrice, priceRange, paddedBottom, paddedTop);
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(_chartLeft, y, 0f));
        lr.SetPosition(1, new Vector3(_chartRight, y, 0f));
        PositionLabelAtWorldY(label, _chartLeft, y);
    }

    // --- Coordinate Transform Helpers (Task 5) ---

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
        return Mathf.LerpUnclamped(paddedBottom, paddedTop, normalized);
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

    // --- Quad Mesh Overlays (Task 6) ---

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

    private void UpdateTimeZoneY(MeshFilter mf, Mesh mesh, float zoneCenter,
        float zoneHalfWidth, float paddedBottom, float paddedTop)
    {
        var (xLeft, xRight) = TimeZoneToWorldX(zoneCenter, zoneHalfWidth,
            _chartLeft, _chartRight);
        BuildQuadMesh(mesh, xLeft, xRight, paddedBottom, paddedTop);
    }

    private void BuildQuadMesh(Mesh mesh, float left, float right,
        float bottom, float top)
    {
        _quadVerts[0] = new Vector3(left, bottom, 0f);
        _quadVerts[1] = new Vector3(left, top, 0f);
        _quadVerts[2] = new Vector3(right, top, 0f);
        _quadVerts[3] = new Vector3(right, bottom, 0f);

        // SetVertices(List) avoids allocation; triangles set once in Initialize
        mesh.SetVertices(_quadVerts);
    }

    // --- Vertical Marker Overlays and Direction Arrow (Task 7) ---

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

            PositionLabelAtWorldXY(_eventMarkerLabels[i], x, _chartTop);
        }

        for (int i = _activeEventMarkerCount;
             i < ChartVisualConfig.MaxEventTimingMarkers; i++)
        {
            _eventMarkerLines[i].gameObject.SetActive(false);
            _eventMarkerLabels[i].gameObject.SetActive(false);
        }
    }

    private void ActivateTimeZone(TipOverlayData overlay, ref float zoneCenter,
        ref float zoneHalfWidth, ref bool zoneActive, MeshFilter mf, Text label)
    {
        zoneCenter = overlay.TimeZoneCenter;
        zoneHalfWidth = overlay.TimeZoneHalfWidth;
        label.text = overlay.Label;
        mf.gameObject.SetActive(true);
        label.gameObject.SetActive(true);
        zoneActive = true;

        // Position label at top of zone, centered on X
        float centerX = NormalizedTimeToWorldX(zoneCenter, _chartLeft, _chartRight);
        PositionLabelAtWorldXY(label, centerX, _chartTop);
    }

    private void ActivateDirectionArrow(TipOverlayData overlay)
    {
        bool isUp = overlay.DirectionSign > 0;
        _directionArrowText.text = isUp ? "\u25B2" : "\u25BC";
        _directionArrowText.color = isUp
            ? ColorPalette.WithAlpha(ColorPalette.Green, ChartVisualConfig.OverlayLabelAlpha)
            : ColorPalette.WithAlpha(ColorPalette.Red, ChartVisualConfig.OverlayLabelAlpha);
        _directionLabel.text = overlay.Label;
        _directionLabel.color = _directionArrowText.color;
        _directionArrowText.gameObject.SetActive(true);
        _directionLabel.gameObject.SetActive(true);
        _directionActive = true;

        // Position inward from right edge to avoid overlap with Y-axis labels
        float midY = (_chartTop + _chartBottom) * 0.5f;
        float arrowX = _chartRight - (_chartRight - _chartLeft) * 0.04f;
        PositionLabelAtWorldXY(_directionArrowText, arrowX, midY + 0.1f);
        PositionLabelAtWorldXY(_directionLabel, arrowX, midY - 0.1f);
    }

    private void PositionVerticalLine(LineRenderer lr, float normalizedTime)
    {
        float x = NormalizedTimeToWorldX(normalizedTime, _chartLeft, _chartRight);
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(x, _chartBottom, 0f));
        lr.SetPosition(1, new Vector3(x, _chartTop, 0f));
    }

    private void UpdateVerticalLineYExtents(LineRenderer[] lines, int count,
        float paddedBottom, float paddedTop)
    {
        for (int i = 0; i < count; i++)
        {
            if (!lines[i].gameObject.activeSelf) continue;
            float x = lines[i].GetPosition(0).x;
            lines[i].SetPosition(0, new Vector3(x, paddedBottom, 0f));
            lines[i].SetPosition(1, new Vector3(x, paddedTop, 0f));
        }
    }

    private void UpdateSingleVerticalLineY(LineRenderer lr, float paddedBottom, float paddedTop)
    {
        if (!lr.gameObject.activeSelf) return;
        float x = lr.GetPosition(0).x;
        lr.SetPosition(0, new Vector3(x, paddedBottom, 0f));
        lr.SetPosition(1, new Vector3(x, paddedTop, 0f));
    }

    // --- Label Positioning Helpers (Task 8) ---

    private void PositionLabelAtWorldY(Text label, float worldX, float worldY)
    {
        if (label == null || _canvasRect == null) return;
        if (_cachedCamera == null) _cachedCamera = Camera.main;
        if (_cachedCamera == null) return;

        Vector3 screenPos = _cachedCamera.WorldToScreenPoint(new Vector3(worldX, worldY, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out Vector2 localPoint);

        label.rectTransform.anchoredPosition = new Vector2(localPoint.x + 5f, localPoint.y);
    }

    private void PositionLabelAtWorldXY(Text label, float worldX, float worldY)
    {
        if (label == null || _canvasRect == null) return;
        if (_cachedCamera == null) _cachedCamera = Camera.main;
        if (_cachedCamera == null) return;

        Vector3 screenPos = _cachedCamera.WorldToScreenPoint(new Vector3(worldX, worldY, 0f));
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, null, out Vector2 localPoint);

        label.rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y + 10f);
    }
}
