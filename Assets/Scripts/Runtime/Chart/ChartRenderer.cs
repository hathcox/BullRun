using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data point on the chart: elapsed time (seconds) and price value.
/// Normalization to 0-1 happens at render time using ChartRenderer.RoundDuration.
/// </summary>
public struct ChartPoint
{
    public float ElapsedTime;
    public float Price;
}

/// <summary>
/// Records a trade execution point on the chart for visual markers.
/// ElapsedTime stored in seconds; normalized at render time.
/// </summary>
public struct TradeMarker
{
    public float ElapsedTime;
    public float Price;
    public bool IsBuy;
    public bool IsShort;
}

/// <summary>
/// Core chart rendering logic. Stores price points over time and tracks
/// price range for axis scaling. Subscribes to PriceUpdatedEvent for the
/// active stock only. LineRenderer visual updates handled by MonoBehaviour wrapper.
/// </summary>
public class ChartRenderer
{
    private readonly List<ChartPoint> _points = new List<ChartPoint>();
    private readonly List<TradeMarker> _tradeMarkers = new List<TradeMarker>();
    private float _averageBuyPrice;
    private int _totalSharesBought;
    private bool _hasOpenPosition;
    private float _shortEntryPrice;
    private bool _hasShortPosition;
    private int _activeStockId = -1;
    private float _roundDuration;
    private float _targetRoundDuration;
    private float _elapsedTime;
    private bool _roundActive;
    private float _minPrice = float.MaxValue;
    private float _maxPrice = float.MinValue;

    // Exponential lerp speed for duration tween (~98% in 0.5s)
    private const float DurationLerpSpeed = 8f;

    public int PointCount => _points.Count;
    public int ActiveStockId => _activeStockId;
    public float MinPrice => _minPrice;
    public float MaxPrice => _maxPrice;

    /// <summary>
    /// Computes live min/max from current points. Use this instead of MinPrice/MaxPrice
    /// for rendering, since those track all-time extremes and never contract.
    /// </summary>
    public void GetLivePriceRange(out float liveMin, out float liveMax)
    {
        liveMin = float.MaxValue;
        liveMax = float.MinValue;
        for (int i = 0; i < _points.Count; i++)
        {
            float p = _points[i].Price;
            if (p < liveMin) liveMin = p;
            if (p > liveMax) liveMax = p;
        }
        if (_points.Count == 0)
        {
            liveMin = 0f;
            liveMax = 0f;
        }
    }
    public float RoundDuration => _roundDuration;

    public float ElapsedTime => _elapsedTime;
    public float CurrentPrice => _points.Count > 0 ? _points[_points.Count - 1].Price : 0f;
    public List<TradeMarker> TradeMarkers => _tradeMarkers;
    public float AverageBuyPrice => _averageBuyPrice;
    public bool HasOpenPosition => _hasOpenPosition;
    public float ShortEntryPrice => _shortEntryPrice;
    public bool HasShortPosition => _hasShortPosition;

    public ChartRenderer()
    {
        _roundDuration = GameConfig.RoundDurationSeconds;
        _targetRoundDuration = _roundDuration;
    }

    public ChartPoint GetPoint(int index)
    {
        return _points[index];
    }

    public void AddPoint(float elapsedTime, float price)
    {
        _points.Add(new ChartPoint { ElapsedTime = elapsedTime, Price = price });

        if (price < _minPrice) _minPrice = price;
        if (price > _maxPrice) _maxPrice = price;
    }

    public void ResetChart()
    {
        _points.Clear();
        _minPrice = float.MaxValue;
        _maxPrice = float.MinValue;
        _elapsedTime = 0f;
        _roundActive = false;
        _tradeMarkers.Clear();
        _averageBuyPrice = 0f;
        _totalSharesBought = 0;
        _hasOpenPosition = false;
        _shortEntryPrice = 0f;
        _hasShortPosition = false;
    }

    public void ProcessTrade(TradeExecutedEvent evt)
    {
        if (!_roundActive) return;

        // Only record markers for the stock currently displayed on the chart
        // TradeExecutedEvent.StockId is a string (from TradeExecutor), activeStockId is int
        if (evt.StockId != _activeStockId.ToString()) return;

        _tradeMarkers.Add(new TradeMarker
        {
            ElapsedTime = _elapsedTime,
            Price = evt.Price,
            IsBuy = evt.IsBuy,
            IsShort = evt.IsShort
        });

        if (evt.IsBuy && !evt.IsShort)
        {
            // Update running average buy price
            float totalCost = _averageBuyPrice * _totalSharesBought + evt.Price * evt.Shares;
            _totalSharesBought += evt.Shares;
            _averageBuyPrice = _totalSharesBought > 0 ? totalCost / _totalSharesBought : 0f;
            _hasOpenPosition = true;
        }
        else if (!evt.IsBuy && !evt.IsShort)
        {
            // Reduce share count; only clear position when fully sold
            _totalSharesBought -= evt.Shares;
            if (_totalSharesBought <= 0)
            {
                _totalSharesBought = 0;
                _averageBuyPrice = 0f;
                _hasOpenPosition = false;
            }
        }
        else if (!evt.IsBuy && evt.IsShort)
        {
            // Short opened — record entry price
            _shortEntryPrice = evt.Price;
            _hasShortPosition = true;
        }
        else if (evt.IsBuy && evt.IsShort)
        {
            // Short covered — clear short position
            _shortEntryPrice = 0f;
            _hasShortPosition = false;
        }
    }

    public void SetActiveStock(int stockId)
    {
        bool wasRoundActive = _roundActive;
        float savedElapsed = _elapsedTime;
        _activeStockId = stockId;
        ResetChart();
        // Preserve round state — switching stocks mid-round must not kill the chart
        if (wasRoundActive)
        {
            _roundActive = true;
            _elapsedTime = savedElapsed;
        }
    }

    /// <summary>
    /// Sets the active stock ID without resetting the chart.
    /// Used by MarketOpenEvent to avoid clearing roundActive state.
    /// </summary>
    public void SetActiveStockId(int stockId)
    {
        _activeStockId = stockId;
    }

    public void SetRoundDuration(float duration)
    {
        _roundDuration = duration;
        _targetRoundDuration = duration;
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

        // Tween round duration toward target for smooth chart expansion
        if (_roundDuration != _targetRoundDuration)
        {
            _roundDuration = Mathf.Lerp(_roundDuration, _targetRoundDuration,
                1f - Mathf.Exp(-DurationLerpSpeed * evt.DeltaTime));
            // Snap when close enough to avoid floating-point drift
            if (Mathf.Abs(_roundDuration - _targetRoundDuration) < 0.01f)
                _roundDuration = _targetRoundDuration;
        }

        AddPoint(_elapsedTime, evt.NewPrice);
    }

    /// <summary>
    /// Handles timer extension (e.g., Time Buyer relic). Sets target duration
    /// and tweens toward it over ~0.5s for a smooth "zoom out" effect.
    /// </summary>
    public void HandleTimerExtended(RoundTimerExtendedEvent evt)
    {
        _targetRoundDuration = evt.NewDuration;
    }

}
