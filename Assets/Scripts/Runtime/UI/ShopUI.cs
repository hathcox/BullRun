using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop UI overlay. Displays 3 item cards (one per category) and Reputation display.
/// FIX-12: Shop uses Reputation currency, not Portfolio.Cash.
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

        // Update affordability on remaining cards (FIX-12: Rep, not cash)
        for (int i = 0; i < _cards.Length && i < _items.Length; i++)
        {
            if (i == cardIndex) continue;
            if (!_items[i].HasValue) continue; // sold out slot
            if (!_cards[i].PurchaseButton.interactable) continue; // already purchased

            bool canAfford = _ctx.Reputation.CanAfford(_items[i].Value.Cost);
            _cards[i].PurchaseButton.interactable = canAfford;
            _cards[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
            _cards[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
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
