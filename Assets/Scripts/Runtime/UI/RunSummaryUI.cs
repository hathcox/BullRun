using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Run Summary overlay. Shows "MARGIN CALL" or "RUN COMPLETE" header,
/// run statistics, and "Press any key to continue" prompt.
/// Subscribes to RunEndedEvent to display results.
/// </summary>
public class RunSummaryUI : MonoBehaviour
{
    private static readonly Color MarginCallColor = new Color(1f, 0.15f, 0.15f, 1f);
    private static readonly Color RunCompleteColor = new Color(0f, 1f, 0.4f, 1f);
    private static readonly Color ProfitColor = new Color(0f, 1f, 0.4f, 1f);
    private static readonly Color LossColor = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color StatsColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    private static readonly Color PromptColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private GameObject _panel;
    private Text _headerText;
    private Text _statsText;
    private Text _promptText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;

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

        if (_headerText != null)
        {
            _headerText.text = GetHeaderText(evt.WasMarginCalled, RunSummaryState.IsVictory);
            _headerText.color = evt.WasMarginCalled ? MarginCallColor : RunCompleteColor;
        }

        if (_statsText != null)
        {
            _statsText.text = BuildStatsText(evt);
            _statsText.color = StatsColor;
        }

        if (_promptText != null)
        {
            _promptText.text = "Press any key to continue";
            _promptText.color = PromptColor;
        }

        _visible = true;
        if (_panel != null) _panel.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (!_initialized || !_visible) return;

        // Hide when RunSummaryState is no longer active
        if (!RunSummaryState.IsActive)
        {
            _visible = false;
            if (_panel != null) _panel.SetActive(false);
        }
    }

    // --- Static utility methods for testability ---

    public static string GetHeaderText(bool wasMarginCalled, bool isVictory = false)
    {
        if (wasMarginCalled) return "MARGIN CALL";
        if (isVictory) return "BULL RUN!";
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

    private static string BuildStatsText(RunEndedEvent evt)
    {
        return $"Rounds Completed: {evt.RoundsCompleted}\n" +
               $"Final Cash: {FormatCash(evt.FinalCash)}\n" +
               $"Total Profit: {FormatProfit(evt.TotalProfit)}\n" +
               $"Items Collected: {evt.ItemsCollected}\n" +
               $"Reputation Earned: {evt.ReputationEarned}";
    }
}
