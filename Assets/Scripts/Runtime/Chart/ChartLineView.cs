using UnityEngine;

/// <summary>
/// MonoBehaviour that drives LineRenderer visuals from ChartRenderer data.
/// Manages main line, glow trail, and current price indicator.
/// Created by ChartSetup during F5 rebuild.
/// </summary>
public class ChartLineView : MonoBehaviour
{
    private ChartRenderer _chartRenderer;
    private LineRenderer _mainLine;
    private LineRenderer _glowLine;
    private Transform _indicator;
    private ChartVisualConfig _config;

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

    private void ConfigureLineRenderer(LineRenderer lr, Color color, float width)
    {
        lr.startColor = color;
        lr.endColor = color;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.positionCount = 0;
    }

    private void LateUpdate()
    {
        if (_chartRenderer == null) return;

        int pointCount = _chartRenderer.PointCount;
        _mainLine.positionCount = pointCount;
        _glowLine.positionCount = pointCount;

        float minPrice = _chartRenderer.MinPrice;
        float maxPrice = _chartRenderer.MaxPrice;
        float priceRange = maxPrice - minPrice;

        // Add padding to prevent line from touching edges
        if (priceRange < 0.01f)
        {
            priceRange = 1f;
            minPrice = _chartRenderer.CurrentPrice - 0.5f;
        }

        for (int i = 0; i < pointCount; i++)
        {
            var point = _chartRenderer.GetPoint(i);
            float x = Mathf.Lerp(_chartLeft, _chartRight, point.NormalizedTime);
            float y = Mathf.Lerp(_chartBottom, _chartTop, (point.Price - minPrice) / priceRange);

            var worldPos = new Vector3(x, y, 0f);
            _mainLine.SetPosition(i, worldPos);
            _glowLine.SetPosition(i, worldPos);
        }

        // Update indicator position to chart head
        if (pointCount > 0 && _indicator != null)
        {
            var lastPoint = _chartRenderer.GetPoint(pointCount - 1);
            float ix = Mathf.Lerp(_chartLeft, _chartRight, lastPoint.NormalizedTime);
            float iy = Mathf.Lerp(_chartBottom, _chartTop, (lastPoint.Price - minPrice) / priceRange);
            _indicator.position = new Vector3(ix, iy, 0f);
            _indicator.gameObject.SetActive(true);
        }
        else if (_indicator != null)
        {
            _indicator.gameObject.SetActive(false);
        }
    }
}
