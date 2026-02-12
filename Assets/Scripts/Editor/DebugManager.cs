#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
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
    private ChartRenderer _chartRenderer;
    private GUIStyle _headerStyle;
    private GUIStyle _stockStyle;
    private GUIStyle _eventStyle;
    private GUIStyle _chartStyle;
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

    /// <summary>
    /// Injects the ChartRenderer reference for chart debug data.
    /// </summary>
    public void SetChartRenderer(ChartRenderer chartRenderer)
    {
        _chartRenderer = chartRenderer;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // F1: Toggle debug overlay
        if (keyboard.f1Key.wasPressedThisFrame)
        {
            _isOverlayVisible = !_isOverlayVisible;
            IsOverlayVisible = _isOverlayVisible;
            Debug.Log($"[Debug] Overlay {(_isOverlayVisible ? "ON" : "OFF")}");
        }

        // F2: God mode (placeholder)
        if (keyboard.f2Key.wasPressedThisFrame)
        {
            Debug.Log("[Debug] God mode not yet implemented");
        }

        // F3: Skip to round (placeholder)
        if (keyboard.f3Key.wasPressedThisFrame)
        {
            Debug.Log("[Debug] Skip to round not yet implemented");
        }

        // F4: Event trigger (placeholder)
        if (keyboard.f4Key.wasPressedThisFrame)
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

        _chartStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.4f, 0.8f, 1f) }
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

        InitStyles();

        float panelWidth = 360f;
        float panelX = Screen.width - panelWidth - 10f;
        float panelY = 10f;

        // Calculate panel height dynamically
        float panelHeight = 30f;
        if (_chartRenderer != null) panelHeight += 80f;
        if (_priceGenerator != null) panelHeight += _priceGenerator.GetDebugInfo().Count * 60f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", _bgStyle);

        GUILayout.BeginArea(new Rect(panelX + 8, panelY + 5, panelWidth - 16, panelHeight - 10));
        GUILayout.Label("=== DEBUG OVERLAY (F1) ===", _headerStyle);

        // Chart debug section
        if (_chartRenderer != null)
        {
            GUILayout.Label("--- CHART ---", _chartStyle);
            GUILayout.Label($"  ActiveStock: {_chartRenderer.ActiveStockId} | Points: {_chartRenderer.PointCount}", _chartStyle);
            GUILayout.Label($"  Price: ${_chartRenderer.CurrentPrice:F2} | Range: ${_chartRenderer.MinPrice:F2}-${_chartRenderer.MaxPrice:F2}", _chartStyle);
            GUILayout.Label($"  Elapsed: {_chartRenderer.ElapsedTime:F1}s / {_chartRenderer.RoundDuration:F0}s | Markers: {_chartRenderer.TradeMarkers.Count} | BEP: {(_chartRenderer.HasOpenPosition ? $"${_chartRenderer.AverageBuyPrice:F2}" : "none")}", _chartStyle);
        }

        // Price engine section
        if (_priceGenerator != null)
        {
            var debugInfos = _priceGenerator.GetDebugInfo();
            foreach (var info in debugInfos)
            {
                string arrow = info.TrendDirection == TrendDirection.Bull ? "\u25B2" :
                               info.TrendDirection == TrendDirection.Bear ? "\u25BC" : "\u25C6";

                GUILayout.Label($"{arrow} {info.Ticker} ${info.CurrentPrice:F2} | {info.TrendDirection} | Trend: {info.TrendPerSecond:F3}/s", _stockStyle);
                GUILayout.Label($"  TrendLine: ${info.TrendLinePrice:F2} | Noise: {info.NoiseAmplitude:F3} | Seg: {info.SegmentSlope:F3} ({info.SegmentTimeRemaining:F2}s) | Revert: {info.ReversionSpeed:F2}", _stockStyle);

                if (info.HasActiveEvent)
                {
                    GUILayout.Label($"  EVENT: {info.ActiveEventType} ({info.EventTimeRemaining:F1}s remaining)", _eventStyle);
                }
            }
        }

        GUILayout.EndArea();
    }
}
#endif
