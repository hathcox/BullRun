using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Run Summary overlay. Shows "MARGIN CALL" or "BULL RUN COMPLETE!" header,
/// run statistics, and "Press any key to continue" prompt.
/// Victory variant includes gold theme, count-up number animation, and sparkle effects.
/// Subscribes to RunEndedEvent to display results.
/// </summary>
public class RunSummaryUI : MonoBehaviour
{
    // AC 8: Margin call slam-in constants
    public static readonly float MarginCallSlamDuration = 0.4f;
    public static readonly float MarginCallSlamStartScale = 2.5f;

    // Story 14.6: Color constants migrated to CRTThemeData
    private static Color MarginCallColor => CRTThemeData.Danger;
    private static Color RunCompleteColor => CRTThemeData.TextHigh;
    private static readonly Color VictoryGoldColor = ColorPalette.Gold;
    private static Color ProfitColor => CRTThemeData.TextHigh;
    private static Color LossColor => CRTThemeData.Danger;
    private static readonly Color StatsColor = ColorPalette.WhiteDim;
    private static readonly Color PromptColor = ColorPalette.Dimmed(ColorPalette.WhiteDim, 0.7f);

    private GameObject _panel;
    private Text _headerText;
    private Text _statsText;
    private Text _promptText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;

    // Victory count-up animation state
    private bool _isVictoryCountUp;
    private float _countUpTimer;
    private RunEndedEvent _targetEvent;
    private const float CountUpDuration = 2f;

    // Victory sparkle effect
    private Image[] _sparkles;
    private float[] _sparklePhases;
    private const int SparkleCount = 12;

    public void Initialize(GameObject panel, Text headerText, Text statsText, Text promptText, CanvasGroup canvasGroup)
    {
        _panel = panel;
        _headerText = headerText;
        _statsText = statsText;
        _promptText = promptText;
        _canvasGroup = canvasGroup;
        _initialized = true;
        _visible = false;

        if (_panel != null) _panel.SetActive(false);

        EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
    }

    private void OnRunEnded(RunEndedEvent evt)
    {
        if (!_initialized) return;

        bool isVictory = RunSummaryState.IsVictory;
        _targetEvent = evt;

        if (_headerText != null)
        {
            _headerText.text = GetHeaderText(evt.WasMarginCalled, isVictory);
            _headerText.color = evt.WasMarginCalled ? MarginCallColor
                : isVictory ? VictoryGoldColor : RunCompleteColor;
        }

        if (_statsText != null)
        {
            if (isVictory)
            {
                // Start count-up animation — show zeros initially
                _isVictoryCountUp = true;
                _countUpTimer = 0f;
                _statsText.text = BuildVictoryStatsTextAnimated(evt, 0f);
            }
            else
            {
                _isVictoryCountUp = false;
                _statsText.text = BuildLossStatsText(evt);
            }
            _statsText.color = isVictory ? VictoryGoldColor : StatsColor;
        }

        if (_promptText != null)
        {
            _promptText.text = "Press any key to continue";
            _promptText.color = PromptColor;
        }

        _visible = true;
        if (_panel != null) _panel.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        // AC 8: Margin call slam-in animation
        if (evt.WasMarginCalled && _headerText != null)
        {
            var rect = _headerText.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = new Vector3(MarginCallSlamStartScale, MarginCallSlamStartScale, 1f);
                rect.DOPunchScale(
                    new Vector3(-(MarginCallSlamStartScale - 1f), -(MarginCallSlamStartScale - 1f), 0f),
                    MarginCallSlamDuration, 2, 0.5f)
                    .SetUpdate(false);
            }
        }

        // Create victory sparkle effects
        if (isVictory && _panel != null)
        {
            CreateSparkles();
        }
    }

    private void Update()
    {
        if (!_initialized || !_visible) return;

        // Hide when RunSummaryState is no longer active
        if (!RunSummaryState.IsActive)
        {
            _visible = false;
            _isVictoryCountUp = false;
            if (_panel != null) _panel.SetActive(false);
            DestroySparkles();
            return;
        }

        // Victory count-up animation
        if (_isVictoryCountUp && _statsText != null)
        {
            _countUpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_countUpTimer / CountUpDuration);

            // Ease-out curve for satisfying deceleration
            float easedT = 1f - (1f - t) * (1f - t);
            _statsText.text = BuildVictoryStatsTextAnimated(_targetEvent, easedT);

            if (t >= 1f)
            {
                _isVictoryCountUp = false;
                _statsText.text = BuildVictoryStatsText(_targetEvent);
            }
        }

        // Animate sparkles
        AnimateSparkles();
    }

    private void CreateSparkles()
    {
        DestroySparkles();
        if (_panel == null) return;

        _sparkles = new Image[SparkleCount];
        _sparklePhases = new float[SparkleCount];

        for (int i = 0; i < SparkleCount; i++)
        {
            var sparkleGo = new GameObject($"Sparkle_{i}");
            sparkleGo.transform.SetParent(_panel.transform, false);
            var rect = sparkleGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4f, 4f);
            // Spread sparkles across the screen
            rect.anchorMin = new Vector2(Random.Range(0.1f, 0.9f), Random.Range(0.1f, 0.9f));
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = Vector2.zero;

            var img = sparkleGo.AddComponent<Image>();
            img.color = new Color(1f, 0.85f + Random.Range(0f, 0.15f), Random.Range(0f, 0.4f), 0f);
            _sparkles[i] = img;
            _sparklePhases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void AnimateSparkles()
    {
        if (_sparkles == null) return;

        for (int i = 0; i < _sparkles.Length; i++)
        {
            if (_sparkles[i] == null) continue;
            _sparklePhases[i] += Time.deltaTime * (2f + i * 0.3f);
            float alpha = Mathf.Abs(Mathf.Sin(_sparklePhases[i]));
            var c = _sparkles[i].color;
            _sparkles[i].color = new Color(c.r, c.g, c.b, alpha * 0.8f);
        }
    }

    private void DestroySparkles()
    {
        if (_sparkles == null) return;
        for (int i = 0; i < _sparkles.Length; i++)
        {
            if (_sparkles[i] != null)
                Destroy(_sparkles[i].gameObject);
        }
        _sparkles = null;
        _sparklePhases = null;
    }

    // --- Static utility methods for testability ---

    public static string GetHeaderText(bool wasMarginCalled, bool isVictory = false)
    {
        if (wasMarginCalled) return "MARGIN CALL";
        if (isVictory) return "BULL RUN COMPLETE!";
        return "RUN COMPLETE";
    }

    public static string FormatCash(float amount)
    {
        if (amount < 0)
            return $"-${Mathf.Abs(amount):N2}";
        return $"${amount:N2}";
    }

    public static string FormatProfit(float profit)
    {
        string sign = profit >= 0 ? "+" : "-";
        return $"{sign}${Mathf.Abs(profit):N2}";
    }

    /// <summary>
    /// Victory stats: full 8-round summary with all run statistics.
    /// </summary>
    public static string BuildVictoryStatsText(RunEndedEvent evt)
    {
        return $"Total Profit: {FormatProfit(evt.TotalProfit)}\n" +
               $"Peak Cash: {FormatCash(evt.PeakCash)}\n" +
               $"Rounds Completed: {evt.RoundsCompleted}/{GameConfig.TotalRounds}\n" +
               $"Items Collected: {evt.ItemsCollected}\n" +
               $"Best Round: {FormatProfit(evt.BestRoundProfit)}\n" +
               $"Reputation Earned: {evt.ReputationEarned}";
    }

    /// <summary>
    /// Animated victory stats that interpolates values from 0 to final.
    /// </summary>
    public static string BuildVictoryStatsTextAnimated(RunEndedEvent evt, float t)
    {
        float profit = evt.TotalProfit * t;
        float peak = evt.PeakCash * t;
        int rounds = Mathf.RoundToInt(evt.RoundsCompleted * t);
        int items = Mathf.RoundToInt(evt.ItemsCollected * t);
        float bestRound = evt.BestRoundProfit * t;
        int rep = Mathf.RoundToInt(evt.ReputationEarned * t);

        return $"Total Profit: {FormatProfit(profit)}\n" +
               $"Peak Cash: {FormatCash(peak)}\n" +
               $"Rounds Completed: {rounds}/{GameConfig.TotalRounds}\n" +
               $"Items Collected: {items}\n" +
               $"Best Round: {FormatProfit(bestRound)}\n" +
               $"Reputation Earned: {rep}";
    }

    /// <summary>
    /// Loss stats: partial summary focused on what happened.
    /// </summary>
    public static string BuildLossStatsText(RunEndedEvent evt)
    {
        return $"Rounds Completed: {evt.RoundsCompleted}\n" +
               $"Final Cash: {FormatCash(evt.FinalCash)}\n" +
               $"Total Profit: {FormatProfit(evt.TotalProfit)}\n" +
               $"Items Collected: {evt.ItemsCollected}\n" +
               $"Reputation Earned: {evt.ReputationEarned}";
    }
}
