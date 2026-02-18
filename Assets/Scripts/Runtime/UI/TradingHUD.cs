using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Real-time trading HUD displaying cash, portfolio value, round profit, and margin target.
/// Reads from RunContext.Portfolio (one-way dependency). Refreshes on PriceUpdatedEvent and TradeExecutedEvent.
/// </summary>
public class TradingHUD : MonoBehaviour
{
    // Story 14.6: Color constants migrated to CRTThemeData
    public static Color ProfitGreen => CRTThemeData.TextHigh;
    public static Color LossRed => CRTThemeData.Danger;
    public static Color WarningYellow => CRTThemeData.Warning;

    // AC 2: Cash count-up tween constants
    public static readonly float CashTweenDuration = 0.3f;

    // AC 12: Progress bar tween constants
    public static readonly float BarTweenDuration = 0.2f;

    // AC 13: Streak minimum display count
    public static readonly int StreakMinDisplay = 2;

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

    // Story 13.5: Insider tips display during trading
    private Text _tipsDisplayText;

    // AC 2: Cash count-up animation state
    private float _displayedCash;
    private Tweener _cashTween;

    // AC 12: Progress bar smooth tween state
    private float _targetFillAmount;
    private Tweener _barTween;

    // AC 9 & 13: Floating text service + rep/streak references
    private FloatingTextService _floatingTextService;
    private RectTransform _repTextRect;
    private ChartLineView _chartLineView;

    // AC 13: Streak state
    private int _streakCount;
    private Text _streakText;

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

        _displayedCash = 0f;
        _targetFillAmount = 0f;
        _streakCount = 0;

        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Subscribe<RoundCompletedEvent>(OnRoundCompleted);
    }

    /// <summary>
    /// Story 14.2: Initialize from DashboardReferences instead of individual fields.
    /// Extracts text references from DashboardReferences into existing private fields.
    /// Backward-compatible: null refs are handled gracefully by RefreshDisplay null checks.
    /// </summary>
    public void Initialize(DashboardReferences dashRefs, RunContext runContext, int currentRound, float roundDuration)
    {
        Initialize(
            runContext, currentRound, roundDuration,
            dashRefs.CashText,
            null, // portfolioValueText — populated by future story
            null, // portfolioChangeText — populated by future story
            dashRefs.ProfitText,
            dashRefs.TargetText,
            dashRefs.TargetProgressBar
        );

        if (dashRefs.RepText != null)
            SetReputationDisplay(dashRefs.RepText);
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

    /// <summary>
    /// Story 13.5: Sets the insider tips display text reference. Called by UISetup.
    /// </summary>
    public void SetTipsDisplay(Text tipsDisplayText)
    {
        _tipsDisplayText = tipsDisplayText;
    }

    /// <summary>
    /// AC 9 & 13: Sets the FloatingTextService reference for rep popups. Called by UISetup.
    /// </summary>
    public void SetFloatingTextService(FloatingTextService service)
    {
        _floatingTextService = service;
    }

    /// <summary>
    /// AC 9: Sets the reputation Text's RectTransform so popups spawn near it. Called by UISetup.
    /// </summary>
    public void SetRepTextRect(RectTransform repTextRect)
    {
        _repTextRect = repTextRect;
    }

    /// <summary>
    /// Sets the ChartLineView so profit/loss popups spawn at the current price on the chart.
    /// Called by UISetup after chart setup completes.
    /// </summary>
    public void SetChartLineView(ChartLineView chartLineView)
    {
        _chartLineView = chartLineView;
    }

    /// <summary>
    /// AC 13: Sets the streak display text. Initially hidden. Called by UISetup.
    /// </summary>
    public void SetStreakDisplay(Text streakText)
    {
        _streakText = streakText;
        if (_streakText != null)
            _streakText.gameObject.SetActive(false);
    }

    private void OnRoundCompleted(RoundCompletedEvent evt)
    {
        // AC 13: Streak tracking
        if (evt.TargetMet)
            _streakCount++;
        else
            _streakCount = 0;

        if (_streakText != null)
        {
            if (_streakCount >= StreakMinDisplay)
            {
                _streakText.gameObject.SetActive(true);
                _streakText.text = $"STREAK \u00D7{_streakCount}";
                _streakText.color = ColorPalette.Gold;
            }
            else
            {
                _streakText.gameObject.SetActive(false);
            }
        }

        // AC 9: Floating "+X REP ⭐" popup
        if (evt.RepEarned > 0 && _floatingTextService != null && _repTextRect != null)
        {
            _floatingTextService.Spawn(
                $"+{evt.RepEarned} REP \u2605",
                _repTextRect,
                ColorPalette.Amber);
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Unsubscribe<RoundCompletedEvent>(OnRoundCompleted);
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

        // AC 2: Cash count-up animation
        if (_cashText != null && _runContext != null)
        {
            float targetCash = _runContext.Portfolio.Cash;
            _cashTween?.Kill();
            _cashTween = DOTween.To(
                () => _displayedCash,
                x => _displayedCash = x,
                targetCash,
                CashTweenDuration)
                .SetUpdate(false)
                .OnUpdate(() =>
                {
                    _cashText.text = FormatCurrency(_displayedCash);
                    _cashText.color = _displayedCash < 0f ? LossRed : CRTThemeData.TextHigh;
                });
        }

        // AC 3: Floating profit/loss popup on sell (close long) or cover (close short)
        bool isSell = !evt.IsBuy && !evt.IsShort;
        bool isCover = evt.IsBuy && evt.IsShort;
        if ((isSell || isCover) && _floatingTextService != null)
        {
            float profit = evt.TotalCost;
            string popupText = FormatProfit(profit);
            Color popupColor = profit >= 0f ? ProfitGreen : LossRed;
            if (_chartLineView != null && _chartLineView.HasActiveChartHead)
                _floatingTextService.SpawnAtWorldPos(popupText, _chartLineView.ChartHeadWorldPosition, popupColor);
            else if (_cashText != null)
                _floatingTextService.Spawn(popupText, _cashText.rectTransform, popupColor);
        }
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

        // Cash — only set directly if no active tween (AC 2 drives it via tween on trade)
        if (_cashText != null && (_cashTween == null || !_cashTween.IsActive()))
        {
            _displayedCash = portfolio.Cash;
            _cashText.text = FormatCurrency(portfolio.Cash);
            _cashText.color = portfolio.Cash < 0f ? LossRed : CRTThemeData.TextHigh;
        }

        // FIX-12: Reputation
        if (_reputationText != null)
            _reputationText.text = $"\u2605 {_runContext.Reputation.Current}";

        // Story 13.5: Insider tips
        if (_tipsDisplayText != null)
        {
            if (_runContext.RevealedTips != null && _runContext.RevealedTips.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _runContext.RevealedTips.Count; i++)
                {
                    if (i > 0) sb.Append(" | ");
                    sb.Append(_runContext.RevealedTips[i].RevealedText);
                }
                _tipsDisplayText.text = sb.ToString();
                _tipsDisplayText.gameObject.SetActive(true);
            }
            else
            {
                _tipsDisplayText.gameObject.SetActive(false);
            }
        }

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

        // FIX-14: Margin target — targets are now cumulative value targets (total portfolio value).
        // Show total value vs target, not profit vs target.
        float target = MarginCallTargets.GetTarget(_runContext.CurrentRound);
        float targetProgress = CalculateTargetProgress(totalValue, target);

        if (_targetText != null)
            _targetText.text = $"{FormatCurrency(totalValue)} / {FormatCurrency(target)}";

        if (_targetProgressBar != null)
        {
            // AC 12: Smooth tween for progress bar, skip micro-jitter changes
            float timeProgress = _roundDuration > 0f ? Mathf.Clamp01(_elapsedTime / _roundDuration) : 0f;
            _targetProgressBar.color = GetTargetBarColor(targetProgress, timeProgress);

            if (Mathf.Abs(targetProgress - _targetFillAmount) >= 0.005f)
            {
                _targetFillAmount = targetProgress;
                _barTween?.Kill();
                _barTween = _targetProgressBar.DOFillAmount(_targetFillAmount, BarTweenDuration).SetUpdate(false);
            }
        }
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        var theme = TierVisualData.GetThemeForAct(evt.NewAct);
        ApplyTierTheme(theme);
    }

    /// <summary>
    /// Story 14.6: Tier themes no longer tint the Control Deck background.
    /// CRT base colors (Panel, TextHigh, TextLow) remain constant across tiers.
    /// Chart line color is handled separately by ChartLineView.ApplyTierTheme().
    /// </summary>
    public void ApplyTierTheme(TierVisualTheme theme)
    {
        // No-op: CRT dashboard uses fixed CRTThemeData.Panel color regardless of tier.
        // Chart line color (tier accent) is handled by ChartLineView.ApplyTierTheme.
    }

    // --- Static utility methods for testability ---

    public static string FormatCurrency(float value)
    {
        if (value < 0f)
            return "-" + (-value).ToString("$#,##0.00");
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
    public static float CalculateTargetProgress(float currentValue, float target)
    {
        if (target <= 0f) return 1f;
        return Mathf.Clamp01(currentValue / target);
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
