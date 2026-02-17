using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Act transition interstitial. Shows "ACT X — Tier Name" when the act changes.
/// Subscribes to ActTransitionEvent and displays for a brief dramatic reveal.
/// </summary>
public class ActTransitionUI : MonoBehaviour
{
    private static readonly Color HeaderColor = ColorPalette.Gold;

    private GameObject _panel;
    private Text _headerText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;
    private float _displayTimer;

    /// <summary>
    /// How long the act transition interstitial stays visible.
    /// </summary>
    public const float DisplayDuration = 1.5f;

    /// <summary>
    /// Static accessor for whether the interstitial is currently showing.
    /// </summary>
    public static bool IsShowing { get; private set; }

    public void Initialize(GameObject panel, Text headerText, CanvasGroup canvasGroup)
    {
        _panel = panel;
        _headerText = headerText;
        _canvasGroup = canvasGroup;
        _initialized = true;
        _visible = false;
        IsShowing = false;

        if (_panel != null) _panel.SetActive(false);

        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        if (!_initialized) return;

        if (_headerText != null)
        {
            _headerText.text = BuildDisplayText(evt.NewAct);
            _headerText.color = HeaderColor;
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

        if (_displayTimer >= DisplayDuration)
        {
            _visible = false;
            IsShowing = false;
            if (_panel != null) _panel.SetActive(false);
        }
    }

    // --- Static utility methods for testability ---

    public static string GetHeaderText(int actNumber)
    {
        return $"ACT {actNumber}";
    }

    public static string GetTierDisplayName(int actNumber)
    {
        if (actNumber >= 1 && actNumber < GameConfig.Acts.Length)
            return GameConfig.Acts[actNumber].DisplayName;
        return "Unknown";
    }

    public static string BuildDisplayText(int actNumber)
    {
        return $"{GetHeaderText(actNumber)} \u2014 {GetTierDisplayName(actNumber)}";
    }
}
