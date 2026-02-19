using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// Dynamic music system. Manages act-specific trading music, urgency/critical layers,
/// event overrides, phase transitions, and stingers. All playback via MMSoundManager Music track.
/// Story 11.2 — subscribes to EventBus events and crossfades between music states.
/// </summary>
public class MusicManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════════════════════
    // STATE
    // ════════════════════════════════════════════════════════════════════

    public static MusicManager Instance { get; private set; }

    internal enum MusicState { None, TitleScreen, Trading, Shop, Victory, Defeat, ActTransition }

    private AudioClipLibrary _clips;
    private MusicState _currentMusicState = MusicState.None;

    // Active AudioSource handles for controlling individual layers
    private AudioSource _currentActTrack;
    private AudioSource _urgencyLayer;
    private AudioSource _criticalLayer;
    private AudioSource _overrideTrack;
    private AudioSource _ambientTrack;     // shop/title/victory/defeat music
    private AudioSource _ambientBedTrack;  // title ambient bed layer

    // Timer tracking for urgency/critical layers
    private bool _urgencyActive;
    private bool _criticalActive;

    // Override tracking
    private bool _overrideActive;
    private MarketEventType _activeOverrideEvent;

    // Stinger tracking
    private AudioSource _stingerSource;
    private AudioClip _pendingPostStingerClip;
    private float _stingerTimer;
    private bool _waitingForStinger;

    // Act tracking to avoid restarting same act music
    private int _currentAct;

    // Fade tracking — lightweight Update-driven volume fading
    private struct FadeEntry
    {
        public AudioSource Source;
        public float StartVolume;
        public float TargetVolume;
        public float Duration;
        public float Elapsed;
        public bool StopOnComplete;
    }
    private readonly List<FadeEntry> _activeFades = new List<FadeEntry>();

    // ════════════════════════════════════════════════════════════════════
    // INITIALIZATION (Task 1)
    // ════════════════════════════════════════════════════════════════════

    public void Initialize(AudioClipLibrary clips)
    {
        Instance = this;
        _clips = clips;

        // Task 2: Act-specific trading music
        EventBus.Subscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);

        // Task 3: Urgency & critical layers
        EventBus.Subscribe<PriceUpdatedEvent>(OnPriceUpdatedForMusic);
        EventBus.Subscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Subscribe<MarketClosedEvent>(OnMarketClosed);

        // Task 4: Event overrides
        EventBus.Subscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);

        // Task 5: Phase transitions
        EventBus.Subscribe<RunStartedEvent>(OnRunStarted);
        EventBus.Subscribe<ShopOpenedEvent>(OnShopOpened);
        EventBus.Subscribe<ShopClosedEvent>(OnShopClosed);
        EventBus.Subscribe<RunEndedEvent>(OnRunEnded);
        EventBus.Subscribe<ActTransitionEvent>(OnActTransition);

        // Task 6: Round victory stinger
        EventBus.Subscribe<RoundCompletedEvent>(OnRoundCompleted);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[Music] MusicManager initialized — 14 event subscriptions active");
        #endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        EventBus.Unsubscribe<MarketOpenEvent>(OnMarketOpen);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<PriceUpdatedEvent>(OnPriceUpdatedForMusic);
        EventBus.Unsubscribe<TradingPhaseEndedEvent>(OnTradingPhaseEnded);
        EventBus.Unsubscribe<MarketClosedEvent>(OnMarketClosed);
        EventBus.Unsubscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
        EventBus.Unsubscribe<RunStartedEvent>(OnRunStarted);
        EventBus.Unsubscribe<ShopOpenedEvent>(OnShopOpened);
        EventBus.Unsubscribe<ShopClosedEvent>(OnShopClosed);
        EventBus.Unsubscribe<RunEndedEvent>(OnRunEnded);
        EventBus.Unsubscribe<ActTransitionEvent>(OnActTransition);
        EventBus.Unsubscribe<RoundCompletedEvent>(OnRoundCompleted);

        StopAllMusic();
    }

    /// <summary>
    /// Stops all active music tracks with an optional fade out.
    /// </summary>
    public void StopAllMusic(float fadeDuration = 0.5f)
    {
        FadeOutAndStop(ref _currentActTrack, fadeDuration);
        FadeOutAndStop(ref _urgencyLayer, fadeDuration);
        FadeOutAndStop(ref _criticalLayer, fadeDuration);
        FadeOutAndStop(ref _overrideTrack, fadeDuration);
        FadeOutAndStop(ref _ambientTrack, fadeDuration);
        FadeOutAndStop(ref _ambientBedTrack, fadeDuration);
        FadeOutAndStop(ref _stingerSource, fadeDuration);

        _urgencyActive = false;
        _criticalActive = false;
        _overrideActive = false;
        _waitingForStinger = false;
        _currentMusicState = MusicState.None;
        _currentAct = 0;
    }

    /// <summary>
    /// Plays title screen music and ambient bed. Called by MainMenuState.Enter.
    /// Story 16.1: Separated from OnRunStarted so title music plays on main menu display.
    /// </summary>
    public void PlayTitleMusic()
    {
        StopAllMusic(0.3f);
        _currentMusicState = MusicState.TitleScreen;

        if (_clips.MusicTitleScreen != null)
        {
            _ambientTrack = PlayMusicLoop(_clips.MusicTitleScreen, SettingsManager.MusicVolume, 0.5f);
        }

        if (_clips.MusicTitleAmbientBed != null)
        {
            _ambientBedTrack = PlayMusicLoop(_clips.MusicTitleAmbientBed,
                GameConfig.MusicTitleAmbientVolume, 0.5f);
        }
    }

    /// <summary>
    /// Updates the volume of all currently-playing music AudioSources to reflect
    /// current SettingsManager values. Called by settings sliders for real-time preview
    /// without restarting tracks.
    /// </summary>
    public void UpdateVolumes()
    {
        float musicVol = SettingsManager.MusicVolume * SettingsManager.MasterVolume;

        if (_ambientTrack != null)
            _ambientTrack.volume = musicVol;
        if (_ambientBedTrack != null)
            _ambientBedTrack.volume = GameConfig.MusicTitleAmbientVolume * SettingsManager.MasterVolume;
        if (_currentActTrack != null)
            _currentActTrack.volume = musicVol;
        if (_overrideTrack != null)
            _overrideTrack.volume = musicVol;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Process active volume fades
        for (int i = _activeFades.Count - 1; i >= 0; i--)
        {
            var fade = _activeFades[i];

            // Source was destroyed or already stopped externally
            if (fade.Source == null)
            {
                _activeFades.RemoveAt(i);
                continue;
            }

            fade.Elapsed += dt;
            float t = Mathf.Clamp01(fade.Elapsed / fade.Duration);
            fade.Source.volume = Mathf.Lerp(fade.StartVolume, fade.TargetVolume, t);

            if (t >= 1f)
            {
                if (fade.StopOnComplete)
                {
                    fade.Source.Stop();
                }
                _activeFades.RemoveAt(i);
            }
            else
            {
                _activeFades[i] = fade;
            }
        }

        // Track stinger completion to trigger post-stinger music
        if (_waitingForStinger)
        {
            _stingerTimer -= dt;
            if (_stingerTimer <= 0f)
            {
                _waitingForStinger = false;
                if (_pendingPostStingerClip != null)
                {
                    FadeOutAndStop(ref _ambientTrack, 0.3f);
                    _ambientTrack = PlayMusicLoop(_pendingPostStingerClip, SettingsManager.MusicVolume, 1.0f);
                    _pendingPostStingerClip = null;
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 2: ACT-SPECIFIC TRADING MUSIC (AC: 2, 3)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the act music clip for the given act number. Internal for testability.
    /// </summary>
    internal AudioClip GetActClip(int act)
    {
        switch (act)
        {
            case 1: return _clips.MusicAct1Penny;
            case 2: return _clips.MusicAct2LowValue;
            case 3: return _clips.MusicAct3MidValue;
            case 4: return _clips.MusicAct4BlueChip;
            default: return null;
        }
    }

    /// <summary>
    /// Maps a round number to its act. Internal for testability.
    /// </summary>
    internal static int GetActForRound(int roundNumber)
    {
        return RunContext.GetActForRound(roundNumber);
    }

    private void OnMarketOpen(MarketOpenEvent evt)
    {
        // Stop title/ambient music when first market opens
        FadeOutAndStop(ref _ambientTrack, 0.5f);
        FadeOutAndStop(ref _ambientBedTrack, 0.5f);

        // Music starts at MarketOpen (preview phase) so it's already playing when trading begins
        int act = evt.Act;
        if (act == _currentAct && _currentActTrack != null) return; // same act, keep playing

        var clip = GetActClip(act);
        if (clip == null)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[Music] No act music clip for act {act}");
            #endif
            return;
        }

        _currentMusicState = MusicState.Trading;

        if (_currentActTrack != null)
        {
            // Crossfade to new act music
            CrossfadeToTrack(clip, GameConfig.MusicCrossfadeDuration);
        }
        else
        {
            // First track of the run — fade in from silence
            _currentActTrack = PlayMusicLoop(clip, SettingsManager.MusicVolume, GameConfig.MusicCrossfadeDuration);
        }

        _currentAct = act;
    }

    private void OnRoundStarted(RoundStartedEvent evt)
    {
        if (_currentMusicState != MusicState.Trading && _currentMusicState != MusicState.TitleScreen) return;

        // Reset urgency/critical state at round start
        FadeOutAndStop(ref _urgencyLayer, 0.3f);
        FadeOutAndStop(ref _criticalLayer, 0.3f);
        _urgencyActive = false;
        _criticalActive = false;
    }

    /// <summary>
    /// Crossfades from the current act track to a new clip over the given duration.
    /// </summary>
    private void CrossfadeToTrack(AudioClip newClip, float duration)
    {
        // Fade out current
        FadeOutAndStop(ref _currentActTrack, duration);

        // Fade in new
        _currentActTrack = PlayMusicLoop(newClip, SettingsManager.MusicVolume, duration);
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 3: URGENCY & CRITICAL LAYERS (AC: 4, 5, 6)
    // ════════════════════════════════════════════════════════════════════

    private void OnPriceUpdatedForMusic(PriceUpdatedEvent evt)
    {
        if (!TradingState.IsActive) return;
        if (_currentMusicState != MusicState.Trading) return;

        float timeRemaining = TradingState.ActiveTimeRemaining;

        // Critical layer at 5s threshold (replaces urgency)
        if (!_criticalActive && timeRemaining <= GameConfig.TimerCriticalThreshold)
        {
            // Fade out urgency if active
            if (_urgencyActive)
            {
                FadeOutAndStop(ref _urgencyLayer, GameConfig.MusicCriticalFadeIn);
                _urgencyActive = false;
            }

            // Fade in critical layer
            if (_clips.MusicCriticalLayer != null)
            {
                _criticalLayer = PlayMusicLoop(_clips.MusicCriticalLayer,
                    GameConfig.MusicCriticalVolume, GameConfig.MusicCriticalFadeIn);
                _criticalActive = true;
            }
        }
        // Urgency layer at 15s threshold
        else if (!_urgencyActive && !_criticalActive && timeRemaining <= GameConfig.TimerWarningThreshold)
        {
            if (_clips.MusicUrgencyLayer != null)
            {
                _urgencyLayer = PlayMusicLoop(_clips.MusicUrgencyLayer,
                    GameConfig.MusicUrgencyVolume, GameConfig.MusicUrgencyFadeIn);
                _urgencyActive = true;
            }
        }
    }

    private void OnTradingPhaseEnded(TradingPhaseEndedEvent evt)
    {
        // Fade out urgency/critical layers on trading end
        StopLayers(0.5f);
    }

    private void OnMarketClosed(MarketClosedEvent evt)
    {
        // Ensure layers are stopped on market close too
        StopLayers(0.5f);
    }

    private void StopLayers(float fadeDuration)
    {
        if (_urgencyActive)
        {
            FadeOutAndStop(ref _urgencyLayer, fadeDuration);
            _urgencyActive = false;
        }
        if (_criticalActive)
        {
            FadeOutAndStop(ref _criticalLayer, fadeDuration);
            _criticalActive = false;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 4: EVENT MUSIC OVERRIDES (AC: 7, 8, 9)
    // ════════════════════════════════════════════════════════════════════

    private void OnEventPopupCompleted(EventPopupCompletedEvent evt)
    {
        if (_currentMusicState != MusicState.Trading) return;

        AudioClip overrideClip = null;

        if (evt.EventType == MarketEventType.MarketCrash)
            overrideClip = _clips.MusicMarketCrashOverride;
        else if (evt.EventType == MarketEventType.BullRun)
            overrideClip = _clips.MusicBullRunOverride;

        if (overrideClip == null) return; // clip doesn't exist, skip gracefully

        // Duck act music to 30% volume
        if (_currentActTrack != null)
        {
            FadeSourceTo(_currentActTrack, GameConfig.MusicEventDuckVolume * SettingsManager.MusicVolume * SettingsManager.MasterVolume,
                GameConfig.MusicEventDuckFade);
        }

        // Play override at full music volume
        _overrideTrack = PlayMusicLoop(overrideClip, SettingsManager.MusicVolume, 0f);
        _overrideActive = true;
        _activeOverrideEvent = evt.EventType;
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        if (!_overrideActive) return;
        if (evt.EventType != _activeOverrideEvent) return;

        // Fade out override
        FadeOutAndStop(ref _overrideTrack, GameConfig.MusicEventRestoreFade);

        // Restore act music to full volume
        if (_currentActTrack != null)
        {
            FadeSourceTo(_currentActTrack, SettingsManager.MusicVolume * SettingsManager.MasterVolume,
                GameConfig.MusicEventRestoreFade);
        }

        _overrideActive = false;
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 5: PHASE TRANSITION MUSIC (AC: 10, 11, 12, 13)
    // ════════════════════════════════════════════════════════════════════

    private void OnRunStarted(RunStartedEvent evt)
    {
        // Story 16.1: If title music is already playing (from MainMenuState.Enter),
        // skip to avoid stopping and restarting the same tracks wastefully.
        if (_currentMusicState == MusicState.TitleScreen) return;

        // Title screen music — plays at run start (published by RunContext.StartNewRun).
        // Plays until first MarketOpenEvent fades it out and starts act music.
        PlayTitleMusic();
    }

    private void OnShopOpened(ShopOpenedEvent evt)
    {
        // Stop urgency/critical layers
        StopLayers(0.3f);

        // Stop any active override
        if (_overrideActive)
        {
            FadeOutAndStop(ref _overrideTrack, 0.3f);
            _overrideActive = false;
        }

        _currentMusicState = MusicState.Shop;

        if (_clips.MusicShop != null)
        {
            // Crossfade from act music to shop music
            FadeOutAndStop(ref _currentActTrack, GameConfig.MusicShopCrossfade);
            _ambientTrack = PlayMusicLoop(_clips.MusicShop, SettingsManager.MusicVolume, GameConfig.MusicShopCrossfade);
        }
    }

    private void OnShopClosed(ShopClosedEvent evt)
    {
        // Stop shop music — next MarketOpenEvent will start act music
        FadeOutAndStop(ref _ambientTrack, 0.5f);
        _currentMusicState = MusicState.None;
    }

    private void OnRunEnded(RunEndedEvent evt)
    {
        // Stop all current music quickly
        StopAllMusic(0.3f);

        if (evt.IsVictory)
        {
            _currentMusicState = MusicState.Victory;

            if (_clips.MusicVictoryFanfare != null)
            {
                // Play fanfare stinger, then crossfade to victory screen music
                _stingerSource = PlayMusicOneShot(_clips.MusicVictoryFanfare, SettingsManager.MusicVolume);
                _pendingPostStingerClip = _clips.MusicVictoryScreen;
                _stingerTimer = _clips.MusicVictoryFanfare.length;
                _waitingForStinger = true;
            }
            else if (_clips.MusicVictoryScreen != null)
            {
                // No fanfare — crossfade directly to victory screen music
                _ambientTrack = PlayMusicLoop(_clips.MusicVictoryScreen, SettingsManager.MusicVolume, 1.0f);
            }
        }
        else
        {
            _currentMusicState = MusicState.Defeat;

            AudioClip stingerClip = null;

            // WasMarginCalled → try margin call stinger first
            if (evt.WasMarginCalled && _clips.MusicMarginCall != null)
                stingerClip = _clips.MusicMarginCall;
            else if (_clips.MusicDefeat != null)
                stingerClip = _clips.MusicDefeat;

            if (stingerClip != null)
            {
                _stingerSource = PlayMusicOneShot(stingerClip, SettingsManager.MusicVolume);
                _pendingPostStingerClip = _clips.MusicDefeatScreen;
                _stingerTimer = stingerClip.length;
                _waitingForStinger = true;
            }
            else if (_clips.MusicDefeatScreen != null)
            {
                // No stinger — go directly to defeat screen music
                _ambientTrack = PlayMusicLoop(_clips.MusicDefeatScreen, SettingsManager.MusicVolume, 1.0f);
            }
        }
    }

    private void OnActTransition(ActTransitionEvent evt)
    {
        if (_currentMusicState != MusicState.Trading) return;

        // Play act transition stinger as one-shot over current music (don't stop act music)
        if (_clips.MusicActTransition != null)
        {
            PlayMusicOneShot(_clips.MusicActTransition,
                SettingsManager.MusicVolume * GameConfig.MusicActTransitionStingerVolume);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // TASK 6: ROUND VICTORY STINGER (AC: 17)
    // ════════════════════════════════════════════════════════════════════

    private void OnRoundCompleted(RoundCompletedEvent evt)
    {
        if (_currentMusicState != MusicState.Trading) return;

        // Play round victory stinger as one-shot over act music
        if (_clips.MusicRoundVictory != null)
        {
            PlayMusicOneShot(_clips.MusicRoundVictory,
                SettingsManager.MusicVolume * GameConfig.MusicRoundVictoryStingerVolume);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CORE PLAYBACK HELPERS
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Plays a looping music clip via MMSoundManager Music track.
    /// Returns AudioSource handle for later fade/stop control.
    /// </summary>
    private AudioSource PlayMusicLoop(AudioClip clip, float volume, float fadeDuration)
    {
        if (clip == null) return null;

        float targetVolume = volume * SettingsManager.MasterVolume;
        var options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
        options.Loop = true;

        // Start at 0 and use custom linear fade for consistent crossfade curves.
        // Both fade-in and fade-out use the same linear interpolation, preventing
        // volume dips during crossfades (previously EaseInCubic caused a hole).
        options.Volume = fadeDuration > 0f ? 0f : targetVolume;

        var source = MMSoundManagerSoundPlayEvent.Trigger(clip, options);

        if (fadeDuration > 0f && source != null)
        {
            FadeSourceTo(source, targetVolume, fadeDuration);
        }

        return source;
    }

    /// <summary>
    /// Plays a one-shot music clip (stinger) via MMSoundManager Music track.
    /// Does not loop. Returns AudioSource handle.
    /// </summary>
    private AudioSource PlayMusicOneShot(AudioClip clip, float volume)
    {
        if (clip == null) return null;

        var options = MMSoundManagerPlayOptions.Default;
        options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
        options.Loop = false;
        options.Volume = volume * SettingsManager.MasterVolume;
        return MMSoundManagerSoundPlayEvent.Trigger(clip, options);
    }

    /// <summary>
    /// Smoothly fades an AudioSource to a target volume over the given duration, then stops it.
    /// Nulls the reference immediately so callers don't reuse it.
    /// </summary>
    private void FadeOutAndStop(ref AudioSource source, float fadeDuration)
    {
        if (source == null) return;

        if (fadeDuration > 0f)
        {
            _activeFades.Add(new FadeEntry
            {
                Source = source,
                StartVolume = source.volume,
                TargetVolume = 0f,
                Duration = fadeDuration,
                Elapsed = 0f,
                StopOnComplete = true
            });
        }
        else
        {
            source.Stop();
        }

        source = null;
    }

    /// <summary>
    /// Smoothly fades an AudioSource to a target volume (duck or restore). Does NOT stop.
    /// </summary>
    private void FadeSourceTo(AudioSource source, float targetVolume, float duration)
    {
        if (source == null) return;

        // Remove any existing fade for this source
        for (int i = _activeFades.Count - 1; i >= 0; i--)
        {
            if (_activeFades[i].Source == source)
            {
                _activeFades.RemoveAt(i);
            }
        }

        if (duration <= 0f)
        {
            source.volume = targetVolume;
            return;
        }

        _activeFades.Add(new FadeEntry
        {
            Source = source,
            StartVolume = source.volume,
            TargetVolume = targetVolume,
            Duration = duration,
            Elapsed = 0f,
            StopOnComplete = false
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // INTERNAL ACCESSORS (for testing)
    // ════════════════════════════════════════════════════════════════════

    internal MusicState CurrentState => _currentMusicState;
    internal bool IsUrgencyActive => _urgencyActive;
    internal bool IsCriticalActive => _criticalActive;
    internal bool IsOverrideActive => _overrideActive;
    internal int CurrentAct => _currentAct;
}
