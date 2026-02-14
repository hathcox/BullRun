using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop UI overlay. Displays 3 item cards (one per category), optional upgrade card, and Reputation display.
/// FIX-12: Shop uses Reputation currency, not Portfolio.Cash.
/// FIX-13: Adds Trade Volume upgrade card for quantity tier unlocks.
/// MonoBehaviour created by UISetup during F5 generation.
/// </summary>
public class ShopUI : MonoBehaviour
{
    // Rarity colors per project spec
    public static readonly Color CommonColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public static readonly Color UncommonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public static readonly Color RareColor = new Color(0.3f, 0.5f, 1f, 1f);
    public static readonly Color LegendaryColor = new Color(1f, 0.84f, 0f, 1f);

    // FIX-12: Reputation display color (amber/gold)
    public static readonly Color ReputationColor = new Color(1f, 0.7f, 0f, 1f);

    // FIX-13: Upgrade card accent color (cyan/teal)
    public static readonly Color UpgradeAccentColor = new Color(0f, 0.8f, 0.9f, 1f);

    private GameObject _root;
    private Text _repText;
    private Text _headerText;
    private ItemCardView[] _cards;
    private CanvasGroup _canvasGroup;

    private Button _continueButton;

    private ShopItemDef?[] _items;
    private RunContext _ctx;
    private System.Action<int> _onPurchase;
    private System.Action _onClose;

    // FIX-13: Upgrade card elements
    private GameObject _upgradeCardRoot;
    private Text _upgradeNameText;
    private Text _upgradeDescText;
    private Text _upgradeCostText;
    private Button _upgradeBuyButton;
    private Text _upgradeBuyButtonText;
    private int _upgradeCost;
    private System.Action _onUpgradePurchase;

    public struct ItemCardView
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
        Text headerText,
        ItemCardView[] cards,
        CanvasGroup canvasGroup)
    {
        _root = root;
        _repText = repText;
        _headerText = headerText;
        _cards = cards;
        _canvasGroup = canvasGroup;
        _root.SetActive(false);
    }

    /// <summary>
    /// FIX-13: Sets the upgrade card UI element references. Called by UISetup during F5.
    /// </summary>
    public void SetUpgradeCard(GameObject root, Text nameText, Text descText, Text costText,
        Button buyButton, Text buyButtonText)
    {
        _upgradeCardRoot = root;
        _upgradeNameText = nameText;
        _upgradeDescText = descText;
        _upgradeCostText = costText;
        _upgradeBuyButton = buyButton;
        _upgradeBuyButtonText = buyButtonText;
    }

    /// <summary>
    /// Sets the Continue button reference. Called by UISetup during F5 generation.
    /// </summary>
    public void SetContinueButton(Button continueButton)
    {
        _continueButton = continueButton;
        if (_onClose != null)
        {
            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(() => _onClose?.Invoke());
        }
    }

    /// <summary>
    /// Registers a callback for the Continue button. ShopState calls this to close shop.
    /// </summary>
    public void SetOnCloseCallback(System.Action callback)
    {
        _onClose = callback;
        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveAllListeners();
            _continueButton.onClick.AddListener(() => _onClose?.Invoke());
        }
    }

    /// <summary>
    /// Shows the shop with the given items. Called by ShopState.Enter.
    /// Null items indicate a category pool is exhausted — shows "SOLD OUT" state.
    /// </summary>
    public void Show(RunContext ctx, ShopItemDef?[] items, System.Action<int> onPurchase)
    {
        _ctx = ctx;
        _items = items;
        _onPurchase = onPurchase;
        _root.SetActive(true);

        _headerText.text = $"DRAFT SHOP \u2014 ROUND {ctx.CurrentRound}";
        UpdateReputationDisplay();

        for (int i = 0; i < _cards.Length && i < items.Length; i++)
        {
            if (items[i].HasValue)
            {
                SetupCard(i, items[i].Value);
            }
            else
            {
                SetupSoldOutCard(i);
            }
        }
    }

    /// <summary>
    /// FIX-13: Shows the Trade Volume upgrade card with the next available tier.
    /// </summary>
    public void ShowUpgrade(int tierValue, int repCost, System.Action onPurchase)
    {
        if (_upgradeCardRoot == null) return;

        _onUpgradePurchase = onPurchase;
        _upgradeCost = repCost;
        _upgradeCardRoot.SetActive(true);
        _upgradeNameText.text = $"Trade Volume: x{tierValue}";
        _upgradeDescText.text = $"Unlock x{tierValue} quantity preset for trading";
        _upgradeCostText.text = $"\u2605 {repCost}";

        bool canAfford = _ctx != null && _ctx.Reputation.CanAfford(repCost);
        _upgradeBuyButton.interactable = canAfford;
        _upgradeBuyButtonText.text = canAfford ? "UNLOCK" : "CAN'T AFFORD";
        _upgradeCostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);

        _upgradeBuyButton.onClick.RemoveAllListeners();
        _upgradeBuyButton.onClick.AddListener(() => _onUpgradePurchase?.Invoke());
    }

    /// <summary>
    /// FIX-13: Hides the upgrade card (all tiers unlocked or no upgrade available).
    /// </summary>
    public void HideUpgrade()
    {
        if (_upgradeCardRoot != null)
            _upgradeCardRoot.SetActive(false);
    }

    /// <summary>
    /// FIX-13: Marks upgrade as purchased and refreshes affordability on all cards.
    /// </summary>
    public void RefreshAfterUpgradePurchase()
    {
        UpdateReputationDisplay();

        // Mark upgrade card as purchased
        if (_upgradeCardRoot != null)
        {
            _upgradeBuyButton.interactable = false;
            _upgradeBuyButtonText.text = "UNLOCKED";
        }

        // Refresh affordability on item cards
        RefreshItemCardAffordability();
    }

    /// <summary>
    /// Hides the shop overlay. Called by ShopState.Exit.
    /// </summary>
    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    /// <summary>
    /// Called after a purchase to refresh affordability state.
    /// FIX-12: Uses Reputation.CanAfford instead of Portfolio.CanAfford.
    /// </summary>
    public void RefreshAfterPurchase(int cardIndex)
    {
        UpdateReputationDisplay();

        // Mark purchased card as sold
        if (cardIndex >= 0 && cardIndex < _cards.Length)
        {
            _cards[cardIndex].PurchaseButton.interactable = false;
            _cards[cardIndex].ButtonText.text = "PURCHASED";
            _cards[cardIndex].CardBackground.color = new Color(0.1f, 0.15f, 0.1f, 0.7f);
        }

        // Update affordability on remaining cards and upgrade card
        RefreshItemCardAffordability();
        RefreshUpgradeAffordability();
    }

    private void RefreshItemCardAffordability()
    {
        for (int i = 0; i < _cards.Length && i < _items.Length; i++)
        {
            if (!_items[i].HasValue) continue; // sold out slot
            if (!_cards[i].PurchaseButton.interactable) continue; // already purchased

            bool canAfford = _ctx.Reputation.CanAfford(_items[i].Value.Cost);
            _cards[i].PurchaseButton.interactable = canAfford;
            _cards[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
            _cards[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    /// <summary>
    /// FIX-13: Refreshes upgrade card affordability after any purchase.
    /// </summary>
    private void RefreshUpgradeAffordability()
    {
        if (_upgradeCardRoot == null || !_upgradeCardRoot.activeSelf) return;
        if (_upgradeBuyButtonText.text == "UNLOCKED") return; // Already purchased this session

        if (_ctx != null)
        {
            bool canAfford = _ctx.Reputation.CanAfford(_upgradeCost);
            _upgradeBuyButton.interactable = canAfford;
            _upgradeBuyButtonText.text = canAfford ? "UNLOCK" : "CAN'T AFFORD";
            _upgradeCostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    private void SetupSoldOutCard(int index)
    {
        var card = _cards[index];
        string categoryName = index switch
        {
            0 => "TRADING TOOL",
            1 => "MARKET INTEL",
            2 => "PASSIVE PERK",
            _ => "ITEM"
        };
        card.CategoryLabel.text = categoryName;
        card.NameText.text = "SOLD OUT";
        card.DescriptionText.text = "No items available in this category";
        card.CostText.text = "";
        card.RarityText.text = "";
        card.RarityBadge.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        card.PurchaseButton.interactable = false;
        card.ButtonText.text = "SOLD OUT";
        card.CardBackground.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        card.PurchaseButton.onClick.RemoveAllListeners();
    }

    private void SetupCard(int index, ShopItemDef item)
    {
        var card = _cards[index];

        string categoryName = item.Category switch
        {
            ItemCategory.TradingTool => "TRADING TOOL",
            ItemCategory.MarketIntel => "MARKET INTEL",
            ItemCategory.PassivePerk => "PASSIVE PERK",
            _ => "ITEM"
        };
        card.CategoryLabel.text = categoryName;
        card.NameText.text = item.Name;
        card.DescriptionText.text = item.Description;
        // FIX-12: Show Rep cost with star icon instead of $
        card.CostText.text = $"\u2605 {item.Cost}";

        Color rarityColor = GetRarityColor(item.Rarity);
        card.RarityText.text = item.Rarity.ToString().ToUpper();
        card.RarityText.color = rarityColor;
        card.RarityBadge.color = rarityColor;

        // FIX-12: Check Reputation affordability, not cash
        bool canAfford = _ctx.Reputation.CanAfford(item.Cost);
        card.PurchaseButton.interactable = canAfford;
        card.ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
        card.CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);

        // Wire button
        card.PurchaseButton.onClick.RemoveAllListeners();
        int capturedIndex = index;
        card.PurchaseButton.onClick.AddListener(() => _onPurchase?.Invoke(capturedIndex));
    }

    /// <summary>
    /// FIX-12: Updates the Reputation balance display (was cash display).
    /// </summary>
    private void UpdateReputationDisplay()
    {
        if (_repText != null && _ctx != null)
        {
            _repText.text = $"\u2605 {_ctx.Reputation.Current}";
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
