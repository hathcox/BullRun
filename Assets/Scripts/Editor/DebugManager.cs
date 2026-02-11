#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Debug overlay manager. F1 toggles price engine overlay.
/// F2-F4 reserved for future debug tools.
/// Wrapped in UNITY_EDITOR || DEVELOPMENT_BUILD — excluded from release builds.
/// </summary>
public class DebugManager : MonoBehaviour
{
    private bool _isOverlayVisible;
    private PriceGenerator _priceGenerator;
    private GUIStyle _headerStyle;
    private GUIStyle _stockStyle;
    private GUIStyle _eventStyle;
    private GUIStyle _bgStyle;
    private bool _stylesInitialized;

    public static bool IsOverlayVisible { get; private set; }

    /// <summary>
    /// Injects the PriceGenerator reference for debug data access.
    /// Called during setup or scene initialization.
    /// </summary>
    public void SetPriceGenerator(PriceGenerator priceGenerator)
    {
        _priceGenerator = priceGenerator;
    }

    private void Update()
    {
        // F1: Toggle debug overlay
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _isOverlayVisible = !_isOverlayVisible;
            IsOverlayVisible = _isOverlayVisible;
            Debug.Log($"[Debug] Overlay {(_isOverlayVisible ? "ON" : "OFF")}");
        }

        // F2: God mode (placeholder)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[Debug] God mode not yet implemented");
        }

        // F3: Skip to round (placeholder)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[Debug] Skip to round not yet implemented");
        }

        // F4: Event trigger (placeholder)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[Debug] Event trigger not yet implemented");
        }
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0f, 1f, 0.8f) }
        };

        _stockStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };

        _eventStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.6f, 0.2f) }
        };

        _bgStyle = new GUIStyle(GUI.skin.box);
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        bgTex.Apply();
        _bgStyle.normal.background = bgTex;

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        if (!_isOverlayVisible) return;
        if (_priceGenerator == null) return;

        InitStyles();

        var debugInfos = _priceGenerator.GetDebugInfo();
        if (debugInfos.Count == 0) return;

        float panelWidth = 320f;
        float panelHeight = 30f + (debugInfos.Count * 60f);
        float panelX = Screen.width - panelWidth - 10f;
        float panelY = 10f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", _bgStyle);

        GUILayout.BeginArea(new Rect(panelX + 8, panelY + 5, panelWidth - 16, panelHeight - 10));
        GUILayout.Label("=== PRICE ENGINE DEBUG (F1) ===", _headerStyle);

        foreach (var info in debugInfos)
        {
            string arrow = info.TrendDirection == TrendDirection.Bull ? "\u25B2" :
                           info.TrendDirection == TrendDirection.Bear ? "\u25BC" : "\u25C6";

            GUILayout.Label($"{arrow} {info.Ticker} ${info.CurrentPrice:F2} | {info.TrendDirection} | Trend: {info.TrendPerSecond:F3}/s", _stockStyle);
            GUILayout.Label($"  TrendLine: ${info.TrendLinePrice:F2} | Noise: {info.NoiseAmplitude:F3} | Revert: {info.ReversionSpeed:F2}", _stockStyle);

            if (info.HasActiveEvent)
            {
                GUILayout.Label($"  EVENT: {info.ActiveEventType} ({info.EventTimeRemaining:F1}s remaining)", _eventStyle);
            }
        }

        GUILayout.EndArea();
    }
}
#endif
