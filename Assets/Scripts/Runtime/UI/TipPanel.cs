using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Story 18.4: Compact tip indicator strip for the trading HUD.
/// Displays badge dots for chart-overlay tips and a live countdown for EventCount tips.
/// Subscribes to EventBus events — never references game systems directly.
/// </summary>
public class TipPanel : MonoBehaviour
{
    // AC 6: Pulse animation constants
    public static readonly float PulseDuration = 0.15f;
    public static readonly float PulseScale = 1.2f;

    private int _eventCountdown = -1;   // -1 = no event count tip active
    private float[] _eventFireTimes;    // absolute seconds from round start, sorted
    private int _nextEventIndex;        // index of next unfired event in _eventFireTimes
    private Text _countdownText;
    private RectTransform _countdownRect;
    private readonly List<GameObject> _badgeSlots = new List<GameObject>();
    private GameObject _panelRoot;
    private Tweener _pulseTween;

    public void Initialize(GameObject panelRoot, Text countdownText)
    {
        _panelRoot = panelRoot;
        _countdownText = countdownText;
        _countdownRect = countdownText != null ? countdownText.GetComponent<RectTransform>() : null;
        _panelRoot.SetActive(false); // Hidden until tips arrive

        EventBus.Subscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ShopOpenedEvent>(OnShopOpened);
    }

    private void OnDestroy()
    {
        _pulseTween?.Kill();
        EventBus.Unsubscribe<TipOverlaysActivatedEvent>(OnTipOverlaysActivated);
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<ShopOpenedEvent>(OnShopOpened);
    }

    // ── Event Handlers ──────────────────────────────────────────────────

    private void OnTipOverlaysActivated(TipOverlaysActivatedEvent evt)
    {
        // Clear previous state
        ClearBadges();
        _eventCountdown = -1;

        if (evt.Overlays == null || evt.Overlays.Count == 0)
        {
            _panelRoot.SetActive(false);
            return;
        }

        bool hasCountdown = false;
        foreach (var overlay in evt.Overlays)
        {
            if (overlay.Type == InsiderTipType.EventCount && overlay.EventCountdown >= 0)
            {
                _eventCountdown = overlay.EventCountdown;
                _eventFireTimes = overlay.EventFireTimes;
                _nextEventIndex = 0;
                hasCountdown = true;
            }
            else
            {
                CreateBadge(overlay.Type, _panelRoot.transform);
            }
        }

        // Show/hide countdown text
        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(hasCountdown);
            if (hasCountdown)
                UpdateCountdownDisplay();
        }

        _panelRoot.SetActive(true);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[TipPanel] Activated with {evt.Overlays.Count} overlays, EventCountdown={_eventCountdown}");
        #endif
    }

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        if (_eventCountdown <= 0) return;

        int old = _eventCountdown;
        _eventCountdown--;
        _nextEventIndex++;
        UpdateCountdownDisplay();

        // AC 6: Pulse animation on decrement
        if (_countdownRect != null)
            PlayPulse(_countdownRect);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[TipPanel] Event countdown: {old} -> {_eventCountdown}");
        if (_eventCountdown == 0)
            Debug.Log("[TipPanel] ALL CLEAR — no remaining events");
        #endif
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        ClearBadges();
        _eventCountdown = -1;
        _eventFireTimes = null;
        _nextEventIndex = 0;
        _pulseTween?.Kill();
        if (_countdownText != null)
            _countdownText.gameObject.SetActive(false);
        _panelRoot.SetActive(false);
    }

    private void OnShopOpened(ShopOpenedEvent evt)
    {
        ClearBadges();
        _eventCountdown = -1;
        _eventFireTimes = null;
        _nextEventIndex = 0;
        _pulseTween?.Kill();
        if (_countdownText != null)
            _countdownText.gameObject.SetActive(false);
        _panelRoot.SetActive(false);
    }

    // ── Per-Frame Update ────────────────────────────────────────────────

    private void Update()
    {
        if (_eventCountdown <= 0 || !TradingState.IsActive) return;
        if (_countdownText == null) return;

        float secondsUntilNext = GetSecondsUntilNextEvent();
        _countdownText.text = FormatCountdownText(_eventCountdown, secondsUntilNext);
    }

    // ── Internal Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Returns seconds until the next event fires, or -1 if no more events.
    /// </summary>
    private float GetSecondsUntilNextEvent()
    {
        if (_eventFireTimes == null || _nextEventIndex >= _eventFireTimes.Length)
            return -1f;

        float elapsed = TradingState.ActiveRoundDuration - TradingState.ActiveTimeRemaining;
        float secondsUntil = _eventFireTimes[_nextEventIndex] - elapsed;
        return secondsUntil > 0f ? secondsUntil : 0f;
    }

    private void UpdateCountdownDisplay()
    {
        if (_countdownText == null) return;

        if (_eventCountdown <= 0)
        {
            _countdownText.text = FormatCountdownText(_eventCountdown);
            _countdownText.color = _eventCountdown == 0
                ? ColorPalette.Green
                : CRTThemeData.TextHigh;
        }
        else
        {
            float secondsUntil = GetSecondsUntilNextEvent();
            _countdownText.text = FormatCountdownText(_eventCountdown, secondsUntil);
            _countdownText.color = CRTThemeData.TextHigh;
        }
    }

    private void PlayPulse(RectTransform target)
    {
        _pulseTween?.Kill();
        target.localScale = Vector3.one;
        _pulseTween = target.DOScale(PulseScale, PulseDuration * 0.5f)
            .SetUpdate(true)
            .OnComplete(() =>
                _pulseTween = target.DOScale(1f, PulseDuration * 0.5f).SetUpdate(true));
    }

    private void CreateBadge(InsiderTipType type, Transform parent)
    {
        var badgeGo = new GameObject($"Badge_{type}");
        badgeGo.transform.SetParent(parent, false);

        // Background dot (24x24)
        var badgeRect = badgeGo.AddComponent<RectTransform>();
        badgeRect.sizeDelta = new Vector2(24f, 24f);

        var bgImage = badgeGo.AddComponent<Image>();
        bgImage.color = ColorPalette.WithAlpha(GetBadgeColor(type), 0.7f);
        bgImage.raycastTarget = false;

        // Abbreviation label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(badgeGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelGo.AddComponent<Text>();
        labelText.text = GetBadgeAbbreviation(type);
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 9;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = ColorPalette.Background;
        labelText.raycastTarget = false;

        // Add LayoutElement for HorizontalLayoutGroup sizing
        var layout = badgeGo.AddComponent<LayoutElement>();
        layout.preferredWidth = 24f;
        layout.preferredHeight = 24f;

        _badgeSlots.Add(badgeGo);
    }

    private void ClearBadges()
    {
        foreach (var slot in _badgeSlots)
        {
            if (slot != null) Object.Destroy(slot);
        }
        _badgeSlots.Clear();
    }

    // ── Static Utility Methods (Pure Logic, Testable) ───────────────────

    /// <summary>
    /// Returns formatted countdown text. "ALL CLEAR" when count is 0,
    /// "EVENTS: X" when count > 0, empty string when count less than 0 (inactive).
    /// </summary>
    public static string FormatCountdownText(int count)
    {
        if (count < 0) return "";
        if (count == 0) return "ALL CLEAR";
        return $"EVENTS: {count}";
    }

    /// <summary>
    /// Returns formatted countdown text with seconds until the next event.
    /// Shows "EVENTS: X | NEXT: Ns" when countdown is available,
    /// falls back to the base format otherwise.
    /// </summary>
    public static string FormatCountdownText(int count, float secondsUntilNext)
    {
        if (count <= 0 || secondsUntilNext < 0f) return FormatCountdownText(count);
        int secs = Mathf.CeilToInt(secondsUntilNext);
        return $"EVENTS: {count} | NEXT: {secs}s";
    }

    /// <summary>
    /// Returns the 2-3 letter badge abbreviation for a tip type.
    /// Chart overlay tips get short labels; EventCount returns "EVT" (but is
    /// displayed as the live countdown, not a badge).
    /// </summary>
    public static string GetBadgeAbbreviation(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceFloor:        return "FLR";
            case InsiderTipType.PriceCeiling:      return "CLG";
            case InsiderTipType.PriceForecast:      return "FC";
            case InsiderTipType.DipMarker:         return "DIP";
            case InsiderTipType.PeakMarker:        return "PK";
            case InsiderTipType.ClosingDirection:  return "DIR";
            case InsiderTipType.EventTiming:       return "ET";
            case InsiderTipType.TrendReversal:     return "TR";
            case InsiderTipType.EventCount:        return "EVT";
            default:                               return "?";
        }
    }

    /// <summary>
    /// Returns the badge dot color for a given tip type.
    /// Uses ColorPalette colors keyed to tip category.
    /// </summary>
    public static Color GetBadgeColor(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceFloor:        return ColorPalette.Cyan;
            case InsiderTipType.PriceCeiling:      return ColorPalette.Amber;
            case InsiderTipType.PriceForecast:      return ColorPalette.Cyan;
            case InsiderTipType.DipMarker:         return ColorPalette.Green;
            case InsiderTipType.PeakMarker:        return ColorPalette.Amber;
            case InsiderTipType.ClosingDirection:  return ColorPalette.White;
            case InsiderTipType.EventTiming:       return ColorPalette.Red;
            case InsiderTipType.TrendReversal:     return ColorPalette.Cyan;
            case InsiderTipType.EventCount:        return ColorPalette.Green;
            default:                               return ColorPalette.WhiteDim;
        }
    }
}
