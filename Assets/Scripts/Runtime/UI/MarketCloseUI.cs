using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Market Close overlay. Shows "MARKET CLOSED" text and final round profit.
/// Subscribes to MarketClosedEvent and hides when MarketCloseState becomes inactive.
/// Flash effect on appear, then fade to steady state with profit display.
/// </summary>
public class MarketCloseUI : MonoBehaviour
{
    // Story 14.6: Color constants migrated to CRTThemeData
    private static Color ProfitColor => CRTThemeData.TextHigh;
    private static Color LossColor => CRTThemeData.Danger;
    private static readonly Color HeaderColor = ColorPalette.Red;

    private GameObject _panel;
    private Text _headerText;
    private Text _profitText;
    private CanvasGroup _canvasGroup;

    private bool _initialized;
    private bool _visible;
    private float _effectTimer;

    // Flash: starts at full alpha, dips to 0, then fades back in — punctuates the moment
    private static readonly float FlashDuration = 0.15f;
    private static readonly float FadeInDuration = 0.3f;
    private static readonly float TotalEffectDuration = FlashDuration + FadeInDuration;

    public void Initialize(GameObject panel, Text headerText, Text profitText, CanvasGroup canvasGroup)
    {
        _panel = panel;
        _headerText = headerText;
        _profitText = profitText;
        _canvasGroup = canvasGroup;
        _initialized = true;
        _visible = false;

        if (_panel != null) _panel.SetActive(false);

        EventBus.Subscribe<MarketClosedEvent>(OnMarketClosed);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MarketClosedEvent>(OnMarketClosed);
    }

    private void OnMarketClosed(MarketClosedEvent evt)
    {
        if (!_initialized) return;

        if (_headerText != null)
        {
            _headerText.text = "MARKET CLOSED";
            _headerText.color = HeaderColor;
        }

        if (_profitText != null)
        {
            _profitText.text = FormatProfit(evt.RoundProfit);
            _profitText.color = evt.RoundProfit >= 0 ? ProfitColor : LossColor;
        }

        _visible = true;
        _effectTimer = 0f;
        if (_panel != null) _panel.SetActive(true);
        // Start at full alpha for the flash punch, then dip and fade back in
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        if (!_initialized || !_visible) return;

        _effectTimer += Time.deltaTime;

        if (_canvasGroup != null)
        {
            if (_effectTimer < FlashDuration)
            {
                // Flash phase: bright flash decays to black (alpha 1 → 0)
                float flashProgress = _effectTimer / FlashDuration;
                _canvasGroup.alpha = 1f - flashProgress;
            }
            else if (_effectTimer < TotalEffectDuration)
            {
                // Fade-in phase: panel fades back in from black (alpha 0 → 1)
                float fadeProgress = (_effectTimer - FlashDuration) / FadeInDuration;
                _canvasGroup.alpha = Mathf.Clamp01(fadeProgress);
            }
            else
            {
                _canvasGroup.alpha = 1f;
            }
        }

        // Hide when MarketCloseState is no longer active
        if (!MarketCloseState.IsActive)
        {
            _visible = false;
            if (_panel != null) _panel.SetActive(false);
        }
    }

    // --- Static utility methods for testability ---

    /// <summary>
    /// Formats a profit value as "+$650" or "-$120".
    /// </summary>
    public static string FormatProfit(float profit)
    {
        string sign = profit >= 0 ? "+" : "-";
        return $"{sign}${Mathf.Abs(profit):F0}";
    }
}
