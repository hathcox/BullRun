using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dramatic center-screen popup for market events.
/// Pauses the game (Time.timeScale = 0), displays headline with directional indicator,
/// then flies the popup up (positive) or down (negative) before resuming gameplay.
/// Queues multiple rapid events to avoid stacking pauses.
/// </summary>
public class EventPopup : MonoBehaviour
{
    // --- Configuration ---
    public static readonly float PauseDuration = 1.2f;
    public static readonly float QueuedPauseDuration = 0.8f;
    public static readonly float FlyDuration = 0.4f;
    public static readonly float FlyDistance = 1200f;
    public static readonly float FlyScaleEnd = 1.2f;
    public static readonly Color PositiveColor = new Color(0f, 0.8f, 0.3f, 0.85f);
    public static readonly Color NegativeColor = new Color(0.9f, 0f, 0.2f, 0.85f);
    public static readonly Color PositiveTextColor = new Color(0.7f, 1f, 0.7f, 1f);
    public static readonly Color NegativeTextColor = new Color(1f, 0.7f, 0.7f, 1f);
    public static readonly string UpArrow = "\u25B2";
    public static readonly string DownArrow = "\u25BC";

    private GameObject _popupRoot;
    private Image _backgroundImage;
    private Text _arrowText;
    private Text _headlineText;
    private Text _tickerText;
    private CanvasGroup _canvasGroup;
    private RectTransform _popupRect;

    private Queue<MarketEventFiredEvent> _eventQueue = new Queue<MarketEventFiredEvent>();
    private bool _isActive;
    private bool _isFirstEvent = true;
    private float _savedTimeScale = 1f;

    public bool IsActive => _isActive;
    public int QueueCount => _eventQueue.Count;

    public void Initialize(
        GameObject popupRoot,
        Image backgroundImage,
        Text arrowText,
        Text headlineText,
        Text tickerText,
        CanvasGroup canvasGroup,
        RectTransform popupRect)
    {
        _popupRoot = popupRoot;
        _backgroundImage = backgroundImage;
        _arrowText = arrowText;
        _headlineText = headlineText;
        _tickerText = tickerText;
        _canvasGroup = canvasGroup;
        _popupRect = popupRect;

        _popupRoot.SetActive(false);

        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);

        // Safety: restore timeScale if destroyed while active
        if (_isActive)
        {
            Time.timeScale = _savedTimeScale;
        }
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Headline)) return;

        if (_isActive)
        {
            _eventQueue.Enqueue(evt);
            return;
        }

        ShowPopup(evt, true);
    }

    private void ShowPopup(MarketEventFiredEvent evt, bool isFirst)
    {
        _isActive = true;
        _isFirstEvent = isFirst;

        // Configure visuals based on positive/negative
        bool positive = evt.IsPositive;
        _backgroundImage.color = positive ? PositiveColor : NegativeColor;
        _arrowText.text = positive ? UpArrow : DownArrow;
        _arrowText.color = positive ? PositiveTextColor : NegativeTextColor;
        _headlineText.text = evt.Headline;
        _headlineText.color = Color.white;

        // Show affected tickers
        if (evt.AffectedTickerSymbols != null && evt.AffectedTickerSymbols.Length > 0)
        {
            _tickerText.text = string.Join("  ", evt.AffectedTickerSymbols);
        }
        else
        {
            _tickerText.text = "ALL STOCKS";
        }
        _tickerText.color = positive ? PositiveTextColor : NegativeTextColor;

        // Reset popup position and appearance
        _popupRect.anchoredPosition = Vector2.zero;
        _popupRect.localScale = Vector3.one;
        _canvasGroup.alpha = 1f;
        _popupRoot.SetActive(true);

        // Pause the game
        _savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Start the popup sequence using unscaled time
        StartCoroutine(PopupSequence(positive, isFirst));
    }

    private IEnumerator PopupSequence(bool isPositive, bool isFirst)
    {
        // Pause duration — shorter for queued events
        float pause = isFirst ? PauseDuration : QueuedPauseDuration;
        yield return new WaitForSecondsRealtime(pause);

        // Fly animation using unscaled time
        float elapsed = 0f;
        float direction = isPositive ? 1f : -1f;
        Vector2 startPos = Vector2.zero;

        while (elapsed < FlyDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / FlyDuration);

            // Ease-in curve (start slow, accelerate out)
            float eased = t * t;

            // Translate
            float yOffset = eased * FlyDistance * direction;
            _popupRect.anchoredPosition = new Vector2(startPos.x, startPos.y + yOffset);

            // Scale up slightly
            float scale = Mathf.Lerp(1f, FlyScaleEnd, eased);
            _popupRect.localScale = new Vector3(scale, scale, 1f);

            // Fade out
            _canvasGroup.alpha = 1f - eased;

            yield return null;
        }

        // Animation complete — hide popup
        _popupRoot.SetActive(false);

        // Restore timeScale
        Time.timeScale = _savedTimeScale;

        _isActive = false;

        // Check queue for next event
        if (_eventQueue.Count > 0)
        {
            var nextEvt = _eventQueue.Dequeue();
            ShowPopup(nextEvt, false);
        }
    }

    /// <summary>
    /// Returns the appropriate arrow string for an event direction.
    /// Static for testability.
    /// </summary>
    public static string GetDirectionArrow(bool isPositive)
    {
        return isPositive ? UpArrow : DownArrow;
    }

    /// <summary>
    /// Returns the appropriate background color for an event direction.
    /// Static for testability.
    /// </summary>
    public static Color GetPopupColor(bool isPositive)
    {
        return isPositive ? PositiveColor : NegativeColor;
    }
}
