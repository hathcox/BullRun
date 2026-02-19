# Epic 11: Audio System — 8-Bit Retro Sound & Music

**Description:** Complete audio implementation for BullRun — from infrastructure to atmosphere. Builds an AudioManager (mirroring GameFeelManager's pattern), wires 65+ SFX to EventBus events, implements a dynamic music system with act-specific tracks and urgency layers, and adds ambient atmosphere beds for immersive depth. Uses MMSoundManager from the Feel package as the playback engine. All audio assets follow an 8-bit retro, crunchy lo-fi, chiptune-inspired style with modern punch and clarity.

**Status:** Ready for dev
**Phase:** Post-Epic 15 (Game Feel Phase 2), audio layer on top of visual juice
**Depends On:** Epic 13 (Store Rework — done), Epic 14 (Terminal 1999 UI — done), Epic 15 (Game Feel Phase 2 — done)

**Audio Asset Status:**
- 65+ SFX files on disk at `Assets/Audio/*.mp3`
- 11 music tracks on disk at `Assets/Audio/Music/*.mp3`
- 43 assets still need generation (16 SFX + 27 music/stingers/ambient) — see Story 11.3

**Architecture:**
```
AudioSetup.Execute()          ← called from GameRunner.Start() after GameFeelSetup
    │
    ├── MMSoundManager        ← Feel package singleton (playback engine)
    │
    ├── AudioManager          ← MonoBehaviour, subscribes to EventBus events
    │   ├── PlaySfx()         ← one-shot SFX via MMSoundManager Sfx track
    │   ├── PlayUI()          ← UI sounds via MMSoundManager UI track
    │   ├── PlayLoop()        ← looping SFX (crash rumble, etc.)
    │   └── StopLoop()        ← stop looping SFX
    │
    ├── MusicManager          ← MonoBehaviour, manages music state machine
    │   ├── Act music          ← per-act trading tracks with crossfade
    │   ├── Urgency layers     ← additive layers at timer thresholds
    │   ├── Event overrides    ← crash/bull run music ducks act music
    │   ├── Phase transitions  ← shop, victory, defeat crossfades
    │   └── Stingers           ← one-shot musical moments
    │
    └── AudioClipLibrary      ← plain C# class holding all AudioClip references
```

**Key Design Decisions:**
- MMSoundManager is the playback engine — no raw AudioSource management
- AudioManager mirrors GameFeelManager pattern exactly (EventBus subscribe/unsubscribe)
- All audio config in GameConfig.cs as `public static readonly` fields (no ScriptableObjects)
- Asset loading at F5 setup time — never Resources.Load()
- All missing clips handled gracefully (null check → debug warning → skip)
- 3-tier market event sounds: positive, negative, extreme (not unique per event type)

---

## Story 11.1: Audio Infrastructure & SFX Wiring

As a player, I want every trading action, market event, UI interaction, and game state change to play a satisfying 8-bit sound effect, so that the game feels alive, responsive, and juicy with audio feedback matching every visual event.

**Acceptance Criteria:**
- AudioManager MonoBehaviour at `Scripts/Runtime/Audio/AudioManager.cs` following GameFeelManager pattern
- AudioSetup static class at `Scripts/Setup/AudioSetup.cs` following GameFeelSetup pattern
- AudioClipLibrary data class at `Scripts/Runtime/Audio/AudioClipLibrary.cs` with all clip references
- GameConfig extended with audio volume constants
- AudioSetup.Execute() called from GameRunner.Start() after GameFeelSetup
- MMSoundManager initialized as playback engine
- All 65+ existing SFX wired to EventBus events
- Trade intensity scaling on sell_profit
- 3-tier market event system (positive/negative/extreme)
- Short lifecycle sounds at correct state transitions
- Timer sounds at 15s warning and 5s critical thresholds
- All shop, overlay, and UI sounds wired
- Graceful null-clip handling

**Story File:** `_bmad-output/implementation-artifacts/11-1-audio-infrastructure-and-sfx.md`

---

## Story 11.2: Music System & Dynamic Layering

As a player, I want a dynamic music system that plays act-specific trading music, layers urgency tracks as the timer runs low, overrides with dramatic music during market crashes, and crossfades smoothly between game phases, so that the soundtrack feels alive and responsive.

**Acceptance Criteria:**
- MusicManager class managing all music state
- Act-specific trading music (4 tracks, one per act) with 2-second crossfade between acts
- Urgency layer fades in at 15s, critical layer at 5s (additive over act music)
- Market crash/bull run overrides duck act music to 30%
- Shop music crossfade on ShopOpenedEvent
- Title screen music with ambient bed layer
- Victory/defeat music transitions with optional stinger-first sequencing
- All music loops seamlessly
- Missing clips degrade gracefully to silence

**Story File:** `_bmad-output/implementation-artifacts/11-2-music-system-and-dynamic-layers.md`

---

## Story 11.3: Ambient Loops, Atmosphere & Missing Audio Assets

As a player, I want subtle ambient atmosphere beds playing beneath the music, plus looping effects for sustained events, so that every moment has atmospheric depth and the audio landscape feels rich and immersive.

**Status:** blocked (43 audio assets need generation)

**Acceptance Criteria:**
- Per-act ambient atmosphere beds at 10-15% volume beneath act music
- Shop, victory, defeat ambient beds
- Tension drone ambient with urgency music layer
- CRT scanline hum at 3-5% volume (constant)
- Chart dust particle sound (throttled, very low volume)
- Victory sparkle and bond pulse hum loops
- News banner slide-in/fade-out sounds
- Screen effect sounds synchronized with GameFeelManager visual effects
- All 43 missing audio assets generated and placed on disk

**Story File:** `_bmad-output/implementation-artifacts/11-3-ambient-loops-and-atmosphere.md`

---

## Dependency Graph

```
11.1 (Audio Infrastructure + SFX) ← READY — all assets on disk
  └── 11.2 (Music System + Dynamic Layers) ← READY — 11 of 18 tracks on disk
        └── 11.3 (Ambient Loops + Atmosphere) ← BLOCKED — 43 assets need generation
```

**Recommended implementation order:** 11.1 → 11.2 → 11.3 (sequential — each builds on the previous)

---

## Audio Asset Inventory

### On Disk — Ready (76 files)

**SFX (65 at `Assets/Audio/`):**
buy_success, sell_profit, sell_loss, trade_rejected, short_open, short_cashout_profit, short_cashout_loss, short_auto_close, trade_cooldown_start, trade_cooldown_end, timer_warning_15s, timer_critical_tick, short_countdown_tick, short_cashout_window, short_cashout_urgency, market_open_preview, round_start, round_complete_success, market_closed, margin_call, run_victory, run_defeat, run_start, act_transition, act_title_reveal, event_popup_appear, event_positive, event_negative, event_extreme, event_popup_dismiss_up, event_popup_dismiss__down, crash_rumble_loop, bullrun_shimmer_loop, flash_crash_impact, shop_open, shop_close, relic_purchase, relic_hover, expansion_purchase, insider_tip_reveal, shop_reroll, bond_purchase, bond_rep_payout, shop_card_cascade_in, token_launch, token_land, token_burst, profit_popup, loss_popup, rep_earned, streak_milestone, ui_button_hover, ui_panel_open, ui_panel_close, ui_tab_switch, ui_navigate, ui_confirm, ui_cancel, stock_selected, market_closed_stamp, margin_call_slam, victory_header_appear, stats_count_up, results_dismiss

**Music (11 at `Assets/Audio/Music/`):**
music_title_screen, music_title_ambient_bed, music_act1_penny, music_act2_lowvalue, music_act3_midvalue, music_act4_bluechip, music_urgency_layer, music_critical_layer, music_shop, music_victory_screen, music_defeat_screen

### Not Yet on Disk — Need Generation (43 files)

See Story 11.3 for the complete list with generator prompts.

---

## Key Technical Notes for Dev Agent

### Follow GameFeelManager Pattern
`GameFeelManager` and `GameFeelSetup` are the direct templates. AudioManager subscribes to EventBus in Initialize(), unsubscribes in OnDestroy(). AudioSetup creates the GameObject and wires dependencies.

### MMSoundManager API
```csharp
// One-shot SFX:
MMSoundManagerSoundPlayEvent.Trigger(clip, MMSoundManager.MMSoundManagerTracks.Sfx, Vector3.zero);

// Looping music with fade:
var options = MMSoundManagerPlayOptions.Default;
options.MmSoundManagerTrack = MMSoundManager.MMSoundManagerTracks.Music;
options.Loop = true;
options.Volume = 0.7f;
options.Fade = true;
options.FadeDuration = 1.5f;
MMSoundManagerSoundPlayEvent.Trigger(clip, options);
```

### Audio References
- Full SFX manifest: `_bmad-output/audio-sfx-manifest.md` (81 sounds defined)
- Full music manifest: `_bmad-output/audio-music-manifest.md` (40 tracks defined)
- Event definitions: `Assets/Scripts/Runtime/Core/GameEvents.cs`
- Feel pattern template: `Assets/Scripts/Runtime/UI/GameFeelManager.cs`
- Feel setup template: `Assets/Scripts/Setup/GameFeelSetup.cs`
