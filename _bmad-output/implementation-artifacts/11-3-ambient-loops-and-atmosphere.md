# Story 11.3: Ambient Loops, Atmosphere & Missing Audio Assets

Status: blocked

## Story

As a player,
I want subtle ambient atmosphere beds playing beneath the music during trading, shop, and results screens, plus looping sound effects for sustained events like market crashes and victory sparkles,
so that every moment in the game has atmospheric depth and the audio landscape feels rich and immersive.

## Blocked By

- **Missing audio assets:** The SFX files for News Banner (#66-67), Ambient SFX Loops (#68-75), and Screen Effects (#76-81) have NOT been generated yet. These 14 files need to be created before this story can be fully implemented.
- **Missing music assets:** 18 music tracks from the music manifest are not yet on disk (stingers, ambient beds, CRT sounds). These should be generated as part of this story's scope.

## Acceptance Criteria

1. Per-act ambient atmosphere beds play at low volume (~10-15%) beneath the act music during trading rounds
2. Shop ambient bed plays beneath shop music during ShopState
3. Victory/defeat ambient beds play beneath their respective screen music
4. Tension drone ambient fades in alongside urgency music layer at timer thresholds
5. CRT scanline hum plays at near-subliminal volume (~3-5%) as a constant background layer during all gameplay
6. Looping SFX for sustained events: `crash_rumble_loop` during MarketCrash, `bullrun_shimmer_loop` during BullRun (these may already be wired in 11.1 — verify and ensure they're ambient-volume balanced)
7. `chart_dust_particle` sound plays at very low volume when chart head is actively moving (throttled to max 8 plays/second, only during trading phase)
8. `victory_sparkle_loop` plays during victory screen alongside victory music
9. `bond_pulse_hum` plays at barely perceptible volume when bond panel is visible in shop
10. News banner slide-in and fade-out sounds play on banner appear/disappear
11. Screen effect sounds (flash green/red/amber/white, shake small/large) play synchronized with GameFeelManager visual effects
12. All ambient layers respect `GameConfig.AmbientVolume` as their base volume multiplier
13. Ambient layers crossfade smoothly on phase transitions (no abrupt cuts)
14. All missing assets handled gracefully — features degrade to silence with debug warning

## Missing Audio Assets — Generation Needed

### SFX Not Yet on Disk (from audio-sfx-manifest.md)

| # | File Name | Generator Prompt (from manifest) |
|---|---|---|
| 66 | `news_banner_slide_in.mp3` | Quick 8-bit notification slide, crunchy digital swoosh from edge of screen, breaking news banner appearing |
| 67 | `news_banner_fade_out.mp3` | Subtle 8-bit fade dissolve, quiet crunchy digital evaporation, news banner disappearing |
| 68 | `amb_trading_floor.mp3` | Lo-fi 8-bit stock market ambience loop, crunchy muffled digital chatter with subtle ticker tape clicks and distant chiptune murmurs, retro trading floor atmosphere, loopable, 8 seconds |
| 69 | `amb_shop.mp3` | Cozy 8-bit shop ambience loop, warm crunchy chiptune background hum with gentle digital wind chimes, calm between-rounds atmosphere, RPG shop vibes, loopable, 8 seconds |
| 70 | `amb_penny_stocks.mp3` | Chaotic 8-bit penny stock ambience loop, fast crunchy digital noise with erratic blips and volatile energy, act 1 wild west feeling, loopable, 8 seconds |
| 71 | `amb_blue_chips.mp3` | Prestigious 8-bit blue chip ambience loop, deep crunchy digital hum with confident slow pulses and rich undertones, act 4 high stakes atmosphere, loopable, 8 seconds |
| 72 | `amb_tension_rising.mp3` | Escalating 8-bit tension loop, crunchy low-frequency pulse gradually building with ticking urgency, used during timer countdown final seconds, loopable, 4 seconds |
| 73 | `chart_dust_particle.mp3` | Extremely subtle 8-bit sparkle whisper, barely audible crunchy digital fairy dust, single tiny particle sound for chart head movement, must not be annoying at high frequency |
| 74 | `victory_sparkle_loop.mp3` | Gentle 8-bit celebration sparkle loop, crunchy twinkling chiptune stars with random pitch variation, victory screen decoration, loopable, 4 seconds |
| 75 | `bond_pulse_hum.mp3` | Very subtle 8-bit pulsing glow hum, deep quiet crunchy sine wave oscillation, bond card glowing effect, barely perceptible, loopable, 2 seconds |
| 76 | `screen_flash_green.mp3` | Quick 8-bit positive flash sound, bright crunchy digital burst, green screen flash accompanying good event |
| 77 | `screen_flash_red.mp3` | Quick 8-bit negative flash sound, harsh crunchy digital burst, red screen flash accompanying bad event |
| 78 | `screen_flash_amber.mp3` | Quick 8-bit neutral flash sound, warm crunchy digital burst, amber screen flash for short trades and transitions |
| 79 | `screen_flash_white.mp3` | Sharp 8-bit bright flash sound, clean crunchy digital camera flash, white screen flash for reveals and impacts |
| 80 | `screen_shake_small.mp3` | Brief 8-bit rattle, tiny crunchy vibration wobble, subtle screen movement |
| 81 | `screen_shake_large.mp3` | Heavy 8-bit earthquake bass, deep crunchy rumbling impact with distortion, major screen shake event |

### Music Not Yet on Disk (from audio-music-manifest.md)

| # | File Name | Generator Prompt (from manifest) |
|---|---|---|
| 9 | `music_market_crash_override.mp3` | Chaotic 8-bit disaster music, crunchy distorted descending chromatic chaos with pounding bass and alarm-like melody, completely overrides normal act music during active market crash event, feels like the world is ending in chiptune, loopable, 20 seconds |
| 10 | `music_bull_run_override.mp3` | Euphoric 8-bit rocket music, crunchy soaring ascending chiptune melody with rapid ascending arpeggios and triumphant fanfare energy, completely overrides normal act music during active bull run event, everything is going up and it feels incredible, loopable, 20 seconds |
| 12 | `music_shop_browse.mp3` | Very subtle 8-bit browsing ambient layer, crunchy quiet digital sparkle texture with soft melodic fragments, plays underneath shop theme when hovering items, adds depth without distraction, loopable, 30 seconds |
| 13 | `music_act_transition.mp3` | Dramatic 8-bit act transition stinger, crunchy ascending chiptune power chord swell building to a climactic hit then resolving, leveling up to higher stakes, impressive and brief, 5 seconds |
| 14 | `music_round_victory.mp3` | Triumphant 8-bit round-clear jingle, bright crunchy chiptune 6-note ascending victory melody with sparkle finish, you passed the round, celebration but not over-the-top, classic retro level-clear energy, 5-8 seconds |
| 15 | `music_round_stats.mp3` | Calm 8-bit results screen, gentle crunchy chiptune holding pattern at 85 BPM, soft reflective melody while stats display, subtle anticipation of what comes next, peaceful but not sleepy, loopable, 30 seconds |
| 16 | `music_victory_fanfare.mp3` | Epic 8-bit ultimate victory fanfare, crunchy triumphant chiptune orchestra hit into soaring melody, the big win, you conquered Wall Street, maximum celebration energy building to a glorious peak then resolving with sparkle, retro game ending credits vibes, goosebumps, 10-15 seconds |
| 18 | `music_defeat.mp3` | Somber 8-bit game over melody, slow crunchy descending chiptune dirge, 6 notes of melancholy, you lost your fortune, sad but dignified, classic retro game over with emotional weight, fades to silence, 8-10 seconds |
| 20 | `music_margin_call.mp3` | Devastating 8-bit margin call stinger, crunchy heavy descending bass slam into dissonant alarm chord, financial devastation in chiptune form, heavier and more dramatic than normal defeat, the worst possible outcome, 5-8 seconds |
| 21 | `stinger_big_profit.mp3` | Excited 8-bit big money stinger, crunchy rapid ascending chiptune arpeggio burst into bright hit, massive profitable trade, ka-ching moment, celebratory and punchy, 3 seconds |
| 22 | `stinger_big_loss.mp3` | Painful 8-bit big loss stinger, crunchy descending chromatic slide into dull thud, massive losing trade, ouch moment, brief and impactful, 3 seconds |
| 23 | `stinger_streak_start.mp3` | Cool 8-bit streak activation, crunchy ascending power-up tone with confidence, win streak begins, momentum building, 2 seconds |
| 24 | `stinger_streak_break.mp3` | Deflating 8-bit streak lost, crunchy descending tone with brief static, win streak broken, momentum killed, 2 seconds |
| 25 | `stinger_expansion_unlock.mp3` | Grand 8-bit permanent unlock, crunchy majestic chiptune chord progression with power-up shimmer, expansion purchased, game-changing upgrade acquired, feels bigger and more permanent than a relic purchase, 4 seconds |
| 26 | `stinger_event_incoming.mp3` | Tense 8-bit breaking news stinger, crunchy urgent two-note alert with rising tension, market event about to fire, attention-grabbing newsflash feeling, 1.5 seconds |
| 27 | `stinger_short_squeeze_warning.mp3` | Alarming 8-bit squeeze alert, crunchy rapid pulsing tone with escalating pitch, your short position is in danger, panic-inducing warning, 2 seconds |
| 28-36 | `amb_market_hum.mp3` through `amb_defeat_void.mp3` | (See music manifest for full prompts — 9 ambient bed tracks) |
| 37-40 | `crt_power_on.mp3` through `crt_scanline_hum.mp3` | (See music manifest for full prompts — 4 CRT system sounds) |

**Total missing: 16 SFX + 27 music/stinger/ambient tracks = 43 assets to generate**

## Tasks / Subtasks

- [ ] Task 1: Generate missing SFX assets (AC: 14) — BLOCKED until user generates files
  - [ ] Generate all 16 SFX files listed above using sound effect generator
  - [ ] Place in `Assets/Audio/` alongside existing SFX
  - [ ] Verify all files are valid .mp3 and load in Unity

- [ ] Task 2: Generate missing music/stinger/ambient assets (AC: 14) — BLOCKED until user generates files
  - [ ] Generate all 27 music tracks listed above
  - [ ] Place stingers and ambient beds in `Assets/Audio/Music/`
  - [ ] Place CRT system sounds in `Assets/Audio/` (or `Assets/Audio/CRT/`)
  - [ ] Verify all files are valid .mp3 and load in Unity

- [ ] Task 3: Add ambient clip references to AudioClipLibrary (AC: 1-13)
  - [ ] Extend `AudioClipLibrary.cs` with ambient fields:
    ```
    // Ambient Atmosphere Beds
    public AudioClip AmbMarketHum, AmbTradingFloor
    public AudioClip AmbPennyStocks, AmbBlueChips  // (act 2/3 use AmbTradingFloor)
    public AudioClip AmbShopCalm, AmbTensionDrone
    public AudioClip AmbVictoryGlow, AmbDefeatVoid
    // Ambient SFX Loops
    public AudioClip ChartDustParticle, VictorySparkleLoop, BondPulseHum
    // News Banner
    public AudioClip NewsBannerSlideIn, NewsBannerFadeOut
    // Screen Effects
    public AudioClip ScreenFlashGreen, ScreenFlashRed, ScreenFlashAmber, ScreenFlashWhite
    public AudioClip ScreenShakeSmall, ScreenShakeLarge
    // CRT
    public AudioClip CrtPowerOn, CrtPowerOff, CrtStaticBurst, CrtScanlineHum
    // Stingers (music)
    public AudioClip StingerBigProfit, StingerBigLoss
    public AudioClip StingerStreakStart, StingerStreakBreak
    public AudioClip StingerExpansionUnlock, StingerEventIncoming
    public AudioClip StingerShortSqueezeWarning
    ```
  - [ ] Update `AudioSetup.Execute()` to load these new clips

- [ ] Task 4: Wire per-act ambient atmosphere (AC: 1, 2, 3, 12, 13)
  - [ ] In MusicManager or AudioManager, add ambient layer management
  - [ ] On `MarketOpenEvent` / `RoundStartedEvent`:
    - Determine act from RunContext.CurrentRound
    - Act 1 → fade in `AmbPennyStocks` (or `AmbTradingFloor` if penny not available)
    - Act 2-3 → fade in `AmbTradingFloor`
    - Act 4 → fade in `AmbBlueChips`
    - Volume: `GameConfig.AmbientVolume` (~10-15%)
  - [ ] On `ShopOpenedEvent`:
    - Crossfade ambient from act bed to `AmbShopCalm` over 1.5 seconds
  - [ ] On victory screen → crossfade to `AmbVictoryGlow`
  - [ ] On defeat screen → crossfade to `AmbDefeatVoid`
  - [ ] All ambient crossfades happen simultaneously with music crossfades

- [ ] Task 5: Wire tension drone ambient (AC: 4)
  - [ ] When timer crosses 15s → fade in `AmbTensionDrone` at `GameConfig.AmbientVolume`
  - [ ] Layers under the urgency music layer (additive)
  - [ ] Fade out on round end or timer reset
  - [ ] Coordinate with MusicManager urgency layer (Story 11.2)

- [ ] Task 6: Wire CRT scanline hum (AC: 5)
  - [ ] On game start (after AudioSetup.Execute), start `CrtScanlineHum` looping at 3-5% volume
  - [ ] Plays continuously across ALL game states (trading, shop, menus, results)
  - [ ] Never stops except on application quit
  - [ ] Add `CrtPowerOn` on first game load / MetaHubState.Enter (play once)
  - [ ] Add `CrtStaticBurst` on act transitions (play once during TierTransitionState)

- [ ] Task 7: Wire chart dust particle sound (AC: 7)
  - [ ] Subscribe to `PriceUpdatedEvent` in AudioManager
  - [ ] Only play during trading phase (check state)
  - [ ] Throttle: track last play time, minimum interval = `1f / 8f` seconds (max 8 plays/sec)
  - [ ] Only play when price is actually moving (check slope/delta from PriceUpdatedEvent data)
  - [ ] Volume: extremely low (~5% of SfxVolume) — this must NOT be annoying
  - [ ] Use slight random pitch variation (0.95-1.05) to prevent monotony

- [ ] Task 8: Wire victory sparkle and bond hum loops (AC: 8, 9)
  - [ ] `VictorySparkleLoop`: start on RunEndedEvent with IsVictory, loop at low volume, stop on state exit
  - [ ] `BondPulseHum`: start when bond panel becomes visible in shop (if bonds owned > 0), stop on shop close
  - [ ] Both at very low volume — atmosphere, not attention-grabbing

- [ ] Task 9: Wire news banner sounds (AC: 10)
  - [ ] Check if NewsBanner.cs has events for slide-in and fade-out
  - [ ] If yes → subscribe and play `NewsBannerSlideIn` / `NewsBannerFadeOut`
  - [ ] If no events exist → add `NewsBannerAppearedEvent` and `NewsBannerDismissedEvent` to GameEvents.cs, fire from NewsBanner.cs
  - [ ] These are quick one-shot sounds, play on Sfx track

- [ ] Task 10: Wire screen effect sounds (AC: 11)
  - [ ] Coordinate with `GameFeelManager` — screen effects are visual, these are their audio counterparts
  - [ ] Option A: Subscribe to the same EventBus events GameFeelManager uses, play matching sound
  - [ ] Option B: Have GameFeelManager fire secondary events when effects trigger (less clean)
  - [ ] Option A is preferred — AudioManager subscribes to same events and plays:
    - Green flash triggers → play `ScreenFlashGreen`
    - Red flash triggers → play `ScreenFlashRed`
    - Amber flash triggers → play `ScreenFlashAmber`
    - White flash triggers → play `ScreenFlashWhite`
    - Small shake triggers → play `ScreenShakeSmall`
    - Large shake triggers → play `ScreenShakeLarge`
  - [ ] IMPORTANT: Deduplicate — don't play both a specific SFX (e.g. `buy_success`) AND a flash sound (`screen_flash_green`) for the same event. The screen effect sounds are for flashes that DON'T already have a specific SFX. Review all mappings and ensure no double-sounds.

- [ ] Task 11: Wire stinger music (AC: 14)
  - [ ] These stingers layer on top of current music (don't replace):
    - `StingerBigProfit` → on sell/cover with profit exceeding 2x expected (intensity > 0.8)
    - `StingerBigLoss` → on sell/cover with loss exceeding 2x expected
    - `StingerStreakStart` → on win streak reaching 2
    - `StingerStreakBreak` → on win streak breaking
    - `StingerExpansionUnlock` → on `ShopExpansionPurchasedEvent`
    - `StingerEventIncoming` → just before `MarketEventFiredEvent` (check if there's a pre-event signal)
    - `StingerShortSqueezeWarning` → on ShortSqueeze `MarketEventFiredEvent` when player has active short
  - [ ] All stingers play as one-shot on Sfx track at 80% volume
  - [ ] All gracefully skip if clip is null

- [ ] Task 12: Verify and test (All AC)
  - [ ] Verify ambient layers play at correct low volumes
  - [ ] Verify ambient crossfades are smooth between phases
  - [ ] Verify chart dust sound is not annoying at max frequency
  - [ ] Verify CRT hum is subliminal — ask for playtester feedback
  - [ ] Verify screen effect sounds don't double-up with event SFX
  - [ ] Verify all missing assets skip gracefully
  - [ ] Verify 60fps maintained with ambient layers active
  - [ ] Verify no audio source leaks (ambient sources properly cleaned up between runs)

## Dev Notes

### Volume Hierarchy Reminder

From the audio-music-manifest.md:
1. Stingers: 100% (momentary)
2. Act Music: 70%
3. Dynamic Layers: 50-60%
4. Shop Music: 65%
5. Screen/Results Loops: 55%
6. Ambient Beds: 10-15%
7. CRT Hum: 3-5%

All volumes multiply against `GameConfig.MasterVolume` and their category volume (`MusicVolume`, `SfxVolume`, `AmbientVolume`).

### Screen Effect Sound Deduplication

This is the trickiest part of this story. GameFeelManager fires visual effects (flashes, shakes) in response to gameplay events. AudioManager ALSO fires SFX in response to those same events. Adding screen effect sounds on top risks double-audio.

**Rule:** Screen effect sounds (`screen_flash_*`, `screen_shake_*`) should ONLY play for visual effects that don't already have a dedicated SFX. For example:
- Buy trade → `buy_success` SFX plays + green flash visual (NO `screen_flash_green` sound)
- Market crash sustained red pulse → `crash_rumble_loop` already playing (NO `screen_flash_red` sound)
- Act transition amber flash → `act_transition` SFX plays (NO `screen_flash_amber` sound)

Screen effect sounds are essentially a FALLBACK for visual effects that lack a specific SFX. The dev agent should audit all flash/shake triggers and only wire screen effect sounds where no other SFX covers the moment.

### Depends On

- Story 11.1 (Audio Infrastructure) — AudioManager, AudioSetup, AudioClipLibrary exist
- Story 11.2 (Music System) — MusicManager exists for ambient layer coordination
- **User generating missing audio files** — 43 assets need to be created

### Blocking Status

This story is **blocked** until the user generates the 43 missing audio assets. Stories 11.1 and 11.2 can proceed immediately with existing assets. This story should be picked up after assets are delivered.

However, Tasks 3-11 (the code wiring) can be written in advance with null-safety — the code handles missing clips gracefully. The dev agent could implement the code first, and audio files can be dropped in later without code changes.

## Dev Agent Record

### Agent Model Used

### Completion Notes List

### Change Log

### Senior Developer Review (AI)

### File List
