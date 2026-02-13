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
    private RectTransform _priceLabelRect;
    private RectTransform _canvasRect;
    private Text _stockNameLabel;
    private Text _stockPriceLabel;
    private int _labelCount = 5;

    // Chart bounds in world space (for positioning price label at chart head)
    private float _chartLeft, _chartRight, _chartBottom, _chartTop;

    public void Initialize(ChartRenderer chartRenderer, Text[] axisLabels,
        Image timeProgressBar, Text currentPriceLabel, Rect chartBounds)
    {
        _chartRenderer = chartRenderer;
        _axisLabels = axisLabels;
        _timeProgressBar = timeProgressBar;
        _currentPriceLabel = currentPriceLabel;
        _labelCount = axisLabels != null ? axisLabels.Length : 5;

        _chartLeft = chartBounds.xMin;
        _chartRight = chartBounds.xMax;
        _chartBottom = chartBounds.yMin;
        _chartTop = chartBounds.yMax;

        if (_currentPriceLabel != null)
        {
            _priceLabelRect = _currentPriceLabel.GetComponent<RectTransform>();
            _canvasRect = _currentPriceLabel.canvas.GetComponent<RectTransform>();
        }
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
        if (_currentPriceLabel == null || _chartRenderer.PointCount < 2) return;

        _currentPriceLabel.text = FormatPrice(_chartRenderer.CurrentPrice);

        // Position label at chart head (last data point)
        if (_priceLabelRect == null) return;

        var lastPoint = _chartRenderer.GetPoint(_chartRenderer.PointCount - 1);
        _chartRenderer.GetLivePriceRange(out float minPrice, out float maxPrice);
        float priceRange = maxPrice - minPrice;
        if (priceRange < 0.01f)
        {
            float center = (minPrice + maxPrice) * 0.5f;
            minPrice = center - 0.5f;
            priceRange = 1f;
        }

        // Match ChartLineView's 10% padding
        float chartHeight = _chartTop - _chartBottom;
        float padding = chartHeight * 0.1f;
        float paddedBottom = _chartBottom + padding;
        float paddedTop = _chartTop - padding;

        float worldX = Mathf.Lerp(_chartLeft, _chartRight, lastPoint.NormalizedTime);
        float worldY = Mathf.Lerp(paddedBottom, paddedTop, (lastPoint.Price - minPrice) / priceRange);

        // Convert world position to canvas position via screen space
        var cam = Camera.main;
        if (cam != null && _canvasRect != null)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(new Vector3(worldX, worldY, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, null, out Vector2 localPoint);
            // Offset right so label doesn't overlap the line head
            _priceLabelRect.anchoredPosition = new Vector2(localPoint.x + 10f, localPoint.y);
        }
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
