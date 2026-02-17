using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terminal 1999 CRT theme colors and styling helpers.
/// All values are hex-accurate constants for the cockpit UI overhaul (Epic 14).
/// </summary>
public static class CRTThemeData
{
    // ── Background & Structure ──────────────────────────────────────────
    public static readonly Color Background  = new Color(5 / 255f, 10 / 255f, 10 / 255f, 1f);      // #050a0a
    public static readonly Color Panel       = new Color(6 / 255f, 24 / 255f, 24 / 255f, 0.9f);     // #061818 @ 90%
    public static readonly Color Border      = new Color(34 / 255f, 68 / 255f, 68 / 255f, 1f);      // #224444

    // ── Text ────────────────────────────────────────────────────────────
    public static readonly Color TextHigh    = new Color(40 / 255f, 245 / 255f, 141 / 255f, 1f);    // #28f58d  phosphor green
    public static readonly Color TextLow     = new Color(59 / 255f, 110 / 255f, 110 / 255f, 1f);    // #3b6e6e  dim green

    // ── Accent / Action ─────────────────────────────────────────────────
    public static readonly Color Warning     = new Color(1f, 184 / 255f, 0f, 1f);                   // #ffb800
    public static readonly Color Danger      = new Color(1f, 68 / 255f, 68 / 255f, 1f);             // #ff4444

    // ── CRT Overlay ──────────────────────────────────────────────────────
    public static readonly float ScanlineOpacity   = 0.08f;  // 5-10% opacity for scanline dark lines
    public static readonly float VignetteIntensity = 0.6f;   // Max alpha at vignette edges

    // ── Button Colors ───────────────────────────────────────────────────
    public static readonly Color ButtonBuy   = new Color(40 / 255f, 245 / 255f, 141 / 255f, 1f);    // #28f58d
    public static readonly Color ButtonSell  = new Color(1f, 68 / 255f, 68 / 255f, 1f);             // #ff4444
    public static readonly Color ButtonShort = new Color(1f, 184 / 255f, 0f, 1f);                   // #ffb800

    // ── Styling Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Apply CRT label styling to a Text element.
    /// Highlight = phosphor green (TextHigh), dim = muted green (TextLow).
    /// </summary>
    public static void ApplyLabelStyle(Text text, bool highlight)
    {
        if (text == null) return;
        text.color = highlight ? TextHigh : TextLow;
    }

    /// <summary>
    /// Apply CRT panel styling to an Image — Panel fill with Border outline.
    /// Sets the image color to Panel and adds a border-colored outline via Outline component.
    /// </summary>
    public static void ApplyPanelStyle(Image image)
    {
        if (image == null) return;
        image.color = Panel;

        var outline = image.GetComponent<Outline>();
        if (outline == null)
            outline = image.gameObject.AddComponent<Outline>();
        outline.effectColor = Border;
        outline.effectDistance = new Vector2(1f, -1f);
    }
}
