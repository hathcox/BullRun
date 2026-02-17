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

    // Story 14.6: Color constants migrated to CRTThemeData
    private static Color NormalColor => CRTThemeData.TextHigh;
    private static Color UrgencyColor => CRTThemeData.Warning;
    private static Color CriticalColor => CRTThemeData.Danger;

    private Text _timerText;
    private Image _progressBarFill;
    private RectTransform _timerTextRect;
    private GameObject _container;

    private bool _initialized;
    private bool _wasActive;

    public void Initialize(Text timerText, Image progressBarFill, GameObject container)
    {
        _timerText = timerText;
        _progressBarFill = progressBarFill;
        _timerTextRect = timerText != null ? timerText.GetComponent<RectTransform>() : null;
        _container = container;
        _initialized = true;
        _wasActive = false;

        // Start hidden until TradingState activates
        if (_container != null) _container.SetActive(false);
    }

    private void Update()
    {
        if (!_initialized) return;

        bool isActive = TradingState.IsActive;

        // Show/hide container when TradingState activates/deactivates
        if (isActive != _wasActive)
        {
            _wasActive = isActive;
            if (_container != null) _container.SetActive(isActive);
        }

        if (!isActive) return;

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
