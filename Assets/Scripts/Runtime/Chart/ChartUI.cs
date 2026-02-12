using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Chart UI logic for Y-axis price labels and time progress bar.
/// Pure calculation methods are static for testability.
/// MonoBehaviour side manages uGUI elements created by ChartSetup.
/// </summary>
public class ChartUI : MonoBehaviour
{
    private ChartRenderer _chartRenderer;
    private Text[] _axisLabels;
    private Image _timeProgressBar;
    private Text _currentPriceLabel;
    private Text _stockNameLabel;
    private Text _stockPriceLabel;
    private int _labelCount = 5;

    public void Initialize(ChartRenderer chartRenderer, Text[] axisLabels,
        Image timeProgressBar, Text currentPriceLabel)
    {
        _chartRenderer = chartRenderer;
        _axisLabels = axisLabels;
        _timeProgressBar = timeProgressBar;
        _currentPriceLabel = currentPriceLabel;
        _labelCount = axisLabels != null ? axisLabels.Length : 5;
    }

    public void SetStockLabels(Text stockNameLabel, Text stockPriceLabel)
    {
        _stockNameLabel = stockNameLabel;
        _stockPriceLabel = stockPriceLabel;

        EventBus.Subscribe<StockSelectedEvent>(evt =>
        {
            if (_stockNameLabel != null)
                _stockNameLabel.text = evt.TickerSymbol;
        });

        EventBus.Subscribe<MarketOpenEvent>(evt =>
        {
            if (_stockNameLabel != null && evt.TickerSymbols != null && evt.TickerSymbols.Length > 0)
                _stockNameLabel.text = evt.TickerSymbols[0];
        });
    }

    public void ResetForNewRound()
    {
        if (_timeProgressBar != null)
            _timeProgressBar.fillAmount = 0f;
    }

    private void Update()
    {
        if (_chartRenderer == null) return;

        UpdateAxisLabels();
        UpdateTimeBar();
        UpdateCurrentPriceLabel();
        UpdateStockPriceLabel();
    }

    private void UpdateAxisLabels()
    {
        if (_axisLabels == null || _axisLabels.Length == 0) return;

        _chartRenderer.GetLivePriceRange(out float min, out float max);

        if (min >= max)
        {
            float center = _chartRenderer.CurrentPrice > 0 ? _chartRenderer.CurrentPrice : 100f;
            min = center - 5f;
            max = center + 5f;
        }

        var labels = CalculateAxisLabels(min, max, _axisLabels.Length);
        for (int i = 0; i < _axisLabels.Length; i++)
        {
            if (_axisLabels[i] != null)
                _axisLabels[i].text = FormatPrice(labels[i]);
        }
    }

    private void UpdateTimeBar()
    {
        if (_timeProgressBar == null) return;

        _timeProgressBar.fillAmount = CalculateTimeProgress(
            _chartRenderer.ElapsedTime, _chartRenderer.RoundDuration);
    }

    private void UpdateCurrentPriceLabel()
    {
        if (_currentPriceLabel == null || _chartRenderer.PointCount == 0) return;

        _currentPriceLabel.text = FormatPrice(_chartRenderer.CurrentPrice);
    }

    private void UpdateStockPriceLabel()
    {
        if (_stockPriceLabel == null || _chartRenderer.PointCount == 0) return;
        _stockPriceLabel.text = FormatPrice(_chartRenderer.CurrentPrice);
    }

    // --- Static utility methods for testability ---

    /// <summary>
    /// Calculates evenly spaced axis label values between min and max price.
    /// </summary>
    public static float[] CalculateAxisLabels(float minPrice, float maxPrice, int labelCount)
    {
        var labels = new float[labelCount];
        if (labelCount <= 1)
        {
            labels[0] = minPrice;
            return labels;
        }

        float step = (maxPrice - minPrice) / (labelCount - 1);
        for (int i = 0; i < labelCount; i++)
        {
            labels[i] = minPrice + step * i;
        }
        return labels;
    }

    /// <summary>
    /// Calculates normalized time progress (0-1) for the progress bar.
    /// </summary>
    public static float CalculateTimeProgress(float elapsed, float duration)
    {
        if (duration <= 0f) return 0f;
        return Mathf.Clamp01(elapsed / duration);
    }

    /// <summary>
    /// Formats a price value for display with dollar sign and appropriate decimals.
    /// </summary>
    public static string FormatPrice(float price)
    {
        return price.ToString("$#,##0.00");
    }
}
