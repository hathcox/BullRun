using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Store UI controller for the multi-panel Balatro-style store layout (Epic 13).
/// Top section: control panel (Next Round + Reroll) + 3 relic card slots.
/// Bottom section: 3 panels — Expansions (left), Insider Tips (center), Bonds (right).
/// Currency bar: Reputation (amber star) and Cash displays.
/// Keyboard navigation: Tab cycles panels, arrow keys navigate within.
/// MonoBehaviour created by UISetup during F5 generation.
/// </summary>
public class ShopUI : MonoBehaviour
{
    // Rarity colors per project spec
    public static readonly Color CommonColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public static readonly Color UncommonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public static readonly Color RareColor = new Color(0.3f, 0.5f, 1f, 1f);
    public static readonly Color LegendaryColor = new Color(1f, 0.84f, 0f, 1f);

    // Reputation display color (amber/gold)
    public static readonly Color ReputationColor = new Color(1f, 0.7f, 0f, 1f);

    // Cash display color (green)
    public static readonly Color CashColor = new Color(0.2f, 0.9f, 0.3f, 1f);

    // Panel header color (muted blue-grey)
    public static readonly Color PanelHeaderColor = new Color(0.5f, 0.6f, 0.8f, 1f);

    // Panel border/background colors
    public static readonly Color PanelBgColor = new Color(0.06f, 0.08f, 0.16f, 0.9f);
    public static readonly Color PanelBorderColor = new Color(0.2f, 0.25f, 0.4f, 1f);

    // Focus indicator color
    public static readonly Color FocusColor = new Color(0.3f, 0.5f, 1f, 0.6f);

    private GameObject _root;
    private Text _repText;
    private Text _cashText;
    private Text _headerText;
    private CanvasGroup _canvasGroup;

    // Top section: relic card slots
    private RelicSlotView[] _relicSlots;

    // Bottom panels
    private GameObject _expansionsPanel;
    private GameObject _tipsPanel;
    private GameObject _bondsPanel;

    // Control buttons
    private Button _nextRoundButton;
    private Button _rerollButton;
    private Text _rerollCostText;

    // Keyboard navigation
    private GameObject[] _focusablePanels;
    private Image[] _panelFocusIndicators;
    private int _focusedPanelIndex = -1;

    // State
    private ShopItemDef?[] _items;
    private RunContext _ctx;
    private System.Action<int> _onPurchase;
    private System.Action _onClose;

    public struct RelicSlotView
    {
        public GameObject Root;
        public Text CategoryLabel;
        public Text NameText;
        public Text DescriptionText;
        public Text CostText;
        public Text RarityText;
        public Image RarityBadge;
        public Button PurchaseButton;
        public Text ButtonText;
        public Image CardBackground;
    }

    public void Initialize(
        GameObject root,
        Text repText,
        Text cashText,
        Text headerText,
        RelicSlotView[] relicSlots,
        CanvasGroup canvasGroup)
    {
        _root = root;
        _repText = repText;
        _cashText = cashText;
        _headerText = headerText;
        _relicSlots = relicSlots;
        _canvasGroup = canvasGroup;
        _root.SetActive(false);
    }

    public void SetBottomPanels(GameObject expansionsPanel, GameObject tipsPanel, GameObject bondsPanel)
    {
        _expansionsPanel = expansionsPanel;
        _tipsPanel = tipsPanel;
        _bondsPanel = bondsPanel;

        // Build focusable panel list for keyboard navigation
        _focusablePanels = new GameObject[] { expansionsPanel, tipsPanel, bondsPanel };
        _panelFocusIndicators = new Image[_focusablePanels.Length];
        for (int i = 0; i < _focusablePanels.Length; i++)
        {
            var focusGo = new GameObject($"FocusIndicator_{i}");
            focusGo.transform.SetParent(_focusablePanels[i].transform, false);
            var focusRect = focusGo.AddComponent<RectTransform>();
            focusRect.anchorMin = Vector2.zero;
            focusRect.anchorMax = Vector2.one;
            focusRect.offsetMin = new Vector2(-2f, -2f);
            focusRect.offsetMax = new Vector2(2f, 2f);
            var focusImg = focusGo.AddComponent<Image>();
            focusImg.color = FocusColor;
            focusImg.raycastTarget = false;
            focusGo.SetActive(false);
            _panelFocusIndicators[i] = focusImg;
        }
        _focusedPanelIndex = -1;
    }

    public void SetNextRoundButton(Button nextRoundButton)
    {
        _nextRoundButton = nextRoundButton;
        if (_onClose != null)
        {
            _nextRoundButton.onClick.RemoveAllListeners();
            _nextRoundButton.onClick.AddListener(() => _onClose?.Invoke());
        }
    }

    public void SetRerollButton(Button rerollButton, Text costText)
    {
        _rerollButton = rerollButton;
        _rerollCostText = costText;
    }

    public void SetOnCloseCallback(System.Action callback)
    {
        _onClose = callback;
        if (_nextRoundButton != null)
        {
            _nextRoundButton.onClick.RemoveAllListeners();
            _nextRoundButton.onClick.AddListener(() => _onClose?.Invoke());
        }
    }

    /// <summary>
    /// Shows the store with the given relic items. Called by ShopState.Enter.
    /// </summary>
    public void Show(RunContext ctx, ShopItemDef?[] items, System.Action<int> onPurchase)
    {
        _ctx = ctx;
        _items = items;
        _onPurchase = onPurchase;
        _root.SetActive(true);
        _focusedPanelIndex = -1;

        _headerText.text = $"STORE \u2014 ROUND {ctx.CurrentRound}";
        UpdateCurrencyDisplays();

        for (int i = 0; i < _relicSlots.Length && i < items.Length; i++)
        {
            if (items[i].HasValue)
            {
                SetupRelicSlot(i, items[i].Value);
            }
            else
            {
                SetupEmptyRelicSlot(i);
            }
        }
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    public void RefreshAfterPurchase(int cardIndex)
    {
        UpdateCurrencyDisplays();

        if (cardIndex >= 0 && cardIndex < _relicSlots.Length)
        {
            _relicSlots[cardIndex].PurchaseButton.interactable = false;
            _relicSlots[cardIndex].ButtonText.text = "PURCHASED";
            _relicSlots[cardIndex].CardBackground.color = new Color(0.1f, 0.15f, 0.1f, 0.7f);
        }

        RefreshRelicAffordability();
    }

    private void Update()
    {
        if (_root == null || !_root.activeSelf) return;
        HandleKeyboardNavigation();
    }

    private void HandleKeyboardNavigation()
    {
        if (_focusablePanels == null) return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            // Cycle to next panel
            int next = (_focusedPanelIndex + 1) % _focusablePanels.Length;
            SetFocusedPanel(next);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (_focusedPanelIndex > 0)
                SetFocusedPanel(_focusedPanelIndex - 1);
            else if (_focusedPanelIndex < 0)
                SetFocusedPanel(0);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (_focusedPanelIndex < _focusablePanels.Length - 1)
                SetFocusedPanel(_focusedPanelIndex + 1);
            else if (_focusedPanelIndex < 0)
                SetFocusedPanel(0);
        }
    }

    private void SetFocusedPanel(int index)
    {
        // Clear previous focus
        if (_focusedPanelIndex >= 0 && _focusedPanelIndex < _panelFocusIndicators.Length)
        {
            _panelFocusIndicators[_focusedPanelIndex].gameObject.SetActive(false);
        }

        _focusedPanelIndex = index;

        // Show new focus
        if (_focusedPanelIndex >= 0 && _focusedPanelIndex < _panelFocusIndicators.Length)
        {
            _panelFocusIndicators[_focusedPanelIndex].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Returns the currently focused panel index (-1 if none).
    /// Used by tests and potential future keyboard interaction.
    /// </summary>
    public int FocusedPanelIndex => _focusedPanelIndex;

    private void SetupEmptyRelicSlot(int index)
    {
        var slot = _relicSlots[index];
        slot.CategoryLabel.text = "RELIC";
        slot.NameText.text = "EMPTY";
        slot.DescriptionText.text = "No relic available";
        slot.CostText.text = "";
        slot.RarityText.text = "";
        slot.RarityBadge.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        slot.PurchaseButton.interactable = false;
        slot.ButtonText.text = "SOLD OUT";
        slot.CardBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        slot.PurchaseButton.onClick.RemoveAllListeners();
    }

    private void SetupRelicSlot(int index, ShopItemDef item)
    {
        var slot = _relicSlots[index];

        string categoryName = item.Category switch
        {
            ItemCategory.TradingTool => "TRADING TOOL",
            ItemCategory.MarketIntel => "MARKET INTEL",
            ItemCategory.PassivePerk => "PASSIVE PERK",
            _ => "RELIC"
        };
        slot.CategoryLabel.text = categoryName;
        slot.NameText.text = item.Name;
        slot.DescriptionText.text = item.Description;
        slot.CostText.text = $"\u2605 {item.Cost}";

        Color rarityColor = GetRarityColor(item.Rarity);
        slot.RarityText.text = item.Rarity.ToString().ToUpper();
        slot.RarityText.color = rarityColor;
        slot.RarityBadge.color = rarityColor;

        bool canAfford = _ctx.Reputation.CanAfford(item.Cost);
        slot.PurchaseButton.interactable = canAfford;
        slot.ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
        slot.CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);

        slot.PurchaseButton.onClick.RemoveAllListeners();
        int capturedIndex = index;
        slot.PurchaseButton.onClick.AddListener(() => _onPurchase?.Invoke(capturedIndex));
    }

    private void RefreshRelicAffordability()
    {
        if (_items == null) return;
        for (int i = 0; i < _relicSlots.Length && i < _items.Length; i++)
        {
            if (!_items[i].HasValue) continue;
            if (!_relicSlots[i].PurchaseButton.interactable) continue;

            bool canAfford = _ctx.Reputation.CanAfford(_items[i].Value.Cost);
            _relicSlots[i].PurchaseButton.interactable = canAfford;
            _relicSlots[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
            _relicSlots[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    private void UpdateCurrencyDisplays()
    {
        if (_repText != null && _ctx != null)
        {
            _repText.text = $"\u2605 {_ctx.Reputation.Current}";
        }
        if (_cashText != null && _ctx != null)
        {
            _cashText.text = $"$ {_ctx.Portfolio.Cash:N0}";
        }
    }

    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => CommonColor,
            ItemRarity.Uncommon => UncommonColor,
            ItemRarity.Rare => RareColor,
            ItemRarity.Legendary => LegendaryColor,
            _ => CommonColor
        };
    }
}
