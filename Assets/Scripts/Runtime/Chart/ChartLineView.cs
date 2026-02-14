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
    private ChartVisualConfig _config;

    // Price gridlines (FIX-8)
    private LineRenderer[] _gridlines;

    // Break-even line and trade markers
    private LineRenderer _breakEvenLine;
    private Transform _markerPool;
    private readonly List<GameObject> _markerObjects = new List<GameObject>();
    private Sprite _circleSprite;

    // Chart bounds in world space
    private float _chartLeft;
    private float _chartRight;
    private float _chartBottom;
    private float _chartTop;

    // Reusable position list to avoid allocations
    private readonly List<Vector3> _positionBuffer = new List<Vector3>();

    public void Initialize(ChartRenderer chartRenderer, MeshFilter mainMeshFilter, MeshFilter glowMeshFilter,
        Transform indicator, ChartVisualConfig config, Rect chartBounds)
    {
        _chartRenderer = chartRenderer;
        _mainMeshFilter = mainMeshFilter;
        _glowMeshFilter = glowMeshFilter;
        _indicator = indicator;
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
        if (_indicator != null)
        {
            var sr = _indicator.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = newConfig.LineColor;
        }
    }

    public void SetGridlines(LineRenderer[] gridlines)
    {
        _gridlines = gridlines;
    }

    public void SetTradeVisuals(LineRenderer breakEvenLine, Transform markerPool)
    {
        _breakEvenLine = breakEvenLine;
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
            return;
        }

        // Use live min/max from actual points (not all-time extremes)
        _chartRenderer.GetLivePriceRange(out float minPrice, out float maxPrice);
        float priceRange = maxPrice - minPrice;
        if (priceRange < 0.01f)
        {
            float center = (minPrice + maxPrice) * 0.5f;
            minPrice = center - 0.5f;
            priceRange = 1f;
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
            float x = Mathf.Lerp(_chartLeft, _chartRight, point.NormalizedTime);
            float y = Mathf.Lerp(paddedBottom, paddedTop, (point.Price - minPrice) / priceRange);
            _positionBuffer.Add(new Vector3(x, y, 0f));
        }

        // Convert pixel widths to world units
        float mainWidth = _config.GetWorldWidth(_config.LineWidthPixels);
        float glowWidth = _config.GetWorldWidth(_config.GlowWidthPixels);

        // Update meshes
        _mainMeshLine.UpdateMesh(_positionBuffer, mainWidth, _config.LineColor);
        _glowMeshLine.UpdateMesh(_positionBuffer, glowWidth, _config.GlowColor);

        // Update indicator position to chart head
        if (_indicator != null)
        {
            _indicator.position = _positionBuffer[pointCount - 1];
            _indicator.gameObject.SetActive(true);
        }

        // Update trade markers and break-even line
        UpdateTradeMarkers(minPrice, priceRange, paddedBottom, paddedTop);
        UpdateBreakEvenLine(minPrice, priceRange, paddedBottom, paddedTop);
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

            float x = Mathf.Lerp(_chartLeft, _chartRight, marker.NormalizedTime);
            float y = Mathf.Lerp(paddedBottom, paddedTop, (marker.Price - minPrice) / priceRange);
            go.transform.position = new Vector3(x, y, 0f);
            go.transform.localScale = new Vector3(0.15f, 0.15f, 1f);

            var sr = go.GetComponent<SpriteRenderer>();
            sr.color = marker.IsBuy ? new Color(0f, 1f, 0.4f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);
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
}
