# Story 14.6: CRT Bezel Overlay & Visual Polish

Status: ready-for-dev

## Story

As a player,
I want the entire screen framed as a curved 1999 CRT monitor with scanline effects and phosphor glow,
so that the trading cockpit has a cohesive retro-futuristic aesthetic.

## Acceptance Criteria

1. **CRT Overlay Panel:** New full-screen ScreenSpaceOverlay canvas (sorting order 999, raycast disabled):
   - Vignette image: dark edges fading to transparent center
   - Scanline overlay: subtle horizontal lines (~3px spacing, 5-10% opacity)
   - Both non-interactive (raycast disabled) so they don't block input
2. **Panel Border Styling:** All Control Deck panels get thin border effect using nested Image outlines (1px `CRTThemeData.Border` color)
3. **Global CRT Theme Application** — all scattered color constants updated to reference `CRTThemeData`:
   - `UISetup.BarBackgroundColor` → `CRTThemeData.Panel`
   - `UISetup.LabelColor` → `CRTThemeData.TextLow`
   - `UISetup.ValueColor` → `CRTThemeData.TextHigh`
   - `TradingHUD.ProfitGreen` → `CRTThemeData.TextHigh`
   - `TradingHUD.LossRed` → `CRTThemeData.Danger`
   - `TradingHUD.WarningYellow` → `CRTThemeData.Warning`
   - `PositionOverlay.LongColor` → `CRTThemeData.TextHigh`
   - `NewsBanner.PositiveBannerColor` → green variant of CRT theme
   - `NewsBanner.NegativeBannerColor` → `CRTThemeData.Danger`
   - `ChartSetup.BackgroundColor` → `CRTThemeData.Background`
4. **URP Post-Processing:** Bloom intensity increased slightly for phosphor glow on bright green text (adjust URP Volume — may need programmatic Volume setup)
5. **TierVisualData Integration:** Tier themes layer OVER the CRT base theme (tier accent color tints the chart line and accents; CRT base colors remain for panels/text)
6. No performance regressions — overlay is static imagery, not per-frame computation

## Tasks / Subtasks

- [ ] Task 1: Create CRT Overlay method `UISetup.ExecuteCRTOverlay()` (AC: 1)
  - [ ] 1.1: Create CRTOverlayCanvas (ScreenSpaceOverlay, sortingOrder=999)
  - [ ] 1.2: Disable GraphicRaycaster (no input blocking)
  - [ ] 1.3: Create vignette panel — full-screen Image with gradient (dark edges, transparent center)
  - [ ] 1.4: Create scanline panel — full-screen Image with horizontal line pattern (3px spacing, 5-10% opacity)
  - [ ] 1.5: Set both images to raycastTarget=false
  - [ ] 1.6: Add configuration values to CRTThemeData (ScanlineOpacity, VignetteIntensity)
- [ ] Task 2: Generate vignette and scanline textures procedurally (AC: 1)
  - [ ] 2.1: Create procedural vignette texture (radial gradient, dark edges)
  - [ ] 2.2: Create procedural scanline texture (horizontal lines, repeating pattern)
  - [ ] 2.3: Apply textures to Image sprites via Sprite.Create()
  - [ ] 2.4: Ensure textures are created once and cached (no per-frame allocation)
- [ ] Task 3: Add panel border styling to Control Deck (AC: 2)
  - [ ] 3.1: Add border outlines to Control_Deck_Panel (nested Image with CRTThemeData.Border)
  - [ ] 3.2: Add border outlines to Left_Wing, Center_Core, Right_Wing panels
  - [ ] 3.3: Borders should be 1px, subtle, non-obstructive
- [ ] Task 4: Migrate UISetup color constants (AC: 3)
  - [ ] 4.1: Replace `BarBackgroundColor` usages with `CRTThemeData.Panel`
  - [ ] 4.2: Replace `LabelColor` usages with `CRTThemeData.TextLow`
  - [ ] 4.3: Replace `ValueColor` usages with `CRTThemeData.TextHigh`
  - [ ] 4.4: Replace `NeonGreen` usages with `CRTThemeData.TextHigh`
  - [ ] 4.5: Remove old constant declarations from UISetup (or keep as deprecated aliases)
- [ ] Task 5: Migrate TradingHUD color constants (AC: 3)
  - [ ] 5.1: Replace `ProfitGreen` with `CRTThemeData.TextHigh`
  - [ ] 5.2: Replace `LossRed` with `CRTThemeData.Danger`
  - [ ] 5.3: Replace `WarningYellow` with `CRTThemeData.Warning`
  - [ ] 5.4: Update `GetProfitColor()` and `GetTargetBarColor()` to use CRTThemeData colors
  - [ ] 5.5: Note: TradingHUD.ProfitGreen is referenced by UISetup.cs (line 91) — update that reference too
- [ ] Task 6: Migrate PositionOverlay color constants (AC: 3)
  - [ ] 6.1: Replace `LongColor` with `CRTThemeData.TextHigh`
  - [ ] 6.2: Replace `ShortColor` — decide new CRT-theme short color (amber or keep distinct)
  - [ ] 6.3: Replace `ProfitGreen` / `LossRed` with CRTThemeData equivalents
  - [ ] 6.4: Update `GetPnLColor()` to use CRTThemeData colors
- [ ] Task 7: Migrate NewsBanner color constants (AC: 3)
  - [ ] 7.1: Replace `PositiveBannerColor` with CRT green variant (CRTThemeData.TextHigh with alpha)
  - [ ] 7.2: Replace `NegativeBannerColor` with `CRTThemeData.Danger` with alpha
  - [ ] 7.3: Update `GetBannerColor()` static method
- [ ] Task 8: Migrate ChartSetup color constants (AC: 3)
  - [ ] 8.1: Replace `BackgroundColor` with `CRTThemeData.Background`
  - [ ] 8.2: Update camera clear color or chart background panel to CRT background
- [ ] Task 9: Migrate other scattered color references (AC: 3)
  - [ ] 9.1: Search codebase for remaining hardcoded neon-green (#00FF88) references
  - [ ] 9.2: Search for remaining hardcoded navy-blue background references
  - [ ] 9.3: Update RoundTimerUI color constants (NormalColor, UrgencyColor, CriticalColor)
  - [ ] 9.4: Update QuantitySelector color constants (ActiveButtonColor, InactiveButtonColor)
- [ ] Task 10: URP Bloom post-processing (AC: 4)
  - [ ] 10.1: Check if URP Volume exists in scene; if not, create programmatically
  - [ ] 10.2: Increase Bloom intensity slightly (to make phosphor green text "glow")
  - [ ] 10.3: Set Bloom threshold to catch bright green text but not blow out other elements
  - [ ] 10.4: Test that glow effect is visible but not overwhelming
- [ ] Task 11: TierVisualData integration (AC: 5)
  - [ ] 11.1: Ensure tier themes only override chart line color and accent highlights
  - [ ] 11.2: CRT base colors (Panel, TextHigh, TextLow, Background) remain constant across tiers
  - [ ] 11.3: Update `TradingHUD.ApplyTierTheme()` to only tint Control Deck accents, not base panels
  - [ ] 11.4: Update `TierVisualData.ToChartVisualConfig()` if needed for CRT compatibility
- [ ] Task 12: Performance verification (AC: 6)
  - [ ] 12.1: Verify overlay textures are created once, not per-frame
  - [ ] 12.2: Verify no additional draw calls beyond 2 (vignette + scanlines)
  - [ ] 12.3: Profile frame time before/after overlay to ensure no measurable impact

## Dev Notes

### Architecture Compliance

- **Static overlay:** The CRT bezel/scanline overlay is purely visual — no logic, no updates, no event subscriptions. It's a set-it-and-forget-it canvas with two Images.
- **Color migration:** This is a pure find-and-replace operation per constant. Each old constant becomes a reference to CRTThemeData. The static readonly pattern means no runtime allocation difference.
- **Bloom post-processing:** URP Bloom works on the full rendered frame. Bright phosphor-green UI text (and chart lines) will naturally glow without per-element configuration.

### Vignette Texture Generation

Create a small texture (e.g., 256x256) with radial gradient:
```csharp
var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
for (int y = 0; y < 256; y++)
    for (int x = 0; x < 256; x++)
    {
        float dx = (x - 128f) / 128f;
        float dy = (y - 128f) / 128f;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        float alpha = Mathf.Clamp01((dist - 0.5f) * 1.5f); // Fade starts at 50% from center
        tex.SetPixel(x, y, new Color(0, 0, 0, alpha * 0.6f));
    }
tex.Apply();
```

### Scanline Texture Generation

Create a small repeating texture (e.g., 1x6) tiled across screen:
```csharp
var tex = new Texture2D(1, 6, TextureFormat.RGBA32, false);
tex.SetPixel(0, 0, new Color(0, 0, 0, 0.08f)); // Dark line
tex.SetPixel(0, 1, Color.clear);
tex.SetPixel(0, 2, Color.clear);
tex.SetPixel(0, 3, new Color(0, 0, 0, 0.08f)); // Dark line
tex.SetPixel(0, 4, Color.clear);
tex.SetPixel(0, 5, Color.clear);
tex.wrapMode = TextureWrapMode.Repeat;
tex.filterMode = FilterMode.Point;
tex.Apply();
```

Set Image UV rect to tile across screen: `image.uvRect = new Rect(0, 0, 1, Screen.height / 6f);`

### Color Constant Migration Map

| Old Location | Old Constant | New Reference |
|-------------|-------------|--------------|
| UISetup.cs:19 | BarBackgroundColor | CRTThemeData.Panel |
| UISetup.cs:21 | LabelColor | CRTThemeData.TextLow |
| UISetup.cs:22 | ValueColor | CRTThemeData.TextHigh |
| UISetup.cs:23 | NeonGreen | CRTThemeData.TextHigh |
| TradingHUD.cs:11 | ProfitGreen | CRTThemeData.TextHigh |
| TradingHUD.cs:12 | LossRed | CRTThemeData.Danger |
| TradingHUD.cs:13 | WarningYellow | CRTThemeData.Warning |
| PositionOverlay.cs:12 | LongColor | CRTThemeData.TextHigh |
| PositionOverlay.cs:13 | ShortColor | CRTThemeData.Warning (amber) or keep distinct |
| PositionOverlay.cs:15 | ProfitGreen | CRTThemeData.TextHigh |
| PositionOverlay.cs:16 | LossRed | CRTThemeData.Danger |
| NewsBanner.cs:13 | PositiveBannerColor | CRTThemeData.TextHigh (with 0.8 alpha) |
| NewsBanner.cs:14 | NegativeBannerColor | CRTThemeData.Danger (with 0.8 alpha) |
| ChartSetup.cs:15 | BackgroundColor | CRTThemeData.Background |
| RoundTimerUI.cs:17 | NormalColor | CRTThemeData.TextHigh |
| RoundTimerUI.cs:18 | UrgencyColor | CRTThemeData.Warning |
| RoundTimerUI.cs:19 | CriticalColor | CRTThemeData.Danger |
| QuantitySelector.cs:11 | ActiveButtonColor | CRTThemeData-derived green |
| QuantitySelector.cs:12 | InactiveButtonColor | CRTThemeData.Panel |

### External References to Constants

Before removing old constants, search for all references. For example, `TradingHUD.ProfitGreen` is used in:
- `UISetup.cs:91` — portfolio change label color
- `TradingHUD.cs:248` — GetProfitColor()

These must be updated to `CRTThemeData.TextHigh` simultaneously.

### TierVisualData Layering Strategy

Current tier themes change:
- `TradingHUD._topBarBackground.color` → Now `ControlDeckPanel` background tint
- Chart line color via `ChartLineView.ApplyTierTheme()`

After CRT integration:
- Control Deck panel background stays `CRTThemeData.Panel` (no tier tinting on dashboard)
- Chart line color: tier theme applied ON TOP of CRT base (this already works)
- Chart background: stays `CRTThemeData.Background` (not tier-tinted)

Minimal change needed — just remove `TradingHUD.ApplyTierTheme()` background tinting since dashboard uses fixed CRT colors.

### URP Bloom Setup

If no Volume exists:
```csharp
var volumeGo = new GameObject("CRTBloomVolume");
var volume = volumeGo.AddComponent<UnityEngine.Rendering.Volume>();
volume.isGlobal = true;
var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>();
bloom.intensity.Override(0.5f);
bloom.threshold.Override(0.8f);
volume.profile = profile;
```

Note: This requires `using UnityEngine.Rendering` and `using UnityEngine.Rendering.Universal`. Check if these are already imported in the project.

### Testing Approach

- Visual testing: CRT vignette visible as dark corners/edges. Scanlines visible as subtle horizontal lines.
- Input testing: Verify clicking buttons still works through the overlay (raycast disabled).
- Color consistency: All text should now use CRT theme colors — phosphor green for values, dim cyan for labels.
- Bloom: Bright green text should have a subtle glow/bloom effect.
- Performance: No frame time increase from overlay (profile if concerned).
- Tier transitions: Changing acts should only affect chart line color, not dashboard panel colors.

### GameRunner.Start() Final Flow (After All Epic 14 Stories)

```csharp
// 1. Chart (repositioned)
ChartSetup.Execute();

// 2. Control Deck (replaces old HUD + trade panel + position overlay)
var dashRefs = UISetup.ExecuteControlDeck(_ctx, _ctx.CurrentRound, GameConfig.RoundDurationSeconds);

// 3. CRT Overlay (bezel + scanlines on top)
UISetup.ExecuteCRTOverlay();

// 4. Other overlays (MarketOpen, RoundResults, RunSummary, etc.)
UISetup.ExecuteMarketOpenUI();
// ... etc
```

### References

- [Source: _bmad-output/planning-artifacts/epic-14-terminal-1999-ui.md#Story 14.6]
- [Source: Assets/Scripts/Setup/UISetup.cs:16-23] — color constants to migrate
- [Source: Assets/Scripts/Setup/ChartSetup.cs:15] — BackgroundColor to migrate
- [Source: Assets/Scripts/Runtime/UI/TradingHUD.cs:11-13] — ProfitGreen/LossRed/WarningYellow to migrate
- [Source: Assets/Scripts/Runtime/UI/TradingHUD.cs:205-223] — ApplyTierTheme to update
- [Source: Assets/Scripts/Runtime/UI/PositionOverlay.cs:12-16] — LongColor/ShortColor/ProfitGreen/LossRed to migrate
- [Source: Assets/Scripts/Runtime/UI/NewsBanner.cs:13-14] — banner colors to migrate
- [Source: Assets/Scripts/Runtime/UI/RoundTimerUI.cs:17-19] — NormalColor/UrgencyColor/CriticalColor to migrate
- [Source: Assets/Scripts/Runtime/UI/QuantitySelector.cs:11-12] — ActiveButtonColor/InactiveButtonColor to migrate
- [Source: Assets/Scripts/Setup/Data/CRTThemeData.cs] — all CRT color constants
- [Source: Assets/Scripts/Setup/Data/TierVisualData.cs] — tier theme integration

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
