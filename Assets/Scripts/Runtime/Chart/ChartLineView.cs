using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoBehaviour that drives LineRenderer visuals from ChartRenderer data.
/// Manages main line, glow trail, current price indicator, trade markers, and break-even line.
/// Created by ChartSetup during F5 rebuild.
/// </summary>
public class ChartLineView : MonoBehaviour
{
    private ChartRenderer _chartRenderer;
    private LineRenderer _mainLine;
    private LineRenderer _glowLine;
    private Transform _indicator;
    private ChartVisualConfig _config;

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

    public void Initialize(ChartRenderer chartRenderer, LineRenderer mainLine, LineRenderer glowLine,
        Transform indicator, ChartVisualConfig config, Rect chartBounds)
    {
        _chartRenderer = chartRenderer;
        _mainLine = mainLine;
        _glowLine = glowLine;
        _indicator = indicator;
        _config = config;

        _chartLeft = chartBounds.xMin;
        _chartRight = chartBounds.xMax;
        _chartBottom = chartBounds.yMin;
        _chartTop = chartBounds.yMax;

        ConfigureLineRenderer(_mainLine, _config.LineColor, _config.LineWidth);
        ConfigureLineRenderer(_glowLine, _config.GlowColor, _config.GlowWidth);
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

    private void ConfigureLineRenderer(LineRenderer lr, Color color, float width)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        // Fixed facing direction — prevents billboard width variation in 2D
        lr.alignment = LineAlignment.TransformZ;
        // Smooth corners and caps to prevent pinching at sharp angles
        lr.numCornerVertices = 5;
        lr.numCapVertices = 3;
        // Flat width curve — no tapering
        lr.widthMultiplier = 1f;
    }

    private void LateUpdate()
    {
        if (_chartRenderer == null) return;

        int pointCount = _chartRenderer.PointCount;
        _mainLine.positionCount = pointCount;
        _glowLine.positionCount = pointCount;

        if (pointCount == 0) return;

        // Use live min/max from actual points (not all-time extremes)
        // This prevents one outlier from squishing the entire chart
        _chartRenderer.GetLivePriceRange(out float minPrice, out float maxPrice);
        float priceRange = maxPrice - minPrice;
        if (priceRange < 0.01f)
        {
            // Flat line — center it with some breathing room
            float center = (minPrice + maxPrice) * 0.5f;
            minPrice = center - 0.5f;
            priceRange = 1f;
        }

        // 10% Y-axis padding so line never touches chart edges
        float chartHeight = _chartTop - _chartBottom;
        float padding = chartHeight * 0.1f;
        float paddedBottom = _chartBottom + padding;
        float paddedTop = _chartTop - padding;

        for (int i = 0; i < pointCount; i++)
        {
            var point = _chartRenderer.GetPoint(i);
            float x = Mathf.Lerp(_chartLeft, _chartRight, point.NormalizedTime);
            float y = Mathf.Lerp(paddedBottom, paddedTop, (point.Price - minPrice) / priceRange);

            var worldPos = new Vector3(x, y, 0f);
            _mainLine.SetPosition(i, worldPos);
            _glowLine.SetPosition(i, worldPos);
        }

        // Update indicator position to chart head
        if (pointCount > 0 && _indicator != null)
        {
            var lastPoint = _chartRenderer.GetPoint(pointCount - 1);
            float ix = Mathf.Lerp(_chartLeft, _chartRight, lastPoint.NormalizedTime);
            float iy = Mathf.Lerp(paddedBottom, paddedTop, (lastPoint.Price - minPrice) / priceRange);
            _indicator.position = new Vector3(ix, iy, 0f);
            _indicator.gameObject.SetActive(true);
        }
        else if (_indicator != null)
        {
            _indicator.gameObject.SetActive(false);
        }

        // Update trade markers and break-even line
        UpdateTradeMarkers(minPrice, priceRange, paddedBottom, paddedTop);
        UpdateBreakEvenLine(minPrice, priceRange, paddedBottom, paddedTop);
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
