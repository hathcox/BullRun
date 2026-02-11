using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Round timer display with countdown, progress bar, and urgency visual cues.
/// Subscribes to RoundStartedEvent for timer start. Reads TradingState.TimeRemaining each frame.
/// Color transitions: white/green (normal) → yellow (15s) → red pulsing (5s).
/// </summary>
public class RoundTimerUI : MonoBehaviour
{
    public static readonly float UrgencyThreshold = 15f;
    public static readonly float CriticalThreshold = 5f;
    public static readonly float PulseSpeed = 4f;
    public static readonly float PulseMinScale = 0.85f;
    public static readonly float PulseMaxScale = 1.15f;

    private static readonly Color NormalColor = new Color(0f, 1f, 0.533f, 1f); // Neon green
    private static readonly Color UrgencyColor = new Color(1f, 0.85f, 0.2f, 1f); // Yellow
    private static readonly Color CriticalColor = new Color(1f, 0.2f, 0.2f, 1f); // Red

    private Text _timerText;
    private Image _progressBarFill;
    private RectTransform _timerTextRect;

    private bool _initialized;

    public void Initialize(Text timerText, Image progressBarFill)
    {
        _timerText = timerText;
        _progressBarFill = progressBarFill;
        _timerTextRect = timerText != null ? timerText.GetComponent<RectTransform>() : null;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized || !TradingState.IsActive) return;

        // Read authoritative timer values from TradingState (one-way UI dependency)
        float timeRemaining = TradingState.ActiveTimeRemaining;
        float roundDuration = TradingState.ActiveRoundDuration;

        RefreshDisplay(timeRemaining, roundDuration);
    }

    private void RefreshDisplay(float timeRemaining, float roundDuration)
    {
        if (_timerText != null)
        {
            _timerText.text = FormatTime(timeRemaining);
            _timerText.color = GetTimerColor(timeRemaining);
        }

        if (_progressBarFill != null)
        {
            _progressBarFill.fillAmount = GetProgressFraction(timeRemaining, roundDuration);
            _progressBarFill.color = GetTimerColor(timeRemaining);
        }

        // Pulse animation at critical threshold
        if (_timerTextRect != null && timeRemaining <= CriticalThreshold && timeRemaining > 0f)
        {
            float pulse = Mathf.Lerp(PulseMinScale, PulseMaxScale,
                (Mathf.Sin(Time.time * PulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f);
            _timerTextRect.localScale = new Vector3(pulse, pulse, 1f);
        }
        else if (_timerTextRect != null)
        {
            _timerTextRect.localScale = Vector3.one;
        }
    }

    // --- Static utility methods for testability ---

    /// <summary>
    /// Formats seconds to "M:SS" display format.
    /// </summary>
    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int totalSeconds = Mathf.CeilToInt(seconds);
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return $"{minutes}:{secs:D2}";
    }

    /// <summary>
    /// Returns the timer color based on remaining time.
    /// Green (normal) → Yellow (urgency at 15s) → Red (critical at 5s).
    /// </summary>
    public static Color GetTimerColor(float timeRemaining)
    {
        if (timeRemaining <= CriticalThreshold) return CriticalColor;
        if (timeRemaining <= UrgencyThreshold) return UrgencyColor;
        return NormalColor;
    }

    /// <summary>
    /// Returns progress bar fraction (1.0 = full, 0.0 = empty).
    /// </summary>
    public static float GetProgressFraction(float timeRemaining, float totalDuration)
    {
        if (totalDuration <= 0f) return 0f;
        return Mathf.Clamp01(timeRemaining / totalDuration);
    }
}
