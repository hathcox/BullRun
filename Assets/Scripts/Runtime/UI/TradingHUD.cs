using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Real-time trading HUD displaying cash, portfolio value, round profit, and margin target.
/// Reads from RunContext.Portfolio (one-way dependency). Refreshes on PriceUpdatedEvent and TradeExecutedEvent.
/// </summary>
public class TradingHUD : MonoBehaviour
{
    // Color constants for profit/loss states
    public static readonly Color ProfitGreen = new Color(0f, 1f, 0.533f, 1f); // #00FF88
    public static readonly Color LossRed = new Color(1f, 0.2f, 0.2f, 1f);     // #FF3333
    public static readonly Color WarningYellow = new Color(1f, 0.85f, 0.2f, 1f); // #FFD933

    private RunContext _runContext;
    private int _currentRound;
    private float _roundDuration;
    private float _elapsedTime;

    // UI elements — assigned by UISetup
    private Text _cashText;
    private Text _portfolioValueText;
    private Text _portfolioChangeText;
    private Text _roundProfitText;
    private Text _targetText;
    private Image _targetProgressBar;

    // FIX-12: Reputation display in HUD
    private Text _reputationText;

    private float _startingPortfolioValue;
    private bool _initialized;
    private bool _dirty;

    // Tier theme reference — background image for tinting
    private Image _topBarBackground;

    public void Initialize(RunContext runContext, int currentRound, float roundDuration,
        Text cashText, Text portfolioValueText, Text portfolioChangeText,
        Text roundProfitText, Text targetText, Image targetProgressBar)
    {
        _runContext = runContext;
        _currentRound = currentRound;
        _roundDuration = roundDuration;
        _elapsedTime = 0f;

        _cashText = cashText;
        _portfolioValueText = portfolioValueText;
        _portfolioChangeText = portfolioChangeText;
        _roundProfitText = roundProfitText;
        _targetText = targetText;
        _targetProgressBar = targetProgressBar;

        _startingPortfolioValue = 0f;
        _initialized = true;
        _dirty = true;

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);
    }

    /// <summary>
    /// Sets the top bar background image reference for tier theme tinting.
    /// Called by UISetup after creating the HUD.
    /// </summary>
    public void SetTopBarBackground(Image topBarBackground)
    {
        _topBarBackground = topBarBackground;
    }

    /// <summary>
    /// FIX-12: Sets the Reputation display text reference. Called by UISetup.
    /// </summary>
    public void SetReputationDisplay(Text reputationText)
    {
        _reputationText = reputationText;
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
    }

    /// <summary>
    /// Captures the current portfolio value as the round start baseline.
    /// Call this at round start, not at HUD initialization.
    /// </summary>
    public void SetRoundStartBaseline()
    {
        if (_runContext != null)
            _startingPortfolioValue = _runContext.Portfolio.GetTotalValue();
        _elapsedTime = 0f;
        _dirty = true;
    }

    private void OnPriceUpdated(PriceUpdatedEvent evt)
    {
        _dirty = true;
    }

    private void OnTradeExecuted(TradeExecutedEvent evt)
    {
        _dirty = true;
    }

    private void Update()
    {
        if (!_initialized) return;
        _elapsedTime += Time.deltaTime;
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

        var portfolio = _runContext.Portfolio;

        // Cash
        if (_cashText != null)
            _cashText.text = FormatCurrency(portfolio.Cash);

        // FIX-12: Reputation
        if (_reputationText != null)
            _reputationText.text = $"\u2605 {_runContext.Reputation.Current}";

        // Portfolio value + % change
        float totalValue = portfolio.GetTotalValue();
        if (_portfolioValueText != null)
            _portfolioValueText.text = FormatCurrency(totalValue);

        if (_portfolioChangeText != null)
        {
            float pctChange = _startingPortfolioValue > 0f
                ? (totalValue - _startingPortfolioValue) / _startingPortfolioValue
                : 0f;
            _portfolioChangeText.text = FormatPercentChange(pctChange);
            _portfolioChangeText.color = GetProfitColor(pctChange);
        }

        // Round profit
        float roundProfit = portfolio.GetRoundProfit();
        if (_roundProfitText != null)
        {
            _roundProfitText.text = FormatProfit(roundProfit);
            _roundProfitText.color = GetProfitColor(roundProfit);
        }

        // Margin target — use live round from RunContext, not cached _currentRound
        float target = MarginCallTargets.GetTarget(_runContext.CurrentRound);
        float targetProgress = CalculateTargetProgress(roundProfit, target);

        if (_targetText != null)
            _targetText.text = $"{FormatCurrency(roundProfit)} / {FormatCurrency(target)}";

        if (_targetProgressBar != null)
        {
            _targetProgressBar.fillAmount = targetProgress;
            float timeProgress = _roundDuration > 0f ? Mathf.Clamp01(_elapsedTime / _roundDuration) : 0f;
            _targetProgressBar.color = GetTargetBarColor(targetProgress, timeProgress);
        }
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        var theme = TierVisualData.GetThemeForAct(evt.NewAct);
        ApplyTierTheme(theme);
    }

    /// <summary>
    /// Applies tier visual theme colors to HUD elements.
    /// Updates top bar background tint with the tier's background color.
    /// </summary>
    public void ApplyTierTheme(TierVisualTheme theme)
    {
        if (_topBarBackground != null)
        {
            _topBarBackground.color = new Color(
                theme.BackgroundTint.r, theme.BackgroundTint.g,
                theme.BackgroundTint.b, 0.9f);
        }
    }

    // --- Static utility methods for testability ---

    public static string FormatCurrency(float value)
    {
        return value.ToString("$#,##0.00");
    }

    public static string FormatProfit(float value)
    {
        if (value >= 0f)
            return "+" + value.ToString("$#,##0.00");
        return "-" + (-value).ToString("$#,##0.00");
    }

    public static string FormatPercentChange(float decimalPercent)
    {
        float pct = decimalPercent * 100f;
        string sign = pct >= 0f ? "+" : "";
        return $"{sign}{pct:F1}%";
    }

    public static Color GetProfitColor(float value)
    {
        if (value > 0f) return ProfitGreen;
        if (value < 0f) return LossRed;
        return Color.white;
    }

    /// <summary>
    /// Calculates progress toward margin call target (0-1 clamped).
    /// </summary>
    public static float CalculateTargetProgress(float currentProfit, float target)
    {
        if (target <= 0f) return 1f;
        return Mathf.Clamp01(currentProfit / target);
    }

    /// <summary>
    /// Returns target progress bar color based on pace.
    /// Green: on pace, Yellow: falling behind, Red: significantly behind.
    /// </summary>
    public static Color GetTargetBarColor(float profitProgress, float timeProgress)
    {
        if (profitProgress <= 0f) return LossRed;
        if (profitProgress >= timeProgress) return ProfitGreen;
        // Less than 50% of target met with more than 50% time elapsed = yellow
        // Otherwise significantly behind = red
        float pace = timeProgress > 0f ? profitProgress / timeProgress : 1f;
        if (pace >= 0.5f) return WarningYellow;
        return LossRed;
    }
}
