# Story 11.2: Music System & Dynamic Layering

Status: ready-for-dev

## Story

As a player,
I want a dynamic music system that plays act-specific trading music, layers urgency tracks as the timer runs low, overrides with dramatic music during market crashes and bull runs, and crossfades smoothly between game phases (trading, shop, victory, defeat),
so that the soundtrack feels alive, responsive to gameplay, and escalates tension naturally throughout each run.

## Acceptance Criteria

1. A `MusicManager` class exists (either as a separate MonoBehaviour or integrated into AudioManager) that manages all music state
2. Act-specific trading music plays during each trading round: `music_act1_penny` (Act 1), `music_act2_lowvalue` (Act 2), `music_act3_midvalue` (Act 3), `music_act4_bluechip` (Act 4)
3. Music crossfades over 2 seconds when transitioning between acts (not hard cuts)
4. Urgency layer (`music_urgency_layer`) fades in over 1 second when round timer crosses 15s threshold
5. Critical layer (`music_critical_layer`) replaces urgency layer when timer crosses 5s threshold
6. Urgency/critical layers are additive — they play on top of act music (mixed, not replacing)
7. Market Crash override: when MarketCrash event is active, act music ducks to 30% and `music_market_crash_override` plays at full volume (if asset exists; skip gracefully if not)
8. Bull Run override: when BullRun event is active, act music ducks to 30% and `music_bull_run_override` plays at full volume (if asset exists; skip gracefully if not)
9. When override events end, act music fades back to full volume over 1 second
10. Shop music (`music_shop`) crossfades in over 1.5 seconds when ShopOpenedEvent fires, replacing act music
11. Title screen music (`music_title_screen`) plays with `music_title_ambient_bed` layered beneath at 15% volume during MetaHubState
12. Victory screen music (`music_victory_screen`) crossfades in after victory fanfare stinger completes (if `music_victory_fanfare` exists; otherwise crossfade immediately on RunEndedEvent with victory)
13. Defeat screen music (`music_defeat_screen`) crossfades in after defeat stinger completes (if `music_defeat` stinger exists; otherwise crossfade immediately on RunEndedEvent with defeat)
14. All music loops seamlessly
15. Music stops cleanly on run end — no orphaned audio sources or bleed between runs
16. Music respects `GameConfig.MusicVolume` for base volume level
17. Round victory jingle (`music_round_victory`) plays as a one-shot stinger over act music at round completion, before transitioning to shop (if asset exists)
18. Act transition stinger (`music_act_transition`) plays during TierTransitionState (if asset exists)
19. All missing music assets handled gracefully — debug warning, no exceptions, feature degrades to silence

## Tasks / Subtasks

- [ ] Task 1: Create MusicManager (AC: 1, 14, 15, 16)
  - [ ] Create `Scripts/Runtime/Audio/MusicManager.cs` as MonoBehaviour (or integrate into AudioManager — dev agent's discretion based on complexity)
  - [ ] Internal state tracking:
    - `_currentActTrack` (AudioSource handle for current act music)
    - `_urgencyLayer` (AudioSource handle for urgency overlay)
    - `_criticalLayer` (AudioSource handle for critical overlay)
    - `_overrideTrack` (AudioSource handle for crash/bull run override)
    - `_ambientTrack` (AudioSource handle for shop/title/victory/defeat music)
    - `_currentMusicState` enum: `None, TitleScreen, Trading, Shop, Victory, Defeat, ActTransition`
  - [ ] All music playback through MMSoundManager Music track
  - [ ] `Initialize(AudioClipLibrary clips)` — store clip references, subscribe to EventBus
  - [ ] `OnDestroy()` — unsubscribe, stop all tracks
  - [ ] `StopAllMusic(float fadeDuration = 0.5f)` — fades out everything cleanly

- [ ] Task 2: Act-Specific Trading Music (AC: 2, 3)
  - [ ] Subscribe to `RoundStartedEvent` — determine current act from `RunContext.CurrentRound`:
    - Rounds 1-2 → Act 1 → `music_act1_penny`
    - Rounds 3-4 → Act 2 → `music_act2_lowvalue`
    - Rounds 5-6 → Act 3 → `music_act3_midvalue`
    - Rounds 7-8 → Act 4 → `music_act4_bluechip`
  - [ ] If act music changes from previous round → crossfade over 2 seconds
  - [ ] If same act → let current track continue (don't restart)
  - [ ] Music starts playing at `MarketOpenEvent` (preview phase) so it's already playing when trading begins
  - [ ] Helper: `CrossfadeToTrack(AudioClip newClip, float duration)` — fade out current, fade in new simultaneously

- [ ] Task 3: Urgency & Critical Layers (AC: 4, 5, 6)
  - [ ] Track round timer (via AudioManager's timer tracking from Story 11.1, or subscribe to a timer event)
  - [ ] When timer crosses `GameConfig.TimerWarningThreshold` (15s):
    - Fade in `music_urgency_layer` over 1 second at 50% volume, looping
    - Layer plays additively on top of act music
  - [ ] When timer crosses `GameConfig.TimerCriticalThreshold` (5s):
    - Fade out urgency layer over 0.3 seconds
    - Fade in `music_critical_layer` over 0.3 seconds at 60% volume, looping
  - [ ] On `TradingPhaseEndedEvent` or `MarketClosedEvent`:
    - Fade out urgency/critical layers over 0.5 seconds
  - [ ] On `RoundStartedEvent`:
    - Reset urgency/critical state (ensure layers are off at round start)

- [ ] Task 4: Event Music Overrides (AC: 7, 8, 9)
  - [ ] Subscribe to `EventPopupCompletedEvent`:
    - If MarketCrash and `music_market_crash_override` clip exists:
      - Duck act music to 30% volume over 0.5 seconds
      - Play override track at full volume, looping
    - If BullRun and `music_bull_run_override` clip exists:
      - Duck act music to 30% volume over 0.5 seconds
      - Play override track at full volume, looping
  - [ ] Subscribe to `MarketEventEndedEvent`:
    - If active override is for the ending event:
      - Fade out override track over 1 second
      - Restore act music to full volume over 1 second
  - [ ] If override clips don't exist on disk → skip gracefully, no duck, debug log

- [ ] Task 5: Phase Transition Music (AC: 10, 11, 12, 13)
  - [ ] **Title Screen:** On `MetaHubState` enter (or RunStartedEvent for first time):
    - Play `music_title_screen` looping at `GameConfig.MusicVolume`
    - Layer `music_title_ambient_bed` at 15% volume if clip exists
  - [ ] **Shop:** On `ShopOpenedEvent`:
    - Crossfade from act music to `music_shop` over 1.5 seconds
    - Stop urgency/critical layers if active
  - [ ] **Return to Trading:** On `ShopClosedEvent` → music will restart on next `MarketOpenEvent`
  - [ ] **Victory:** On `RunEndedEvent` with `IsVictory`:
    - Stop all current music (quick 0.3s fade)
    - If `music_victory_fanfare` stinger exists → play it, then crossfade to `music_victory_screen` when stinger ends
    - If no fanfare → crossfade directly to `music_victory_screen` over 1 second
  - [ ] **Defeat:** On `RunEndedEvent` with `!IsVictory`:
    - Stop all current music (quick 0.3s fade)
    - If `music_defeat` stinger exists → play it, then crossfade to `music_defeat_screen`
    - If `WasMarginCalled` and `music_margin_call` stinger exists → play that instead
    - Crossfade to `music_defeat_screen` after stinger or immediately
  - [ ] **Act Transition:** On `ActTransitionEvent`:
    - If `music_act_transition` stinger exists → play it as one-shot over current music (don't stop act music, just layer the stinger)

- [ ] Task 6: Round Victory Stinger (AC: 17)
  - [ ] On `RoundCompletedEvent`:
    - If `music_round_victory` clip exists → play as one-shot stinger over act music
    - Stinger should be brief (5-8s) and not interrupt the music flow
    - After stinger, normal shop transition handles the crossfade

- [ ] Task 7: Wire MusicManager into AudioSetup (AC: 1)
  - [ ] `AudioSetup.Execute()` creates MusicManager (either on same GameObject as AudioManager or separate)
  - [ ] Pass music clips from AudioClipLibrary
  - [ ] Ensure MusicManager initializes after AudioManager
  - [ ] Add music clips to AudioClipLibrary:
    ```
    // Music
    public AudioClip MusicTitleScreen, MusicTitleAmbientBed
    public AudioClip MusicAct1Penny, MusicAct2LowValue, MusicAct3MidValue, MusicAct4BlueChip
    public AudioClip MusicUrgencyLayer, MusicCriticalLayer
    public AudioClip MusicMarketCrashOverride, MusicBullRunOverride
    public AudioClip MusicShop, MusicShopBrowse
    public AudioClip MusicActTransition, MusicRoundVictory, MusicRoundStats
    public AudioClip MusicVictoryFanfare, MusicVictoryScreen
    public AudioClip MusicDefeat, MusicDefeatScreen, MusicMarginCall
    ```

- [ ] Task 8: Add GameConfig music constants (AC: 16)
  - [ ] Add to `GameConfig.cs`:
    ```csharp
    // Music System
    public static readonly float MusicCrossfadeDuration = 2.0f;
    public static readonly float MusicUrgencyFadeIn = 1.0f;
    public static readonly float MusicCriticalFadeIn = 0.3f;
    public static readonly float MusicEventDuckVolume = 0.3f;
    public static readonly float MusicEventDuckFade = 0.5f;
    public static readonly float MusicEventRestoreFade = 1.0f;
    public static readonly float MusicShopCrossfade = 1.5f;
    public static readonly float MusicUrgencyVolume = 0.5f;
    public static readonly float MusicCriticalVolume = 0.6f;
    public static readonly float MusicTitleAmbientVolume = 0.15f;
    ```

- [ ] Task 9: Test and verify (All AC)
  - [ ] Verify act music plays correctly for each act (rounds 1-2, 3-4, 5-6, 7-8)
  - [ ] Verify crossfade between acts sounds smooth (no gap, no overlap spike)
  - [ ] Verify urgency layer fades in at 15s and critical replaces it at 5s
  - [ ] Verify layers stop at round end
  - [ ] Verify shop music crossfade works
  - [ ] Verify victory/defeat music transitions
  - [ ] Verify missing clips don't cause exceptions
  - [ ] Verify no orphaned AudioSources between runs (EventBus.Clear in Awake)
  - [ ] Verify music loops seamlessly (no audible gap at loop point)
  - [ ] Verify 60fps maintained with multiple simultaneous audio layers

## Dev Notes

### MMSoundManager Multi-Track Playback

The MMSoundManager supports playing multiple sounds simultaneously on the Music track. Each `MMSoundManagerSoundPlayEvent.Trigger()` returns an AudioSource reference. Store these handles to control individual layers:

```csharp
// Start a loop:
var options = MMSoundManagerPlayOptions.Default;
options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
options.Loop = true;
options.Volume = volume;
options.Fade = true;
options.FadeDuration = fadeDuration;
var source = MMSoundManagerSoundPlayEvent.Trigger(clip, options);

// Later, fade out:
MMSoundManagerSoundFadeEvent.Trigger(source, fadeDuration, 0f, new MMTweenType(MMTween.MMTweenCurve.EaseInCubic));
```

### Music State Machine

The MusicManager should track its state to prevent conflicting transitions:
- Multiple events firing in rapid succession should not create audio chaos
- Use a simple state enum to gate transitions
- Override events take priority over normal act music
- Stingers play independently and don't affect state

### Existing Music Assets on Disk

At `Assets/Audio/Music/`:
- `music_title_screen.mp3` ✓
- `music_title_ambient_bed.mp3` ✓
- `music_act1_penny.mp3` ✓
- `music_act2_lowvalue.mp3` ✓
- `music_act3_midvalue.mp3` ✓
- `music_act4_bluechip.mp3` ✓
- `music_urgency_layer.mp3` ✓
- `music_critical_layer.mp3` ✓
- `music_shop.mp3` ✓
- `music_victory_screen.mp3` ✓
- `music_defeat_screen.mp3` ✓

**NOT on disk yet (handle gracefully):**
- `music_market_crash_override` — skip override, keep act music at full volume
- `music_bull_run_override` — skip override, keep act music at full volume
- `music_shop_browse` — skip browse layer
- `music_act_transition` — skip stinger
- `music_round_victory` — skip stinger
- `music_round_stats` — skip, use act music
- `music_victory_fanfare` — skip, go directly to victory_screen
- `music_defeat` — skip stinger, go directly to defeat_screen
- `music_margin_call` — skip stinger, go directly to defeat_screen

### Depends On

- Story 11.1 (Audio Infrastructure) — AudioSetup, AudioClipLibrary, AudioManager exist
- GameFeelManager timer tracking — or implement timer tracking in AudioManager

### What This Story Does NOT Cover

- Ambient atmosphere beds → Story 11.3
- SFX playback → Story 11.1
- Per-act ambient texture layers → Story 11.3
- Volume settings UI → Epic 12

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### Senior Developer Review (AI)

### File List
