using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data point on the chart: normalized time (0-1) and price value.
/// </summary>
public struct ChartPoint
{
    public float NormalizedTime;
    public float Price;
}

/// <summary>
/// Core chart rendering logic. Stores price points over time and tracks
/// price range for axis scaling. Subscribes to PriceUpdatedEvent for the
/// active stock only. LineRenderer visual updates handled by MonoBehaviour wrapper.
/// </summary>
public class ChartRenderer
{
    private readonly List<ChartPoint> _points = new List<ChartPoint>();
    private int _activeStockId = -1;
    private float _roundDuration;
    private float _elapsedTime;
    private bool _roundActive;
    private float _minPrice = float.MaxValue;
    private float _maxPrice = float.MinValue;
    private int _decimationThreshold = 3600; // Default: 60fps * 60s

    public int PointCount => _points.Count;
    public int ActiveStockId => _activeStockId;
    public float MinPrice => _minPrice;
    public float MaxPrice => _maxPrice;
    public float RoundDuration => _roundDuration;

    public float ElapsedTime => _elapsedTime;
    public float CurrentPrice => _points.Count > 0 ? _points[_points.Count - 1].Price : 0f;

    public ChartRenderer()
    {
        _roundDuration = GameConfig.RoundDurationSeconds;
    }

    public ChartPoint GetPoint(int index)
    {
        return _points[index];
    }

    public void AddPoint(float normalizedTime, float price)
    {
        _points.Add(new ChartPoint { NormalizedTime = normalizedTime, Price = price });

        if (price < _minPrice) _minPrice = price;
        if (price > _maxPrice) _maxPrice = price;

        if (_points.Count > _decimationThreshold)
        {
            DecimateOlderPoints();
        }
    }

    public void ResetChart()
    {
        _points.Clear();
        _minPrice = float.MaxValue;
        _maxPrice = float.MinValue;
        _elapsedTime = 0f;
        _roundActive = false;
    }

    public void SetActiveStock(int stockId)
    {
        _activeStockId = stockId;
        ResetChart();
    }

    public void SetRoundDuration(float duration)
    {
        _roundDuration = duration;
    }

    public void StartRound()
    {
        _elapsedTime = 0f;
        _roundActive = true;
    }

    public void SetElapsedTime(float elapsed)
    {
        _elapsedTime = elapsed;
    }

    /// <summary>
    /// Processes a PriceUpdatedEvent. Only adds a point if the event
    /// is for the currently active stock and a round is in progress.
    /// </summary>
    public void ProcessPriceUpdate(PriceUpdatedEvent evt)
    {
        if (evt.StockId != _activeStockId || !_roundActive)
            return;

        _elapsedTime += evt.DeltaTime;
        float normalizedTime = _roundDuration > 0f ? Mathf.Clamp01(_elapsedTime / _roundDuration) : 0f;

        AddPoint(normalizedTime, evt.NewPrice);
    }

    public void SetDecimationThreshold(int threshold)
    {
        _decimationThreshold = threshold;
    }

    /// <summary>
    /// Decimates older points by keeping every 3rd point in the older half of data.
    /// Preserves recent data at full resolution.
    /// </summary>
    private void DecimateOlderPoints()
    {
        int halfCount = _points.Count / 2;
        var decimated = new List<ChartPoint>(_points.Count);

        // Keep every 3rd point in the older half
        for (int i = 0; i < halfCount; i++)
        {
            if (i % 3 == 0)
            {
                decimated.Add(_points[i]);
            }
        }

        // Keep all recent points at full resolution
        for (int i = halfCount; i < _points.Count; i++)
        {
            decimated.Add(_points[i]);
        }

        _points.Clear();
        _points.AddRange(decimated);
    }
}
