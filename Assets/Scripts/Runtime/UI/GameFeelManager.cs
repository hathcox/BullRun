using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;
using DG.Tweening;
using PI = AssetKits.ParticleImage;

/// <summary>
/// Central game feel controller. Subscribes to all EventBus events and triggers
/// layered feedback: Feel camera shakes, screen flash overlays, ParticleImage bursts,
/// DOTween scale/position punches, floating P&L numbers, and accumulator fly-to-UI tokens.
///
/// Philosophy: When you think you have enough game feel, multiply by 5.
/// </summary>
public class GameFeelManager : MonoBehaviour
{
    // Flash overlays
    private Image _greenFlash;
    private Image _redFlash;
    private Image _amberFlash;
    private Image _whiteFlash;
    private Image _goldFlash;

    // ParticleImage emitters
    private PI.ParticleImage _tradeParticles;
    private PI.ParticleImage _celebrationParticles;
    private PI.ParticleImage _currencyParticles;
    private PI.ParticleImage _fullScreenParticles;
    private PI.ParticleImage _dustParticles; // tiny chart-movement fairy dust

    // Chart head tracking — positions trade particles at the chart line tip
    private ChartLineView _chartLineView;
    private RectTransform _canvasRect;

    // Floating number pool
    private Text[] _floatingTexts;
    private int _floatingIndex;

    // Accumulator token pool — $ and ★ tokens that fly from chart head to UI elements
    private GameObject[] _accumulatorPool;
    private int _accumulatorIndex;
    private int _pendingExplosionCount; // stored so Invoke can fire a delayed accumulator burst

    // Last known chart-head canvas position (updated every trade and price tick)
    private Vector2 _lastChartHeadCanvasPos;

    // Price change particle throttle + slope tracking
    private float _lastPriceParticleTime;
    private const float PriceParticleInterval = 0.12f; // ~8 bursts/sec for a continuous feel
    private float _smoothedSlope = 0f;                 // EMA of per-tick price movement, 0–1

    public void Initialize(
        Image greenFlash, Image redFlash, Image amberFlash, Image whiteFlash, Image goldFlash,
        PI.ParticleImage tradeParticles, PI.ParticleImage celebrationParticles,
        PI.ParticleImage currencyParticles, PI.ParticleImage fullScreenParticles,
        PI.ParticleImage dustParticles,
        Text[] floatingTexts, RectTransform canvasRect, ChartLineView chartLineView,
        GameObject[] accumulatorPool)
    {
        _greenFlash = greenFlash;
        _redFlash = redFlash;
        _amberFlash = amberFlash;
        _whiteFlash = whiteFlash;
        _goldFlash = goldFlash;
        _tradeParticles = tradeParticles;
        _celebrationParticles = celebrationParticles;
        _currencyParticles = currencyParticles;
        _fullScreenParticles = fullScreenParticles;
        _dustParticles = dustParticles;
        _floatingTexts = floatingTexts;
        _canvasRect = canvasRect;
        _chartLineView = chartLineView;
        _accumulatorPool = accumulatorPool;

        // Subscribe to all events
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<TradeFeedbackEvent>(OnTradeFeedback);
        EventBus.Subscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Subscribe<RoundCompletedEvent>(OnRoundCompleted);
        EventBus.Subscribe<MarginCallTriggeredEvent>(OnMarginCall);
        EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        EventBus.Subscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
        EventBus.Subscribe<ShopExpansionPurchasedEvent>(OnShopExpansionPurchased);
        EventBus.Subscribe<InsiderTipPurchasedEvent>(OnInsiderTipPurchased);
        EventBus.Subscribe<BondPurchasedEvent>(OnBondPurchased);
        EventBus.Subscribe<BondRepPaidEvent>(OnBondRepPaid);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdated);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameFeel] Manager initialized — 14 event subscriptions active");
        #endif
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<TradeFeedbackEvent>(OnTradeFeedback);
        EventBus.Unsubscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Unsubscribe<RoundCompletedEvent>(OnRoundCompleted);
        EventBus.Unsubscribe<MarginCallTriggeredEvent>(OnMarginCall);
        EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        EventBus.Unsubscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
        EventBus.Unsubscribe<ShopExpansionPurchasedEvent>(OnShopExpansionPurchased);
        EventBus.Unsubscribe<InsiderTipPurchasedEvent>(OnInsiderTipPurchased);
        EventBus.Unsubscribe<BondPurchasedEvent>(OnBondPurchased);
        EventBus.Unsubscribe<BondRepPaidEvent>(OnBondRepPaid);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdated);
    }

    // ════════════════════════════════════════════════════════════════════
    // TRADING EFFECTS
    // ════════════════════════════════════════════════════════════════════

    private void OnTradeExecuted(TradeExecutedEvent evt)
    {
        float baseline = GameConfig.StartingCapital;
        float tradeValue = evt.TotalCost;

        // Position trade particles at the chart head so the burst appears where the trade dot is.
        // Also caches _lastChartHeadCanvasPos for the accumulator effect.
        PositionTradeParticlesAtChartHead();

        if (evt.IsBuy && !evt.IsShort)
        {
            // Buy: small shake + subtle green flash + green particles
            Log($"BUY effect: shake(0.15,3), flash(green,0.05), particles(trade,24,green)");
            PlayScreenShake(0.15f, 3f, 25f);
            PlayScreenFlash(_greenFlash, 0.25f, 0.05f);
            PlayParticleBurst(_tradeParticles, 24, ColorPalette.Green);
            PunchDashboardElement("CashText", 0.2f);
        }
        else if (!evt.IsBuy && !evt.IsShort)
        {
            float intensity = CalculateIntensity(tradeValue, baseline);

            if (tradeValue > 0f)
            {
                Log($"SELL(profit) effect: intensity={intensity:F2}, shake({0.25f * intensity + 0.1f:F2},{6f * intensity + 2f:F1}), flash(green,{0.07f * intensity + 0.04f:F3})");
                PlayScreenShake(0.25f * intensity + 0.1f, 6f * intensity + 2f, 20f);
                PlayScreenFlash(_greenFlash, 0.3f, 0.07f * intensity + 0.04f);
                PlayParticleBurst(_tradeParticles, (int)(36 * intensity) + 12, ColorPalette.Green);
                PlayParticleBurst(_currencyParticles, (int)(24 * intensity) + 6, ColorPalette.Gold);
                PunchDashboardElement("CashText", 0.25f * intensity + 0.1f);
                PlayAccumulatorEffect(Mathf.Clamp((int)(12f * intensity), 2, 12), 1);
            }
            else
            {
                Log("SELL(loss) effect: shake(0.2,4), flash(red,0.05), particles(trade,12,red)");
                PlayScreenShake(0.2f, 4f, 20f);
                PlayScreenFlash(_redFlash, 0.25f, 0.05f);
                PlayParticleBurst(_tradeParticles, 18, ColorPalette.Red);
            }
        }
        else if (!evt.IsBuy && evt.IsShort)
        {
            // SHORT OPEN: IsBuy=false, IsShort=true (selling short — amber burst, no accumulator)
            Log("SHORT OPEN effect: shake(0.2,5), flash(amber,0.05), particles(trade,20,amber)");
            PlayScreenShake(0.2f, 5f, 22f);
            PlayScreenFlash(_amberFlash, 0.3f, 0.05f);
            PlayParticleBurst(_tradeParticles, 30, ColorPalette.Amber);
        }
        else if (evt.IsBuy && evt.IsShort)
        {
            // COVER: IsBuy=true, IsShort=true (buying back to close short — intensity-scaled green + accumulator)
            float intensity = CalculateIntensity(tradeValue, baseline);
            Log($"COVER effect: intensity={intensity:F2}");
            PlayScreenShake(0.3f * intensity + 0.15f, 10f * intensity + 3f, 18f);

            if (tradeValue > 0f)
            {
                PlayScreenFlash(_greenFlash, 0.35f, 0.10f * intensity);
                PlayParticleBurst(_tradeParticles, (int)(45 * intensity) + 15, ColorPalette.Green);
                PlayParticleBurst(_currencyParticles, (int)(30 * intensity) + 9, ColorPalette.Gold);
                PunchDashboardElement("CashText", 0.3f * intensity + 0.1f);
                PlayAccumulatorEffect(Mathf.Clamp((int)(12f * intensity), 2, 12), 1);
            }
            else
            {
                PlayScreenFlash(_redFlash, 0.35f, 0.10f * intensity + 0.04f);
                PlayParticleBurst(_tradeParticles, (int)(30 * intensity) + 12, ColorPalette.Red);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TRADE FEEDBACK (failures)
    // ════════════════════════════════════════════════════════════════════

    private void OnTradeFeedback(TradeFeedbackEvent evt)
    {
        if (!evt.IsSuccess)
        {
            Log($"TRADE FAIL effect: '{evt.Message}' (no particles — action was invalid)");
            PlayScreenShake(0.1f, 2f, 40f);
            PlayScreenFlash(_redFlash, 0.1f, 0.04f);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // MARKET EVENT EFFECTS
    // ════════════════════════════════════════════════════════════════════

    private void OnEventPopupCompleted(EventPopupCompletedEvent evt)
    {
        switch (evt.EventType)
        {
            case MarketEventType.MarketCrash:
                Log("MARKET CRASH effect: shake(3s,40), flash(red,5s,0.12), particles(fullscreen,80,red) + staggered");
                PlayScreenShake(3f, 40f, 15f);
                PlayScreenFlash(_redFlash, 5f, 0.12f);
                PlayParticleBurst(_fullScreenParticles, 110, ColorPalette.Red);
                Invoke(nameof(CrashParticleBurst2), 0.5f);
                Invoke(nameof(CrashParticleBurst3), 1.2f);
                break;

            case MarketEventType.BullRun:
                Log("BULL RUN effect: shake(0.4,8), flash(green,0.07), particles(celebration+fullscreen)");
                PlayScreenShake(0.4f, 8f, 12f);
                PlayScreenFlash(_greenFlash, 0.5f, 0.07f);
                PlayParticleBurst(_celebrationParticles, 70, ColorPalette.Green);
                PlayParticleBurst(_fullScreenParticles, 50, ColorPalette.Green);
                break;

            case MarketEventType.FlashCrash:
                Log("FLASH CRASH effect: shake(0.8,30), flash(white,0.20+red,0.10), particles(fullscreen+trade)");
                PlayScreenShake(0.8f, 30f, 30f);
                PlayScreenFlash(_whiteFlash, 0.15f, 0.20f);
                PlayScreenFlash(_redFlash, 0.6f, 0.10f);
                PlayParticleBurst(_fullScreenParticles, 90, ColorPalette.White);
                PlayParticleBurst(_tradeParticles, 50, ColorPalette.Red);
                break;
        }
    }

    private void CrashParticleBurst2()
    {
        PlayParticleBurst(_fullScreenParticles, 80, ColorPalette.Red);
    }

    private void CrashParticleBurst3()
    {
        PlayParticleBurst(_fullScreenParticles, 65, ColorPalette.RedDim);
    }

    // ════════════════════════════════════════════════════════════════════
    // ROUND & RUN EFFECTS
    // ════════════════════════════════════════════════════════════════════

    private void OnRoundCompleted(RoundCompletedEvent evt)
    {
        if (!evt.TargetMet) return;

        float intensity = CalculateIntensity(evt.RoundProfit, evt.ProfitTarget);
        Log($"ROUND COMPLETE effect: profit=${evt.RoundProfit:F0}, intensity={intensity:F2}, rep={evt.RepEarned}");

        PlayScreenShake(0.5f * intensity + 0.2f, 12f * intensity + 4f, 15f);
        PlayScreenFlash(_greenFlash, 0.4f, 0.08f * intensity + 0.04f);
        PlayParticleBurst(_celebrationParticles, (int)(60 * intensity) + 20, ColorPalette.Green);
        PlayParticleBurst(_currencyParticles, (int)(38 * intensity) + 12, ColorPalette.Gold);

        PlayFloatingNumber($"+${evt.RoundProfit:F0}", ColorPalette.Green, new Vector2(0f, 50f));
        if (evt.RepEarned > 0)
            PlayFloatingNumber($"+{evt.RepEarned} REP", ColorPalette.Amber, new Vector2(0f, 100f));

        PunchDashboardElement("CashText", 0.3f * intensity + 0.15f);
        PunchDashboardElement("RepText", 0.25f * intensity + 0.1f);

        // Accumulator: scale dollar tokens by how much profit was earned this round.
        // Baseline = 40% of starting capital — earning that in a single round is a strong win.
        // Above 60% intensity (~24% of capital earned) = explosion burst after a short delay.
        float profitIntensity = CalculateIntensity(evt.RoundProfit, GameConfig.StartingCapital * 0.4f);
        int dollarTokens = Mathf.Max(4, (int)(30f * profitIntensity));
        int repTokens    = evt.RepEarned > 0 ? Mathf.Min(evt.RepEarned, 8) : 0;
        PlayAccumulatorEffect(dollarTokens, repTokens);

        if (profitIntensity > 0.6f)
        {
            _pendingExplosionCount = (int)(50f * profitIntensity); // 30–50 extra tokens
            Invoke(nameof(FireAccumulatorExplosion), 0.5f);
        }
    }

    private void OnMarginCall(MarginCallTriggeredEvent evt)
    {
        Log($"MARGIN CALL effect: round={evt.RoundNumber}, shortfall=${evt.Shortfall:F0}");
        PlayScreenShake(2f, 50f, 12f);
        PlayScreenFlash(_redFlash, 3f, 0.15f);
        PlayParticleBurst(_fullScreenParticles, 110, ColorPalette.Red);
        PlayParticleBurst(_tradeParticles, 60, ColorPalette.Red);

        PlayFloatingNumber("MARGIN CALL", ColorPalette.Red, new Vector2(0f, 0f));
        Invoke(nameof(MarginCallBurst2), 0.7f);
    }

    private void MarginCallBurst2()
    {
        PlayParticleBurst(_fullScreenParticles, 75, ColorPalette.RedDim);
    }

    private void OnRunEnded(RunEndedEvent evt)
    {
        if (evt.IsVictory)
        {
            Log($"VICTORY effect: rounds={evt.RoundsCompleted}, cash=${evt.FinalCash:F0}");
            PlayScreenShake(1f, 25f, 15f);
            PlayScreenFlash(_whiteFlash, 0.3f, 0.17f);
            PlayScreenFlash(_greenFlash, 1f, 0.08f);
            PlayParticleBurst(_celebrationParticles, 100, ColorPalette.Gold);
            PlayParticleBurst(_fullScreenParticles, 90, ColorPalette.Green);
            PlayParticleBurst(_currencyParticles, 70, ColorPalette.Gold);

            PlayFloatingNumber("VICTORY!", ColorPalette.Gold, new Vector2(0f, 50f));

            Invoke(nameof(VictoryBurst2), 0.4f);
            Invoke(nameof(VictoryBurst3), 0.9f);
            Invoke(nameof(VictoryBurst4), 1.5f);
        }
        else
        {
            Log($"DEFEAT effect: rounds={evt.RoundsCompleted}, marginCalled={evt.WasMarginCalled}");
            PlayScreenShake(1.5f, 15f, 10f);
            PlayScreenFlash(_redFlash, 2f, 0.12f);
            PlayParticleBurst(_fullScreenParticles, 50, ColorPalette.Red);

            PlayFloatingNumber("RUN OVER", ColorPalette.Red, new Vector2(0f, 0f));
        }
    }

    private void VictoryBurst2()
    {
        PlayScreenShake(0.5f, 12f, 18f);
        PlayParticleBurst(_celebrationParticles, 75, ColorPalette.Green);
        PlayParticleBurst(_currencyParticles, 50, ColorPalette.Gold);
    }

    private void VictoryBurst3()
    {
        PlayScreenShake(0.4f, 10f, 20f);
        PlayParticleBurst(_celebrationParticles, 90, ColorPalette.Gold);
    }

    private void VictoryBurst4()
    {
        PlayParticleBurst(_fullScreenParticles, 70, ColorPalette.Green);
        PlayParticleBurst(_celebrationParticles, 55, ColorPalette.Gold);
    }

    // ════════════════════════════════════════════════════════════════════
    // SHOP EFFECTS
    // ════════════════════════════════════════════════════════════════════

    private void OnShopItemPurchased(ShopItemPurchasedEvent evt)
    {
        float intensity = CalculateIntensity(evt.Cost, 50f);
        Log($"SHOP ITEM effect: '{evt.ItemName}' cost={evt.Cost}, intensity={intensity:F2}");

        PlayScreenShake(0.15f * intensity + 0.05f, 4f * intensity + 1f, 20f);
        PlayScreenFlash(_greenFlash, 0.2f, 0.04f * intensity + 0.02f);
        PlayParticleBurst(_currencyParticles, (int)(27 * intensity) + 8, ColorPalette.Gold);
        PunchDashboardElement("RepText", 0.2f * intensity + 0.1f);
    }

    private void OnShopExpansionPurchased(ShopExpansionPurchasedEvent evt)
    {
        Log($"EXPANSION effect: '{evt.DisplayName}' cost={evt.Cost}");
        PlayScreenShake(0.35f, 8f, 18f);
        PlayScreenFlash(_greenFlash, 0.3f, 0.07f);
        PlayParticleBurst(_tradeParticles, 50, ColorPalette.Cyan);
        PlayParticleBurst(_celebrationParticles, 35, ColorPalette.Cyan);
        PunchDashboardElement("RepText", 0.25f);
    }

    private void OnInsiderTipPurchased(InsiderTipPurchasedEvent evt)
    {
        Log($"INSIDER TIP effect: cost={evt.Cost}");
        PlayScreenShake(0.1f, 2f, 25f);
        PlayScreenFlash(_whiteFlash, 0.15f, 0.04f);
        PlayParticleBurst(_tradeParticles, 22, ColorPalette.White);
    }

    private void OnBondPurchased(BondPurchasedEvent evt)
    {
        Log($"BOND effect: price=${evt.PricePaid:F0}, total={evt.TotalBondsOwned}");
        PlayScreenShake(0.12f, 3f, 20f);
        PlayScreenFlash(_amberFlash, 0.2f, 0.04f);
        PlayParticleBurst(_currencyParticles, 27, ColorPalette.Gold);
    }

    private void OnBondRepPaid(BondRepPaidEvent evt)
    {
        float intensity = CalculateIntensity(evt.RepEarned, 10f);
        Log($"BOND REP effect: earned={evt.RepEarned}, intensity={intensity:F2}");

        PlayParticleBurst(_currencyParticles, (int)(21 * intensity) + 8, ColorPalette.Gold);
        PunchDashboardElement("RepText", 0.2f * intensity + 0.1f);

        if (evt.RepEarned > 0)
            PlayFloatingNumber($"+{evt.RepEarned} REP", ColorPalette.Amber, new Vector2(300f, -350f));
    }

    // ════════════════════════════════════════════════════════════════════
    // ROUND START & ACT TRANSITION
    // ════════════════════════════════════════════════════════════════════

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        Log($"ROUND START effect: round={evt.RoundNumber}, act={evt.Act}");
        PlayScreenFlash(_greenFlash, 0.3f, 0.03f);
        PunchDashboardElement("CashText", 0.1f);
        PunchDashboardElement("RepText", 0.1f);
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        Log($"ACT TRANSITION effect: act {evt.PreviousAct}→{evt.NewAct}");
        PlayScreenShake(0.6f, 15f, 12f);
        PlayScreenFlash(_whiteFlash, 0.2f, 0.10f);
        PlayScreenFlash(_amberFlash, 0.5f, 0.05f);
        PlayParticleBurst(_celebrationParticles, 65, ColorPalette.Amber);
        PlayParticleBurst(_fullScreenParticles, 50, ColorPalette.Gold);
    }

    // ════════════════════════════════════════════════════════════════════
    // CONTINUOUS PRICE CHANGE PARTICLES
    // ════════════════════════════════════════════════════════════════════

    private void OnPriceUpdated(PriceUpdatedEvent evt)
    {
        if (evt.PreviousPrice <= 0f) return;

        float delta     = evt.NewPrice - evt.PreviousPrice;
        float pctChange = Mathf.Abs(delta) / evt.PreviousPrice;

        // Exponential moving average keeps the slope reading smooth so particles
        // ramp up as the chart steepens and fade out as it flattens.
        // 1% per-tick change → rawSlope ≈ 1.0 (very aggressive); 0.05% → ≈ 0.05 (gentle drift).
        float rawSlope  = Mathf.Clamp01(pctChange * 100f);
        _smoothedSlope  = Mathf.Lerp(_smoothedSlope, rawSlope, 0.25f);

        // Skip bursts on very flat sections; still allow decaying emission to taper naturally
        if (_smoothedSlope < 0.02f) return;
        if (Time.time - _lastPriceParticleTime < PriceParticleInterval) return;

        _lastPriceParticleTime = Time.time;

        // Always chart-line green — no red, this is fairy dust not a trade event.
        // Range: 3 (gentle drift) → 9 (aggressive slope) — 50% of trade burst counts.
        int count = Mathf.Max(3, (int)(9f * _smoothedSlope));

        // No log — price ticks are too frequent
        PositionTradeParticlesAtChartHead();
        PlayParticleBurst(_dustParticles, count, ColorPalette.Green);
    }

    // ════════════════════════════════════════════════════════════════════
    // CORE EFFECT METHODS
    // ════════════════════════════════════════════════════════════════════

    private void PlayScreenShake(float duration, float amplitude, float frequency)
    {
        MMCameraShakeEvent.Trigger(duration, amplitude, frequency, 0f, 0f, 0f);
    }

    private void PlayScreenFlash(Image overlay, float duration, float peakAlpha)
    {
        if (overlay == null) return;

        overlay.DOKill();

        float fadeInTime = Mathf.Min(duration * 0.2f, 0.05f);
        float fadeOutTime = duration - fadeInTime;

        var c = overlay.color;
        c.a = 0f;
        overlay.color = c;

        // SetUpdate(true) = unscaled time so flashes play through Time.timeScale=0 announcement pauses
        overlay.DOFade(peakAlpha, fadeInTime).SetUpdate(true).OnComplete(() =>
        {
            overlay.DOFade(0f, fadeOutTime).SetUpdate(true);
        });
    }

    /// <summary>
    /// Moves the trade particle emitter RectTransform to the chart head's screen position
    /// so bursts originate exactly where the price line tip is.
    /// Also caches _lastChartHeadCanvasPos for use by the accumulator effect.
    /// </summary>
    private void PositionTradeParticlesAtChartHead()
    {
        if (_chartLineView == null || !_chartLineView.HasActiveChartHead
            || _canvasRect == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = _chartLineView.ChartHeadWorldPosition;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, null, out Vector2 localPos))
        {
            _lastChartHeadCanvasPos = localPos;

            if (_tradeParticles != null)
                _tradeParticles.GetComponent<RectTransform>().anchoredPosition = localPos;

            // Dust emitter must also track the chart head — it has no other way to get there
            if (_dustParticles != null)
                _dustParticles.GetComponent<RectTransform>().anchoredPosition = localPos;

            Log($"Particles → canvas({localPos.x:F0},{localPos.y:F0}) from world({worldPos.x:F1},{worldPos.y:F1})");
        }
    }

    /// <summary>
    /// Burst particles from an emitter with a specific color and count.
    /// Uses SetBurst(0) to update the pre-allocated burst slot — never grows the burst list.
    /// </summary>
    private void PlayParticleBurst(PI.ParticleImage emitter, int count, Color color)
    {
        if (emitter == null || count <= 0) return;

        emitter.startColor = new ParticleSystem.MinMaxGradient(color);

        // Update color over lifetime gradient to match
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        emitter.colorOverLifetime = new ParticleSystem.MinMaxGradient(gradient);

        emitter.rateOverTime = 0f;
        emitter.loop = false;

        // Update existing burst slot 0 instead of AddBurst (prevents list growth)
        emitter.SetBurst(0, 0f, count);

        emitter.Stop(true);
        emitter.Play();
    }

    private void PunchDashboardElement(string elementName, float punchScale)
    {
        var dashRefs = UISetup.DashRefs;
        if (dashRefs == null) return;

        RectTransform target = null;
        switch (elementName)
        {
            case "CashText":
                if (dashRefs.CashText != null)
                    target = dashRefs.CashText.GetComponent<RectTransform>();
                break;
            case "RepText":
                if (dashRefs.RepText != null)
                    target = dashRefs.RepText.GetComponent<RectTransform>();
                break;
            case "ProfitText":
                if (dashRefs.ProfitText != null)
                    target = dashRefs.ProfitText.GetComponent<RectTransform>();
                break;
        }

        if (target != null)
            PlayScalePunch(target, punchScale, 0.3f);
    }

    private void PlayScalePunch(RectTransform target, float punchScale, float duration)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * punchScale, duration, 8, 0.5f).SetUpdate(true);
    }

    private void PlayFloatingNumber(string text, Color color, Vector2 screenPos)
    {
        if (_floatingTexts == null || _floatingTexts.Length == 0) return;

        var txt = _floatingTexts[_floatingIndex];
        _floatingIndex = (_floatingIndex + 1) % _floatingTexts.Length;

        Log($"Float: '{text}' at ({screenPos.x},{screenPos.y})");

        txt.gameObject.SetActive(true);
        txt.text = text;
        txt.color = color;
        txt.fontSize = 36;

        var rect = txt.GetComponent<RectTransform>();
        rect.anchoredPosition = screenPos;

        var cg = txt.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.DOKill();
            cg.alpha = 1f;
        }

        rect.DOKill();
        rect.localScale = Vector3.one;

        rect.DOPunchScale(Vector3.one * 0.3f, 0.2f, 6, 0.5f).SetUpdate(true);
        rect.DOAnchorPosY(screenPos.y + 120f, 1.5f).SetEase(Ease.OutQuad).SetUpdate(true);

        if (cg != null)
        {
            cg.DOFade(0f, 1.5f).SetDelay(0.3f).SetUpdate(true).OnComplete(() =>
            {
                txt.gameObject.SetActive(false);
            });
        }
        else
        {
            Invoke(nameof(DeactivateLastFloat), 1.8f);
        }
    }

    private void DeactivateLastFloat()
    {
        for (int i = 0; i < _floatingTexts.Length; i++)
        {
            if (_floatingTexts[i].gameObject.activeSelf)
            {
                _floatingTexts[i].gameObject.SetActive(false);
                break;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // ACCUMULATOR EFFECT — $ and ★ tokens fly from chart head to UI
    // ════════════════════════════════════════════════════════════════════

    /// Delayed second-wave accumulator burst fired after a big round profit.
    /// Count is stored in _pendingExplosionCount before Invoke is called.
    private void FireAccumulatorExplosion()
    {
        PlayAccumulatorEffect(_pendingExplosionCount, 0);
    }

    /// <summary>
    /// Spawns dollar-sign and/or star tokens at the last chart head canvas position
    /// and animates them flying to the CashText and RepText UI elements respectively.
    /// On the final token's arrival the destination UI element gets a scale punch.
    /// </summary>
    private void PlayAccumulatorEffect(int dollarCount, int repCount)
    {
        if (_accumulatorPool == null) return;

        var dashRefs = UISetup.DashRefs;
        if (dashRefs == null) return;

        if (dollarCount > 0 && dashRefs.CashText != null)
        {
            Vector2 dest = GetCanvasLocalPos(dashRefs.CashText.GetComponent<RectTransform>());
            SpawnAccumulatorTokens("$", ColorPalette.Gold, dollarCount, _lastChartHeadCanvasPos, dest, "CashText");
            Log($"Accumulator: {dollarCount} $ tokens → CashText at canvas({dest.x:F0},{dest.y:F0})");
        }

        if (repCount > 0 && dashRefs.RepText != null)
        {
            Vector2 dest = GetCanvasLocalPos(dashRefs.RepText.GetComponent<RectTransform>());
            SpawnAccumulatorTokens("★", ColorPalette.Amber, repCount, _lastChartHeadCanvasPos, dest, "RepText");
            Log($"Accumulator: {repCount} ★ tokens → RepText at canvas({dest.x:F0},{dest.y:F0})");
        }
    }

    private void SpawnAccumulatorTokens(string symbol, Color color, int count, Vector2 from, Vector2 to, string punchTarget)
    {
        for (int i = 0; i < count; i++)
        {
            int idx = (_accumulatorIndex + i) % _accumulatorPool.Length;
            var go = _accumulatorPool[idx];
            if (go == null) continue;

            var txt  = go.GetComponent<Text>();
            var rect = go.GetComponent<RectTransform>();
            var cg   = go.GetComponent<CanvasGroup>();
            if (txt == null || rect == null || cg == null) continue;

            rect.DOKill();
            cg.DOKill();

            txt.text  = symbol;
            txt.color = color;
            // Scatter tokens slightly at the source for a natural burst
            rect.anchoredPosition = from + Random.insideUnitCircle * 35f;
            cg.alpha = 1f;
            go.SetActive(true);

            float delay  = i * 0.055f;
            bool  isLast = (i == count - 1);

            // Capture loop variables for the closure
            var capturedGo     = go;
            var capturedCg     = cg;
            var capturedRect   = rect;
            var capturedTarget = punchTarget;

            capturedRect
                .DOAnchorPos(to + Random.insideUnitCircle * 8f, 0.45f + Random.Range(0f, 0.08f))
                .SetDelay(delay)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    capturedCg.DOFade(0f, 0.1f).SetUpdate(true).OnComplete(() => capturedGo.SetActive(false));
                    if (isLast)
                        PunchDashboardElement(capturedTarget, 0.35f);
                });
        }

        _accumulatorIndex = (_accumulatorIndex + count) % _accumulatorPool.Length;
    }

    /// <summary>
    /// Converts a UI RectTransform's world position to a local canvas position
    /// suitable for use as anchoredPosition on the GameFeel canvas (ScreenSpaceOverlay).
    /// </summary>
    private Vector2 GetCanvasLocalPos(RectTransform target)
    {
        if (target == null || _canvasRect == null) return Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            RectTransformUtility.WorldToScreenPoint(null, target.position),
            null, out Vector2 localPos);
        return localPos;
    }

    private static float CalculateIntensity(float value, float baseline)
    {
        if (baseline <= 0f) return 0.5f;
        return Mathf.Clamp(Mathf.Abs(value) / baseline, 0.2f, 1.0f);
    }

    // ════════════════════════════════════════════════════════════════════
    // DEBUG LOGGING
    // ════════════════════════════════════════════════════════════════════

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void Log(string message)
    {
        Debug.Log($"[GameFeel] {message}");
    }
}
