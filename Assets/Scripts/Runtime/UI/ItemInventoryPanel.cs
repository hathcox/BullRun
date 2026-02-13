using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom bar displaying active Trading Tools (with hotkey labels Q/E/R),
/// Intel badges, and Passive Perks during trading rounds.
/// Reads from RunContext.ActiveItems (one-way dependency).
/// Refreshes on RoundStartedEvent and ShopItemPurchasedEvent.
/// Created by UISetup.ExecuteItemInventoryPanel() at runtime.
/// </summary>
public class ItemInventoryPanel : MonoBehaviour
{
    public static readonly string[] ToolHotkeys = { "Q", "E", "R" };
    public static readonly int MaxToolSlots = 3;

    // Dimmed color for empty/placeholder slots
    public static readonly Color DimmedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public static readonly Color DimmedBorderColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    private RunContext _runContext;
    private bool _initialized;
    private bool _dirty;

    // Tool slot UI references (always 3)
    private ToolSlotView[] _toolSlots;

    // Intel badge UI references (pre-created, hidden when unused)
    private IntelBadgeView[] _intelBadges;

    // Perk entry UI references (pre-created, hidden when unused)
    private PerkEntryView[] _perkEntries;

    private GameObject _panelRoot;
    private Text _intelEmptyText;
    private Text _perkEmptyText;

    // Cached lists for RefreshDisplay — avoids per-refresh heap allocation
    private List<ShopItemDef> _cachedTools;
    private List<ShopItemDef> _cachedIntel;
    private List<ShopItemDef> _cachedPerks;

    public struct ToolSlotView
    {
        public Text HotkeyText;
        public Text NameText;
        public Image RarityBorder;
    }

    public struct IntelBadgeView
    {
        public Text NameText;
        public Image RarityIndicator;
        public GameObject Root;
    }

    public struct PerkEntryView
    {
        public Text NameText;
        public Image RarityDot;
        public GameObject Root;
    }

    public void Initialize(RunContext runContext, GameObject panelRoot,
        ToolSlotView[] toolSlots, IntelBadgeView[] intelBadges, PerkEntryView[] perkEntries,
        Text intelEmptyText, Text perkEmptyText)
    {
        _runContext = runContext;
        _panelRoot = panelRoot;
        _toolSlots = toolSlots;
        _intelBadges = intelBadges;
        _perkEntries = perkEntries;
        _intelEmptyText = intelEmptyText;
        _perkEmptyText = perkEmptyText;

        _cachedTools = new List<ShopItemDef>();
        _cachedIntel = new List<ShopItemDef>();
        _cachedPerks = new List<ShopItemDef>();

        _initialized = true;
        _dirty = true;

        // Start hidden — shown when TradingState activates via RoundStartedEvent
        _panelRoot.SetActive(false);

        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Subscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Unsubscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        Show();
        Refresh();
    }

    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        Hide();
    }

    private void OnShopItemPurchased(ShopItemPurchasedEvent evt)
    {
        _dirty = true;
    }

    public void Show()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void Refresh()
    {
        _dirty = true;
    }

    private void LateUpdate()
    {
        if (!_initialized || !_dirty) return;
        _dirty = false;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (!_initialized || _runContext == null) return;

        // Partition ActiveItems into categories — reuse cached lists to avoid allocation
        _cachedTools.Clear();
        _cachedIntel.Clear();
        _cachedPerks.Clear();
        var tools = _cachedTools;
        var intel = _cachedIntel;
        var perks = _cachedPerks;

        for (int i = 0; i < _runContext.ActiveItems.Count; i++)
        {
            string itemId = _runContext.ActiveItems[i];
            ShopItemDef? def = ItemLookup.GetItemById(itemId);
            if (def == null) continue;

            switch (def.Value.Category)
            {
                case ItemCategory.TradingTool:
                    tools.Add(def.Value);
                    break;
                case ItemCategory.MarketIntel:
                    intel.Add(def.Value);
                    break;
                case ItemCategory.PassivePerk:
                    perks.Add(def.Value);
                    break;
            }
        }

        // Update tool slots (first Tool = Q, second = E, third = R)
        for (int i = 0; i < _toolSlots.Length; i++)
        {
            if (i < tools.Count)
            {
                _toolSlots[i].HotkeyText.text = FormatHotkey(ToolHotkeys[i]);
                _toolSlots[i].HotkeyText.color = TradingHUD.WarningYellow;
                _toolSlots[i].NameText.text = tools[i].Name;
                _toolSlots[i].NameText.color = Color.white;
                _toolSlots[i].RarityBorder.color = ItemLookup.GetRarityColor(tools[i].Rarity);
            }
            else
            {
                _toolSlots[i].HotkeyText.text = FormatHotkey(ToolHotkeys[i]);
                _toolSlots[i].HotkeyText.color = DimmedColor;
                _toolSlots[i].NameText.text = "---";
                _toolSlots[i].NameText.color = DimmedColor;
                _toolSlots[i].RarityBorder.color = DimmedBorderColor;
            }
        }

        // Update intel badges
        for (int i = 0; i < _intelBadges.Length; i++)
        {
            if (i < intel.Count)
            {
                _intelBadges[i].Root.SetActive(true);
                _intelBadges[i].NameText.text = intel[i].Name;
                _intelBadges[i].RarityIndicator.color = ItemLookup.GetRarityColor(intel[i].Rarity);
            }
            else
            {
                _intelBadges[i].Root.SetActive(false);
            }
        }

        // Update perk entries
        for (int i = 0; i < _perkEntries.Length; i++)
        {
            if (i < perks.Count)
            {
                _perkEntries[i].Root.SetActive(true);
                _perkEntries[i].NameText.text = perks[i].Name;
                _perkEntries[i].RarityDot.color = ItemLookup.GetRarityColor(perks[i].Rarity);
            }
            else
            {
                _perkEntries[i].Root.SetActive(false);
            }
        }

        // Show/hide empty state text for Intel and Perk sections
        if (_intelEmptyText != null)
            _intelEmptyText.gameObject.SetActive(intel.Count == 0);
        if (_perkEmptyText != null)
            _perkEmptyText.gameObject.SetActive(perks.Count == 0);
    }

    // --- Static utility methods for testability ---

    public static string FormatToolSlot(string hotkey, string itemName)
    {
        return $"[{hotkey}] {itemName}";
    }

    public static string FormatEmptyToolSlot(string hotkey)
    {
        return $"[{hotkey}] ---";
    }

    public static string FormatHotkey(string hotkey)
    {
        return $"[{hotkey}]";
    }

}
