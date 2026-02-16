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
    // Rarity colors — retained for UISetup and legacy compat (13.9 cleanup)
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

    // Sold card color
    public static readonly Color SoldCardColor = new Color(0.1f, 0.15f, 0.1f, 0.7f);

    // Sold out card color
    public static readonly Color SoldOutCardColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);

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

    // State — now uses RelicDef
    private RelicDef?[] _relicOffering;
    private bool[] _soldFlags;
    private RunContext _ctx;
    private System.Action<int> _onPurchase;
    private System.Action _onClose;
    private System.Action _onReroll;

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

    public void SetOnRerollCallback(System.Action callback)
    {
        _onReroll = callback;
        if (_rerollButton != null)
        {
            _rerollButton.onClick.RemoveAllListeners();
            _rerollButton.onClick.AddListener(() => _onReroll?.Invoke());
        }
    }

    /// <summary>
    /// Shows the store with the given relic offering. Called by ShopState.Enter.
    /// </summary>
    public void ShowRelics(RunContext ctx, RelicDef?[] relicOffering, System.Action<int> onPurchase)
    {
        _ctx = ctx;
        _relicOffering = relicOffering;
        _onPurchase = onPurchase;
        _soldFlags = new bool[relicOffering.Length];
        _root.SetActive(true);
        _focusedPanelIndex = -1;

        _headerText.text = $"STORE \u2014 ROUND {ctx.CurrentRound}";
        UpdateCurrencyDisplays();
        UpdateRerollDisplay();

        for (int i = 0; i < _relicSlots.Length && i < relicOffering.Length; i++)
        {
            if (relicOffering[i].HasValue)
            {
                SetupRelicSlot(i, relicOffering[i].Value);
            }
            else
            {
                SetupSoldOutRelicSlot(i);
            }
        }

        RefreshRelicCapacity();
    }

    /// <summary>
    /// Legacy Show method for backwards compatibility with ShopItemDef.
    /// Converts to RelicDef and delegates.
    /// </summary>
    public void Show(RunContext ctx, ShopItemDef?[] items, System.Action<int> onPurchase)
    {
        var relics = new RelicDef?[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].HasValue)
            {
                var item = items[i].Value;
                relics[i] = new RelicDef(item.Id, item.Name, item.Description, item.Cost);
            }
        }
        ShowRelics(ctx, relics, onPurchase);
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    /// <summary>
    /// Refreshes the relic slots with a new offering after reroll (AC 10).
    /// Only regenerates unsold slots.
    /// </summary>
    public void RefreshRelicOffering(RelicDef?[] newOffering)
    {
        _relicOffering = newOffering;

        for (int i = 0; i < _relicSlots.Length && i < newOffering.Length; i++)
        {
            if (_soldFlags[i])
            {
                // Keep sold state — don't regenerate sold slots
                continue;
            }

            if (newOffering[i].HasValue)
            {
                SetupRelicSlot(i, newOffering[i].Value);
            }
            else
            {
                SetupSoldOutRelicSlot(i);
            }
        }

        UpdateRerollDisplay();
        RefreshRelicCapacity();
        RefreshRelicAffordability();
    }

    public void RefreshAfterPurchase(int cardIndex)
    {
        UpdateCurrencyDisplays();

        if (cardIndex >= 0 && cardIndex < _relicSlots.Length)
        {
            _soldFlags[cardIndex] = true;
            _relicSlots[cardIndex].PurchaseButton.interactable = false;
            _relicSlots[cardIndex].ButtonText.text = "SOLD";
            _relicSlots[cardIndex].CardBackground.color = SoldCardColor;
        }

        UpdateRerollDisplay();
        RefreshRelicCapacity();
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
        if (_focusedPanelIndex >= 0 && _focusedPanelIndex < _panelFocusIndicators.Length)
        {
            _panelFocusIndicators[_focusedPanelIndex].gameObject.SetActive(false);
        }

        _focusedPanelIndex = index;

        if (_focusedPanelIndex >= 0 && _focusedPanelIndex < _panelFocusIndicators.Length)
        {
            _panelFocusIndicators[_focusedPanelIndex].gameObject.SetActive(true);
        }
    }

    public int FocusedPanelIndex => _focusedPanelIndex;

    private void SetupSoldOutRelicSlot(int index)
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
        slot.CardBackground.color = SoldOutCardColor;
        slot.PurchaseButton.onClick.RemoveAllListeners();
    }

    private void SetupRelicSlot(int index, RelicDef relic)
    {
        var slot = _relicSlots[index];

        slot.CategoryLabel.text = "RELIC";
        slot.NameText.text = relic.Name;
        slot.DescriptionText.text = relic.Description;
        slot.CostText.text = $"\u2605 {relic.Cost}";

        // No rarity — hide rarity display
        slot.RarityText.text = "";
        slot.RarityBadge.color = ReputationColor;

        bool canAfford = _ctx.Reputation.CanAfford(relic.Cost);
        bool atCapacity = IsAtRelicCapacity();
        bool canBuy = canAfford && !atCapacity;

        slot.PurchaseButton.interactable = canBuy;
        if (atCapacity)
            slot.ButtonText.text = "FULL";
        else if (!canAfford)
            slot.ButtonText.text = "CAN'T AFFORD";
        else
            slot.ButtonText.text = "BUY";

        slot.CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);

        slot.PurchaseButton.onClick.RemoveAllListeners();
        int capturedIndex = index;
        slot.PurchaseButton.onClick.AddListener(() => _onPurchase?.Invoke(capturedIndex));
    }

    /// <summary>
    /// Checks if player is at relic capacity (AC 11, 12, 13).
    /// </summary>
    private bool IsAtRelicCapacity()
    {
        if (_ctx == null) return false;
        return _ctx.OwnedRelics.Count >= ShopTransaction.GetEffectiveMaxRelicSlots(_ctx);
    }

    /// <summary>
    /// Refreshes all non-sold relic slots for capacity check (AC 12).
    /// If at capacity, all purchase buttons disabled with "FULL".
    /// </summary>
    private void RefreshRelicCapacity()
    {
        if (_relicOffering == null) return;
        bool atCapacity = IsAtRelicCapacity();

        for (int i = 0; i < _relicSlots.Length && i < _relicOffering.Length; i++)
        {
            if (!_relicOffering[i].HasValue) continue;
            if (_soldFlags != null && i < _soldFlags.Length && _soldFlags[i]) continue;

            if (atCapacity)
            {
                _relicSlots[i].PurchaseButton.interactable = false;
                _relicSlots[i].ButtonText.text = "FULL";
            }
        }
    }

    private void RefreshRelicAffordability()
    {
        if (_relicOffering == null) return;
        bool atCapacity = IsAtRelicCapacity();

        for (int i = 0; i < _relicSlots.Length && i < _relicOffering.Length; i++)
        {
            if (!_relicOffering[i].HasValue) continue;
            if (_soldFlags != null && i < _soldFlags.Length && _soldFlags[i]) continue;

            if (atCapacity)
            {
                _relicSlots[i].PurchaseButton.interactable = false;
                _relicSlots[i].ButtonText.text = "FULL";
            }
            else
            {
                bool canAfford = _ctx.Reputation.CanAfford(_relicOffering[i].Value.Cost);
                _relicSlots[i].PurchaseButton.interactable = canAfford;
                _relicSlots[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
                _relicSlots[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
            }
        }
    }

    /// <summary>
    /// Updates the reroll button cost display and interactability (AC 7, 8).
    /// </summary>
    private void UpdateRerollDisplay()
    {
        if (_rerollButton == null || _rerollCostText == null || _ctx == null) return;

        int cost = ShopTransaction.GetRerollCost(_ctx.CurrentShopRerollCount);
        _rerollCostText.text = $"\u2605 {cost}";
        _rerollButton.interactable = _ctx.Reputation.CanAfford(cost);
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

    /// <summary>
    /// Returns rarity color — retained for UISetup and legacy compat (13.9 cleanup).
    /// </summary>
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
