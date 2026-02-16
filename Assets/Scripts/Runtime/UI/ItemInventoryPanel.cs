using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom bar displaying active relics during trading rounds.
/// Shows relic names with hotkey labels (Q/E/R) for quick reference.
/// Reads from RunContext.OwnedRelics (one-way dependency).
/// Refreshes on RoundStartedEvent and ShopItemPurchasedEvent.
/// Created by UISetup.ExecuteItemInventoryPanel() at runtime.
/// Story 13.9: Removed category partitioning and rarity display.
/// All relics displayed in a single flat list (no Tools/Intel/Perks split).
/// </summary>
public class ItemInventoryPanel : MonoBehaviour
{
    public static readonly string[] ToolHotkeys = { "Q", "E", "R" };
    public static readonly int MaxToolSlots = 3;

    // Dimmed color for empty/placeholder slots
    public static readonly Color DimmedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    public static readonly Color DimmedBorderColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    // Default relic border color (replaces rarity-based colors)
    public static readonly Color RelicBorderColor = new Color(1f, 0.7f, 0f, 1f);

    private RunContext _runContext;
    private bool _initialized;
    private bool _dirty;

    // Relic slot UI references (up to MaxToolSlots displayed)
    private RelicSlotView[] _relicSlots;

    private GameObject _panelRoot;

    public struct RelicSlotView
    {
        public Text HotkeyText;
        public Text NameText;
        public Image Border;
    }

    public void Initialize(RunContext runContext, GameObject panelRoot,
        RelicSlotView[] relicSlots)
    {
        _runContext = runContext;
        _panelRoot = panelRoot;
        _relicSlots = relicSlots;

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

        // Display owned relics in flat list (no category partitioning)
        for (int i = 0; i < _relicSlots.Length; i++)
        {
            if (i < _runContext.OwnedRelics.Count)
            {
                string relicId = _runContext.OwnedRelics[i];
                RelicDef? def = ItemLookup.GetRelicById(relicId);

                _relicSlots[i].HotkeyText.text = i < ToolHotkeys.Length ? FormatHotkey(ToolHotkeys[i]) : "";
                _relicSlots[i].HotkeyText.color = TradingHUD.WarningYellow;
                _relicSlots[i].NameText.text = def.HasValue ? def.Value.Name : relicId;
                _relicSlots[i].NameText.color = Color.white;
                _relicSlots[i].Border.color = RelicBorderColor;
            }
            else
            {
                _relicSlots[i].HotkeyText.text = i < ToolHotkeys.Length ? FormatHotkey(ToolHotkeys[i]) : "";
                _relicSlots[i].HotkeyText.color = DimmedColor;
                _relicSlots[i].NameText.text = "---";
                _relicSlots[i].NameText.color = DimmedColor;
                _relicSlots[i].Border.color = DimmedBorderColor;
            }
        }
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
