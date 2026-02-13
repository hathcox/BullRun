using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Horizontal scrolling news ticker in the bottom bar.
/// Subscribes to MarketEventFiredEvent and scrolls headlines left-to-right.
/// Multiple headlines can be visible simultaneously.
/// </summary>
public class NewsTicker : MonoBehaviour
{
    public static readonly float ScrollSpeed = 100f;
    public static readonly float HeadlineSpacing = 60f;
    public static readonly int FontSize = 14;

    private Transform _scrollContainer;
    private List<TickerEntry> _entries = new List<TickerEntry>();
    private float _containerWidth;

    public int EntryCount => _entries.Count;

    public void Initialize(Transform scrollContainer, float containerWidth)
    {
        _scrollContainer = scrollContainer;
        _containerWidth = containerWidth;

        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (string.IsNullOrEmpty(evt.Headline)) return;
        EnqueueHeadline(evt.Headline, evt.IsPositive);
    }

    private void EnqueueHeadline(string headline, bool isPositive)
    {
        var entryGo = new GameObject("TickerEntry");
        entryGo.transform.SetParent(_scrollContainer, false);

        var rect = entryGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(400f, 0f);

        // Start at right edge of container
        float startX = _containerWidth;
        if (_entries.Count > 0)
        {
            var lastEntry = _entries[_entries.Count - 1];
            float lastRightEdge = lastEntry.Rect.anchoredPosition.x + lastEntry.TextWidth + HeadlineSpacing;
            startX = Mathf.Max(startX, lastRightEdge);
        }
        rect.anchoredPosition = new Vector2(startX, 0f);

        var text = entryGo.AddComponent<Text>();
        text.text = $"\u25C6 {headline}";
        text.color = isPositive ? NewsBanner.PositiveBannerColor : NewsBanner.NegativeBannerColor;
        text.fontSize = FontSize;
        text.fontStyle = FontStyle.Normal;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        float textWidth = CalculateTextWidth(text);

        _entries.Add(new TickerEntry
        {
            Root = entryGo,
            Rect = rect,
            TextWidth = textWidth
        });
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float scrollDelta = ScrollSpeed * dt;

        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Root == null)
            {
                _entries.RemoveAt(i);
                continue;
            }

            var pos = entry.Rect.anchoredPosition;
            pos.x -= scrollDelta;
            entry.Rect.anchoredPosition = pos;

            // Remove when scrolled off the left edge
            if (pos.x + entry.TextWidth < 0f)
            {
                Destroy(entry.Root);
                _entries.RemoveAt(i);
            }
        }
    }

    private float CalculateTextWidth(Text text)
    {
        // Approximate width based on character count and font size
        // Unity's TextGenerator needs a canvas to work properly, so we estimate
        return text.text.Length * FontSize * 0.55f;
    }

    /// <summary>
    /// Static helper for formatting ticker headline text.
    /// </summary>
    public static string FormatHeadline(string headline)
    {
        return $"\u25C6 {headline}";
    }

    private class TickerEntry
    {
        public GameObject Root;
        public RectTransform Rect;
        public float TextWidth;
    }
}
