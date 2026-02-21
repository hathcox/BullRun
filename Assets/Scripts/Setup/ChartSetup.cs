using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup class that generates chart GameObjects and UI elements during F5 rebuild.
/// Creates MeshFilter/MeshRenderer for main line and glow trail, price axis labels,
/// time progress bar, and current price indicator.
/// </summary>
public static class ChartSetup
{
    // FIX-5: No sidebar on left. FIX-7: No positions panel on right — chart uses full width.
    private static readonly float ChartWidthPercent = 0.80f;
    // Story 14.5: Reduced from 0.70 to 0.55 — chart occupies middle ~55%, leaving
    // bottom ~25% for Control Deck and top ~10% for stock label + event ticker.
    private static readonly float ChartHeightPercent = 0.55f;
    private static readonly int AxisLabelCount = 5;
    // Note: Chart background is determined by Camera clear color, not a panel.
    // AC 6 (CRT background) deferred to Story 14.6 global CRT theme application.
    private static Material _lineMaterial;
    private static Material _meshMaterial;

    public static void Execute()
    {
        // Create chart parent
        var chartParent = new GameObject("ChartSystem");

        // Create ChartRenderer (pure C# logic)
        var chartRenderer = new ChartRenderer();

        // Store reference on a holder MonoBehaviour
        var holder = chartParent.AddComponent<ChartDataHolder>();
        holder.Renderer = chartRenderer;

        // Create MeshFilter/MeshRenderer objects for main and glow lines
        var mainLineGo = CreateMeshObject("ChartMainLine", chartParent.transform, 1);
        var glowLineGo = CreateMeshObject("ChartGlowLine", chartParent.transform, -1);

        // Create price indicator
        var indicatorGo = new GameObject("PriceIndicator");
        indicatorGo.transform.SetParent(chartParent.transform);
        var indicatorSprite = indicatorGo.AddComponent<SpriteRenderer>();
        indicatorSprite.color = ChartVisualConfig.Default.LineColor;
        indicatorGo.SetActive(false);

        // Calculate chart bounds based on screen center ~65% (FIX-5: wider, no sidebar)
        float screenWidth = Screen.width > 0 ? Screen.width : 1920f;
        float screenHeight = Screen.height > 0 ? Screen.height : 1080f;

        var cam = Camera.main;
        float worldHeight = cam != null ? cam.orthographicSize * 2f : 10f;
        float worldWidth = worldHeight * (screenWidth / screenHeight);

        float chartWorldWidth = worldWidth * ChartWidthPercent;
        float chartWorldHeight = worldHeight * ChartHeightPercent;
        float chartLeft = -chartWorldWidth / 2f;
        float chartRight = chartWorldWidth / 2f;
        // Story 14.5: Shift chart upward so bottom edge clears the Control Deck.
        // Reserve bottom ~25% for Control Deck, chart starts at -25% from center.
        float chartBottom = -worldHeight * 0.5f + worldHeight * 0.25f;
        float chartTop = chartBottom + chartWorldHeight;

        var chartBounds = new Rect(chartLeft, chartBottom, chartWorldWidth, chartWorldHeight);

        // Initialize ChartLineView with MeshFilters
        var chartLineView = chartParent.AddComponent<ChartLineView>();
        chartLineView.Initialize(
            chartRenderer,
            mainLineGo.GetComponent<MeshFilter>(),
            glowLineGo.GetComponent<MeshFilter>(),
            indicatorGo.transform,
            ChartVisualConfig.Default,
            chartBounds
        );

        // Create break-even line (yellow, thin horizontal — kept as LineRenderer)
        var breakEvenGo = CreateLineRendererObject("BreakEvenLine", chartParent.transform);
        var breakEvenLR = breakEvenGo.GetComponent<LineRenderer>();
        breakEvenLR.startColor = ColorPalette.WithAlpha(ColorPalette.White, 0.8f);
        breakEvenLR.endColor = ColorPalette.WithAlpha(ColorPalette.White, 0.8f);
        breakEvenLR.startWidth = 0.015f;
        breakEvenLR.endWidth = 0.015f;
        breakEvenLR.sortingOrder = 2;
        breakEvenLR.alignment = LineAlignment.TransformZ;
        breakEvenGo.SetActive(false);

        // Create short position line (pink, thin horizontal — shows short entry price)
        var shortPositionGo = CreateLineRendererObject("ShortPositionLine", chartParent.transform);
        var shortPositionLR = shortPositionGo.GetComponent<LineRenderer>();
        shortPositionLR.startColor = ColorPalette.WithAlpha(ColorPalette.Amber, 0.9f);
        shortPositionLR.endColor = ColorPalette.WithAlpha(ColorPalette.Amber, 0.9f);
        shortPositionLR.startWidth = 0.015f;
        shortPositionLR.endWidth = 0.015f;
        shortPositionLR.sortingOrder = 2;
        shortPositionLR.alignment = LineAlignment.TransformZ;
        shortPositionGo.SetActive(false);

        // Create price gridlines (FIX-8: horizontal reference lines at each axis label)
        var gridlinesParent = new GameObject("PriceGridlines");
        gridlinesParent.transform.SetParent(chartParent.transform);
        var gridlines = new LineRenderer[AxisLabelCount];
        // Story 14.5 AC 5: CRT border color with subtle opacity
        var gridColor = ColorPalette.WithAlpha(ColorPalette.Border, 0.2f);
        for (int i = 0; i < AxisLabelCount; i++)
        {
            var gridlineGo = CreateLineRendererObject($"Gridline_{i}", gridlinesParent.transform);
            var lr = gridlineGo.GetComponent<LineRenderer>();
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            lr.startWidth = 0.005f;
            lr.endWidth = 0.005f;
            lr.sortingOrder = -2;
            lr.alignment = LineAlignment.TransformZ;
            gridlineGo.SetActive(false);
            gridlines[i] = lr;
        }
        chartLineView.SetGridlines(gridlines);

        // Create marker pool parent
        var markerPoolGo = new GameObject("TradeMarkerPool");
        markerPoolGo.transform.SetParent(chartParent.transform);

        // Wire trade visuals to ChartLineView
        chartLineView.SetTradeVisuals(breakEvenLR, shortPositionLR, markerPoolGo.transform);

        // Create chart UI Canvas — returns ChartUI for event wiring
        var chartUI = CreateChartUI(chartParent, chartRenderer, chartBounds, worldHeight);

        // Subscribe ChartRenderer to EventBus
        EventBus.Subscribe<PriceUpdatedEvent>(chartRenderer.ProcessPriceUpdate);
        EventBus.Subscribe<TradeExecutedEvent>(chartRenderer.ProcessTrade);

        // Wire stock selection: switch chart to selected stock (Story 3.3 AC3)
        EventBus.Subscribe<StockSelectedEvent>(evt => chartRenderer.SetActiveStock(evt.StockId));

        // Wire round lifecycle
        EventBus.Subscribe<RoundEndedEvent>(_ =>
        {
            chartRenderer.ResetChart();
            chartUI.ResetForNewRound();
        });
        EventBus.Subscribe<RoundStartedEvent>(evt =>
        {
            chartRenderer.ResetChart();
            chartRenderer.SetRoundDuration(evt.TimeLimit);
            chartRenderer.StartRound();
            chartUI.ResetForNewRound();
        });
        EventBus.Subscribe<RoundTimerExtendedEvent>(chartRenderer.HandleTimerExtended);
        // Auto-select first stock when market opens (before trading begins).
        EventBus.Subscribe<MarketOpenEvent>(evt =>
        {
            if (evt.StockIds != null && evt.StockIds.Length > 0)
            {
                chartRenderer.SetActiveStockId(evt.StockIds[0]);
            }
        });

        // Apply tier theme colors when act changes
        var chartLineViewRef = chartLineView;
        var chartUIRef = chartUI;
        EventBus.Subscribe<ActTransitionEvent>(evt =>
        {
            var theme = TierVisualData.GetThemeForAct(evt.NewAct);
            var config = TierVisualData.ToChartVisualConfig(theme);
            chartLineViewRef.ApplyTierTheme(config);
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Setup] ChartSystem created: bounds={chartBounds}, labels={AxisLabelCount}");
        #endif
    }

    private static GameObject CreateMeshObject(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        if (_meshMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[Setup] Shader 'Sprites/Default' not found — using fallback.");
                shader = Shader.Find("UI/Default");
            }
            _meshMaterial = new Material(shader);
        }
        mr.sharedMaterial = _meshMaterial;
        mr.sortingOrder = sortingOrder;

        return go;
    }

    private static GameObject CreateLineRendererObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 0;
        lr.useWorldSpace = true;

        // Reuse a single shared material to avoid per-LineRenderer leaks
        if (_lineMaterial == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogWarning("[Setup] Shader 'Sprites/Default' not found — using fallback. Ensure it's included in Always Included Shaders.");
                shader = Shader.Find("UI/Default");
            }
            _lineMaterial = new Material(shader);
        }
        lr.sharedMaterial = _lineMaterial;

        return go;
    }

    private static ChartUI CreateChartUI(GameObject chartParent, ChartRenderer chartRenderer, Rect chartBounds, float worldHeight)
    {
        // Create Canvas for chart UI elements
        var canvasGo = new GameObject("ChartCanvas");
        canvasGo.transform.SetParent(chartParent.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.AddComponent<GraphicRaycaster>();

        // Create Y-axis labels on right side (FIX-7: no positions panel, labels at chart edge)
        var axisLabels = new Text[AxisLabelCount];
        // 960 = half canvas width. Labels placed at right edge of chart area with margin.
        float axisLabelX = 960f * ChartWidthPercent + 10f; // Just past right edge of chart

        // Story 14.5: Convert world-space chart bounds to canvas-space Y positions.
        // Canvas ref is 1920x1080, center at (0,0). World bounds map proportionally.
        float canvasChartBottom = (chartBounds.yMin / worldHeight) * 1080f;
        float canvasChartTop = (chartBounds.yMax / worldHeight) * 1080f;

        for (int i = 0; i < AxisLabelCount; i++)
        {
            var labelGo = new GameObject($"AxisLabel_{i}");
            labelGo.transform.SetParent(canvasGo.transform);
            var rectTransform = labelGo.AddComponent<RectTransform>();

            // Position labels on right side, evenly distributed vertically within chart bounds
            float yNorm = (float)i / (AxisLabelCount - 1);
            float yPos = Mathf.Lerp(canvasChartBottom, canvasChartTop, yNorm);

            rectTransform.anchoredPosition = new Vector2(axisLabelX, yPos);
            rectTransform.sizeDelta = new Vector2(70f, 30f);

            var text = labelGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = CRTThemeData.TextLow; // Story 14.5: dim cyan for axis labels
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "$0.00";

            axisLabels[i] = text;
        }

        // Create time progress bar along bottom of chart area
        var progressBarBg = new GameObject("TimeProgressBarBg");
        progressBarBg.transform.SetParent(canvasGo.transform);
        var bgRect = progressBarBg.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(0f, canvasChartBottom - 20f);
        bgRect.sizeDelta = new Vector2(1920f * ChartWidthPercent, 6f);
        var bgImage = progressBarBg.AddComponent<Image>();
        bgImage.color = ColorPalette.WithAlpha(ColorPalette.Border, 0.5f);

        var progressBarFill = new GameObject("TimeProgressBarFill");
        progressBarFill.transform.SetParent(progressBarBg.transform);
        var fillRect = progressBarFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = progressBarFill.AddComponent<Image>();
        fillImage.color = ColorPalette.WithAlpha(ColorPalette.Green, 0.4f); // CRT phosphor green
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;

        // Create current price label at chart head — aligned with axis labels
        var priceLabelGo = new GameObject("CurrentPriceLabel");
        priceLabelGo.transform.SetParent(canvasGo.transform);
        var priceRect = priceLabelGo.AddComponent<RectTransform>();
        priceRect.anchoredPosition = new Vector2(axisLabelX, 0f);
        priceRect.sizeDelta = new Vector2(70f, 30f);
        var priceText = priceLabelGo.AddComponent<Text>();
        priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        priceText.fontSize = 16;
        priceText.color = CRTThemeData.TextHigh; // Story 14.5 AC 8: phosphor green for current price
        priceText.fontStyle = FontStyle.Bold;
        priceText.alignment = TextAnchor.MiddleLeft;

        // Story 14.5: Stock name label — centered at top, larger font for CRT readability
        var stockNameGo = new GameObject("StockNameLabel");
        stockNameGo.transform.SetParent(canvasGo.transform);
        var stockNameRect = stockNameGo.AddComponent<RectTransform>();
        stockNameRect.anchorMin = new Vector2(0.5f, 1f);
        stockNameRect.anchorMax = new Vector2(0.5f, 1f);
        stockNameRect.pivot = new Vector2(0.5f, 1f);
        stockNameRect.anchoredPosition = new Vector2(0f, -30f);
        stockNameRect.sizeDelta = new Vector2(400f, 36f);
        var stockNameText = stockNameGo.AddComponent<Text>();
        stockNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stockNameText.fontSize = 28; // AC 3: larger 28pt for CRT readability
        stockNameText.color = CRTThemeData.TextHigh; // AC 3: phosphor green
        stockNameText.fontStyle = FontStyle.Bold;
        stockNameText.alignment = TextAnchor.MiddleCenter;
        stockNameText.text = "";

        // Story 14.5: Stock price label — just below ticker, 20pt white
        var stockPriceGo = new GameObject("StockPriceLabel");
        stockPriceGo.transform.SetParent(canvasGo.transform);
        var stockPriceRect = stockPriceGo.AddComponent<RectTransform>();
        stockPriceRect.anchorMin = new Vector2(0.5f, 1f);
        stockPriceRect.anchorMax = new Vector2(0.5f, 1f);
        stockPriceRect.pivot = new Vector2(0.5f, 1f);
        stockPriceRect.anchoredPosition = new Vector2(0f, -66f);
        stockPriceRect.sizeDelta = new Vector2(400f, 28f);
        var stockPriceText = stockPriceGo.AddComponent<Text>();
        stockPriceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        stockPriceText.fontSize = 20; // AC 3: larger 20pt for CRT readability
        stockPriceText.color = Color.white;
        stockPriceText.alignment = TextAnchor.MiddleCenter;
        stockPriceText.text = "";

        // Initialize ChartUI MonoBehaviour
        var chartUIComponent = chartParent.AddComponent<ChartUI>();
        chartUIComponent.Initialize(chartRenderer, axisLabels, fillImage, priceText, chartBounds);
        chartUIComponent.SetStockLabels(stockNameText, stockPriceText);
        return chartUIComponent;
    }
}

/// <summary>
/// Simple MonoBehaviour to hold a reference to the ChartRenderer instance.
/// Allows other systems to find the ChartRenderer at runtime.
/// </summary>
public class ChartDataHolder : MonoBehaviour
{
    public ChartRenderer Renderer;
}
