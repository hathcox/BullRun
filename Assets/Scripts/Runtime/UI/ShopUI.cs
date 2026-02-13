using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop UI overlay. Displays 3 item cards (one per category), cash display, and countdown timer.
/// MonoBehaviour created by UISetup during F5 generation.
/// Subscribes to EventBus for purchase feedback.
/// </summary>
public class ShopUI : MonoBehaviour
{
    // Rarity colors per project spec
    public static readonly Color CommonColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public static readonly Color UncommonColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public static readonly Color RareColor = new Color(0.3f, 0.5f, 1f, 1f);
    public static readonly Color LegendaryColor = new Color(1f, 0.84f, 0f, 1f);

    private GameObject _root;
    private Text _cashText;
    private Text _timerText;
    private Text _headerText;
    private ItemCardView[] _cards;
    private CanvasGroup _canvasGroup;

    private ShopItemDef?[] _items;
    private RunContext _ctx;
    private System.Action<int> _onPurchase;

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
        Text cashText,
        Text timerText,
        Text headerText,
        ItemCardView[] cards,
        CanvasGroup canvasGroup)
    {
        _root = root;
        _cashText = cashText;
        _timerText = timerText;
        _headerText = headerText;
        _cards = cards;
        _canvasGroup = canvasGroup;
        _root.SetActive(false);
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
        UpdateCashDisplay();

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
    /// Updates the countdown timer display.
    /// </summary>
    public void UpdateTimer(float secondsRemaining)
    {
        if (_timerText != null)
        {
            int secs = Mathf.CeilToInt(secondsRemaining);
            _timerText.text = $"0:{secs:D2}";
        }
    }

    /// <summary>
    /// Called after a purchase to refresh affordability state.
    /// </summary>
    public void RefreshAfterPurchase(int cardIndex)
    {
        UpdateCashDisplay();

        // Mark purchased card as sold
        if (cardIndex >= 0 && cardIndex < _cards.Length)
        {
            _cards[cardIndex].PurchaseButton.interactable = false;
            _cards[cardIndex].ButtonText.text = "PURCHASED";
            _cards[cardIndex].CardBackground.color = new Color(0.1f, 0.15f, 0.1f, 0.7f);
        }

        // Update affordability on remaining cards
        for (int i = 0; i < _cards.Length && i < _items.Length; i++)
        {
            if (i == cardIndex) continue;
            if (!_items[i].HasValue) continue; // sold out slot
            if (!_cards[i].PurchaseButton.interactable) continue; // already purchased

            bool canAfford = _ctx.Portfolio.Cash >= _items[i].Value.Cost;
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
        card.CostText.text = $"${item.Cost}";

        Color rarityColor = GetRarityColor(item.Rarity);
        card.RarityText.text = item.Rarity.ToString().ToUpper();
        card.RarityText.color = rarityColor;
        card.RarityBadge.color = rarityColor;

        bool canAfford = _ctx.Portfolio.Cash >= item.Cost;
        card.PurchaseButton.interactable = canAfford;
        card.ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
        card.CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);

        // Wire button
        card.PurchaseButton.onClick.RemoveAllListeners();
        int capturedIndex = index;
        card.PurchaseButton.onClick.AddListener(() => _onPurchase?.Invoke(capturedIndex));
    }

    private void UpdateCashDisplay()
    {
        if (_cashText != null && _ctx != null)
        {
            _cashText.text = $"${_ctx.Portfolio.Cash:F0}";
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
