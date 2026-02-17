using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terminal 1999 CRT theme colors and styling helpers.
/// Thin wrapper around ColorPalette for UI styling convenience.
/// </summary>
public static class CRTThemeData
{
    // ── Background & Structure ──────────────────────────────────────────
    public static readonly Color Background  = ColorPalette.Background;
    public static readonly Color Panel       = ColorPalette.Panel;
    public static readonly Color Border      = ColorPalette.Border;

    // ── Text ────────────────────────────────────────────────────────────
    public static readonly Color TextHigh    = ColorPalette.Green;
    public static readonly Color TextLow     = ColorPalette.GreenDim;

    // ── Accent / Action ─────────────────────────────────────────────────
    public static readonly Color Warning     = ColorPalette.Amber;
    public static readonly Color Danger      = ColorPalette.Red;

    // ── CRT Overlay ──────────────────────────────────────────────────────
    public static readonly float ScanlineOpacity   = 0.08f;
    public static readonly float VignetteIntensity = 0.6f;

    // ── Button Colors ───────────────────────────────────────────────────
    public static readonly Color ButtonBuy   = ColorPalette.Green;
    public static readonly Color ButtonSell  = ColorPalette.Red;
    public static readonly Color ButtonShort = ColorPalette.Amber;

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
