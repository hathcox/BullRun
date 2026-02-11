using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Setup class that generates chart GameObjects and UI elements during F5 rebuild.
/// Creates LineRenderers for main line and glow trail, price axis labels,
/// time progress bar, and current price indicator.
/// [SetupClass(SetupPhase.SceneComposition)] attribute to be enabled when SetupPipeline exists.
/// </summary>
// [SetupClass(SetupPhase.SceneComposition, 40)] // Uncomment when SetupPipeline infrastructure exists
public static class ChartSetup
{
    // Chart occupies ~60% center of screen per GDD layout spec
    private static readonly float ChartWidthPercent = 0.60f;
    private static readonly float ChartHeightPercent = 0.70f;
    private static readonly int AxisLabelCount = 5;
    private static readonly Color BackgroundColor = new Color(0.039f, 0.055f, 0.153f, 1f); // #0A0E27 dark navy
    private static Material _lineMaterial;

    public static void Execute()
    {
        // Create chart parent
        var chartParent = new GameObject("ChartSystem");

        // Create ChartRenderer (pure C# logic)
        var chartRenderer = new ChartRenderer();

        // Store reference on a holder MonoBehaviour
        var holder = chartParent.AddComponent<ChartDataHolder>();
        holder.Renderer = chartRenderer;

        // Create LineRenderer objects
        var mainLineGo = CreateLineRendererObject("ChartMainLine", chartParent.transform);
        var glowLineGo = CreateLineRendererObject("ChartGlowLine", chartParent.transform);

        // Glow line renders behind main line
        var glowLR = glowLineGo.GetComponent<LineRenderer>();
        glowLR.sortingOrder = -1;

        // Create price indicator
        var indicatorGo = new GameObject("PriceIndicator");
        indicatorGo.transform.SetParent(chartParent.transform);
        var indicatorSprite = indicatorGo.AddComponent<SpriteRenderer>();
        indicatorSprite.color = ChartVisualConfig.Default.LineColor;
        indicatorGo.SetActive(false);

        // Calculate chart bounds based on screen center ~60%
        float screenWidth = Screen.width > 0 ? Screen.width : 1920f;
        float screenHeight = Screen.height > 0 ? Screen.height : 1080f;

        var cam = Camera.main;
        float worldHeight = cam != null ? cam.orthographicSize * 2f : 10f;
        float worldWidth = worldHeight * (screenWidth / screenHeight);

        float chartWorldWidth = worldWidth * ChartWidthPercent;
        float chartWorldHeight = worldHeight * ChartHeightPercent;
        float chartLeft = -chartWorldWidth / 2f;
        float chartRight = chartWorldWidth / 2f;
        float chartBottom = -chartWorldHeight / 2f;
        float chartTop = chartWorldHeight / 2f;

        var chartBounds = new Rect(chartLeft, chartBottom, chartWorldWidth, chartWorldHeight);

        // Initialize ChartLineView
        var chartLineView = chartParent.AddComponent<ChartLineView>();
        chartLineView.Initialize(
            chartRenderer,
            mainLineGo.GetComponent<LineRenderer>(),
            glowLineGo.GetComponent<LineRenderer>(),
            indicatorGo.transform,
            ChartVisualConfig.Default,
            chartBounds
        );

        // Create chart UI Canvas — returns ChartUI for event wiring
        var chartUI = CreateChartUI(chartParent, chartRenderer, chartBounds);

        // Subscribe ChartRenderer to EventBus
        EventBus.Subscribe<PriceUpdatedEvent>(chartRenderer.ProcessPriceUpdate);

        // Wire stock selection: switch chart to selected stock (Story 3.3 AC3)
        EventBus.Subscribe<StockSelectedEvent>(evt => chartRenderer.SetActiveStock(evt.StockId));

        // Wire round lifecycle: reset chart on round end, start on run start
        // Round events from Epic 4 will trigger these automatically
        EventBus.Subscribe<RoundEndedEvent>(_ =>
        {
            chartRenderer.ResetChart();
            chartUI.ResetForNewRound();
        });
        EventBus.Subscribe<RunStartedEvent>(_ =>
        {
            chartRenderer.ResetChart();
            chartRenderer.StartRound();
            chartUI.ResetForNewRound();
        });

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[Setup] ChartSystem created: bounds={chartBounds}, labels={AxisLabelCount}");
        #endif
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

    private static ChartUI CreateChartUI(GameObject chartParent, ChartRenderer chartRenderer, Rect chartBounds)
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

        // Create Y-axis labels on right side
        var axisLabels = new Text[AxisLabelCount];
        float chartRightScreenX = 0.5f + ChartWidthPercent / 2f; // Right edge of chart in viewport

        for (int i = 0; i < AxisLabelCount; i++)
        {
            var labelGo = new GameObject($"AxisLabel_{i}");
            labelGo.transform.SetParent(canvasGo.transform);
            var rectTransform = labelGo.AddComponent<RectTransform>();

            // Position labels on right side, evenly distributed vertically
            float yNorm = (float)i / (AxisLabelCount - 1);
            float yPos = Mathf.Lerp(-540f * ChartHeightPercent, 540f * ChartHeightPercent, yNorm);

            rectTransform.anchoredPosition = new Vector2(960f * chartRightScreenX + 40f, yPos);
            rectTransform.sizeDelta = new Vector2(100f, 30f);

            var text = labelGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray
            text.alignment = TextAnchor.MiddleLeft;
            text.text = "$0.00";

            axisLabels[i] = text;
        }

        // Create time progress bar along bottom
        var progressBarBg = new GameObject("TimeProgressBarBg");
        progressBarBg.transform.SetParent(canvasGo.transform);
        var bgRect = progressBarBg.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(0f, -540f * ChartHeightPercent - 20f);
        bgRect.sizeDelta = new Vector2(1920f * ChartWidthPercent, 6f);
        var bgImage = progressBarBg.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.5f);

        var progressBarFill = new GameObject("TimeProgressBarFill");
        progressBarFill.transform.SetParent(progressBarBg.transform);
        var fillRect = progressBarFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        var fillImage = progressBarFill.AddComponent<Image>();
        fillImage.color = new Color(0f, 1f, 0.533f, 0.4f); // Subtle neon green
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;

        // Create current price label at chart head
        var priceLabelGo = new GameObject("CurrentPriceLabel");
        priceLabelGo.transform.SetParent(canvasGo.transform);
        var priceRect = priceLabelGo.AddComponent<RectTransform>();
        priceRect.anchoredPosition = new Vector2(960f * chartRightScreenX + 40f, 0f);
        priceRect.sizeDelta = new Vector2(120f, 30f);
        var priceText = priceLabelGo.AddComponent<Text>();
        priceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        priceText.fontSize = 16;
        priceText.color = ChartVisualConfig.Default.LineColor;
        priceText.fontStyle = FontStyle.Bold;
        priceText.alignment = TextAnchor.MiddleLeft;

        // Initialize ChartUI MonoBehaviour
        var chartUIComponent = chartParent.AddComponent<ChartUI>();
        chartUIComponent.Initialize(chartRenderer, axisLabels, fillImage, priceText);
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
