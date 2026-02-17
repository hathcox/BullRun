#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Debug overlay manager. F1 toggles price engine overlay.
/// F2 toggles god mode (infinite cash, skip margin calls).
/// F3 opens skip-to-round selector for testing late-game rounds.
/// F4 skips directly to the ShopState (liquidates open positions first).
/// Wrapped in UNITY_EDITOR || DEVELOPMENT_BUILD — excluded from release builds.
/// </summary>
public class DebugManager : MonoBehaviour
{
    private bool _isOverlayVisible;
    private PriceGenerator _priceGenerator;
    private ChartRenderer _chartRenderer;
    private RunContext _runContext;
    private GameStateMachine _stateMachine;
    private TradeExecutor _tradeExecutor;
    private GUIStyle _headerStyle;
    private GUIStyle _stockStyle;
    private GUIStyle _eventStyle;
    private GUIStyle _chartStyle;
    private GUIStyle _bgStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _godModeStyle;
    private bool _stylesInitialized;

    private bool _showRoundSelector;
    private bool _isGodMode;

    public static bool IsOverlayVisible { get; private set; }
    public static bool IsGodMode { get; private set; }

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

    /// <summary>
    /// Injects game context references for F3 skip-to-round functionality.
    /// </summary>
    public void SetGameContext(RunContext runContext, GameStateMachine stateMachine, TradeExecutor tradeExecutor)
    {
        _runContext = runContext;
        _stateMachine = stateMachine;
        _tradeExecutor = tradeExecutor;
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

        // F2: Toggle god mode (infinite cash, skip margin calls)
        if (keyboard.f2Key.wasPressedThisFrame)
        {
            _isGodMode = !_isGodMode;
            IsGodMode = _isGodMode;

            if (_isGodMode && _runContext != null)
            {
                _runContext.Portfolio.SetCash(999999999f);
            }

            Debug.Log($"[Debug] God mode {(_isGodMode ? "ON" : "OFF")}");
        }

        // F3: Toggle skip-to-round selector
        if (keyboard.f3Key.wasPressedThisFrame)
        {
            _showRoundSelector = !_showRoundSelector;
            Debug.Log($"[Debug] Round selector {(_showRoundSelector ? "ON" : "OFF")}");
        }

        // God mode: keep cash topped up each frame
        if (_isGodMode && _runContext != null && _runContext.Portfolio.Cash < 100000000f)
        {
            _runContext.Portfolio.SetCash(999999999f);
        }

        // F4: Skip to shop — liquidate positions and jump straight to ShopState
        if (keyboard.f4Key.wasPressedThisFrame)
        {
            SkipToShop();
        }
    }

    /// <summary>
    /// Jumps to the specified round. Sets RunContext to the correct act/round,
    /// resets portfolio with debug starting cash, and transitions to MarketOpenState.
    /// </summary>
    public void JumpToRound(int roundNumber)
    {
        if (_runContext == null || _stateMachine == null)
        {
            Debug.LogWarning("[Debug] Cannot jump to round — game context not set");
            return;
        }

        if (roundNumber < 1 || roundNumber > GameConfig.TotalRounds)
        {
            Debug.LogWarning($"[Debug] Invalid round number: {roundNumber}. Must be 1-{GameConfig.TotalRounds}");
            return;
        }

        // Liquidate any open positions before jumping
        _runContext.Portfolio.UnsubscribeFromPriceUpdates();

        // Determine act for the target round
        int targetAct = RunContext.GetActForRound(roundNumber);

        // Get debug starting cash (0-indexed array)
        float debugCash = GameConfig.DebugStartingCash[roundNumber - 1];

        // Reset RunContext
        _runContext.CurrentAct = targetAct;
        _runContext.CurrentRound = roundNumber;
        _runContext.Portfolio = new Portfolio(debugCash);
        _runContext.Portfolio.SubscribeToPriceUpdates();
        _runContext.Portfolio.StartRound(debugCash);
        _runContext.StartingCapital = debugCash;

        // Transition to MarketOpenState
        MarketOpenState.NextConfig = new MarketOpenStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor
        };
        _stateMachine.TransitionTo<MarketOpenState>();

        _showRoundSelector = false;

        Debug.Log($"[Debug] Jumped to Round {roundNumber} (Act {targetAct}, " +
                  $"Tier {RunContext.GetTierForAct(targetAct)}, Cash ${debugCash:F0})");
    }

    /// <summary>
    /// Liquidates open positions and transitions directly to ShopState.
    /// F4 debug shortcut for testing the store without waiting for the round to end.
    /// </summary>
    private void SkipToShop()
    {
        if (_runContext == null || _stateMachine == null)
        {
            Debug.LogWarning("[Debug] Cannot skip to shop — game context not set");
            return;
        }

        // Liquidate any open positions at current prices
        _runContext.Portfolio.LiquidateAllPositions(stockId =>
        {
            if (_priceGenerator != null && int.TryParse(stockId, out int parsedId))
            {
                for (int i = 0; i < _priceGenerator.ActiveStocks.Count; i++)
                {
                    if (_priceGenerator.ActiveStocks[i].StockId == parsedId)
                        return _priceGenerator.ActiveStocks[i].CurrentPrice;
                }
            }
            return 0f;
        });

        // Grant debug reputation so everything is purchasable
        _runContext.Reputation.Add(99999);

        ShopState.NextConfig = new ShopStateConfig
        {
            StateMachine = _stateMachine,
            PriceGenerator = _priceGenerator,
            TradeExecutor = _tradeExecutor
        };
        _stateMachine.TransitionTo<ShopState>();

        Debug.Log($"[Debug] Skipped to ShopState (Round {_runContext.CurrentRound}, Rep={_runContext.Reputation.Current})");
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

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        _godModeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.84f, 0f) } // Gold
        };

        _stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        if (_isGodMode) DrawGodModeIndicator();
        if (_isOverlayVisible) DrawDebugOverlay();
        if (_showRoundSelector) DrawRoundSelector();
    }

    private void DrawGodModeIndicator()
    {
        GUI.Label(new Rect(10f, 10f, 200f, 30f), "GOD MODE", _godModeStyle);
    }

    private void DrawDebugOverlay()
    {
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

    private void DrawRoundSelector()
    {
        float panelWidth = 260f;
        float panelHeight = 320f;
        float panelX = (Screen.width - panelWidth) / 2f;
        float panelY = (Screen.height - panelHeight) / 2f;

        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", _bgStyle);

        GUILayout.BeginArea(new Rect(panelX + 10, panelY + 10, panelWidth - 20, panelHeight - 20));
        GUILayout.Label("JUMP TO ROUND (F3)", _headerStyle);
        GUILayout.Space(8f);

        float[] allTargets = MarginCallTargets.GetAllTargets();

        for (int round = 1; round <= GameConfig.TotalRounds; round++)
        {
            int act = RunContext.GetActForRound(round);
            float debugCash = GameConfig.DebugStartingCash[round - 1];
            float target = allTargets[round - 1];
            string label = $"Round {round} (Act {act}) — ${debugCash:N0} / Target ${target:N0}";

            if (GUILayout.Button(label, _buttonStyle))
            {
                JumpToRound(round);
            }
        }

        GUILayout.Space(8f);
        if (GUILayout.Button("Cancel", _buttonStyle))
        {
            _showRoundSelector = false;
        }

        GUILayout.EndArea();
    }
}
#endif
