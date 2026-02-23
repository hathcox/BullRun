using UnityEngine;

/// <summary>
/// Single source of truth for all colors in the game.
/// 6-color CRT-authentic palette based on split-complementary harmony.
/// Every UI file references ColorPalette instead of hardcoding hex values.
/// </summary>
public static class ColorPalette
{
    // ── 1. Teal (Background Family) ────────────────────────────────────
    public static readonly Color Background = new Color(5 / 255f, 10 / 255f, 10 / 255f, 1f);          // #050a0a
    public static readonly Color Panel      = new Color(6 / 255f, 24 / 255f, 24 / 255f, 1f);           // #061818
    public static readonly Color Border     = new Color(34 / 255f, 68 / 255f, 68 / 255f, 1f);          // #224444

    // ── 2. Green (Profit / Buy / Long / Primary Text) ──────────────────
    public static readonly Color Green      = new Color(61 / 255f, 170 / 255f, 110 / 255f, 1f);        // #3daa6e
    public static readonly Color GreenDim   = new Color(36 / 255f, 80 / 255f, 70 / 255f, 1f);          // #245046

    // ── 3. Red (Loss / Sell / Danger) ──────────────────────────────────
    public static readonly Color Red        = new Color(230 / 255f, 85 / 255f, 85 / 255f, 1f);         // #e65555
    public static readonly Color RedDim     = new Color(140 / 255f, 50 / 255f, 50 / 255f, 1f);         // #8c3232

    // ── 4. Amber (Short / Warning) ─────────────────────────────────────
    public static readonly Color Amber      = new Color(204 / 255f, 148 / 255f, 0f, 1f);               // #cc9400
    public static readonly Color AmberDim   = new Color(144 / 255f, 104 / 255f, 0f, 1f);               // #906800
    public static readonly Color Gold       = new Color(204 / 255f, 172 / 255f, 40 / 255f, 1f);        // #ccac28

    // ── 5. White (Neutral / Headers / Long Strike) ─────────────────────
    public static readonly Color White      = new Color(235 / 255f, 235 / 255f, 228 / 255f, 1f);       // #ebebe4
    public static readonly Color WhiteDim   = new Color(180 / 255f, 180 / 255f, 185 / 255f, 1f);       // #b4b4b9

    // ── 6. Cyan (Accent / Interactive / Sell Feedback) ─────────────────
    public static readonly Color Cyan       = new Color(56 / 255f, 160 / 255f, 176 / 255f, 1f);        // #38a0b0
    public static readonly Color CyanDim    = new Color(32 / 255f, 96 / 255f, 104 / 255f, 1f);         // #206068

    // ── 7. Magenta (Tip Overlays) ────────────────────────────────────
    public static readonly Color Magenta    = new Color(180 / 255f, 80 / 255f, 200 / 255f, 1f);        // #b450c8

    // ── Utility Methods ────────────────────────────────────────────────

    /// <summary>
    /// Returns the color with a new alpha value.
    /// </summary>
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Multiplies RGB channels by a factor (0-1) to create dimmed variants.
    /// Alpha is preserved.
    /// </summary>
    public static Color Dimmed(Color color, float factor)
    {
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }
}
