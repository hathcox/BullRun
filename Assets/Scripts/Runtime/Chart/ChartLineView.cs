using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour that drives procedural mesh visuals from ChartRenderer data.
/// Manages main line, glow trail, current price indicator, trade markers, and break-even line.
/// Created by ChartSetup during F5 rebuild.
/// </summary>
public class ChartLineView : MonoBehaviour
{
    private ChartRenderer _chartRenderer;
    private MeshFilter _mainMeshFilter;
    private MeshFilter _glowMeshFilter;
    private ChartMeshLine _mainMeshLine;
    private ChartMeshLine _glowMeshLine;
    private Transform _indicator;
    private SpriteRenderer _indicatorRenderer; // AC 10: cached to avoid per-frame GetComponent
    private ChartVisualConfig _config;

    // AC 10: Indicator pulse constants
    public static readonly float IndicatorPulseFrequency = 4f;
    public static readonly float IndicatorPulseMin = 0.4f;

    // Price gridlines (FIX-8)
    private LineRenderer[] _gridlines;

    // Break-even line, short position line, and trade markers
    private LineRenderer _breakEvenLine;
    private LineRenderer _shortPositionLine;
    private Transform _markerPool;
    private readonly List<GameObject> _markerObjects = new List<GameObject>();
    private Sprite _circleSprite;

    // Chart bounds in world space
    private float _chartLeft;
    private float _chartRight;
    private float _chartBottom;
    private float _chartTop;

    /// <summary>
    /// World-space position of the chart head (latest price point). Valid only when HasActiveChartHead is true.
    /// </summary>
    public Vector3 ChartHeadWorldPosition => _indicator != null ? _indicator.position : Vector3.zero;
    public bool HasActiveChartHead => _indicator != null && _indicator.gameObject.activeSelf;

    // Reusable position list to avoid allocations
    private readonly List<Vector3> _positionBuffer = new List<Vector3>();
    private readonly List<Color> _colorBuffer = new List<Color>();
    private readonly List<Color> _glowColorBuffer = new List<Color>();

    public void Initialize(ChartRenderer chartRenderer, MeshFilter mainMeshFilter, MeshFilter glowMeshFilter,
        Transform indicator, ChartVisualConfig config, Rect chartBounds)
    {
        _chartRenderer = chartRenderer;
        _mainMeshFilter = mainMeshFilter;
        _glowMeshFilter = glowMeshFilter;
        _indicator = indicator;
        _indicatorRenderer = indicator != null ? indicator.GetComponent<SpriteRenderer>() : null;
        _config = config;

        _chartLeft = chartBounds.xMin;
        _chartRight = chartBounds.xMax;
        _chartBottom = chartBounds.yMin;
        _chartTop = chartBounds.yMax;

        _mainMeshLine = new ChartMeshLine();
        _glowMeshLine = new ChartMeshLine();
        _mainMeshFilter.mesh = _mainMeshLine.Mesh;
        _glowMeshFilter.mesh = _glowMeshLine.Mesh;
    }

    /// <summary>
    /// Applies a new visual config (tier theme colors) at runtime.
    /// Updates line color, glow color, and price indicator color.
    /// </summary>
    public void ApplyTierTheme(ChartVisualConfig newConfig)
    {
        _config = newConfig;
        if (_indicatorRenderer != null)
            _indicatorRenderer.color = newConfig.LineUpColor;
    }

    public void SetGridlines(LineRenderer[] gridlines)
    {
        _gridlines = gridlines;
    }

    public void SetTradeVisuals(LineRenderer breakEvenLine, LineRenderer shortPositionLine, Transform markerPool)
    {
        _breakEvenLine = breakEvenLine;
        _shortPositionLine = shortPositionLine;
        _markerPool = markerPool;

        // Create procedural circle sprite for markers
        _circleSprite = CreateCircleSprite(16);
    }

    private static Sprite CreateCircleSprite(int resolution)
    {
        int size = resolution * 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = dist <= radius ? 1f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void LateUpdate()
    {
        if (_chartRenderer == null) return;

        int pointCount = _chartRenderer.PointCount;

        if (pointCount < 2)
        {
            _mainMeshLine.Clear();
            _glowMeshLine.Clear();
            if (_indicator != null) _indicator.gameObject.SetActive(false);
            HideGridlines();
            UpdateTradeMarkers(0f, 1f, _chartBottom, _chartTop);
            UpdateBreakEvenLine(0f, 1f, _chartBottom, _chartTop);
            UpdateShortPositionLine(0f, 1f, _chartBottom, _chartTop);
            return;
        }

        // Use live min/max from actual points (not all-time extremes)
        _chartRenderer.GetLivePriceRange(out float minPrice, out float maxPrice);
        float priceRange = maxPrice - minPrice;
        // Minimum range: 5% of current price (avoids axis snap when flat line transitions to movement)
        float center = (minPrice + maxPrice) * 0.5f;
        float minRange = center * 0.05f;
        if (minRange < 0.01f) minRange = 0.01f;
        if (priceRange < minRange)
        {
            minPrice = center - minRange * 0.5f;
            priceRange = minRange;
        }

        // 10% Y-axis padding so line never touches chart edges
        float chartHeight = _chartTop - _chartBottom;
        float padding = chartHeight * 0.1f;
        float paddedBottom = _chartBottom + padding;
        float paddedTop = _chartTop - padding;

        // Update price gridlines (FIX-8)
        UpdateGridlines(paddedBottom, paddedTop);

        // Build position list
        _positionBuffer.Clear();
        for (int i = 0; i < pointCount; i++)
        {
            var point = _chartRenderer.GetPoint(i);
            float normalizedTime = _chartRenderer.RoundDuration > 0f
                ? Mathf.Clamp01(point.ElapsedTime / _chartRenderer.RoundDuration) : 0f;
            float x = Mathf.Lerp(_chartLeft, _chartRight, normalizedTime);
            float y = Mathf.Lerp(paddedBottom, paddedTop, (point.Price - minPrice) / priceRange);
            _positionBuffer.Add(new Vector3(x, y, 0f));
        }

        // Build per-point directional color buffers
        _colorBuffer.Clear();
        _glowColorBuffer.Clear();
        for (int i = 0; i < pointCount; i++)
        {
            bool isUp = i == 0 || _positionBuffer[i].y >= _positionBuffer[i - 1].y;
            _colorBuffer.Add(isUp ? _config.LineUpColor : _config.LineDownColor);
            _glowColorBuffer.Add(isUp ? _config.GlowUpColor : _config.GlowDownColor);
        }

        // Convert pixel widths to world units
        float mainWidth = _config.GetWorldWidth(_config.LineWidthPixels);
        float glowWidth = _config.GetWorldWidth(_config.GlowWidthPixels);

        // Update meshes with per-point colors
        _mainMeshLine.UpdateMesh(_positionBuffer, mainWidth, _colorBuffer);
        _glowMeshLine.UpdateMesh(_positionBuffer, glowWidth, _glowColorBuffer);

        // Update indicator position and color to chart head
        if (_indicator != null)
        {
            _indicator.position = _positionBuffer[pointCount - 1];
            _indicator.gameObject.SetActive(true);

            // AC 10: Sinusoidal alpha pulse on the indicator SpriteRenderer
            if (_indicatorRenderer != null && _indicator.gameObject.activeSelf)
            {
                Color baseColor = _colorBuffer[pointCount - 1];
                float alpha = IndicatorPulseMin + (1f - IndicatorPulseMin) *
                    ((Mathf.Sin(Time.time * Mathf.PI * IndicatorPulseFrequency) + 1f) * 0.5f);
                baseColor.a = alpha;
                _indicatorRenderer.color = baseColor;
            }
        }

        // Update trade markers, break-even line, and short position line
        UpdateTradeMarkers(minPrice, priceRange, paddedBottom, paddedTop);
        UpdateBreakEvenLine(minPrice, priceRange, paddedBottom, paddedTop);
        UpdateShortPositionLine(minPrice, priceRange, paddedBottom, paddedTop);
    }

    private void UpdateGridlines(float paddedBottom, float paddedTop)
    {
        if (_gridlines == null) return;

        for (int i = 0; i < _gridlines.Length; i++)
        {
            if (_gridlines[i] == null) continue;

            float t = _gridlines.Length > 1 ? (float)i / (_gridlines.Length - 1) : 0f;
            float y = Mathf.Lerp(paddedBottom, paddedTop, t);

            _gridlines[i].positionCount = 2;
            _gridlines[i].SetPosition(0, new Vector3(_chartLeft, y, 0f));
            _gridlines[i].SetPosition(1, new Vector3(_chartRight, y, 0f));
            if (!_gridlines[i].gameObject.activeSelf)
                _gridlines[i].gameObject.SetActive(true);
        }
    }

    private void HideGridlines()
    {
        if (_gridlines == null) return;

        for (int i = 0; i < _gridlines.Length; i++)
        {
            if (_gridlines[i] != null)
            {
                _gridlines[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateTradeMarkers(float minPrice, float priceRange, float paddedBottom, float paddedTop)
    {
        if (_markerPool == null || _chartRenderer == null) return;

        var markers = _chartRenderer.TradeMarkers;
        int needed = markers.Count;

        // Create markers as needed
        while (_markerObjects.Count < needed)
        {
            var markerGo = new GameObject($"TradeMarker_{_markerObjects.Count}");
            markerGo.transform.SetParent(_markerPool);
            var sr = markerGo.AddComponent<SpriteRenderer>();
            sr.sprite = _circleSprite;
            sr.sortingOrder = 5;
            _markerObjects.Add(markerGo);
        }

        // Position and color active markers
        for (int i = 0; i < needed; i++)
        {
            var marker = markers[i];
            var go = _markerObjects[i];
            go.SetActive(true);

            float markerNormTime = _chartRenderer.RoundDuration > 0f
                ? Mathf.Clamp01(marker.ElapsedTime / _chartRenderer.RoundDuration) : 0f;
            float x = Mathf.Lerp(_chartLeft, _chartRight, markerNormTime);
            float y = Mathf.Lerp(paddedBottom, paddedTop, (marker.Price - minPrice) / priceRange);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(0.15f, 0.15f, 1f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = marker.IsShort
                ? (marker.IsBuy ? ColorPalette.Gold : ColorPalette.Amber)
                : (marker.IsBuy ? ColorPalette.Green : ColorPalette.Red);
        }

        // Hide unused markers
        for (int i = needed; i < _markerObjects.Count; i++)
        {
            _markerObjects[i].SetActive(false);
        }
    }

    /// <summary>
    /// Calculates evenly spaced Y world positions for gridlines between pre-padded chart bounds.
    /// Uses the same coordinate math as the runtime UpdateGridlines path.
    /// Callers must apply 10% padding to raw chart bounds before passing.
    /// Static for testability.
    /// </summary>
    public static float[] CalculateGridlineYPositions(float paddedBottom, float paddedTop, int count)
    {
        var positions = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0f;
            positions[i] = Mathf.Lerp(paddedBottom, paddedTop, t);
        }
        return positions;
    }

    private void UpdateBreakEvenLine(float minPrice, float priceRange, float paddedBottom, float paddedTop)
    {
        if (_breakEvenLine == null || _chartRenderer == null) return;

        if (_chartRenderer.HasOpenPosition && priceRange > 0f)
        {
            _breakEvenLine.gameObject.SetActive(true);
            float y = Mathf.Lerp(paddedBottom, paddedTop,
                (_chartRenderer.AverageBuyPrice - minPrice) / priceRange);

            _breakEvenLine.positionCount = 2;
            _breakEvenLine.SetPosition(0, new Vector3(_chartLeft, y, 0f));
            _breakEvenLine.SetPosition(1, new Vector3(_chartRight, y, 0f));
        }
        else
        {
            _breakEvenLine.gameObject.SetActive(false);
            _breakEvenLine.positionCount = 0;
        }
    }

    private void UpdateShortPositionLine(float minPrice, float priceRange, float paddedBottom, float paddedTop)
    {
        if (_shortPositionLine == null || _chartRenderer == null) return;

        if (_chartRenderer.HasShortPosition && priceRange > 0f)
        {
            _shortPositionLine.gameObject.SetActive(true);
            float y = Mathf.Lerp(paddedBottom, paddedTop,
                (_chartRenderer.ShortEntryPrice - minPrice) / priceRange);

            _shortPositionLine.positionCount = 2;
            _shortPositionLine.SetPosition(0, new Vector3(_chartLeft, y, 0f));
            _shortPositionLine.SetPosition(1, new Vector3(_chartRight, y, 0f));
        }
        else
        {
            _shortPositionLine.gameObject.SetActive(false);
            _shortPositionLine.positionCount = 0;
        }
    }
}
