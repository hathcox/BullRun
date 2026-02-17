using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows news banner flashes when market events fire.
/// Green banner for positive events, red for negative.
/// Slides down from top, auto-fades after 3 seconds or on event end.
/// Stacks multiple banners with vertical offset.
/// </summary>
public class NewsBanner : MonoBehaviour
{
    // Story 14.6: Banner colors migrated to CRTThemeData
    public static readonly Color PositiveBannerColor = new Color(CRTThemeData.TextHigh.r, CRTThemeData.TextHigh.g, CRTThemeData.TextHigh.b, 0.8f);
    public static readonly Color NegativeBannerColor = new Color(CRTThemeData.Danger.r, CRTThemeData.Danger.g, CRTThemeData.Danger.b, 0.8f);
    public static readonly float BannerDuration = 3f;
    public static readonly float SlideSpeed = 400f;
    public static readonly float BannerHeight = 40f;
    public static readonly float BannerSpacing = 4f;
    public static readonly float SlideDuration = 0.3f;
    public static readonly float FadeDuration = 0.5f;

    private Transform _bannerContainer;
    private List<ActiveBanner> _activeBanners = new List<ActiveBanner>();

    public int ActiveBannerCount => _activeBanners.Count;

    public void Initialize(Transform bannerContainer)
    {
        _bannerContainer = bannerContainer;
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

        var banner = CreateBannerElement(evt.Headline, evt.IsPositive, evt.EventType);
        _activeBanners.Add(banner);
        RepositionBanners();
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        for (int i = _activeBanners.Count - 1; i >= 0; i--)
        {
            if (_activeBanners[i].EventType == evt.EventType)
            {
                if (_activeBanners[i].Root != null)
                    SafeDestroy(_activeBanners[i].Root);
                _activeBanners.RemoveAt(i);
                break; // Remove only the first matching banner, not all of same type
            }
        }
        RepositionBanners();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        for (int i = _activeBanners.Count - 1; i >= 0; i--)
        {
            var banner = _activeBanners[i];
            banner.Elapsed += dt;

            // Slide in animation
            if (banner.Elapsed < SlideDuration)
            {
                float t = banner.Elapsed / SlideDuration;
                float targetY = banner.TargetY;
                banner.Rect.anchoredPosition = new Vector2(0f, Mathf.Lerp(BannerHeight, targetY, t));
            }

            // Auto-fade after duration
            if (banner.Elapsed >= BannerDuration)
            {
                float fadeElapsed = banner.Elapsed - BannerDuration;
                float fadeDuration = FadeDuration;
                if (fadeElapsed < fadeDuration)
                {
                    float alpha = 1f - (fadeElapsed / fadeDuration);
                    if (banner.CanvasGroup != null)
                        banner.CanvasGroup.alpha = alpha;
                }
                else
                {
                    if (banner.Root != null)
                        SafeDestroy(banner.Root);
                    _activeBanners.RemoveAt(i);
                    RepositionBanners();
                }
            }
        }
    }

    private ActiveBanner CreateBannerElement(string headline, bool isPositive, MarketEventType eventType)
    {
        var bannerGo = new GameObject($"Banner_{eventType}");
        bannerGo.transform.SetParent(_bannerContainer, false);

        var rect = bannerGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 1f);
        rect.anchorMax = new Vector2(0.9f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, BannerHeight);
        rect.anchoredPosition = new Vector2(0f, BannerHeight); // Start above (will slide down)

        var bg = bannerGo.AddComponent<Image>();
        bg.color = isPositive ? PositiveBannerColor : NegativeBannerColor;

        var canvasGroup = bannerGo.AddComponent<CanvasGroup>();

        // Headline text
        var textGo = new GameObject("HeadlineText");
        textGo.transform.SetParent(bannerGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 0f);
        textRect.offsetMax = new Vector2(-16f, 0f);

        var text = textGo.AddComponent<Text>();
        text.text = headline;
        text.color = Color.white;
        text.fontSize = 16;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return new ActiveBanner
        {
            Root = bannerGo,
            Rect = rect,
            CanvasGroup = canvasGroup,
            EventType = eventType,
            IsPositive = isPositive,
            Elapsed = 0f,
            TargetY = 0f
        };
    }

    private void RepositionBanners()
    {
        for (int i = 0; i < _activeBanners.Count; i++)
        {
            float y = -(i * (BannerHeight + BannerSpacing));
            _activeBanners[i].TargetY = y;

            if (_activeBanners[i].Elapsed >= SlideDuration)
            {
                _activeBanners[i].Rect.anchoredPosition = new Vector2(0f, y);
            }
        }
    }

    /// <summary>
    /// Returns whether a banner color is positive (green).
    /// Static for testability.
    /// </summary>
    public static Color GetBannerColor(bool isPositive)
    {
        return isPositive ? PositiveBannerColor : NegativeBannerColor;
    }

    private static void SafeDestroy(GameObject go)
    {
        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }

    private class ActiveBanner
    {
        public GameObject Root;
        public RectTransform Rect;
        public CanvasGroup CanvasGroup;
        public MarketEventType EventType;
        public bool IsPositive;
        public float Elapsed;
        public float TargetY;
    }
}
