using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// Central audio controller. Subscribes to all EventBus events and plays
/// corresponding SFX via MMSoundManager. Mirrors GameFeelManager pattern.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioClipLibrary _clips;

    // Active loop handles for stopping
    private AudioSource _crashRumbleSource;
    private AudioSource _bullrunShimmerSource;

    // Timer tracking (Task 7)
    private bool _timerWarningPlayed;
    private int _lastTimerCriticalTickSecond = -1;
    private int _lastShortTickSecond = -1;

    // Short cashout window tracking (Task 4 — play once on transition)
    private bool _shortCashoutWindowSoundPlayed;

    // SFX cooldown to prevent sound stacking (Task 4)
    private float _lastTradeSfxTime;

    // Trade cooldown end tracking
    private bool _cooldownEndPending;
    private float _cooldownEndTimer;

    public void Initialize(AudioClipLibrary clips)
    {
        Instance = this;
        _clips = clips;

        // ── Trading Events (Task 4) ──
        EventBus.Subscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Subscribe<TradeFeedbackEvent>(OnTradeFeedback);
        EventBus.Subscribe<ShortCountdownEvent>(OnShortCountdown);

        // ── Game State Events (Task 5) ──
        EventBus.Subscribe<RunStartedEvent>(OnRunStarted);
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<MarketClosedEvent>(OnMarketClosed);
        EventBus.Subscribe<MarginCallTriggeredEvent>(OnMarginCallTriggered);
        EventBus.Subscribe<RoundCompletedEvent>(OnRoundCompleted);
        EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);

        // ── Market Events (Task 6) ──
        EventBus.Subscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Subscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);

        // ── Timer Events (Task 7) — uses PriceUpdatedEvent to track timer ──
        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdatedForTimer);

        // ── Shop Events (Task 8) ──
        EventBus.Subscribe<ShopOpenedEvent>(OnShopOpened);
        EventBus.Subscribe<ShopClosedEvent>(OnShopClosed);
        EventBus.Subscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
        EventBus.Subscribe<ShopExpansionPurchasedEvent>(OnShopExpansionPurchased);
        EventBus.Subscribe<InsiderTipPurchasedEvent>(OnInsiderTipPurchased);
        EventBus.Subscribe<BondPurchasedEvent>(OnBondPurchased);
        EventBus.Subscribe<BondRepPaidEvent>(OnBondRepPaid);
        EventBus.Subscribe<ShopRerollEvent>(OnShopReroll);

        // ── UI Events (Task 10) ──
        EventBus.Subscribe<StockSelectedEvent>(OnStockSelected);
        EventBus.Subscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Audio] AudioManager initialized — 25 event subscriptions active");
        #endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        EventBus.Unsubscribe<TradeExecutedEvent>(OnTradeExecuted);
        EventBus.Unsubscribe<TradeFeedbackEvent>(OnTradeFeedback);
        EventBus.Unsubscribe<ShortCountdownEvent>(OnShortCountdown);
        EventBus.Unsubscribe<RunStartedEvent>(OnRunStarted);
        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<MarketClosedEvent>(OnMarketClosed);
        EventBus.Unsubscribe<MarginCallTriggeredEvent>(OnMarginCallTriggered);
        EventBus.Unsubscribe<RoundCompletedEvent>(OnRoundCompleted);
        EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Unsubscribe<MarketEventFiredEvent>(OnMarketEventFired);
        EventBus.Unsubscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdatedForTimer);
        EventBus.Unsubscribe<ShopOpenedEvent>(OnShopOpened);
        EventBus.Unsubscribe<ShopClosedEvent>(OnShopClosed);
        EventBus.Unsubscribe<ShopItemPurchasedEvent>(OnShopItemPurchased);
        EventBus.Unsubscribe<ShopExpansionPurchasedEvent>(OnShopExpansionPurchased);
        EventBus.Unsubscribe<InsiderTipPurchasedEvent>(OnInsiderTipPurchased);
        EventBus.Unsubscribe<BondPurchasedEvent>(OnBondPurchased);
        EventBus.Unsubscribe<BondRepPaidEvent>(OnBondRepPaid);
        EventBus.Unsubscribe<ShopRerollEvent>(OnShopReroll);
        EventBus.Unsubscribe<StockSelectedEvent>(OnStockSelected);
        EventBus.Unsubscribe<TradeButtonPressedEvent>(OnTradeButtonPressed);

        StopAllLoops();
    }

    private void Update()
    {
        // Track cooldown end for TradeCooldownEnd sound
        if (_cooldownEndPending)
        {
            _cooldownEndTimer -= Time.deltaTime;
            if (_cooldownEndTimer <= 0f)
            {
                _cooldownEndPending = false;
                PlayUI(_clips.TradeCooldownEnd);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 4: TRADING SFX
    // ════════════════════════════════════════════════════════════════════

    private void OnTradeExecuted(TradeExecutedEvent evt)
    {
        // Prevent sound stacking on rapid trades
        if (Time.unscaledTime - _lastTradeSfxTime < GameConfig.TradeSfxCooldown) return;
        _lastTradeSfxTime = Time.unscaledTime;

        if (evt.IsBuy && !evt.IsShort)
        {
            PlaySfx(_clips.BuySuccess);
        }
        else if (!evt.IsBuy && !evt.IsShort)
        {
            if (evt.ProfitLoss > 0f)
            {
                // Sell profit — volume/pitch scaled by intensity (AC: 10)
                float intensity = Mathf.Clamp(evt.ProfitLoss / GameConfig.StartingCapital, 0.2f, 1.0f);
                float volume = Mathf.Lerp(0.7f, 1.0f, intensity);
                float pitch = Mathf.Lerp(0.9f, 1.1f, intensity);
                PlaySfx(_clips.SellProfit, volume, pitch);
            }
            else
            {
                PlaySfx(_clips.SellLoss);
            }
        }
        else if (!evt.IsBuy && evt.IsShort)
        {
            // Short open
            PlaySfx(_clips.ShortOpen);
        }
        else if (evt.IsBuy && evt.IsShort)
        {
            // Short cover (cashout)
            if (evt.ProfitLoss > 0f)
                PlaySfx(_clips.ShortCashoutProfit);
            else
                PlaySfx(_clips.ShortCashoutLoss);
        }
    }

    private void OnTradeFeedback(TradeFeedbackEvent evt)
    {
        if (!evt.IsSuccess)
        {
            PlayUI(_clips.TradeRejected);
            return;
        }

        PlayUI(_clips.UiConfirm);

        // Detect auto-close from feedback message
        if (evt.IsShort && evt.Message != null && evt.Message.Contains("AUTO-CLOSED"))
        {
            PlaySfx(_clips.ShortAutoClose);
        }

        // Trade cooldown start sound on successful non-short trade
        if (!evt.IsShort)
        {
            PlayUI(_clips.TradeCooldownStart);
            // Schedule cooldown end sound
            _cooldownEndTimer = GameConfig.PostTradeCooldown;
            _cooldownEndPending = true;
        }
    }

    private void OnShortCountdown(ShortCountdownEvent evt)
    {
        // Transition to CashOutWindow — play once
        if (evt.IsCashOutWindow && !_shortCashoutWindowSoundPlayed)
        {
            PlaySfx(_clips.ShortCashoutWindowOpen);
            _shortCashoutWindowSoundPlayed = true;
        }

        // Reset tracking when no longer in cashout
        if (!evt.IsCashOutWindow)
        {
            _shortCashoutWindowSoundPlayed = false;

            // Countdown tick during Holding phase (whole-second ticks)
            int wholeSecond = Mathf.FloorToInt(evt.TimeRemaining);
            if (wholeSecond >= 0 && wholeSecond != _lastShortTickSecond)
            {
                _lastShortTickSecond = wholeSecond;
                PlaySfx(_clips.ShortCountdownTick, 0.6f);
            }
        }

        // Urgency during cashout window when time is low
        if (evt.IsCashOutWindow && evt.TimeRemaining <= 2f)
        {
            int wholeSecond = Mathf.FloorToInt(evt.TimeRemaining);
            if (wholeSecond >= 0 && wholeSecond != _lastShortTickSecond)
            {
                _lastShortTickSecond = wholeSecond;
                PlaySfx(_clips.ShortCashoutUrgency);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 5: GAME STATE SFX
    // ════════════════════════════════════════════════════════════════════

    private void OnRunStarted(RunStartedEvent evt)
    {
        PlaySfx(_clips.RunStart);
    }

    private void OnMarketOpen(MarketOpenEvent evt)
    {
        PlaySfx(_clips.MarketOpenPreview);
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        PlaySfx(_clips.RoundStart);

        // Reset timer tracking for this round
        _timerWarningPlayed = false;
        _lastTimerCriticalTickSecond = -1;
        _lastShortTickSecond = -1;
        _shortCashoutWindowSoundPlayed = false;
    }

    private void OnMarketClosed(MarketClosedEvent evt)
    {
        PlaySfx(_clips.MarketClosed);
        // Task 9: Overlay stamp sound
        PlaySfx(_clips.MarketClosedStamp);

        // Stop any active event loops — EventEffects.UpdateActiveEvents() is only called
        // during TradingState, so events that outlast the round timer never expire and
        // MarketEventEndedEvent never fires, leaving loops playing forever.
        StopAllLoops();
    }

    private void OnMarginCallTriggered(MarginCallTriggeredEvent evt)
    {
        PlaySfx(_clips.MarginCall);
        // Task 9: Overlay slam sound
        PlaySfx(_clips.MarginCallSlam);
    }

    private void OnRoundCompleted(RoundCompletedEvent evt)
    {
        PlaySfx(_clips.RoundCompleteSuccess);
    }

    private void OnRunEnded(RunEndedEvent evt)
    {
        if (evt.IsVictory)
        {
            PlaySfx(_clips.RunVictory);
            // Task 9: Victory header overlay sound
            PlaySfx(_clips.VictoryHeaderAppear);
        }
        else if (evt.WasMarginCalled)
        {
            // MarginCallTriggeredEvent already played margin_call + margin_call_slam
            // Skip duplicate here to prevent double sound (AC: verify no duplicate sounds)
        }
        else
        {
            PlaySfx(_clips.RunDefeat);
        }

        // Stop any active loops on run end
        StopAllLoops();
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        PlaySfx(_clips.ActTransition);
        // Play title reveal slightly after transition sound
        PlaySfx(_clips.ActTitleReveal);
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 6: MARKET EVENT SFX (3-Tier System)
    // ════════════════════════════════════════════════════════════════════

    private void OnMarketEventFired(MarketEventFiredEvent evt)
    {
        PlayUI(_clips.EventPopupAppear);

        // 3-tier system (AC: 11)
        var tier = GetEventSoundTier(evt.EventType);
        switch (tier)
        {
            case EventSoundTier.Positive:
                PlaySfx(_clips.EventPositive);
                break;
            case EventSoundTier.Negative:
                PlaySfx(_clips.EventNegative);
                break;
            case EventSoundTier.Extreme:
                PlaySfx(_clips.EventExtreme);
                break;
        }
    }

    /// <summary>
    /// Classifies a market event type into the 3-tier sound system (AC 11).
    /// Internal for testability.
    /// </summary>
    internal enum EventSoundTier { None, Positive, Negative, Extreme }

    internal static EventSoundTier GetEventSoundTier(MarketEventType eventType)
    {
        switch (eventType)
        {
            case MarketEventType.EarningsBeat:
            case MarketEventType.MergerRumor:
                return EventSoundTier.Positive;

            case MarketEventType.EarningsMiss:
            case MarketEventType.SECInvestigation:
            case MarketEventType.SectorRotation:
                return EventSoundTier.Negative;

            // AC 11: MarketCrash/BullRun/FlashCrash/ShortSqueeze are Extreme
            case MarketEventType.MarketCrash:
            case MarketEventType.BullRun:
            case MarketEventType.FlashCrash:
            case MarketEventType.ShortSqueeze:
            case MarketEventType.PumpAndDump:
                return EventSoundTier.Extreme;

            default:
                return EventSoundTier.None;
        }
    }

    private void OnEventPopupCompleted(EventPopupCompletedEvent evt)
    {
        // Dismiss sound based on direction
        if (evt.IsPositive)
            PlaySfx(_clips.EventPopupDismissUp);
        else
            PlaySfx(_clips.EventPopupDismissDown);

        // Start/play loops and impacts based on event type
        switch (evt.EventType)
        {
            case MarketEventType.MarketCrash:
                _crashRumbleSource = PlayTrackedSfx(_clips.CrashRumbleLoop, 0.7f);
                break;
            case MarketEventType.BullRun:
                _bullrunShimmerSource = PlayTrackedSfx(_clips.BullrunShimmerLoop, 0.6f);
                break;
            case MarketEventType.FlashCrash:
                PlaySfx(_clips.FlashCrashImpact);
                break;
        }
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        switch (evt.EventType)
        {
            case MarketEventType.MarketCrash:
                StopLoop(ref _crashRumbleSource);
                break;
            case MarketEventType.BullRun:
                StopLoop(ref _bullrunShimmerSource);
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 7: TIMER SFX
    // ════════════════════════════════════════════════════════════════════

    private void OnPriceUpdatedForTimer(PriceUpdatedEvent evt)
    {
        if (!TradingState.IsActive) return;

        float timeRemaining = TradingState.ActiveTimeRemaining;

        // Timer warning at 15s threshold — play once (AC: 13)
        if (!_timerWarningPlayed && timeRemaining <= GameConfig.TimerWarningThreshold)
        {
            PlaySfx(_clips.TimerWarning15s);
            _timerWarningPlayed = true;
        }

        // Critical tick every second below 5s (AC: 13)
        if (timeRemaining <= GameConfig.TimerCriticalThreshold && timeRemaining > 0f)
        {
            int wholeSecond = Mathf.FloorToInt(timeRemaining);
            if (wholeSecond != _lastTimerCriticalTickSecond && wholeSecond >= 0)
            {
                _lastTimerCriticalTickSecond = wholeSecond;
                PlaySfx(_clips.TimerCriticalTick);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 8: SHOP SFX
    // ════════════════════════════════════════════════════════════════════

    private void OnShopOpened(ShopOpenedEvent evt)
    {
        StopAllLoops();
        PlayUI(_clips.ShopOpen);
        PlayUI(_clips.ShopCardCascadeIn);
    }

    private void OnShopClosed(ShopClosedEvent evt)
    {
        PlayUI(_clips.ShopClose);
    }

    private void OnShopItemPurchased(ShopItemPurchasedEvent evt)
    {
        PlayUI(_clips.RelicPurchase);
    }

    private void OnShopExpansionPurchased(ShopExpansionPurchasedEvent evt)
    {
        PlayUI(_clips.ExpansionPurchase);
    }

    private void OnInsiderTipPurchased(InsiderTipPurchasedEvent evt)
    {
        PlayUI(_clips.InsiderTipReveal);
    }

    private void OnBondPurchased(BondPurchasedEvent evt)
    {
        PlayUI(_clips.BondPurchase);
    }

    private void OnBondRepPaid(BondRepPaidEvent evt)
    {
        PlayUI(_clips.BondRepPayout);
    }

    private void OnShopReroll(ShopRerollEvent evt)
    {
        PlayUI(_clips.ShopReroll);
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 9: OVERLAY & SCREEN SFX
    // Integrated into game state handlers above:
    // - MarketClosedStamp → OnMarketClosed
    // - MarginCallSlam → OnMarginCallTriggered
    // - VictoryHeaderAppear → OnRunEnded (victory)
    // - StatsCountUp / ResultsDismiss → require new events from RunSummaryUI (future)
    // ════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════
    // TASK 10: UI INTERACTION SFX
    // ════════════════════════════════════════════════════════════════════

    private void OnStockSelected(StockSelectedEvent evt)
    {
        PlayUI(_clips.StockSelected);
    }

    private void OnTradeButtonPressed(TradeButtonPressedEvent evt)
    {
        // Sound moved to OnTradeFeedback — only plays on successful trade
    }

    // ════════════════════════════════════════════════════════════════════
    // PUBLIC UI AUDIO HELPERS — called directly by UI scripts
    // ════════════════════════════════════════════════════════════════════

    public void PlayPanelOpen()      => PlayUI(_clips?.UiPanelOpen);
    public void PlayPanelClose()     => PlayUI(_clips?.UiPanelClose);
    public void PlayButtonHover()    => PlayUI(_clips?.UiButtonHover, 0.7f);
    public void PlayRelicHover()     => PlayUI(_clips?.RelicHover, 0.8f);
    public void PlayTabSwitch()      => PlayUI(_clips?.UiTabSwitch);
    public void PlayNavigate()       => PlayUI(_clips?.UiNavigate, 0.8f);
    public void PlayCancel()         => PlayUI(_clips?.UiCancel);
    public void PlayResultsDismiss() => PlayUI(_clips?.ResultsDismiss);
    public void PlayStatsCountUp()   => PlayUI(_clips?.StatsCountUp);
    public void PlayProfitPopup()    => PlaySfx(_clips?.ProfitPopup);
    public void PlayLossPopup()      => PlaySfx(_clips?.LossPopup);
    public void PlayRepEarned()      => PlayUI(_clips?.RepEarned);
    public void PlayStreakMilestone() => PlayUI(_clips?.StreakMilestone);
    public void PlayTokenLaunch()    => PlaySfx(_clips?.TokenLaunch, 0.6f);
    public void PlayTokenLand()      => PlaySfx(_clips?.TokenLand, 0.7f);
    public void PlayTokenBurst()     => PlaySfx(_clips?.TokenBurst);

    // ════════════════════════════════════════════════════════════════════
    // CORE PLAYBACK METHODS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Plays a one-shot SFX on the Sfx track. Skips gracefully if clip is null.
    /// Gameplay SFX respect Time.timeScale (won't play when paused).
    /// </summary>
    private void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[Audio] PlaySfx called with null clip — skipping");
            #endif
            return;
        }

        var options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
        options.Volume = volume * SettingsManager.SfxVolume * SettingsManager.MasterVolume;
        options.Pitch = pitch;
        MMSoundManagerSoundPlayEvent.Trigger(clip, options);
    }

    /// <summary>
    /// Plays a one-shot SFX on the UI track. UI sounds play even during pause.
    /// </summary>
    private void PlayUI(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[Audio] PlayUI called with null clip — skipping");
            #endif
            return;
        }

        var options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.UI;
        options.Volume = volume * SettingsManager.SfxVolume * SettingsManager.MasterVolume;
        MMSoundManagerSoundPlayEvent.Trigger(clip, options);
    }

    /// <summary>
    /// Plays a one-shot SFX on the Sfx track and returns the AudioSource handle
    /// so it can be stopped early (e.g., when the event ends or shop opens).
    /// </summary>
    private AudioSource PlayTrackedSfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[Audio] PlayTrackedSfx called with null clip — skipping");
            #endif
            return null;
        }

        var options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Sfx;
        options.Volume = volume * SettingsManager.SfxVolume * SettingsManager.MasterVolume;
        options.Loop = false;
        return MMSoundManagerSoundPlayEvent.Trigger(clip, options);
    }

    /// <summary>
    /// Stops a looping sound and clears the handle reference.
    /// </summary>
    private void StopLoop(ref AudioSource source)
    {
        if (source != null)
        {
            source.Stop();
            source = null;
        }
    }

    private void StopAllLoops()
    {
        StopLoop(ref _crashRumbleSource);
        StopLoop(ref _bullrunShimmerSource);
    }

}
