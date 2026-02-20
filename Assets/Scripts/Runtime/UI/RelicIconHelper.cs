using UnityEngine;

/// <summary>
/// Story 17.10: Runtime helper for parsing RelicDef hex color strings to UnityEngine.Color.
/// Keeps RelicDef in Scripts/Setup/Data/ free of UnityEngine.Color references.
/// </summary>
public static class RelicIconHelper
{
    /// <summary>
    /// Parses a "#RRGGBB" hex string to a Color. Returns Color.white if parsing fails.
    /// </summary>
    public static Color ParseHexColor(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            Debug.LogWarning("[RelicIconHelper] Empty hex color string, falling back to white");
            return Color.white;
        }

        if (ColorUtility.TryParseHtmlString(hex, out var color))
            return color;

        Debug.LogWarning($"[RelicIconHelper] Failed to parse hex color '{hex}', falling back to white");
        return Color.white;
    }

    /// <summary>
    /// Convenience wrapper: parses the IconColorHex from a RelicDef.
    /// </summary>
    public static Color GetIconColor(RelicDef def)
    {
        return ParseHexColor(def.IconColorHex);
    }
}
