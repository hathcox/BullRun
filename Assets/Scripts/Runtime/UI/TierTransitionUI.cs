using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen tier transition overlay. Shows "ACT X" (large), tier subtitle, and tagline
/// with fade-in (0.5s), hold (2s), fade-out (0.5s) animation.
/// Subscribes to ActTransitionEvent and blocks interaction during display.
/// Replaces the simpler ActTransitionUI for richer act change presentation.
/// </summary>
public class TierTransitionUI : MonoBehaviour
{
    public const float FadeInDuration = 0.5f;
    public const float HoldDuration = 2f;
    public const float FadeOutDuration = 0.5f;
    public static readonly float TotalDuration = FadeInDuration + HoldDuration + FadeOutDuration;

    public static bool IsShowing { get; private set; }

    private GameObject _panel;
    private Text _actHeaderText;
    private Text _subtitleText;
    private Text _taglineText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;
    private float _displayTimer;

    public void Initialize(GameObject panel, Text actHeaderText, Text subtitleText,
        Text taglineText, CanvasGroup canvasGroup)
    {
        _panel = panel;
        _actHeaderText = actHeaderText;
        _subtitleText = subtitleText;
        _taglineText = taglineText;
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

        if (_actHeaderText != null)
            _actHeaderText.text = GetActHeader(evt.NewAct);

        if (_subtitleText != null)
            _subtitleText.text = GetTierSubtitle(evt.NewAct);

        if (_taglineText != null)
            _taglineText.text = GetTagline(evt.NewAct);

        _visible = true;
        IsShowing = true;
        _displayTimer = 0f;

        if (_panel != null) _panel.SetActive(true);
        if (_canvasGroup != null) _canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (!_initialized || !_visible) return;

        _displayTimer += Time.deltaTime;

        if (_canvasGroup != null)
            _canvasGroup.alpha = CalculateAlpha(_displayTimer);

        if (_displayTimer >= TotalDuration)
        {
            _visible = false;
            IsShowing = false;
            if (_panel != null) _panel.SetActive(false);
        }
    }

    // --- Static utility methods for testability ---

    public static string GetActHeader(int actNumber)
    {
        return $"ACT {actNumber}";
    }

    public static string GetTierSubtitle(int actNumber)
    {
        if (actNumber >= 1 && actNumber < GameConfig.Acts.Length)
            return GameConfig.Acts[actNumber].DisplayName.ToUpper();
        return "UNKNOWN";
    }

    public static string GetTagline(int actNumber)
    {
        if (actNumber >= 1 && actNumber < GameConfig.Acts.Length)
            return GameConfig.Acts[actNumber].Tagline;
        return "";
    }

    /// <summary>
    /// Calculates the alpha value for the fade-in/hold/fade-out animation.
    /// Phase 1 (0 to FadeInDuration): Linear fade from 0 to 1.
    /// Phase 2 (FadeInDuration to FadeInDuration + HoldDuration): Hold at 1.
    /// Phase 3 (after hold to TotalDuration): Linear fade from 1 to 0.
    /// </summary>
    public static float CalculateAlpha(float elapsed)
    {
        if (elapsed <= 0f) return 0f;
        if (elapsed >= TotalDuration) return 0f;

        // Phase 1: Fade in
        if (elapsed < FadeInDuration)
            return Mathf.Clamp01(elapsed / FadeInDuration);

        // Phase 2: Hold
        float holdEnd = FadeInDuration + HoldDuration;
        if (elapsed < holdEnd)
            return 1f;

        // Phase 3: Fade out
        float fadeOutProgress = (elapsed - holdEnd) / FadeOutDuration;
        return Mathf.Clamp01(1f - fadeOutProgress);
    }
}
