using System.Collections;
using DG.Tweening;
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
    public static readonly Color ReputationColor = ColorPalette.Amber;

    // Cash display color (green)
    public static readonly Color CashColor = ColorPalette.Green;

    // Story 14.6: Color constants migrated to CRTThemeData
    public static Color PanelHeaderColor => ColorPalette.GreenDim;

    // Panel border/background colors
    public static readonly Color PanelBgColor = ColorPalette.Panel;
    public static readonly Color PanelBorderColor = ColorPalette.Border;

    // Focus indicator color
    public static readonly Color FocusColor = ColorPalette.WithAlpha(ColorPalette.Cyan, 0.6f);

    // Sold card color
    public static readonly Color SoldCardColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 1.5f), 0.7f);

    // Sold out card color
    public static readonly Color SoldOutCardColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);

    // Unified relic card border color (AC 1 — no rarity differentiation)
    public static readonly Color RelicCardColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 1.2f), 0.9f);
    public static readonly Color RelicCardHoverColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 2f), 1f);

    // AC 11: Shop cascade entry constants
    public const float ShopCascadeStagger = 0.06f;
    public const float ShopCascadeDuration = 0.2f;
    public const float ShopCascadeOffset = 40f;

    // Animation timing constants
    public const float HoverScale = 1.05f;
    public const float HoverDuration = 0.15f;
    public const float PurchaseAnimDuration = 0.5f;
    // Story 17.2: Repurposed as post-purchase pause duration (SOLD stamp removed)
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

    private static Font _cachedFont;
    public static Font DefaultFont
    {
        get
        {
            if (_cachedFont == null)
                _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _cachedFont;
        }
    }

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
    public static readonly Color ExpansionCardColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 2.5f), 0.95f);
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

    // Tip card colors (mystery theme — cyan-tinted)
    public static readonly Color TipCardFaceDownColor = ColorPalette.WithAlpha(ColorPalette.CyanDim, 0.9f);
    public static readonly Color TipCardRevealedColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 2.5f), 0.95f);

    // Button colors — clear affordability contrast
    public static readonly Color BuyButtonColor = ColorPalette.Dimmed(ColorPalette.Green, 0.8f);
    public static readonly Color CantAffordButtonColor = ColorPalette.Dimmed(ColorPalette.Border, 0.6f);
    public static readonly Color SellButtonColor = ColorPalette.Dimmed(ColorPalette.Red, 0.6f);

    // Bond panel colors (green/cash theme — Story 13.6)
    public static readonly Color BondCardColor = ColorPalette.WithAlpha(ColorPalette.Dimmed(ColorPalette.Panel, 2.5f), 0.95f);

    // Hover animation state (Story 13.8)
    private Coroutine[] _hoverCoroutines;

    // Bond card pulsing state (Story 13.8)
    private Image _bondCardBg;
    private Outline _bondCardOutline;
    private bool _bondHovered;

    // Flash feedback coroutine tracking (Story 13.10 — prevent stacking)
    private Coroutine[] _relicFlashCoroutines;
    private Coroutine[] _expansionFlashCoroutines;
    private Coroutine[] _tipFlashCoroutines;

    // Owned relics bar (Story 13.10)
    private OwnedRelicSlotView[] _ownedRelicSlots;
    private System.Action<int> _onSellRelic;
    public static readonly Color OwnedRelicSlotColor = ColorPalette.WithAlpha(ColorPalette.Panel, 0.7f);
    public static readonly Color OwnedRelicEmptyColor = ColorPalette.WithAlpha(ColorPalette.Panel, 0.3f);
    public static readonly Color OwnedRelicSellColor = ColorPalette.Dimmed(ColorPalette.Red, 0.5f);
    public const float InventoryFullFlashDuration = 1.5f;

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

    /// <summary>
    /// View data for an owned relic slot in the top bar (Story 13.10).
    /// </summary>
    public struct OwnedRelicSlotView
    {
        public GameObject Root;
        public Text NameLabel;
        public Button SellButton;
        public Text SellButtonText;
        public Text EmptyLabel;
        public CanvasGroup Group;
        public Image Background;
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
        Debug.Log($"[shop-not-showing-bug] SetBottomPanels: expansions={expansionsPanel != null}, tips={tipsPanel != null}, bonds={bondsPanel != null}");
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
        AddButtonHoverFeel(_nextRoundButton);
    }

    public void SetRerollButton(Button rerollButton, Text costText)
    {
        _rerollButton = rerollButton;
        _rerollCostText = costText;
        AddButtonHoverFeel(_rerollButton);
    }

    public void SetOnCloseCallback(System.Action callback)
    {
        _onClose = callback;
        if (_nextRoundButton != null)
        {
            _nextRoundButton.onClick.RemoveAllListeners();
            _nextRoundButton.onClick.AddListener(() => _onClose?.Invoke());
            AddButtonClickFeel(_nextRoundButton);
        }
    }

    public void SetOnRerollCallback(System.Action callback)
    {
        _onReroll = callback;
        if (_rerollButton != null)
        {
            _rerollButton.onClick.RemoveAllListeners();
            _rerollButton.onClick.AddListener(() => _onReroll?.Invoke());
            AddButtonClickFeel(_rerollButton);
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
        _relicFlashCoroutines = new Coroutine[relicOffering.Length];
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
        RefreshOwnedRelicsBar();
        StartCoroutine(AnimateShopEntry());
    }

    // AC 11: Cascade relic card slots in from below on shop open
    private IEnumerator AnimateShopEntry()
    {
        // Force layout rebuild so anchoredPosition values are correct before animating
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < _relicSlots.Length; i++)
        {
            var slot = _relicSlots[i];
            if (slot.Root == null) continue;

            var rect = slot.Root.GetComponent<RectTransform>();
            if (rect == null) continue;

            Vector2 targetPos = rect.anchoredPosition;
            rect.anchoredPosition = targetPos + Vector2.down * ShopCascadeOffset;
            if (slot.Group != null) slot.Group.alpha = 0f;

            yield return new WaitForSecondsRealtime(i * ShopCascadeStagger);

            float elapsed = 0f;
            while (elapsed < ShopCascadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ShopCascadeDuration);
                rect.anchoredPosition = Vector2.Lerp(targetPos + Vector2.down * ShopCascadeOffset, targetPos, t);
                if (slot.Group != null) slot.Group.alpha = t;
                yield return null;
            }

            rect.anchoredPosition = targetPos;
            if (slot.Group != null) slot.Group.alpha = 1f;
        }
    }

    public void Hide()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    /// <summary>
    /// Refreshes the relic slots with a new offering after reroll.
    /// Story 17.2 AC 3: Regenerates ALL 3 slots with fresh relics (including previously purchased slots).
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
    /// Refreshes all store displays after a relic is sold from the owned bar (Story 13.10, AC 6).
    /// Updates currency, relic card states (capacity may have freed up), and other panels.
    /// </summary>
    public void RefreshAfterSell()
    {
        UpdateCurrencyDisplays();
        RefreshRelicAffordability();
        RefreshExpansionAffordability();
        RefreshTipAffordability();
        UpdateRerollDisplay();
    }

    /// <summary>
    /// Populates the expansions panel with available expansion cards (Story 13.4, AC 1, 2, 7).
    /// Called by ShopState after generating the expansion offering.
    /// </summary>
    public void ShowExpansions(RunContext ctx, ExpansionDef[] offering, System.Action<int> onPurchase)
    {
        Debug.Log($"[shop-not-showing-bug] ShowExpansions called — panel={_expansionsPanel != null}, offering={offering?.Length}, offeringNull={offering == null}");
        _ctx = ctx;
        _expansionOffering = offering;
        _onExpansionPurchase = onPurchase;

        // Clear existing expansion card objects
        ClearExpansionCards();

        if (_expansionsPanel == null || offering == null || offering.Length == 0)
        {
            Debug.LogWarning($"[shop-not-showing-bug] ShowExpansions EARLY RETURN — panel={_expansionsPanel != null}, offeringNull={offering == null}, offeringLen={offering?.Length}");
            return;
        }

        // Remove placeholder "Coming soon..." label if present
        var contentLabel = _expansionsPanel.transform.Find("ExpansionsPanelContent");
        if (contentLabel != null) contentLabel.gameObject.SetActive(false);

        _expansionCards = new ExpansionCardView[offering.Length];
        _expansionFlashCoroutines = new Coroutine[offering.Length];
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
                _expansionCards[i].PurchaseButton.interactable = true;
                _expansionCards[i].ButtonText.gameObject.SetActive(false);
                _expansionCards[i].CostText.color = canAfford ? Color.white : ColorPalette.Red;
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
        _expansionCards[index].ButtonText.color = Color.white;
        _expansionCards[index].ButtonText.gameObject.SetActive(true);
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
            _expansionCards[i].CostText.color = canAfford ? Color.white : ColorPalette.Red;
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
        cardLayout.preferredHeight = 95f;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Name — bold/larger for legibility (Story 13.8, AC 7)
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(cardGo.transform, false);
        view.NameText = nameGo.AddComponent<Text>();
        view.NameText.font = DefaultFont;
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
        view.DescriptionText.font = DefaultFont;
        view.DescriptionText.text = expansion.Description;
        view.DescriptionText.fontSize = 11;
        view.DescriptionText.color = ColorPalette.WhiteDim;
        view.DescriptionText.alignment = TextAnchor.MiddleCenter;
        view.DescriptionText.raycastTarget = false;
        view.DescriptionText.lineSpacing = 1.1f;
        var descLayout = descGo.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 16f;

        // Cost — prominently sized (Story 13.8, AC 7)
        var costGo = new GameObject("Cost");
        costGo.transform.SetParent(cardGo.transform, false);
        view.CostText = costGo.AddComponent<Text>();
        view.CostText.font = DefaultFont;
        view.CostText.text = $"\u2605 {expansion.Cost}";
        view.CostText.fontSize = 13;
        view.CostText.color = ReputationColor;
        view.CostText.alignment = TextAnchor.MiddleCenter;
        view.CostText.raycastTarget = false;
        var costLayout = costGo.AddComponent<LayoutElement>();
        costLayout.preferredHeight = 16f;

        // Story 13.10: Click-to-buy — Button on card root, no separate buy button (AC 9)
        view.PurchaseButton = cardGo.AddComponent<Button>();

        // Feedback overlay text — hidden by default, shown for "CAN'T AFFORD" / "OWNED"
        var feedbackGo = new GameObject("Feedback");
        feedbackGo.transform.SetParent(cardGo.transform, false);
        view.ButtonText = feedbackGo.AddComponent<Text>();
        view.ButtonText.font = DefaultFont;
        view.ButtonText.text = "";
        view.ButtonText.fontSize = 12;
        view.ButtonText.fontStyle = FontStyle.Bold;
        view.ButtonText.color = Color.white;
        view.ButtonText.alignment = TextAnchor.MiddleCenter;
        view.ButtonText.raycastTarget = false;
        feedbackGo.SetActive(false);
        var feedbackLayout = feedbackGo.AddComponent<LayoutElement>();
        feedbackLayout.preferredHeight = 16f;

        int capturedIndex = index;
        view.PurchaseButton.onClick.AddListener(() => OnExpansionCardClicked(capturedIndex));
        AddButtonClickFeel(view.PurchaseButton);
        AddButtonHoverFeel(view.PurchaseButton);

        return view;
    }

    /// <summary>
    /// Handles expansion card click for click-to-buy (Story 13.10, AC 9).
    /// </summary>
    private void OnExpansionCardClicked(int index)
    {
        if (_ctx == null) return;
        if (_expansionOffering == null || index >= _expansionOffering.Length) return;

        var expansion = _expansionOffering[index];

        // Already owned — do nothing
        if (_ctx.OwnedExpansions.Contains(expansion.Id)) return;

        if (!_ctx.Reputation.CanAfford(expansion.Cost))
        {
            if (_expansionFlashCoroutines[index] != null) StopCoroutine(_expansionFlashCoroutines[index]);
            _expansionFlashCoroutines[index] = StartCoroutine(FlashExpansionFeedback(index, "CAN'T AFFORD", ColorPalette.Red));
            return;
        }

        _onExpansionPurchase?.Invoke(index);
    }

    private IEnumerator FlashExpansionFeedback(int index, string message, Color color)
    {
        if (_expansionCards == null || index >= _expansionCards.Length) yield break;
        var card = _expansionCards[index];

        card.ButtonText.text = message;
        card.ButtonText.color = color;
        card.ButtonText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(InventoryFullFlashDuration);

        card.ButtonText.gameObject.SetActive(false);
        _expansionFlashCoroutines[index] = null;
    }

    /// <summary>
    /// Populates the insider tips panel with mystery cards (Story 13.5, AC 2, 3).
    /// Cards start face-down showing "?" and cost. After purchase they reveal the tip text.
    /// </summary>
    public void ShowTips(RunContext ctx, InsiderTipGenerator.TipOffering[] offering, System.Action<int> onPurchase)
    {
        Debug.Log($"[shop-not-showing-bug] ShowTips called — panel={_tipsPanel != null}, offering={offering?.Length}, offeringNull={offering == null}");
        _ctx = ctx;
        _tipOffering = offering;
        _onTipPurchase = onPurchase;

        ClearTipCards();

        if (_tipsPanel == null || offering == null || offering.Length == 0)
        {
            Debug.LogWarning($"[shop-not-showing-bug] ShowTips EARLY RETURN — panel={_tipsPanel != null}, offeringNull={offering == null}, offeringLen={offering?.Length}");
            return;
        }

        // Remove placeholder label if present
        var contentLabel = _tipsPanel.transform.Find("TipsPanelContent");
        if (contentLabel != null) contentLabel.gameObject.SetActive(false);

        Debug.Log($"[shop-not-showing-bug] TipsPanel state before cards:" +
            $" active={_tipsPanel.activeSelf}" +
            $" childCount={_tipsPanel.transform.childCount}" +
            $" hasVLG={_tipsPanel.GetComponent<VerticalLayoutGroup>() != null}" +
            $" panelRect={_tipsPanel.GetComponent<RectTransform>()?.rect}");

        _tipCards = new TipCardView[offering.Length];
        _tipFlashCoroutines = new Coroutine[offering.Length];
        for (int i = 0; i < offering.Length; i++)
        {
            _tipCards[i] = CreateTipCard(i, offering[i], _tipsPanel.transform);
            bool canAfford = ctx.Reputation.CanAfford(offering[i].Definition.Cost);
            _tipCards[i].PurchaseButton.interactable = true;
            _tipCards[i].ButtonText.gameObject.SetActive(false);
            _tipCards[i].CostText.color = canAfford ? Color.white : ColorPalette.Red;

            var card = _tipCards[i];
            var cardRT = card.Root.GetComponent<RectTransform>();
            Debug.Log($"[shop-not-showing-bug] TipCard[{i}] created:" +
                $" root.active={card.Root.activeSelf}" +
                $" cardSize={cardRT.rect.width}x{cardRT.rect.height}" +
                $" nameText='{card.NameText.text}' nameFont={card.NameText.font != null} nameColor={card.NameText.color}" +
                $" descText='{card.DescriptionText.text}' descFont={card.DescriptionText.font != null}" +
                $" costText='{card.CostText.text}' costFont={card.CostText.font != null}" +
                $" childCount={card.Root.transform.childCount}" +
                $" parent={card.Root.transform.parent.name}");
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
            _tipCards[i].CostText.color = canAfford ? Color.white : ColorPalette.Red;
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
        cardLayout.preferredHeight = 100f;

        var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2f;
        vlg.padding = new RectOffset(8, 8, 6, 6);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Name — shows the tip type (e.g., "PRICE FORECAST")
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(cardGo.transform, false);
        view.NameText = nameGo.AddComponent<Text>();
        view.NameText.font = DefaultFont;
        view.NameText.text = FormatTipTypeName(offering.Definition.Type);
        view.NameText.fontSize = 13;
        view.NameText.fontStyle = FontStyle.Bold;
        view.NameText.color = Color.white;
        view.NameText.alignment = TextAnchor.MiddleCenter;
        view.NameText.raycastTarget = false;
        var nameLayout = nameGo.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 18f;

        // Description — brief hint of what the tip reveals (value hidden until purchase)
        var descGo = new GameObject("Description");
        descGo.transform.SetParent(cardGo.transform, false);
        view.DescriptionText = descGo.AddComponent<Text>();
        view.DescriptionText.font = DefaultFont;
        view.DescriptionText.text = GetTipFaceDownHint(offering.Definition.Type);
        view.DescriptionText.fontSize = 11;
        view.DescriptionText.fontStyle = FontStyle.Normal;
        view.DescriptionText.color = ColorPalette.WhiteDim;
        view.DescriptionText.alignment = TextAnchor.MiddleCenter;
        view.DescriptionText.raycastTarget = false;
        view.DescriptionText.lineSpacing = 1.1f;
        var descLayout = descGo.AddComponent<LayoutElement>();
        descLayout.preferredHeight = 18f;

        // Cost
        var costGo = new GameObject("Cost");
        costGo.transform.SetParent(cardGo.transform, false);
        view.CostText = costGo.AddComponent<Text>();
        view.CostText.font = DefaultFont;
        view.CostText.text = $"\u2605 {offering.Definition.Cost}";
        view.CostText.fontSize = 13;
        view.CostText.color = ReputationColor;
        view.CostText.alignment = TextAnchor.MiddleCenter;
        view.CostText.raycastTarget = false;
        var costLayout = costGo.AddComponent<LayoutElement>();
        costLayout.preferredHeight = 18f;

        // Story 13.10: Click-to-buy — Button on card root, no separate buy button (AC 10)
        view.PurchaseButton = cardGo.AddComponent<Button>();

        // Feedback overlay text — hidden by default
        var feedbackGo = new GameObject("Feedback");
        feedbackGo.transform.SetParent(cardGo.transform, false);
        view.ButtonText = feedbackGo.AddComponent<Text>();
        view.ButtonText.font = DefaultFont;
        view.ButtonText.text = "";
        view.ButtonText.fontSize = 12;
        view.ButtonText.fontStyle = FontStyle.Bold;
        view.ButtonText.color = Color.white;
        view.ButtonText.alignment = TextAnchor.MiddleCenter;
        view.ButtonText.raycastTarget = false;
        feedbackGo.SetActive(false);
        var feedbackLayout = feedbackGo.AddComponent<LayoutElement>();
        feedbackLayout.preferredHeight = 16f;

        int capturedIndex = index;
        view.PurchaseButton.onClick.AddListener(() => OnTipCardClicked(capturedIndex));
        AddButtonClickFeel(view.PurchaseButton);
        AddButtonHoverFeel(view.PurchaseButton);

        return view;
    }

    /// <summary>
    /// Handles tip card click for click-to-buy (Story 13.10, AC 10).
    /// </summary>
    private void OnTipCardClicked(int index)
    {
        if (_ctx == null) return;
        if (_tipCards == null || index >= _tipCards.Length) return;
        if (_tipCards[index].IsRevealed) return;

        if (_tipOffering == null || index >= _tipOffering.Length) return;
        int cost = _tipOffering[index].Definition.Cost;

        if (!_ctx.Reputation.CanAfford(cost))
        {
            if (_tipFlashCoroutines[index] != null) StopCoroutine(_tipFlashCoroutines[index]);
            _tipFlashCoroutines[index] = StartCoroutine(FlashTipFeedback(index, "CAN'T AFFORD", ColorPalette.Red));
            return;
        }

        _onTipPurchase?.Invoke(index);
    }

    private IEnumerator FlashTipFeedback(int index, string message, Color color)
    {
        if (_tipCards == null || index >= _tipCards.Length) yield break;
        var card = _tipCards[index];

        card.ButtonText.text = message;
        card.ButtonText.color = color;
        card.ButtonText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(InventoryFullFlashDuration);

        card.ButtonText.gameObject.SetActive(false);
        _tipFlashCoroutines[index] = null;
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
    /// Returns a brief face-down hint describing what this tip type reveals,
    /// without giving away the actual value.
    /// </summary>
    public static string GetTipFaceDownHint(InsiderTipType type)
    {
        switch (type)
        {
            case InsiderTipType.PriceForecast: return "Predicts the average price";
            case InsiderTipType.PriceFloor: return "Reveals the price floor";
            case InsiderTipType.PriceCeiling: return "Reveals the price ceiling";
            case InsiderTipType.TrendDirection: return "Shows the market trend";
            case InsiderTipType.EventForecast: return "Previews upcoming events";
            case InsiderTipType.EventCount: return "Reveals how many events";
            case InsiderTipType.VolatilityWarning: return "Warns about volatility";
            case InsiderTipType.OpeningPrice: return "Reveals the opening price";
            default: return "Unknown intel";
        }
    }

    /// <summary>
    /// Populates the bonds panel (Story 13.6, AC 1, 2, 8, 11, 13).
    /// Shows bond price, buy button, bonds owned info, sell button.
    /// </summary>
    public void ShowBonds(RunContext ctx, System.Action onPurchase, System.Action onSell)
    {
        Debug.Log($"[shop-not-showing-bug] ShowBonds called — panel={_bondsPanel != null}");
        _ctx = ctx;
        _onBondPurchase = onPurchase;
        _onBondSell = onSell;

        ClearBondPanel();

        if (_bondsPanel == null)
        {
            Debug.LogWarning("[shop-not-showing-bug] ShowBonds EARLY RETURN — bondsPanel is NULL");
            return;
        }

        // Remove placeholder label if present
        var contentLabel = _bondsPanel.transform.Find("BondsPanelContent");
        if (contentLabel != null) contentLabel.gameObject.SetActive(false);

        // Bond card container — transparent bg, content lives directly in the panel
        var cardGo = new GameObject("BondCard");
        cardGo.transform.SetParent(_bondsPanel.transform, false);
        var cardBg = cardGo.AddComponent<Image>();
        cardBg.color = Color.clear;
        cardBg.raycastTarget = true;
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

        // Price display — prominent with cash icon (Story 13.8, AC 5, 7)
        var priceGo = CreateTextChild(cardGo.transform, "Price", "", 16, FontStyle.Bold, CashColor, 22f);
        _bondPriceText = priceGo.GetComponent<Text>();

        // Info display: bonds owned + earning
        var infoGo = CreateTextChild(cardGo.transform, "Info", "", 11, FontStyle.Normal, ColorPalette.WhiteDim, 32f);
        _bondInfoText = infoGo.GetComponent<Text>();

        // Buy button
        var buyBtnGo = new GameObject("BuyBondButton");
        buyBtnGo.transform.SetParent(cardGo.transform, false);
        var buyBtnImg = buyBtnGo.AddComponent<Image>();
        buyBtnImg.color = BuyButtonColor;
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
        _bondBuyButtonText.font = DefaultFont;
        _bondBuyButtonText.text = "BUY BOND";
        _bondBuyButtonText.fontSize = 12;
        _bondBuyButtonText.fontStyle = FontStyle.Bold;
        _bondBuyButtonText.color = Color.white;
        _bondBuyButtonText.alignment = TextAnchor.MiddleCenter;
        _bondBuyButtonText.raycastTarget = false;
        _bondBuyButton.onClick.AddListener(() => _onBondPurchase?.Invoke());
        AddButtonClickFeel(_bondBuyButton);
        AddButtonHoverFeel(_bondBuyButton);

        // Sell button
        var sellGo = CreateTextChild(cardGo.transform, "SellPrice", "", 10, FontStyle.Normal, ColorPalette.WhiteDim, 16f);
        _bondSellText = sellGo.GetComponent<Text>();

        var sellBtnGo = new GameObject("SellBondButton");
        sellBtnGo.transform.SetParent(cardGo.transform, false);
        var sellBtnImg = sellBtnGo.AddComponent<Image>();
        sellBtnImg.color = SellButtonColor;
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
        _bondSellButtonText.font = DefaultFont;
        _bondSellButtonText.text = "SELL BOND";
        _bondSellButtonText.fontSize = 11;
        _bondSellButtonText.fontStyle = FontStyle.Bold;
        _bondSellButtonText.color = Color.white;
        _bondSellButtonText.alignment = TextAnchor.MiddleCenter;
        _bondSellButtonText.raycastTarget = false;

        // Sell uses confirmation (AC 12)
        _bondSellButton.onClick.AddListener(() => ShowBondSellConfirmation(ctx));
        AddButtonClickFeel(_bondSellButton);
        AddButtonHoverFeel(_bondSellButton);

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
            _bondBuyButton.GetComponent<Image>().color = CantAffordButtonColor;
        }
        else
        {
            _bondPriceText.text = $"Bond Price: ${price}";
            bool canAfford = ctx.Portfolio.CanAfford(price);
            _bondBuyButton.interactable = canAfford;
            _bondBuyButtonText.text = canAfford ? $"BUY BOND (${price})" : "CAN'T AFFORD";
            _bondBuyButton.GetComponent<Image>().color = canAfford ? BuyButtonColor : CantAffordButtonColor;
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
        overlayImg.color = ColorPalette.WithAlpha(ColorPalette.Background, 0.92f);

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
        yesBtnImg.color = SellButtonColor;
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
        AddButtonClickFeel(yesBtn);
        AddButtonHoverFeel(yesBtn);

        // No button
        var noBtnGo = new GameObject("NoButton");
        noBtnGo.transform.SetParent(overlayGo.transform, false);
        var noBtnImg = noBtnGo.AddComponent<Image>();
        noBtnImg.color = CantAffordButtonColor;
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
            AudioManager.Instance?.PlayCancel();
            _bondConfirmOverlay.SetActive(false);
        });
        AddButtonClickFeel(noBtn);
        AddButtonHoverFeel(noBtn);

        return overlayGo;
    }

    private GameObject CreateTextChild(Transform parent, string name, string text, int fontSize, FontStyle style, Color color, float preferredHeight)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = DefaultFont;
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

        AudioManager.Instance?.PlayRelicHover();
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

    /// <summary>
    /// Story 17.2 AC 1: Animate purchase — slide up + fade out, then show empty/blank slot.
    /// No SOLD stamp — card is removed from display.
    /// </summary>
    private IEnumerator AnimateCardPurchase(int cardIndex)
    {
        var slot = _relicSlots[cardIndex];
        var group = slot.Group;
        var rect = slot.Root.GetComponent<RectTransform>();

        if (group == null)
        {
            ApplyPurchasedEmptyState(cardIndex);
            yield break;
        }

        // Animate: slide up + fade out over PurchaseAnimDuration
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

        yield return new WaitForSecondsRealtime(SoldStampDuration * 0.5f);

        // Restore position and show as empty slot
        group.alpha = 1f;
        rect.localScale = Vector3.one;
        rect.anchoredPosition = startPos;
        ApplyPurchasedEmptyState(cardIndex);
    }

    /// <summary>
    /// Story 17.2 AC 1: After purchase, show slot as empty/blank (not SOLD stamp).
    /// Sets offering to null and displays the empty card state.
    /// </summary>
    private void ApplyPurchasedEmptyState(int cardIndex)
    {
        _soldFlags[cardIndex] = true;
        _relicOffering[cardIndex] = null;
        SetupSoldOutRelicSlot(cardIndex);
    }

    // === Reroll Animation (Story 13.8, Task 3, AC 3) ===

    /// <summary>
    /// Story 17.2 AC 3: Reroll animates ALL 3 slots with fresh relics.
    /// Sold flags are reset so previously purchased slots also get new relics.
    /// </summary>
    private IEnumerator AnimateRerollFlip(RelicDef?[] newOffering)
    {
        // Reset sold flags — all slots get fresh relics on reroll
        _soldFlags = new bool[_relicSlots.Length];

        // Animate all cards with staggered flip
        for (int i = 0; i < _relicSlots.Length && i < newOffering.Length; i++)
        {
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
            card.DescriptionText.color = ColorPalette.White;
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

    // ═══════════════════════════════════════════════════════════════
    // BUTTON FEEL — hover sound + scale + click punch
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Attaches hover sound + scale animation to a Button via EventTrigger.
    /// Does NOT touch onClick — safe to call before RemoveAllListeners is used on the button.
    /// Call AddButtonClickFeel separately after the final onClick.AddListener.
    /// </summary>
    private static void AddButtonHoverFeel(Button btn)
    {
        if (btn == null) return;
        var trigger = btn.gameObject.GetComponent<EventTrigger>()
                   ?? btn.gameObject.AddComponent<EventTrigger>();

        // Prevent entry accumulation on repeated calls (e.g. reroll re-setup)
        trigger.triggers.RemoveAll(e => e.eventID == EventTriggerType.PointerEnter
                                      || e.eventID == EventTriggerType.PointerExit);

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener((_) =>
        {
            AudioManager.Instance?.PlayButtonHover();
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOScale(1.07f, 0.1f).SetUpdate(true);
        });
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener((_) =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
        });
        trigger.triggers.Add(exitEntry);
    }

    /// <summary>
    /// Adds a click punch scale to a Button's onClick. Call this AFTER the final
    /// onClick.AddListener so it isn't wiped by a subsequent RemoveAllListeners.
    /// </summary>
    private static void AddButtonClickFeel(Button btn)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;
            btn.transform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 6, 0.5f).SetUpdate(true);
        });
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

        AudioManager.Instance?.PlayTabSwitch();
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
        slot.ButtonText.gameObject.SetActive(false);
        slot.CardBackground.color = SoldOutCardColor;
        slot.PurchaseButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Sets up a relic card for click-to-buy (Story 13.10, AC 8, 12, 13).
    /// Card is always interactable. Click attempts purchase; on failure flashes feedback.
    /// </summary>
    private void SetupRelicSlot(int index, RelicDef relic)
    {
        var slot = _relicSlots[index];

        slot.NameText.text = relic.Name;
        slot.DescriptionText.text = relic.Description;
        slot.CostText.text = $"\u2605 {relic.Cost}";

        bool canAfford = _ctx.Reputation.CanAfford(relic.Cost);
        slot.CostText.color = canAfford ? Color.white : ColorPalette.Red;

        // Card always clickable — feedback flash handles error states (AC 8)
        slot.PurchaseButton.interactable = true;
        slot.ButtonText.gameObject.SetActive(false);

        slot.PurchaseButton.onClick.RemoveAllListeners();
        int capturedIndex = index;
        slot.PurchaseButton.onClick.AddListener(() => OnRelicCardClicked(capturedIndex));
        AddButtonClickFeel(slot.PurchaseButton);
        AddButtonHoverFeel(slot.PurchaseButton);
    }

    /// <summary>
    /// Handles relic card click for click-to-buy (Story 13.10, AC 8, 12, 13).
    /// Checks capacity and affordability, shows flash feedback if can't buy.
    /// </summary>
    private void OnRelicCardClicked(int index)
    {
        if (_ctx == null) return;
        if (_soldFlags != null && index < _soldFlags.Length && _soldFlags[index]) return;
        if (!_relicOffering[index].HasValue) return;

        var relic = _relicOffering[index].Value;

        if (IsAtRelicCapacity())
        {
            if (_relicFlashCoroutines[index] != null) StopCoroutine(_relicFlashCoroutines[index]);
            _relicFlashCoroutines[index] = StartCoroutine(FlashCardFeedback(index, "INVENTORY FULL", ColorPalette.Red));
            return;
        }

        if (!_ctx.Reputation.CanAfford(relic.Cost))
        {
            if (_relicFlashCoroutines[index] != null) StopCoroutine(_relicFlashCoroutines[index]);
            _relicFlashCoroutines[index] = StartCoroutine(FlashCardFeedback(index, "CAN'T AFFORD", ColorPalette.Red));
            return;
        }

        _onPurchase?.Invoke(index);
    }

    /// <summary>
    /// Flashes a brief feedback message on a relic card (Story 13.10).
    /// </summary>
    private IEnumerator FlashCardFeedback(int index, string message, Color color)
    {
        if (index >= _relicSlots.Length) yield break;
        var slot = _relicSlots[index];

        slot.ButtonText.text = message;
        slot.ButtonText.color = color;
        slot.ButtonText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(InventoryFullFlashDuration);

        slot.ButtonText.gameObject.SetActive(false);
        _relicFlashCoroutines[index] = null;
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
    /// Refreshes all non-sold relic slots for capacity and affordability (AC 12, 13).
    /// Story 13.10: Click-to-buy — card always interactable, cost color indicates affordability.
    /// </summary>
    private void RefreshRelicCapacity()
    {
        RefreshRelicAffordability();
    }

    private void RefreshRelicAffordability()
    {
        if (_relicOffering == null) return;

        for (int i = 0; i < _relicSlots.Length && i < _relicOffering.Length; i++)
        {
            if (!_relicOffering[i].HasValue) continue;
            if (_soldFlags != null && i < _soldFlags.Length && _soldFlags[i]) continue;

            bool canAfford = _ctx.Reputation.CanAfford(_relicOffering[i].Value.Cost);
            _relicSlots[i].CostText.color = canAfford ? Color.white : ColorPalette.Red;
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
            _cashText.text = TradingHUD.FormatCurrency(_ctx.Portfolio.Cash);
            _cashText.color = _ctx.Portfolio.Cash < 0f ? TradingHUD.LossRed : CRTThemeData.TextHigh;
        }
    }

    // ── Owned Relics Bar (Story 13.10) ──

    /// <summary>
    /// Sets owned relic slot views. Called by UISetup during store construction.
    /// </summary>
    public void SetOwnedRelicSlots(OwnedRelicSlotView[] slots)
    {
        _ownedRelicSlots = slots;
    }

    /// <summary>
    /// Sets the sell callback for owned relics bar. Called by ShopState during Enter.
    /// </summary>
    public void SetSellRelicCallback(System.Action<int> onSellRelic)
    {
        _onSellRelic = onSellRelic;
    }

    /// <summary>
    /// Refreshes the owned relics bar to reflect current RunContext.OwnedRelics (AC: 3, 4, 5, 7).
    /// Shows relic name + sell button for owned slots, "Empty" for vacant slots.
    /// Handles dynamic slot count (5 base, 7 with Expanded Inventory).
    /// </summary>
    public void RefreshOwnedRelicsBar()
    {
        if (_ownedRelicSlots == null || _ctx == null) return;

        int maxSlots = ShopTransaction.GetEffectiveMaxRelicSlots(_ctx);

        for (int i = 0; i < _ownedRelicSlots.Length; i++)
        {
            var slot = _ownedRelicSlots[i];
            if (slot.Root == null) continue;

            // Show/hide extra slots based on Expanded Inventory
            if (i >= maxSlots)
            {
                slot.Root.SetActive(false);
                continue;
            }
            slot.Root.SetActive(true);

            if (i < _ctx.OwnedRelics.Count)
            {
                // Populated slot: show relic name and sell button
                string relicId = _ctx.OwnedRelics[i];
                var relicDef = ItemLookup.GetRelicById(relicId);

                string displayName = relicDef.HasValue ? relicDef.Value.Name : relicId;
                int refund = relicDef.HasValue ? relicDef.Value.Cost / 2 : 0;

                slot.NameLabel.text = displayName;
                slot.NameLabel.gameObject.SetActive(true);
                slot.SellButton.gameObject.SetActive(true);
                slot.SellButtonText.text = $"SELL \u2605{refund}";
                slot.EmptyLabel.gameObject.SetActive(false);

                slot.SellButton.onClick.RemoveAllListeners();
                int capturedIndex = i;
                slot.SellButton.onClick.AddListener(() => _onSellRelic?.Invoke(capturedIndex));
                AddButtonClickFeel(slot.SellButton);
                AddButtonHoverFeel(slot.SellButton);

                slot.Background.color = OwnedRelicSlotColor;
            }
            else
            {
                // Empty slot
                slot.NameLabel.gameObject.SetActive(false);
                slot.SellButton.gameObject.SetActive(false);
                slot.EmptyLabel.gameObject.SetActive(true);
                slot.EmptyLabel.text = "Empty";

                slot.Background.color = OwnedRelicEmptyColor;
            }
        }
    }

    /// <summary>
    /// Returns the maximum possible owned relic slots (7 with Expanded Inventory).
    /// Used by UISetup to create the right number of slot views.
    /// </summary>
    public static int MaxPossibleOwnedSlots => GameConfig.MaxRelicSlots + 2;

}
