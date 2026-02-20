using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Story 17.8: Displays owned relics during the trading phase with hover tooltips
/// and activation glow effects. Created by UISetup, initialized at runtime.
/// Subscribes to EventBus events — never references game systems directly.
/// </summary>
public class RelicBar : MonoBehaviour
{
    private RunContext _ctx;
    private readonly List<GameObject> _iconSlots = new List<GameObject>();
    private readonly Dictionary<string, float> _glowTimers = new Dictionary<string, float>();
    private readonly Dictionary<string, Image> _iconImages = new Dictionary<string, Image>();
    private readonly Dictionary<string, Color> _iconOriginalColors = new Dictionary<string, Color>();

    // Cached lists to avoid per-frame allocations in Update()
    private readonly List<string> _keysToRemove = new List<string>();
    private readonly List<string> _keysBuffer = new List<string>();

    // Tooltip references (set by Initialize)
    private GameObject _tooltipPanel;
    private Text _tooltipNameText;
    private Text _tooltipDescText;
    private Text _tooltipEffectText;
    private CanvasGroup _tooltipCanvasGroup;

    // Layout parent for icons
    private Transform _iconParent;

    // Glow config
    private static readonly float GlowDuration = 0.3f;
    private static readonly Color GlowColor = ColorPalette.White;
    private static readonly Color IconBgColor = CRTThemeData.Panel;

    public void Initialize(RunContext ctx, Transform iconParent, GameObject tooltipPanel,
        Text tooltipNameText, Text tooltipDescText, Text tooltipEffectText)
    {
        _ctx = ctx;
        _iconParent = iconParent;
        _tooltipPanel = tooltipPanel;
        _tooltipNameText = tooltipNameText;
        _tooltipDescText = tooltipDescText;
        _tooltipEffectText = tooltipEffectText;

        if (_tooltipPanel != null)
            _tooltipCanvasGroup = _tooltipPanel.GetComponent<CanvasGroup>();

        // Start hidden — shows on RoundStartedEvent
        gameObject.SetActive(false);

        // Hide tooltip initially
        HideTooltip();

        // Subscribe to EventBus events
        EventBus.Subscribe<RelicActivatedEvent>(OnRelicActivated);
        EventBus.Subscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
        EventBus.Subscribe<ShopItemSoldEvent>(OnShopItemSold);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Subscribe<MarketClosedEvent>(OnMarketClosed);
        EventBus.Subscribe<ReturnToMenuEvent>(OnReturnToMenu);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RelicActivatedEvent>(OnRelicActivated);
        EventBus.Unsubscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
        EventBus.Unsubscribe<ShopItemSoldEvent>(OnShopItemSold);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Unsubscribe<MarketClosedEvent>(OnMarketClosed);
        EventBus.Unsubscribe<ReturnToMenuEvent>(OnReturnToMenu);
    }

    // ════════════════════════════════════════════════════════════════════
    // Icon Management
    // ════════════════════════════════════════════════════════════════════

    public void RefreshRelicIcons()
    {
        // Clear existing icons
        for (int i = 0; i < _iconSlots.Count; i++)
        {
            if (_iconSlots[i] != null)
            {
                if (Application.isPlaying)
                    Destroy(_iconSlots[i]);
                else
                    DestroyImmediate(_iconSlots[i]);
            }
        }
        _iconSlots.Clear();
        _iconImages.Clear();
        _iconOriginalColors.Clear();
        _glowTimers.Clear();

        if (_ctx == null || _ctx.RelicManager == null) return;

        var relics = _ctx.RelicManager.OrderedRelics;
        for (int i = 0; i < relics.Count; i++)
        {
            var relic = relics[i];
            var slot = CreateIconSlot(relic.Id);
            _iconSlots.Add(slot);
        }
    }

    private GameObject CreateIconSlot(string relicId)
    {
        var slotGo = new GameObject($"RelicIcon_{relicId}");
        slotGo.transform.SetParent(_iconParent, false);

        var rect = slotGo.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40f, 40f);

        // Background image
        var bgImage = slotGo.AddComponent<Image>();
        bgImage.color = IconBgColor;
        _iconImages[relicId] = bgImage;
        _iconOriginalColors[relicId] = IconBgColor;

        // Layout element for HorizontalLayoutGroup
        var layout = slotGo.AddComponent<LayoutElement>();
        layout.preferredWidth = 40f;
        layout.preferredHeight = 40f;

        // Text overlay with relic initial character
        var textGo = new GameObject("Label");
        textGo.transform.SetParent(slotGo.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = CRTThemeData.TextHigh;
        text.raycastTarget = false;

        // Get icon character from relic def
        var def = ItemLookup.GetRelicById(relicId);
        if (def.HasValue)
        {
            text.text = GetRelicIconChar(def.Value.Name);
        }
        else
        {
            text.text = "?";
        }

        // Add hover handler for tooltip
        AddHoverHandler(slotGo, relicId);

        return slotGo;
    }

    /// <summary>
    /// Returns a short text icon for the relic (first 2 chars of name).
    /// Story 17.10 will replace this with proper icon characters.
    /// </summary>
    internal static string GetRelicIconChar(string relicName)
    {
        if (string.IsNullOrEmpty(relicName)) return "?";
        return relicName.Length >= 2 ? relicName.Substring(0, 2).ToUpper() : relicName.ToUpper();
    }

    // ════════════════════════════════════════════════════════════════════
    // Tooltip (Task 2)
    // ════════════════════════════════════════════════════════════════════

    private void AddHoverHandler(GameObject slot, string relicId)
    {
        var trigger = slot.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => ShowTooltip(relicId, slot.GetComponent<RectTransform>()));
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exitEntry);
    }

    private void ShowTooltip(string relicId, RectTransform iconRect)
    {
        if (_tooltipPanel == null) return;

        var def = ItemLookup.GetRelicById(relicId);
        if (!def.HasValue) return;

        // Populate tooltip text
        if (_tooltipNameText != null)
            _tooltipNameText.text = $"<b>{def.Value.Name}</b>";
        if (_tooltipDescText != null)
            _tooltipDescText.text = def.Value.Description;

        string effectText = def.Value.EffectDescription;

        // AC 6: Special case for Compound Rep — show dynamic sell value
        if (relicId == "relic_compound_rep" && _ctx != null && _ctx.RelicManager != null)
        {
            var relicInstance = _ctx.RelicManager.GetRelicById(relicId);
            if (relicInstance != null)
            {
                int? sellValue = relicInstance.GetSellValue(_ctx);
                if (sellValue.HasValue)
                    effectText += $"\nSell value: {sellValue.Value} Rep";
            }
        }

        if (_tooltipEffectText != null)
            _tooltipEffectText.text = effectText;

        // Position tooltip above the icon, clamped to screen
        PositionTooltip(iconRect);

        // Show tooltip
        _tooltipPanel.SetActive(true);
        if (_tooltipCanvasGroup != null)
        {
            _tooltipCanvasGroup.alpha = 1f;
            _tooltipCanvasGroup.blocksRaycasts = true;
        }
    }

    internal void HideTooltip()
    {
        if (_tooltipPanel == null) return;
        _tooltipPanel.SetActive(false);
    }

    private void PositionTooltip(RectTransform iconRect)
    {
        if (_tooltipPanel == null || iconRect == null) return;

        var tooltipRect = _tooltipPanel.GetComponent<RectTransform>();
        if (tooltipRect == null) return;

        // Scale offset by canvas scale factor for resolution independence
        var canvas = _tooltipPanel.GetComponentInParent<Canvas>();
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        float yOffset = 50f * scale;
        float edgePadding = 10f * scale;

        // Position above the icon
        Vector3 iconWorldPos = iconRect.position;
        tooltipRect.position = new Vector3(iconWorldPos.x, iconWorldPos.y + yOffset, iconWorldPos.z);

        // Clamp to screen bounds
        if (canvas == null) return;

        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // If tooltip goes off the right edge, shift left
        if (corners[2].x > screenWidth)
        {
            float overflow = corners[2].x - screenWidth;
            tooltipRect.position -= new Vector3(overflow + edgePadding, 0f, 0f);
        }

        // If tooltip goes off the left edge, shift right
        if (corners[0].x < 0)
        {
            float overflow = -corners[0].x;
            tooltipRect.position += new Vector3(overflow + edgePadding, 0f, 0f);
        }

        // If tooltip goes off the top, position below the icon instead
        if (corners[1].y > screenHeight)
        {
            tooltipRect.position = new Vector3(tooltipRect.position.x,
                iconWorldPos.y - yOffset, tooltipRect.position.z);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Activation Glow (Task 3)
    // ════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (_glowTimers.Count == 0) return;

        float dt = Time.deltaTime;
        _keysToRemove.Clear();
        _keysBuffer.Clear();
        _keysBuffer.AddRange(_glowTimers.Keys);

        for (int i = 0; i < _keysBuffer.Count; i++)
        {
            string relicId = _keysBuffer[i];
            float remaining = _glowTimers[relicId] - dt;

            if (remaining <= 0f)
            {
                // Glow finished — restore original color
                if (_iconImages.TryGetValue(relicId, out var img) &&
                    _iconOriginalColors.TryGetValue(relicId, out var origColor))
                {
                    img.color = origColor;
                }
                _keysToRemove.Add(relicId);
            }
            else
            {
                // Lerp from glow color back to original
                _glowTimers[relicId] = remaining;
                float t = remaining / GlowDuration;
                if (_iconImages.TryGetValue(relicId, out var img) &&
                    _iconOriginalColors.TryGetValue(relicId, out var origColor))
                {
                    img.color = Color.Lerp(origColor, GlowColor, t);
                }
            }
        }

        for (int i = 0; i < _keysToRemove.Count; i++)
        {
            _glowTimers.Remove(_keysToRemove[i]);
        }
    }

    private void StartGlow(string relicId)
    {
        if (!_iconImages.ContainsKey(relicId)) return;
        _glowTimers[relicId] = GlowDuration;
        _iconImages[relicId].color = GlowColor;
    }

    // ════════════════════════════════════════════════════════════════════
    // EventBus Handlers
    // ════════════════════════════════════════════════════════════════════

    private void OnRelicActivated(RelicActivatedEvent evt)
    {
        StartGlow(evt.RelicId);
    }

    private void OnShopItemPurchased(ShopItemPurchasedEvent evt)
    {
        if (evt.ItemId == null || !evt.ItemId.StartsWith("relic_")) return;
        RefreshRelicIcons();
    }

    private void OnShopItemSold(ShopItemSoldEvent evt)
    {
        RefreshRelicIcons();
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        gameObject.SetActive(true);
        RefreshRelicIcons();
    }

    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    private void OnMarketClosed(MarketClosedEvent evt)
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    private void OnReturnToMenu(ReturnToMenuEvent evt)
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    // Test Helpers (internal)
    // ════════════════════════════════════════════════════════════════════

    internal int IconCount => _iconSlots.Count;
    internal bool IsTooltipVisible => _tooltipPanel != null && _tooltipPanel.activeSelf;
    internal bool HasActiveGlow(string relicId) => _glowTimers.ContainsKey(relicId);
    internal IReadOnlyDictionary<string, float> GlowTimers => _glowTimers;
    internal string TooltipNameContent => _tooltipNameText != null ? _tooltipNameText.text : null;
    internal string TooltipDescContent => _tooltipDescText != null ? _tooltipDescText.text : null;
    internal string TooltipEffectContent => _tooltipEffectText != null ? _tooltipEffectText.text : null;
    internal void TestShowTooltip(string relicId) => ShowTooltip(relicId, null);
}
