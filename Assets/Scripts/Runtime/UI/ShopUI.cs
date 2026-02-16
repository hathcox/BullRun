using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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

    // Unified relic card border color (AC 1 — no rarity differentiation)
    public static readonly Color RelicCardColor = new Color(0.08f, 0.1f, 0.22f, 0.9f);
    public static readonly Color RelicCardHoverColor = new Color(0.14f, 0.18f, 0.38f, 1f);

    // Animation timing constants
    public const float HoverScale = 1.05f;
    public const float HoverDuration = 0.15f;
    public const float PurchaseAnimDuration = 0.5f;
    public const float SoldStampDuration = 1.0f;
    public const float RerollFlipDuration = 0.4f;
    public const float RerollStaggerDelay = 0.08f;
    public const float TipFlipDuration = 0.6f;
    public const float TipFlashDuration = 0.15f;
    public const float OwnedFadeDuration = 0.3f;
    public const float BondPulseSpeed = 2f;
    public const float BondPulseMinAlpha = 0.5f;
    public const float BondPulseMaxAlpha = 1f;
    public const float BondPulseHoverBoost = 0.3f;

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

    // Expansion card color (teal accent)
    public static readonly Color ExpansionCardColor = new Color(0.08f, 0.14f, 0.18f, 0.9f);
    public static readonly Color OwnedOverlayColor = new Color(0.1f, 0.2f, 0.1f, 0.85f);

    // State — now uses RelicDef
    private RelicDef?[] _relicOffering;
    private bool[] _soldFlags;
    private RunContext _ctx;
    private System.Action<int> _onPurchase;
    private System.Action _onClose;
    private System.Action _onReroll;

    // Expansion panel state (Story 13.4)
    private ExpansionCardView[] _expansionCards;
    private ExpansionDef[] _expansionOffering;
    private System.Action<int> _onExpansionPurchase;

    // Insider tips panel state (Story 13.5)
    private TipCardView[] _tipCards;
    private InsiderTipGenerator.TipOffering[] _tipOffering;
    private System.Action<int> _onTipPurchase;

    // Tip card colors (dark purple/mystery theme)
    public static readonly Color TipCardFaceDownColor = new Color(0.12f, 0.08f, 0.18f, 0.9f);
    public static readonly Color TipCardRevealedColor = new Color(0.08f, 0.16f, 0.12f, 0.9f);

    // Bond panel colors (green/cash theme — Story 13.6)
    public static readonly Color BondCardColor = new Color(0.08f, 0.16f, 0.10f, 0.9f);

    // Hover animation state (Story 13.8)
    private Coroutine[] _hoverCoroutines;

    // Bond card pulsing state (Story 13.8)
    private Image _bondCardBg;
    private Outline _bondCardOutline;
    private bool _bondHovered;

    // Bond panel state (Story 13.6)
    private Text _bondPriceText;
    private Text _bondInfoText;
    private Text _bondSellText;
    private Button _bondBuyButton;
    private Text _bondBuyButtonText;
    private Button _bondSellButton;
    private Text _bondSellButtonText;
    private GameObject _bondConfirmOverlay;
    private System.Action _onBondPurchase;
    private System.Action _onBondSell;

    public struct ExpansionCardView
    {
        public GameObject Root;
        public Text NameText;
        public Text DescriptionText;
        public Text CostText;
        public Button PurchaseButton;
        public Text ButtonText;
        public Image CardBackground;
    }

    public class TipCardView
    {
        public GameObject Root;
        public Text NameText;
        public Text DescriptionText;
        public Text CostText;
        public Button PurchaseButton;
        public Text ButtonText;
        public Image CardBackground;
        public CanvasGroup Group;
        public bool IsRevealed;
    }

    public struct RelicSlotView
    {
        public GameObject Root;
        public Text NameText;
        public Text DescriptionText;
        public Text CostText;
        public Button PurchaseButton;
        public Text ButtonText;
        public Image CardBackground;
        public CanvasGroup Group;
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

        // Set up hover EventTriggers on relic slots (Story 13.8, AC 1)
        _hoverCoroutines = new Coroutine[relicSlots.Length];
        for (int i = 0; i < relicSlots.Length; i++)
        {
            if (relicSlots[i].Root == null) continue;
            int capturedIndex = i;
            var trigger = relicSlots[i].Root.AddComponent<EventTrigger>();

            var enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((_) => OnRelicHoverEnter(capturedIndex));
            trigger.triggers.Add(enterEntry);

            var exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((_) => OnRelicHoverExit(capturedIndex));
            trigger.triggers.Add(exitEntry);
        }
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

        // Animate the reroll with flip effect (Story 13.8, AC 3)
        StartCoroutine(AnimateRerollFlip(newOffering));
    }

    public void RefreshAfterPurchase(int cardIndex)
    {
        UpdateCurrencyDisplays();

        if (cardIndex >= 0 && cardIndex < _relicSlots.Length)
        {
            // Disable purchase immediately to prevent double-buy
            _relicSlots[cardIndex].PurchaseButton.interactable = false;
            // Animate the purchase (Story 13.8, AC 2)
            StartCoroutine(AnimateCardPurchase(cardIndex));
        }

        UpdateRerollDisplay();
        RefreshRelicCapacity();
        RefreshRelicAffordability();
    }

    /// <summary>
    /// Populates the expansions panel with available expansion cards (Story 13.4, AC 1, 2, 7).
    /// Called by ShopState after generating the expansion offering.
    /// </summary>
    public void ShowExpansions(RunContext ctx, ExpansionDef[] offering, System.Action<int> onPurchase)
    {
        _ctx = ctx;
        _expansionOffering = offering;
        _onExpansionPurchase = onPurchase;

        // Clear existing expansion card objects
        ClearExpansionCards();

        if (_expansionsPanel == null || offering == null || offering.Length == 0) return;

        // Remove placeholder "Coming soon..." label if present
        var contentLabel = _expansionsPanel.transform.Find("ExpansionsPanelContent");
        if (contentLabel != null) contentLabel.gameObject.SetActive(false);

        _expansionCards = new ExpansionCardView[offering.Length];
        for (int i = 0; i < offering.Length; i++)
        {
            _expansionCards[i] = CreateExpansionCard(i, offering[i], _expansionsPanel.transform);
            bool isOwned = ctx.OwnedExpansions.Contains(offering[i].Id);
            if (isOwned)
            {
                SetExpansionCardOwned(i);
            }
            else
            {
                bool canAfford = ctx.Reputation.CanAfford(offering[i].Cost);
                _expansionCards[i].PurchaseButton.interactable = canAfford;
                _expansionCards[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
                _expansionCards[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
            }
        }
    }

    /// <summary>
    /// Refreshes expansion card after a purchase. Marks the card as OWNED
    /// and updates affordability on remaining cards.
    /// </summary>
    public void RefreshExpansionAfterPurchase(int cardIndex)
    {
        UpdateCurrencyDisplays();

        if (cardIndex >= 0 && _expansionCards != null && cardIndex < _expansionCards.Length)
        {
            SetExpansionCardOwned(cardIndex, animate: true);
        }

        RefreshExpansionAffordability();
        RefreshRelicAffordability();
        UpdateRerollDisplay();
    }

    private void SetExpansionCardOwned(int index, bool animate = false)
    {
        if (_expansionCards == null || index < 0 || index >= _expansionCards.Length) return;
        _expansionCards[index].PurchaseButton.interactable = false;
        _expansionCards[index].ButtonText.text = "OWNED";
        _expansionCards[index].CardBackground.color = OwnedOverlayColor;

        // Animate OWNED watermark on purchase (Story 13.8, AC 6)
        if (animate)
        {
            StartCoroutine(AnimateOwnedWatermark(index));
        }
    }

    private void RefreshExpansionAffordability()
    {
        if (_expansionOffering == null || _expansionCards == null || _ctx == null) return;

        for (int i = 0; i < _expansionCards.Length && i < _expansionOffering.Length; i++)
        {
            if (_ctx.OwnedExpansions.Contains(_expansionOffering[i].Id)) continue;

            bool canAfford = _ctx.Reputation.CanAfford(_expansionOffering[i].Cost);
            _expansionCards[i].PurchaseButton.interactable = canAfford;
            _expansionCards[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
            _expansionCards[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    private ExpansionCardView CreateExpansionCard(int index, ExpansionDef expansion, Transform parent)
    {
        var view = new ExpansionCardView();

        var cardGo = new GameObject($"ExpansionCard_{index}");
        cardGo.transform.SetParent(parent, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        view.CardBackground = cardGo.AddComponent<Image>();
        view.CardBackground.color = ExpansionCardColor;
        view.Root = cardGo;

        var cardLayout = cardGo.AddComponent<LayoutElement>();
        cardLayout.flexibleWidth = 1f;
        cardLayout.preferredHeight = 80f;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Name — bold/larger for legibility (Story 13.8, AC 7)
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(cardGo.transform, false);
        view.NameText = nameGo.AddComponent<Text>();
        view.NameText.text = expansion.Name;
        view.NameText.fontSize = 14;
        view.NameText.fontStyle = FontStyle.Bold;
        view.NameText.color = Color.white;
        view.NameText.alignment = TextAnchor.MiddleCenter;
        view.NameText.raycastTarget = false;
        var nameLayout = nameGo.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 18f;

        // Description — improved contrast and line spacing (Story 13.8, AC 7)
        var descGo = new GameObject("Description");
        descGo.transform.SetParent(cardGo.transform, false);
        view.DescriptionText = descGo.AddComponent<Text>();
        view.DescriptionText.text = expansion.Description;
        view.DescriptionText.fontSize = 11;
        view.DescriptionText.color = new Color(0.8f, 0.8f, 0.85f, 1f);
        view.DescriptionText.alignment = TextAnchor.MiddleCenter;
        view.DescriptionText.raycastTarget = false;
        view.DescriptionText.lineSpacing = 1.1f;
        var descLayout = descGo.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 16f;

        // Cost — prominently sized (Story 13.8, AC 7)
        var costGo = new GameObject("Cost");
        costGo.transform.SetParent(cardGo.transform, false);
        view.CostText = costGo.AddComponent<Text>();
        view.CostText.text = $"\u2605 {expansion.Cost}";
        view.CostText.fontSize = 13;
        view.CostText.color = ReputationColor;
        view.CostText.alignment = TextAnchor.MiddleCenter;
        view.CostText.raycastTarget = false;
        var costLayout = costGo.AddComponent<LayoutElement>();
        costLayout.preferredHeight = 16f;

        // Purchase button
        var btnGo = new GameObject("BuyButton");
        btnGo.transform.SetParent(cardGo.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.3f, 0.15f, 1f);
        view.PurchaseButton = btnGo.AddComponent<Button>();
        var btnLayout = btnGo.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 20f;

        var btnTextGo = new GameObject("ButtonText");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnRect = btnTextGo.AddComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.one;
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        view.ButtonText = btnTextGo.AddComponent<Text>();
        view.ButtonText.text = "BUY";
        view.ButtonText.fontSize = 11;
        view.ButtonText.fontStyle = FontStyle.Bold;
        view.ButtonText.color = Color.white;
        view.ButtonText.alignment = TextAnchor.MiddleCenter;
        view.ButtonText.raycastTarget = false;

        int capturedIndex = index;
        view.PurchaseButton.onClick.AddListener(() => _onExpansionPurchase?.Invoke(capturedIndex));

        return view;
    }

    /// <summary>
    /// Populates the insider tips panel with mystery cards (Story 13.5, AC 2, 3).
    /// Cards start face-down showing "?" and cost. After purchase they reveal the tip text.
    /// </summary>
    public void ShowTips(RunContext ctx, InsiderTipGenerator.TipOffering[] offering, System.Action<int> onPurchase)
    {
        _ctx = ctx;
        _tipOffering = offering;
        _onTipPurchase = onPurchase;

        ClearTipCards();

        if (_tipsPanel == null || offering == null || offering.Length == 0) return;

        // Remove placeholder label if present
        var contentLabel = _tipsPanel.transform.Find("TipsPanelContent");
        if (contentLabel != null) contentLabel.gameObject.SetActive(false);

        _tipCards = new TipCardView[offering.Length];
        for (int i = 0; i < offering.Length; i++)
        {
            _tipCards[i] = CreateTipCard(i, offering[i], _tipsPanel.transform);
            bool canAfford = ctx.Reputation.CanAfford(offering[i].Definition.Cost);
            _tipCards[i].PurchaseButton.interactable = canAfford;
            _tipCards[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
            _tipCards[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    /// <summary>
    /// Refreshes tip card after purchase — reveals the tip text (Story 13.5, AC 4, 5).
    /// </summary>
    public void RefreshTipAfterPurchase(int cardIndex)
    {
        UpdateCurrencyDisplays();

        if (cardIndex >= 0 && _tipCards != null && cardIndex < _tipCards.Length)
        {
            // Disable purchase immediately to prevent double-buy
            _tipCards[cardIndex].PurchaseButton.interactable = false;
            // Animate the tip flip reveal (Story 13.8, AC 4)
            StartCoroutine(AnimateTipFlip(cardIndex));
        }

        RefreshTipAffordability();
        RefreshExpansionAffordability();
        RefreshRelicAffordability();
        UpdateRerollDisplay();
    }

    private void RefreshTipAffordability()
    {
        if (_tipOffering == null || _tipCards == null || _ctx == null) return;

        for (int i = 0; i < _tipCards.Length && i < _tipOffering.Length; i++)
        {
            if (_tipCards[i].IsRevealed) continue;

            bool canAfford = _ctx.Reputation.CanAfford(_tipOffering[i].Definition.Cost);
            _tipCards[i].PurchaseButton.interactable = canAfford;
            _tipCards[i].ButtonText.text = canAfford ? "BUY" : "CAN'T AFFORD";
            _tipCards[i].CostText.color = canAfford ? Color.white : new Color(1f, 0.3f, 0.3f, 1f);
        }
    }

    private TipCardView CreateTipCard(int index, InsiderTipGenerator.TipOffering offering, Transform parent)
    {
        var view = new TipCardView();
        view.IsRevealed = false;

        var cardGo = new GameObject($"TipCard_{index}");
        cardGo.transform.SetParent(parent, false);
        var cardRect = cardGo.AddComponent<RectTransform>();
        view.CardBackground = cardGo.AddComponent<Image>();
        view.CardBackground.color = TipCardFaceDownColor;
        view.Root = cardGo;
        view.Group = cardGo.AddComponent<CanvasGroup>();

        var cardLayout = cardGo.AddComponent<LayoutElement>();
        cardLayout.flexibleWidth = 1f;
        cardLayout.preferredHeight = 80f;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Name — face-down shows "INSIDER TIP" (Story 13.8, AC 4, 7)
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(cardGo.transform, false);
        view.NameText = nameGo.AddComponent<Text>();
        view.NameText.text = "INSIDER TIP";
        view.NameText.fontSize = 14;
        view.NameText.fontStyle = FontStyle.Bold;
        view.NameText.color = new Color(0.7f, 0.5f, 0.9f, 1f);
        view.NameText.alignment = TextAnchor.MiddleCenter;
        view.NameText.raycastTarget = false;
        var nameLayout = nameGo.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 18f;

        // Description — face-down shows large "?" symbol (Story 13.8, AC 4, 7)
        var descGo = new GameObject("Description");
        descGo.transform.SetParent(cardGo.transform, false);
        view.DescriptionText = descGo.AddComponent<Text>();
        view.DescriptionText.text = "?";
        view.DescriptionText.fontSize = 24;
        view.DescriptionText.fontStyle = FontStyle.Bold;
        view.DescriptionText.color = new Color(0.5f, 0.4f, 0.7f, 0.8f);
        view.DescriptionText.alignment = TextAnchor.MiddleCenter;
        view.DescriptionText.raycastTarget = false;
        view.DescriptionText.lineSpacing = 1.1f;
        var descLayout = descGo.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 28f;

        // Cost
        var costGo = new GameObject("Cost");
        costGo.transform.SetParent(cardGo.transform, false);
        view.CostText = costGo.AddComponent<Text>();
        view.CostText.text = $"\u2605 {offering.Definition.Cost}";
        view.CostText.fontSize = 12;
        view.CostText.color = ReputationColor;
        view.CostText.alignment = TextAnchor.MiddleCenter;
        view.CostText.raycastTarget = false;
        var costLayout = costGo.AddComponent<LayoutElement>();
        costLayout.preferredHeight = 16f;

        // Purchase button
        var btnGo = new GameObject("BuyButton");
        btnGo.transform.SetParent(cardGo.transform, false);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.15f, 0.3f, 1f);
        view.PurchaseButton = btnGo.AddComponent<Button>();
        var btnLayout = btnGo.AddComponent<LayoutElement>();
        btnLayout.preferredHeight = 20f;

        var btnTextGo = new GameObject("ButtonText");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        var btnRect = btnTextGo.AddComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.one;
        btnRect.offsetMin = Vector2.zero;
        btnRect.offsetMax = Vector2.zero;
        view.ButtonText = btnTextGo.AddComponent<Text>();
        view.ButtonText.text = "BUY";
        view.ButtonText.fontSize = 11;
        view.ButtonText.fontStyle = FontStyle.Bold;
        view.ButtonText.color = Color.white;
        view.ButtonText.alignment = TextAnchor.MiddleCenter;
        view.ButtonText.raycastTarget = false;

        int capturedIndex = index;
        view.PurchaseButton.onClick.AddListener(() => _onTipPurchase?.Invoke(capturedIndex));

        return view;
    }

    private void ClearTipCards()
    {
        if (_tipCards != null)
        {
            for (int i = 0; i < _tipCards.Length; i++)
            {
                if (_tipCards[i].Root != null)
                    Destroy(_tipCards[i].Root);
            }
            _tipCards = null;
        }
    }

    public static string FormatTipTypeName(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceForecast: return "PRICE FORECAST";
            case InsiderTipType.PriceFloor: return "PRICE FLOOR";
            case InsiderTipType.PriceCeiling: return "PRICE CEILING";
            case InsiderTipType.TrendDirection: return "TREND DIRECTION";
            case InsiderTipType.EventForecast: return "EVENT FORECAST";
            case InsiderTipType.EventCount: return "EVENT COUNT";
            case InsiderTipType.VolatilityWarning: return "VOLATILITY WARNING";
            case InsiderTipType.OpeningPrice: return "OPENING PRICE";
            default: return "UNKNOWN";
        }
    }

    /// <summary>
    /// Populates the bonds panel (Story 13.6, AC 1, 2, 8, 11, 13).
    /// Shows bond price, buy button, bonds owned info, sell button.
    /// </summary>
    public void ShowBonds(RunContext ctx, System.Action onPurchase, System.Action onSell)
    {
        _ctx = ctx;
        _onBondPurchase = onPurchase;
        _onBondSell = onSell;

        ClearBondPanel();

        if (_bondsPanel == null) return;

        // Remove placeholder label if present
        var contentLabel = _bondsPanel.transform.Find("BondsPanelContent");
        if (contentLabel != null) contentLabel.gameObject.SetActive(false);

        // Bond card container
        var cardGo = new GameObject("BondCard");
        cardGo.transform.SetParent(_bondsPanel.transform, false);
        var cardBg = cardGo.AddComponent<Image>();
        cardBg.color = BondCardColor;
        var cardLayout = cardGo.AddComponent<LayoutElement>();
        cardLayout.flexibleWidth = 1f;
        cardLayout.flexibleHeight = 1f;

        // Store reference for pulsing glow (Story 13.8, AC 5)
        _bondCardBg = cardBg;
        _bondCardOutline = cardGo.AddComponent<Outline>();
        _bondCardOutline.effectColor = CashColor;
        _bondCardOutline.effectDistance = new Vector2(2f, 2f);

        // Bond hover detection
        _bondHovered = false;
        var bondTrigger = cardGo.AddComponent<EventTrigger>();
        var bondEnterEntry = new EventTrigger.Entry();
        bondEnterEntry.eventID = EventTriggerType.PointerEnter;
        bondEnterEntry.callback.AddListener((_) => _bondHovered = true);
        bondTrigger.triggers.Add(bondEnterEntry);
        var bondExitEntry = new EventTrigger.Entry();
        bondExitEntry.eventID = EventTriggerType.PointerExit;
        bondExitEntry.callback.AddListener((_) => _bondHovered = false);
        bondTrigger.triggers.Add(bondExitEntry);

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(10, 10, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Title
        var titleGo = CreateTextChild(cardGo.transform, "Title", "BONDS", 14, FontStyle.Bold, CashColor, 20f);

        // Price display — prominent with cash icon (Story 13.8, AC 5, 7)
        var priceGo = CreateTextChild(cardGo.transform, "Price", "", 16, FontStyle.Bold, CashColor, 22f);
        _bondPriceText = priceGo.GetComponent<Text>();

        // Info display: bonds owned + earning
        var infoGo = CreateTextChild(cardGo.transform, "Info", "", 11, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f, 1f), 32f);
        _bondInfoText = infoGo.GetComponent<Text>();

        // Buy button
        var buyBtnGo = new GameObject("BuyBondButton");
        buyBtnGo.transform.SetParent(cardGo.transform, false);
        var buyBtnImg = buyBtnGo.AddComponent<Image>();
        buyBtnImg.color = new Color(0.1f, 0.3f, 0.1f, 1f);
        _bondBuyButton = buyBtnGo.AddComponent<Button>();
        var buyBtnLayout = buyBtnGo.AddComponent<LayoutElement>();
        buyBtnLayout.preferredHeight = 24f;

        var buyBtnTextGo = new GameObject("ButtonText");
        buyBtnTextGo.transform.SetParent(buyBtnGo.transform, false);
        var buyBtnRect = buyBtnTextGo.AddComponent<RectTransform>();
        buyBtnRect.anchorMin = Vector2.zero;
        buyBtnRect.anchorMax = Vector2.one;
        buyBtnRect.offsetMin = Vector2.zero;
        buyBtnRect.offsetMax = Vector2.zero;
        _bondBuyButtonText = buyBtnTextGo.AddComponent<Text>();
        _bondBuyButtonText.text = "BUY BOND";
        _bondBuyButtonText.fontSize = 12;
        _bondBuyButtonText.fontStyle = FontStyle.Bold;
        _bondBuyButtonText.color = Color.white;
        _bondBuyButtonText.alignment = TextAnchor.MiddleCenter;
        _bondBuyButtonText.raycastTarget = false;
        _bondBuyButton.onClick.AddListener(() => _onBondPurchase?.Invoke());

        // Sell button
        var sellGo = CreateTextChild(cardGo.transform, "SellPrice", "", 10, FontStyle.Normal, new Color(0.7f, 0.7f, 0.7f, 1f), 16f);
        _bondSellText = sellGo.GetComponent<Text>();

        var sellBtnGo = new GameObject("SellBondButton");
        sellBtnGo.transform.SetParent(cardGo.transform, false);
        var sellBtnImg = sellBtnGo.AddComponent<Image>();
        sellBtnImg.color = new Color(0.3f, 0.15f, 0.1f, 1f);
        _bondSellButton = sellBtnGo.AddComponent<Button>();
        var sellBtnLayout = sellBtnGo.AddComponent<LayoutElement>();
        sellBtnLayout.preferredHeight = 22f;

        var sellBtnTextGo = new GameObject("ButtonText");
        sellBtnTextGo.transform.SetParent(sellBtnGo.transform, false);
        var sellBtnRect = sellBtnTextGo.AddComponent<RectTransform>();
        sellBtnRect.anchorMin = Vector2.zero;
        sellBtnRect.anchorMax = Vector2.one;
        sellBtnRect.offsetMin = Vector2.zero;
        sellBtnRect.offsetMax = Vector2.zero;
        _bondSellButtonText = sellBtnTextGo.AddComponent<Text>();
        _bondSellButtonText.text = "SELL BOND";
        _bondSellButtonText.fontSize = 11;
        _bondSellButtonText.fontStyle = FontStyle.Bold;
        _bondSellButtonText.color = Color.white;
        _bondSellButtonText.alignment = TextAnchor.MiddleCenter;
        _bondSellButtonText.raycastTarget = false;

        // Sell uses confirmation (AC 12)
        _bondSellButton.onClick.AddListener(() => ShowBondSellConfirmation(ctx));

        // Confirmation overlay (initially hidden)
        _bondConfirmOverlay = CreateBondConfirmOverlay(cardGo.transform);
        _bondConfirmOverlay.SetActive(false);

        RefreshBondPanel(ctx);
    }

    /// <summary>
    /// Refreshes the bond panel state: price, affordability, sell visibility (AC 2, 8, 11, 13).
    /// </summary>
    public void RefreshBondPanel(RunContext ctx)
    {
        _ctx = ctx;
        UpdateCurrencyDisplays();

        if (_bondPriceText == null) return;

        int price = BondManager.GetCurrentPrice(ctx.CurrentRound);
        bool isRound8 = ctx.CurrentRound >= GameConfig.TotalRounds;

        // Price and buy button
        if (isRound8)
        {
            _bondPriceText.text = "NO BONDS AVAILABLE";
            _bondBuyButton.interactable = false;
            _bondBuyButtonText.text = "ROUND 8";
        }
        else
        {
            _bondPriceText.text = $"Bond Price: ${price}";
            bool canAfford = ctx.Portfolio.CanAfford(price);
            _bondBuyButton.interactable = canAfford;
            _bondBuyButtonText.text = canAfford ? $"BUY BOND (${price})" : "CAN'T AFFORD";
        }

        // Info display
        int repPerRound = ctx.BondsOwned * GameConfig.BondRepPerRoundPerBond;
        _bondInfoText.text = $"Bonds Owned: {ctx.BondsOwned}\nEarning: +{repPerRound} Rep/round";

        // Sell button visibility (AC 11)
        bool hasBonds = ctx.BondsOwned > 0;
        _bondSellButton.gameObject.SetActive(hasBonds);
        _bondSellText.gameObject.SetActive(hasBonds);

        if (hasBonds && ctx.BondPurchaseHistory.Count > 0)
        {
            var lastBond = ctx.BondPurchaseHistory[ctx.BondPurchaseHistory.Count - 1];
            float sellPrice = lastBond.PricePaid * GameConfig.BondSellMultiplier;
            _bondSellButtonText.text = $"SELL BOND (${sellPrice:F0})";
            _bondSellText.text = $"Sell: ${sellPrice:F0} (half price)";
        }

        // Refresh other panels' affordability since cash changed
        RefreshRelicAffordability();
        RefreshExpansionAffordability();
        RefreshTipAffordability();
        UpdateRerollDisplay();
    }

    /// <summary>
    /// Shows the bond sell confirmation overlay (AC 12).
    /// </summary>
    private void ShowBondSellConfirmation(RunContext ctx)
    {
        if (_bondConfirmOverlay == null) return;

        if (ctx.BondsOwned <= 0 || ctx.BondPurchaseHistory.Count == 0) return;

        var lastBond = ctx.BondPurchaseHistory[ctx.BondPurchaseHistory.Count - 1];
        float sellPrice = lastBond.PricePaid * GameConfig.BondSellMultiplier;

        var confirmText = _bondConfirmOverlay.transform.Find("ConfirmText")?.GetComponent<Text>();
        if (confirmText != null)
        {
            confirmText.text = $"Sell 1 bond for ${sellPrice:F0}?";
        }

        _bondConfirmOverlay.SetActive(true);
    }

    private GameObject CreateBondConfirmOverlay(Transform parent)
    {
        var overlayGo = new GameObject("BondConfirmOverlay");
        overlayGo.transform.SetParent(parent, false);
        var overlayRect = overlayGo.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

        var vlg = overlayGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(10, 10, 16, 10);
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateTextChild(overlayGo.transform, "ConfirmText", "Sell 1 bond for $X?", 12, FontStyle.Bold, Color.white, 24f);

        // Yes button
        var yesBtnGo = new GameObject("YesButton");
        yesBtnGo.transform.SetParent(overlayGo.transform, false);
        var yesBtnImg = yesBtnGo.AddComponent<Image>();
        yesBtnImg.color = new Color(0.3f, 0.15f, 0.1f, 1f);
        var yesBtn = yesBtnGo.AddComponent<Button>();
        var yesBtnLayout = yesBtnGo.AddComponent<LayoutElement>();
        yesBtnLayout.preferredHeight = 22f;

        var yesBtnTextGo = CreateTextChild(yesBtnGo.transform, "ButtonText", "YES, SELL", 11, FontStyle.Bold, Color.white, 0f);
        var yesBtnTextRect = yesBtnTextGo.GetComponent<RectTransform>();
        yesBtnTextRect.anchorMin = Vector2.zero;
        yesBtnTextRect.anchorMax = Vector2.one;
        yesBtnTextRect.offsetMin = Vector2.zero;
        yesBtnTextRect.offsetMax = Vector2.zero;

        yesBtn.onClick.AddListener(() =>
        {
            _bondConfirmOverlay.SetActive(false);
            _onBondSell?.Invoke();
        });

        // No button
        var noBtnGo = new GameObject("NoButton");
        noBtnGo.transform.SetParent(overlayGo.transform, false);
        var noBtnImg = noBtnGo.AddComponent<Image>();
        noBtnImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
        var noBtn = noBtnGo.AddComponent<Button>();
        var noBtnLayout = noBtnGo.AddComponent<LayoutElement>();
        noBtnLayout.preferredHeight = 22f;

        var noBtnTextGo = CreateTextChild(noBtnGo.transform, "ButtonText", "CANCEL", 11, FontStyle.Bold, Color.white, 0f);
        var noBtnTextRect = noBtnTextGo.GetComponent<RectTransform>();
        noBtnTextRect.anchorMin = Vector2.zero;
        noBtnTextRect.anchorMax = Vector2.one;
        noBtnTextRect.offsetMin = Vector2.zero;
        noBtnTextRect.offsetMax = Vector2.zero;

        noBtn.onClick.AddListener(() =>
        {
            _bondConfirmOverlay.SetActive(false);
        });

        return overlayGo;
    }

    private GameObject CreateTextChild(Transform parent, string name, string text, int fontSize, FontStyle style, Color color, float preferredHeight)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        if (preferredHeight > 0f)
        {
            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;
        }
        return go;
    }

    private void ClearBondPanel()
    {
        if (_bondsPanel == null) return;

        var bondCard = _bondsPanel.transform.Find("BondCard");
        if (bondCard != null)
            Destroy(bondCard.gameObject);

        _bondPriceText = null;
        _bondInfoText = null;
        _bondSellText = null;
        _bondBuyButton = null;
        _bondBuyButtonText = null;
        _bondSellButton = null;
        _bondSellButtonText = null;
        _bondConfirmOverlay = null;
        _bondCardBg = null;
        _bondCardOutline = null;
        _bondHovered = false;
    }

    private void ClearExpansionCards()
    {
        if (_expansionCards != null)
        {
            for (int i = 0; i < _expansionCards.Length; i++)
            {
                if (_expansionCards[i].Root != null)
                    Destroy(_expansionCards[i].Root);
            }
            _expansionCards = null;
        }
    }

    // === Hover Effects (Story 13.8, Task 1, AC 1) ===

    private void OnRelicHoverEnter(int index)
    {
        if (index < 0 || index >= _relicSlots.Length) return;
        if (_soldFlags != null && index < _soldFlags.Length && _soldFlags[index]) return;
        if (_relicOffering == null || !_relicOffering[index].HasValue) return;

        if (_hoverCoroutines[index] != null) StopCoroutine(_hoverCoroutines[index]);
        _hoverCoroutines[index] = StartCoroutine(AnimateHover(index, true));
    }

    private void OnRelicHoverExit(int index)
    {
        if (index < 0 || index >= _relicSlots.Length) return;
        if (_soldFlags != null && index < _soldFlags.Length && _soldFlags[index]) return;

        if (_hoverCoroutines[index] != null) StopCoroutine(_hoverCoroutines[index]);
        _hoverCoroutines[index] = StartCoroutine(AnimateHover(index, false));
    }

    private IEnumerator AnimateHover(int index, bool entering)
    {
        var slot = _relicSlots[index];
        var rect = slot.Root.GetComponent<RectTransform>();

        float startScale = rect.localScale.x;
        float endScale = entering ? HoverScale : 1f;
        Color startColor = slot.CardBackground.color;
        Color endColor = entering ? RelicCardHoverColor : RelicCardColor;

        for (float t = 0; t < HoverDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / HoverDuration;
            float s = Mathf.Lerp(startScale, endScale, p);
            rect.localScale = new Vector3(s, s, 1f);
            slot.CardBackground.color = Color.Lerp(startColor, endColor, p);
            yield return null;
        }

        rect.localScale = new Vector3(endScale, endScale, 1f);
        slot.CardBackground.color = endColor;
        _hoverCoroutines[index] = null;
    }

    // === Purchase Animation (Story 13.8, Task 2, AC 2) ===

    private IEnumerator AnimateCardPurchase(int cardIndex)
    {
        var slot = _relicSlots[cardIndex];
        var group = slot.Group;
        var rect = slot.Root.GetComponent<RectTransform>();

        if (group == null)
        {
            ApplySoldState(cardIndex);
            yield break;
        }

        // Animate: slide up + fade out over PurchaseAnimDuration (AC 2)
        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * 50f;
        for (float t = 0; t < PurchaseAnimDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / PurchaseAnimDuration;
            group.alpha = Mathf.Lerp(1f, 0f, p);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
            yield return null;
        }
        group.alpha = 0f;

        // Show SOLD stamp
        var stampGo = new GameObject("SoldStamp");
        stampGo.transform.SetParent(slot.Root.transform, false);
        var stampRect = stampGo.AddComponent<RectTransform>();
        stampRect.anchorMin = Vector2.zero;
        stampRect.anchorMax = Vector2.one;
        stampRect.offsetMin = Vector2.zero;
        stampRect.offsetMax = Vector2.zero;
        var stampText = stampGo.AddComponent<Text>();
        stampText.text = "SOLD";
        stampText.fontSize = 32;
        stampText.fontStyle = FontStyle.Bold;
        stampText.color = new Color(1f, 0.3f, 0.3f, 0.9f);
        stampText.alignment = TextAnchor.MiddleCenter;
        stampText.raycastTarget = false;

        // Make stamp visible while card content is hidden
        // ignoreParentGroups ensures stamp is visible despite parent CanvasGroup alpha=0
        var stampGroup = stampGo.AddComponent<CanvasGroup>();
        stampGroup.ignoreParentGroups = true;
        group.alpha = 0f;
        stampGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(SoldStampDuration);

        // Clean up stamp and apply sold state
        Destroy(stampGo);
        group.alpha = 1f;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = startPos;
        ApplySoldState(cardIndex);
    }

    private void ApplySoldState(int cardIndex)
    {
        _soldFlags[cardIndex] = true;
        _relicSlots[cardIndex].PurchaseButton.interactable = false;
        _relicSlots[cardIndex].ButtonText.text = "SOLD";
        _relicSlots[cardIndex].CardBackground.color = SoldCardColor;
    }

    // === Reroll Animation (Story 13.8, Task 3, AC 3) ===

    private IEnumerator AnimateRerollFlip(RelicDef?[] newOffering)
    {
        // Animate each non-sold card with staggered flip
        for (int i = 0; i < _relicSlots.Length && i < newOffering.Length; i++)
        {
            if (_soldFlags[i]) continue;

            int capturedIndex = i;
            var capturedOffering = newOffering;
            StartCoroutine(AnimateSingleRerollFlip(capturedIndex, capturedOffering));
            yield return new WaitForSecondsRealtime(RerollStaggerDelay);
        }

        // Wait for all flips to complete
        yield return new WaitForSecondsRealtime(RerollFlipDuration);

        UpdateRerollDisplay();
        RefreshRelicCapacity();
        RefreshRelicAffordability();
    }

    private IEnumerator AnimateSingleRerollFlip(int index, RelicDef?[] newOffering)
    {
        var slot = _relicSlots[index];
        var rect = slot.Root.GetComponent<RectTransform>();
        float halfDuration = RerollFlipDuration * 0.5f;

        // First half: scale X from 1 to 0 (flip away)
        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, p);
            rect.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
        rect.localScale = new Vector3(0f, 1f, 1f);

        // Swap content at midpoint
        if (newOffering[index].HasValue)
            SetupRelicSlot(index, newOffering[index].Value);
        else
            SetupSoldOutRelicSlot(index);

        // Second half: scale X from 0 to 1 (flip in)
        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, p);
            rect.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    // === Insider Tip Flip Animation (Story 13.8, Task 4, AC 4) ===

    private IEnumerator AnimateTipFlip(int cardIndex)
    {
        if (_tipCards == null || cardIndex < 0 || cardIndex >= _tipCards.Length) yield break;

        var card = _tipCards[cardIndex];
        var rect = card.Root.GetComponent<RectTransform>();
        float halfDuration = TipFlipDuration * 0.5f;

        // First half: scale X from 1 to 0 (face-down flipping)
        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, p);
            rect.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
        rect.localScale = new Vector3(0f, 1f, 1f);

        // Swap to revealed content at midpoint
        card.IsRevealed = true;
        card.PurchaseButton.interactable = false;
        card.ButtonText.text = "REVEALED";
        if (_tipOffering != null && cardIndex < _tipOffering.Length)
        {
            var offering = _tipOffering[cardIndex];
            card.NameText.text = FormatTipTypeName(offering.Definition.Type);
            card.DescriptionText.text = offering.RevealedText;
            card.DescriptionText.fontSize = 12;
            card.DescriptionText.fontStyle = FontStyle.Normal;
            card.DescriptionText.color = new Color(0.9f, 0.95f, 0.8f, 1f);
        }
        card.CardBackground.color = TipCardRevealedColor;

        // Second half: scale X from 0 to 1 (revealed flipping in)
        for (float t = 0; t < halfDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, p);
            rect.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;

        // Flash effect: brief white overlay
        if (card.Group != null)
        {
            var flashGo = new GameObject("TipFlash");
            flashGo.transform.SetParent(card.Root.transform, false);
            var flashRect = flashGo.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.offsetMin = Vector2.zero;
            flashRect.offsetMax = Vector2.zero;
            var flashImg = flashGo.AddComponent<Image>();
            flashImg.color = new Color(1f, 1f, 1f, 0.4f);
            flashImg.raycastTarget = false;

            for (float t = 0; t < TipFlashDuration; t += Time.unscaledDeltaTime)
            {
                float p = t / TipFlashDuration;
                flashImg.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.4f, 0f, p));
                yield return null;
            }
            Destroy(flashGo);
        }
    }

    // === Expansion OWNED Watermark Animation (Story 13.8, Task 6, AC 6) ===

    private IEnumerator AnimateOwnedWatermark(int index)
    {
        if (_expansionCards == null || index < 0 || index >= _expansionCards.Length) yield break;

        var card = _expansionCards[index];

        // Create OWNED watermark overlay
        var watermarkGo = new GameObject("OwnedWatermark");
        watermarkGo.transform.SetParent(card.Root.transform, false);
        var watermarkRect = watermarkGo.AddComponent<RectTransform>();
        watermarkRect.anchorMin = Vector2.zero;
        watermarkRect.anchorMax = Vector2.one;
        watermarkRect.offsetMin = Vector2.zero;
        watermarkRect.offsetMax = Vector2.zero;
        var watermarkText = watermarkGo.AddComponent<Text>();
        watermarkText.text = "OWNED";
        watermarkText.fontSize = 28;
        watermarkText.fontStyle = FontStyle.Bold;
        watermarkText.color = new Color(1f, 1f, 1f, 0f);
        watermarkText.alignment = TextAnchor.MiddleCenter;
        watermarkText.raycastTarget = false;

        // Fade in over OwnedFadeDuration
        for (float t = 0; t < OwnedFadeDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / OwnedFadeDuration;
            watermarkText.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 0.4f, p));
            yield return null;
        }
        watermarkText.color = new Color(1f, 1f, 1f, 0.4f);
    }

    private void Update()
    {
        if (_root == null || !_root.activeSelf) return;
        HandleKeyboardNavigation();
        UpdateBondPulse();
    }

    private void HandleKeyboardNavigation()
    {
        if (_focusablePanels == null) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            int next = (_focusedPanelIndex + 1) % _focusablePanels.Length;
            SetFocusedPanel(next);
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            if (_focusedPanelIndex > 0)
                SetFocusedPanel(_focusedPanelIndex - 1);
            else if (_focusedPanelIndex < 0)
                SetFocusedPanel(0);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
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

    // === Bond Pulsing Glow (Story 13.8, Task 5, AC 5) ===

    private void UpdateBondPulse()
    {
        if (_bondCardOutline == null) return;

        // Sinusoidal alpha oscillation
        float pulse = (Mathf.Sin(Time.unscaledTime * BondPulseSpeed * Mathf.PI) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(BondPulseMinAlpha, BondPulseMaxAlpha, pulse);

        // Increase intensity on hover
        if (_bondHovered)
            alpha = Mathf.Clamp01(alpha + BondPulseHoverBoost);

        var color = _bondCardOutline.effectColor;
        color.a = alpha;
        _bondCardOutline.effectColor = color;
    }

    public int FocusedPanelIndex => _focusedPanelIndex;

    private void SetupSoldOutRelicSlot(int index)
    {
        var slot = _relicSlots[index];
        slot.NameText.text = "EMPTY";
        slot.DescriptionText.text = "No relic available";
        slot.CostText.text = "";
        slot.PurchaseButton.interactable = false;
        slot.ButtonText.text = "SOLD OUT";
        slot.CardBackground.color = SoldOutCardColor;
        slot.PurchaseButton.onClick.RemoveAllListeners();
    }

    private void SetupRelicSlot(int index, RelicDef relic)
    {
        var slot = _relicSlots[index];

        slot.NameText.text = relic.Name;
        slot.DescriptionText.text = relic.Description;
        slot.CostText.text = $"\u2605 {relic.Cost}";

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

}
