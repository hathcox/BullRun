using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Round results overlay. Shows "ROUND X COMPLETE" header with round profit,
/// target status, and total cash. Subscribes to RoundCompletedEvent.
/// Displays for a brief duration or until player presses continue.
/// </summary>
public class RoundResultsUI : MonoBehaviour
{
    // Story 14.6: Color constants migrated to CRTThemeData
    private static Color HeaderColor => CRTThemeData.TextHigh;
    private static readonly Color StatsColor = ColorPalette.WhiteDim;
    private static Color CheckmarkColor => CRTThemeData.TextHigh;
    private static Color FailCheckmarkColor => CRTThemeData.Danger;

    private GameObject _panel;
    private Text _headerText;
    private Text _statsText;
    private Text _checkmarkText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;
    private float _displayTimer;

    /// <summary>
    /// How long the results overlay stays visible before auto-dismissing.
    /// </summary>
    public const float DisplayDuration = 2.5f;

    /// <summary>
    /// Static accessor for whether the overlay is currently showing.
    /// Used by state machine to know when results display is done.
    /// </summary>
    public static bool IsShowing { get; private set; }

    public void Initialize(GameObject panel, Text headerText, Text statsText, Text checkmarkText, CanvasGroup canvasGroup)
    {
        _panel = panel;
        _headerText = headerText;
        _statsText = statsText;
        _checkmarkText = checkmarkText;
        _canvasGroup = canvasGroup;
        _initialized = true;
        _visible = false;
        IsShowing = false;

        if (_panel != null) _panel.SetActive(false);

        EventBus.Subscribe<RoundCompletedEvent>(OnRoundCompleted);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RoundCompletedEvent>(OnRoundCompleted);
    }

    private void OnRoundCompleted(RoundCompletedEvent evt)
    {
        if (!_initialized) return;

        if (_headerText != null)
        {
            _headerText.text = GetHeaderText(evt.RoundNumber);
            _headerText.color = HeaderColor;
        }

        if (_statsText != null)
        {
            _statsText.text = BuildStatsText(evt);
            _statsText.color = StatsColor;
        }

        if (_checkmarkText != null)
        {
            _checkmarkText.text = evt.TargetMet ? "\u2713" : "\u2717";
            _checkmarkText.color = evt.TargetMet ? CheckmarkColor : FailCheckmarkColor;
        }

        _visible = true;
        IsShowing = true;
        _displayTimer = 0f;
        if (_panel != null) _panel.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (!_initialized || !_visible) return;

        _displayTimer += Time.deltaTime;

        // Auto-dismiss after display duration or on any key press
        if (_displayTimer >= DisplayDuration || (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame))
        {
            _visible = false;
            IsShowing = false;
            if (_panel != null) _panel.SetActive(false);
        }
    }

    // --- Static utility methods for testability ---

    public static string GetHeaderText(int roundNumber)
    {
        return $"ROUND {roundNumber} COMPLETE";
    }

    public static string FormatProfit(float profit)
    {
        string sign = profit >= 0 ? "+" : "-";
        return $"{sign}${Mathf.Abs(profit):F2}";
    }

    public static string FormatTarget(float target, bool met)
    {
        string status = met ? "PASSED" : "FAILED";
        return $"${target:F2} \u2014 {status}";
    }

    public static string FormatCash(float amount)
    {
        return $"${amount:N2}";
    }

    public static string BuildStatsText(RoundCompletedEvent evt)
    {
        string repLine = evt.BonusRep > 0
            ? $"Reputation Earned: \u2605 {evt.RepEarned} (Base: {evt.BaseRep} + Bonus: {evt.BonusRep})"
            : $"Reputation Earned: \u2605 {evt.RepEarned} (Base: {evt.BaseRep})";

        return $"Round Profit: {FormatProfit(evt.RoundProfit)}\n" +
               $"Target: {FormatTarget(evt.ProfitTarget, evt.TargetMet)}\n" +
               $"Total Cash: {FormatCash(evt.TotalCash)}\n" +
               repLine;
    }
}
