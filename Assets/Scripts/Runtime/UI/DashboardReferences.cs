using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Data container holding references to all Control Deck UI elements.
/// Populated by UISetup.ExecuteControlDeck() in Story 14.2.
/// Plain C# class (not MonoBehaviour) — follows Setup-Oriented pattern.
/// </summary>
public class DashboardReferences
{
    // ── Left Wing: Wallet / Stats ───────────────────────────────────────
    public Text CashText;
    public Text ProfitText;
    public Text TargetText;
    public Image TargetProgressBar;

    // ── Center Core: Action Buttons ─────────────────────────────────────
    public Button BuyButton;
    public Button SellButton;
    public Button ShortButton;

    // Buy/Sell UI references (for cooldown dimming + timer text)
    public Image BuyButtonImage;
    public Text BuyButtonText;
    public Image SellButtonImage;
    public Text SellButtonText;

    // Short UI references
    public Image ShortButtonImage;
    public Text ShortButtonText;

    // Short 2 (Dual Short expansion) references
    public Image Short2ButtonImage;
    public Text Short2ButtonText;
    public GameObject Short2Container;

    // Cooldown & Leverage
    public GameObject CooldownOverlay;
    public Text CooldownTimerText;
    public GameObject LeverageBadge;

    // ── Right Wing: Position Info ───────────────────────────────────────
    public Transform PositionEntryContainer;
    public Text PositionEmptyText;
    public Text TimerText;
    public Image TimerProgressBar;
    public Text RepText;

    // ── Container Transforms ────────────────────────────────────────────
    public RectTransform LeftWing;
    public RectTransform CenterCore;
    public RectTransform RightWing;
    public RectTransform ControlDeckPanel;
    public Canvas ControlDeckCanvas;

    // ── Event Ticker & Stock Labels ─────────────────────────────────────
    public Text EventTickerText;
    public Text StockNameLabel;
    public Text StockPriceLabel;
}
