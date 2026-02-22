using UnityEngine;

/// <summary>
/// Visual configuration for chart rendering: line colors, widths, glow settings.
/// Per GDD Section 7.1: neon green (#00FF88) synthwave aesthetic.
/// Widths are specified in pixels and converted to world units at runtime.
/// </summary>
public struct ChartVisualConfig
{
    public Color LineColor;
    public Color GlowColor;
    public float LineWidthPixels;
    public float GlowWidthPixels;
    public float IndicatorSize;

    /// <summary>
    /// Converts a pixel width to world units using the main camera's orthographic size.
    /// </summary>
    public float GetWorldWidth(float pixelWidth)
    {
        var cam = Camera.main;
        if (cam == null) return pixelWidth * 0.01f;
        return pixelWidth * (cam.orthographicSize * 2f / Screen.height);
    }

    // --- Tip Overlay Constants (Story 18.3) ---
    public static readonly Color OverlayFloorColor = ColorPalette.WithAlpha(ColorPalette.Cyan, 0.6f);
    public static readonly Color OverlayCeilingColor = ColorPalette.WithAlpha(ColorPalette.Amber, 0.6f);
    public static readonly Color OverlayForecastColor = ColorPalette.WithAlpha(
        new Color(0.4f, 0.3f, 0.8f, 1f), 0.18f);
    public static readonly Color OverlayEventMarkerColor = ColorPalette.WithAlpha(ColorPalette.Red, 0.4f);
    public static readonly Color OverlayDipZoneColor = ColorPalette.WithAlpha(ColorPalette.Green, 0.18f);
    public static readonly Color OverlayPeakZoneColor = ColorPalette.WithAlpha(ColorPalette.Amber, 0.18f);
    public static readonly Color OverlayReversalColor = ColorPalette.WithAlpha(ColorPalette.Magenta, 0.5f);

    public static readonly float OverlayLineWidth = 0.012f;
    public static readonly float OverlayVerticalLineWidth = 0.008f;
    public static readonly int OverlayQuadSortingOrder = -3;
    public static readonly int OverlayLineSortingOrder = 0;
    public static readonly int OverlayArrowSortingOrder = 6;
    public static readonly int MaxEventTimingMarkers = 15;
    public static readonly float OverlayLabelFontSize = 11f;
    public static readonly float OverlayLabelAlpha = 0.8f;

    /// <summary>
    /// Default synthwave aesthetic config per GDD.
    /// Line: neon green, Glow: same at 30% opacity, 2.5x wider.
    /// </summary>
    public static ChartVisualConfig Default => new ChartVisualConfig
    {
        LineColor = ColorPalette.Green,
        GlowColor = ColorPalette.WithAlpha(ColorPalette.Green, 0.3f),
        LineWidthPixels = 3f,
        GlowWidthPixels = 7.5f, // 2.5x main line
        IndicatorSize = 0.15f
    };
}
