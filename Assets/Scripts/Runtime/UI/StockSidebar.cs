using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour for the stock selection sidebar.
/// Manages visual updates, click handling, and keyboard shortcuts.
/// Pure logic delegated to StockSidebarData.
/// </summary>
public class StockSidebar : MonoBehaviour
{
    private StockSidebarData _data;
    private StockEntryView[] _entryViews;
    private bool _dirty;

    private static readonly Color SelectedBgColor = new Color(0.1f, 0.15f, 0.35f, 0.9f);
    private static readonly Color NormalBgColor = new Color(0.05f, 0.07f, 0.18f, 0.6f);
    private static readonly Color ProfitGreen = new Color(0f, 1f, 0.533f, 1f);
    private static readonly Color LossRed = new Color(1f, 0.2f, 0.2f, 1f);

    public StockSidebarData Data => _data;

    public void Initialize(StockSidebarData data, StockEntryView[] entryViews)
    {
        _data = data;
        _entryViews = entryViews;

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
    }

    private void OnPriceUpdated(PriceUpdatedEvent evt)
    {
        _data.ProcessPriceUpdate(evt);
        _dirty = true;
    }

    private void Update()
    {
        if (_data == null) return;

        // Keyboard shortcuts: 1-4 select stocks
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) { _data.SelectStock(0); _dirty = true; }
        else if (keyboard.digit2Key.wasPressedThisFrame) { _data.SelectStock(1); _dirty = true; }
        else if (keyboard.digit3Key.wasPressedThisFrame) { _data.SelectStock(2); _dirty = true; }
        else if (keyboard.digit4Key.wasPressedThisFrame) { _data.SelectStock(3); _dirty = true; }
    }

    private void LateUpdate()
    {
        if (!_dirty) return;
        _dirty = false;
        RefreshEntryVisuals();
    }

    /// <summary>
    /// Called by entry button click handlers.
    /// </summary>
    public void OnEntryClicked(int index)
    {
        _data.SelectStock(index);
        RefreshEntryVisuals();
    }

    private void RefreshEntryVisuals()
    {
        if (_entryViews == null) return;

        for (int i = 0; i < _entryViews.Length && i < _data.EntryCount; i++)
        {
            var entry = _data.GetEntry(i);
            var view = _entryViews[i];

            if (view.TickerText != null)
                view.TickerText.text = entry.TickerSymbol;

            if (view.PriceText != null)
                view.PriceText.text = TradingHUD.FormatCurrency(entry.CurrentPrice);

            if (view.ChangeText != null)
            {
                view.ChangeText.text = TradingHUD.FormatPercentChange(entry.PercentChange);
                view.ChangeText.color = entry.PercentChange > 0f ? ProfitGreen
                    : entry.PercentChange < 0f ? LossRed
                    : Color.white;
            }

            if (view.Background != null)
                view.Background.color = entry.IsSelected ? SelectedBgColor : NormalBgColor;

            // Update sparkline using cached min/max from StockEntry
            if (view.SparklineRenderer != null)
            {
                int count = entry.SparklinePointCount;
                view.SparklineRenderer.positionCount = count;
                float range = entry.SparklineMax - entry.SparklineMin;
                for (int p = 0; p < count; p++)
                {
                    float x = view.SparklineBounds.xMin + (view.SparklineBounds.width * p / Mathf.Max(1, count - 1));
                    float price = entry.GetSparklinePoint(p);
                    float yNorm = range > 0f ? (price - entry.SparklineMin) / range : 0.5f;
                    float y = view.SparklineBounds.yMin + view.SparklineBounds.height * yNorm;
                    view.SparklineRenderer.SetPosition(p, new Vector3(x, y, 0f));
                }
            }
        }
    }
}

/// <summary>
/// References to UI elements for a single stock entry in the sidebar.
/// Created by UISetup, consumed by StockSidebar.
/// </summary>
public class StockEntryView
{
    public Text TickerText;
    public Text PriceText;
    public Text ChangeText;
    public Image Background;
    public LineRenderer SparklineRenderer;
    public Rect SparklineBounds;
}
