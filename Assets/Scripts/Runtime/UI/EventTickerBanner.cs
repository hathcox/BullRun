using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Story 14.5: Amber event ticker banner positioned between stock label and chart area.
/// Shows market event headlines with warning icon prefix.
/// Replaces the top-of-screen NewsBanner for event display.
/// </summary>
public class EventTickerBanner : MonoBehaviour
{
    public static readonly float BannerHeight = 36f;
    public static readonly float FadeInDuration = 0.3f;
    public static readonly float DisplayDuration = 3f;
    public static readonly float FadeOutDuration = 0.5f;

    private GameObject _bannerPanel;
    private Image _bannerBackground;
    private Text _headlineText;
    private CanvasGroup _canvasGroup;
    private readonly Queue<QueuedHeadline> _headlineQueue = new Queue<QueuedHeadline>();
    private float _elapsed;
    private bool _isShowing;
    private MarketEventType _currentEventType;

    public bool IsShowing => _isShowing;
    public int QueuedCount => _headlineQueue.Count;

    public void Initialize(GameObject bannerPanel, Image background, Text headlineText, CanvasGroup canvasGroup)
    {
        _bannerPanel = bannerPanel;
        _bannerBackground = background;
        _headlineText = headlineText;
        _canvasGroup = canvasGroup;

        // Start hidden
        _bannerPanel.SetActive(false);
        _canvasGroup.alpha = 0f;

        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Headline)) return;

        if (!_isShowing)
        {
            ShowHeadline(evt.Headline, evt.EventType);
        }
        else
        {
            _headlineQueue.Enqueue(new QueuedHeadline { Headline = evt.Headline, EventType = evt.EventType });
        }
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        if (_isShowing && _currentEventType == evt.EventType)
        {
            // Fast-forward to fade out
            _elapsed = FadeInDuration + DisplayDuration;
        }

        // Remove from queue if queued — rebuild queue excluding the matched event
        if (_headlineQueue.Count > 0)
        {
            int count = _headlineQueue.Count;
            bool removed = false;
            for (int i = 0; i < count; i++)
            {
                var item = _headlineQueue.Dequeue();
                if (!removed && item.EventType == evt.EventType)
                {
                    removed = true;
                    continue;
                }
                _headlineQueue.Enqueue(item);
            }
        }
    }

    private void ShowHeadline(string headline, MarketEventType eventType)
    {
        _isShowing = true;
        _currentEventType = eventType;
        _elapsed = 0f;
        _headlineText.text = "\u26A0 " + headline; // ⚠ prefix
        _bannerPanel.SetActive(true);
        _canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        if (!_isShowing) return;

        _elapsed += Time.deltaTime;

        float totalDuration = FadeInDuration + DisplayDuration + FadeOutDuration;

        if (_elapsed < FadeInDuration)
        {
            // Fade in
            _canvasGroup.alpha = _elapsed / FadeInDuration;
        }
        else if (_elapsed < FadeInDuration + DisplayDuration)
        {
            // Fully visible
            _canvasGroup.alpha = 1f;
        }
        else if (_elapsed < totalDuration)
        {
            // Fade out
            float fadeElapsed = _elapsed - FadeInDuration - DisplayDuration;
            _canvasGroup.alpha = 1f - (fadeElapsed / FadeOutDuration);
        }
        else
        {
            // Done — check queue for next headline
            _isShowing = false;
            _bannerPanel.SetActive(false);
            _canvasGroup.alpha = 0f;

            if (_headlineQueue.Count > 0)
            {
                var next = _headlineQueue.Dequeue();
                ShowHeadline(next.Headline, next.EventType);
            }
        }
    }

    /// <summary>
    /// Returns the CRT amber warning color at 85% alpha for the banner background.
    /// Static for testability.
    /// </summary>
    public static Color GetBannerColor()
    {
        return new Color(CRTThemeData.Warning.r, CRTThemeData.Warning.g, CRTThemeData.Warning.b, 0.85f);
    }

    private struct QueuedHeadline
    {
        public string Headline;
        public MarketEventType EventType;
    }
}
