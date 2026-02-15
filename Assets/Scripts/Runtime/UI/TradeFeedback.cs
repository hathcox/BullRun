using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays brief trade feedback text (success/failure) that fades out after 1.5s.
/// Subscribes to TradeFeedbackEvent via EventBus.
/// Colors: green (buy), cyan (sell), hot pink (short/cover), red (failure).
/// Clears on TradingPhaseEndedEvent to prevent stale feedback during state transitions.
/// </summary>
public class TradeFeedback : MonoBehaviour
{
    public static readonly Color ShortPink = new Color(1f, 0.4f, 0.7f, 1f);
    public static readonly Color SellCyan = new Color(0.4f, 0.85f, 1f, 1f);

    private Text _feedbackText;
    private CanvasGroup _canvasGroup;
    private float _displayTimer;

    private const float DisplayDuration = 1.5f;
    private const float FadeDuration = 0.5f;

    public void Initialize(Text feedbackText, CanvasGroup canvasGroup)
    {
        _feedbackText = feedbackText;
        _canvasGroup = canvasGroup;
        _canvasGroup.alpha = 0f;

        EventBus.Subscribe<TradeFeedbackEvent>(OnTradeFeedback);
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<TradeFeedbackEvent>(OnTradeFeedback);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
    }

    private void OnTradeFeedback(TradeFeedbackEvent evt)
    {
        Color color = GetFeedbackColor(evt.IsSuccess, evt.IsBuy, evt.IsShort);
        ShowFeedback(evt.Message, color);
    }

    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        _displayTimer = 0f;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    private void ShowFeedback(string message, Color color)
    {
        if (_feedbackText == null || _canvasGroup == null) return;
        _feedbackText.text = message;
        _feedbackText.color = color;
        _canvasGroup.alpha = 1f;
        _displayTimer = DisplayDuration + FadeDuration;
    }

    private void Update()
    {
        if (_displayTimer <= 0f) return;

        _displayTimer -= Time.deltaTime;

        if (_displayTimer <= FadeDuration)
            _canvasGroup.alpha = Mathf.Max(0f, _displayTimer / FadeDuration);

        if (_displayTimer <= 0f)
            _canvasGroup.alpha = 0f;
    }

    // --- Static utility methods for testability ---

    /// <summary>
    /// Returns feedback color based on trade outcome and type.
    /// Failure: red. Short/cover success: hot pink. Buy success: green. Sell success: cyan.
    /// </summary>
    public static Color GetFeedbackColor(bool isSuccess, bool isBuy, bool isShort)
    {
        if (!isSuccess) return TradingHUD.LossRed;
        if (isShort) return ShortPink;
        if (isBuy) return TradingHUD.ProfitGreen;
        return SellCyan;
    }

    /// <summary>
    /// Determines why a short was rejected based on portfolio state.
    /// </summary>
    public static string GetShortRejectionReason(Portfolio portfolio, string stockId)
    {
        if (portfolio.HasShortPosition(stockId))
            return "Already shorting this stock";
        return "Short rejected";
    }

    /// <summary>
    /// Determines why a cover was rejected based on portfolio state.
    /// Distinguishes between no short position and insufficient shares.
    /// </summary>
    public static string GetCoverRejectionReason(Portfolio portfolio, string stockId)
    {
        var shortPos = portfolio.GetShortPosition(stockId);
        if (shortPos != null)
            return "Insufficient shares to cover";
        return "No short position to cover";
    }
}
