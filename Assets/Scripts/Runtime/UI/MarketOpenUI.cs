using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Market Open preview overlay. Shows act/round header, available stocks,
/// news headline, and profit target. Subscribes to MarketOpenEvent and
/// hides when TradingState becomes active.
/// </summary>
public class MarketOpenUI : MonoBehaviour
{
    private static readonly Color HeaderColor = new Color(0f, 1f, 0.533f, 1f);
    private static readonly Color LabelColor = new Color(0.6f, 0.6f, 0.7f, 1f);
    private static readonly Color ValueColor = Color.white;
    private static readonly Color TargetColor = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color HeadlineColor = new Color(0.8f, 0.85f, 1f, 1f);

    private GameObject _panel;
    private Text _headerText;
    private Text _stockListText;
    private Text _headlineText;
    private Text _targetText;
    private Text _countdownText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;
    private float _fadeTimer;
    private static readonly float FadeInDuration = 0.5f;

    public void Initialize(GameObject panel, Text headerText, Text stockListText,
        Text headlineText, Text targetText, Text countdownText, CanvasGroup canvasGroup)
    {
        _panel = panel;
        _headerText = headerText;
        _stockListText = stockListText;
        _headlineText = headlineText;
        _targetText = targetText;
        _countdownText = countdownText;
        _canvasGroup = canvasGroup;
        _initialized = true;
        _visible = false;

        if (_panel != null) _panel.SetActive(false);

        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnMarketOpen(MarketOpenEvent evt)
    {
        if (_headerText != null)
            _headerText.text = $"ACT {evt.Act} — ROUND {evt.RoundNumber}";

        if (_stockListText != null)
            _stockListText.text = BuildStockList(evt.TickerSymbols, evt.StartingPrices, evt.TierNames);

        if (_headlineText != null)
            _headlineText.text = $"\"{evt.Headline}\"";

        if (_targetText != null)
            _targetText.text = $"${evt.ProfitTarget:N0}";

        _visible = true;
        _fadeTimer = 0f;
        if (_panel != null) _panel.SetActive(true);
        // Start slightly visible so the overlay is immediately perceptible
        if (_canvasGroup != null) _canvasGroup.alpha = 0.1f;
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        // Trading has begun — hide the preview
        _visible = false;
        if (_panel != null) _panel.SetActive(false);
    }

    private void Update()
    {
        if (!_initialized || !_visible) return;

        // Fade in animation
        if (_canvasGroup != null && _fadeTimer < FadeInDuration)
        {
            _fadeTimer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(_fadeTimer / FadeInDuration);
        }

        // Update countdown from MarketOpenState
        if (_countdownText != null && MarketOpenState.IsActive)
        {
            float remaining = MarketOpenState.ActiveTimeRemaining;
            _countdownText.text = $"Trading begins in {Mathf.CeilToInt(remaining)}...";
        }
    }

    // --- Static utility methods for testability ---

    /// <summary>
    /// Builds a formatted stock list showing ticker symbols, starting prices, and tier indicators.
    /// </summary>
    public static string BuildStockList(string[] tickerSymbols, float[] startingPrices, string[] tierNames)
    {
        if (tickerSymbols == null || tickerSymbols.Length == 0)
            return "No stocks available";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < tickerSymbols.Length; i++)
        {
            if (i > 0) sb.Append("\n");
            string price = (startingPrices != null && i < startingPrices.Length)
                ? $"${startingPrices[i]:F2}" : "$?.??";
            string tier = (tierNames != null && i < tierNames.Length)
                ? $"[{tierNames[i]}]" : "";
            sb.Append($"{tickerSymbols[i]}  {price}  {tier}");
        }
        return sb.ToString();
    }
}
