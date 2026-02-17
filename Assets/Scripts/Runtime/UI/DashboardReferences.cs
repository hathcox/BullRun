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

    // ── Center Core: Action Buttons ─────────────────────────────────────
    public Button BuyButton;
    public Button SellButton;
    public Button ShortButton;
    public Image CooldownOverlay;

    // ── Right Wing: Position Info ───────────────────────────────────────
    public Text DirectionText;
    public Text AvgPriceText;
    public Text PnLText;
    public Text TimerText;
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
