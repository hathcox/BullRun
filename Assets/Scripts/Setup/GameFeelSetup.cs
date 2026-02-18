using UnityEngine;
using UnityEngine.UI;
using MoreMountains.Feedbacks;

/// <summary>
/// Static setup class that creates the GameFeel canvas, flash overlays,
/// ParticleImage emitters, MMCameraShaker on the main camera, floating number pool,
/// accumulator token pool, and the GameFeelManager MonoBehaviour.
/// Called from GameRunner.Start().
/// </summary>
public static class GameFeelSetup
{
    public static void Execute()
    {
        var rootGo = new GameObject("GameFeelSystem");

        // ── 1. Canvas (ScreenSpaceOverlay, sortingOrder=15) ──────────────
        var canvasGo = new GameObject("GameFeelCanvas");
        canvasGo.transform.SetParent(rootGo.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15; // Between ScreenEffects(10) and ControlDeck(20)

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        // No GraphicRaycaster — effects should not block input

        // ── 2. Flash Overlay Images (full-screen, raycastTarget=false) ───
        var greenFlash = CreateFlashOverlay("GreenFlash", canvasGo.transform, ColorPalette.Green);
        var redFlash   = CreateFlashOverlay("RedFlash",   canvasGo.transform, ColorPalette.Red);
        var amberFlash = CreateFlashOverlay("AmberFlash", canvasGo.transform, ColorPalette.Amber);
        var whiteFlash = CreateFlashOverlay("WhiteFlash", canvasGo.transform, ColorPalette.White);
        var goldFlash  = CreateFlashOverlay("GoldFlash",  canvasGo.transform, ColorPalette.Gold);

        // ── 3. ParticleImage Emitters ────────────────────────────────────
        // Sizes are in canvas-space pixels (ref 1920x1080).
        // Speeds tuned down from original values for a tighter, more readable feel:
        //   sizes: trade 8px, celebration 2px, currency 1.5px, fullscreen 3px
        //   speeds: trade 3, celebration 35, currency 18, fullscreen 45
        //   All emitters use unscaled time so particles survive Time.timeScale=0 announcement pauses
        //                                                    name                    anchor           color                size  life  speed
        var tradeParticles = CreateParticleEmitter("TradeParticles", canvasGo.transform,
            new Vector2(0f, 0f),     ColorPalette.Green,  8f,   0.7f,  3f);

        var celebrationParticles = CreateParticleEmitter("CelebrationParticles", canvasGo.transform,
            new Vector2(0f, 400f),   ColorPalette.Gold,   2f,   1.5f,  35f);

        var currencyParticles = CreateParticleEmitter("CurrencyParticles", canvasGo.transform,
            new Vector2(-600f, -400f), ColorPalette.Gold, 1.5f, 1.2f,  18f);

        var fullScreenParticles = CreateParticleEmitter("FullScreenParticles", canvasGo.transform,
            new Vector2(0f, 0f),     ColorPalette.Red,    3f,   1.5f,  45f);
        // Make full-screen emitter cover entire screen
        var fsRect = fullScreenParticles.GetComponent<RectTransform>();
        fsRect.anchorMin = Vector2.zero;
        fsRect.anchorMax = Vector2.one;
        fsRect.offsetMin = Vector2.zero;
        fsRect.offsetMax = Vector2.zero;

        // Dust emitter: tiny fairy-dust particles at the chart head while price moves.
        // 10x smaller than trade particles, very low speed so they stay tight to the head.
        var dustParticles = CreateParticleEmitter("DustParticles", canvasGo.transform,
            new Vector2(0f, 0f),     ColorPalette.Green,  2f,   0.35f, 1.5f);

        // ── 4. Floating Number Pool ──────────────────────────────────────
        var floatingPoolGo = new GameObject("FloatingNumberPool");
        floatingPoolGo.transform.SetParent(canvasGo.transform, false);
        var poolRect = floatingPoolGo.AddComponent<RectTransform>();
        poolRect.anchorMin = Vector2.zero;
        poolRect.anchorMax = Vector2.one;
        poolRect.offsetMin = Vector2.zero;
        poolRect.offsetMax = Vector2.zero;

        var floatingTexts = new Text[8];
        for (int i = 0; i < 8; i++)
        {
            var ftGo = new GameObject($"FloatingNumber_{i}");
            ftGo.transform.SetParent(floatingPoolGo.transform, false);
            var ftRect = ftGo.AddComponent<RectTransform>();
            ftRect.sizeDelta = new Vector2(300f, 50f);

            var txt = ftGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 32;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            var cg = ftGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            ftGo.SetActive(false);
            floatingTexts[i] = txt;
        }

        // ── 5. Accumulator Token Pool ────────────────────────────────────
        // 24 text GameObjects showing "$" or "★" that fly from the chart head
        // to the cash/rep UI elements on profitable trades and round completion.
        var accPoolGo = new GameObject("AccumulatorTokenPool");
        accPoolGo.transform.SetParent(canvasGo.transform, false);
        var accPoolRect = accPoolGo.AddComponent<RectTransform>();
        accPoolRect.anchorMin = Vector2.zero;
        accPoolRect.anchorMax = Vector2.one;
        accPoolRect.offsetMin = Vector2.zero;
        accPoolRect.offsetMax = Vector2.zero;

        var accumulatorPool = new GameObject[24];
        for (int i = 0; i < 24; i++)
        {
            var tokenGo = new GameObject($"AccToken_{i}");
            tokenGo.transform.SetParent(accPoolGo.transform, false);

            var tokenRect = tokenGo.AddComponent<RectTransform>();
            tokenRect.sizeDelta = new Vector2(28f, 28f);

            var tokenTxt = tokenGo.AddComponent<Text>();
            tokenTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tokenTxt.fontSize = 22;
            tokenTxt.fontStyle = FontStyle.Bold;
            tokenTxt.alignment = TextAnchor.MiddleCenter;
            tokenTxt.color = ColorPalette.Gold;
            tokenTxt.raycastTarget = false;

            var tokenCg = tokenGo.AddComponent<CanvasGroup>();
            tokenCg.alpha = 0f;
            tokenCg.blocksRaycasts = false;
            tokenCg.interactable = false;

            tokenGo.SetActive(false);
            accumulatorPool[i] = tokenGo;
        }

        // ── 6. MMCameraShaker on Main Camera ─────────────────────────────
        var cam = Camera.main;
        if (cam != null && cam.GetComponent<MMCameraShaker>() == null)
        {
            // MMCameraShaker requires MMWiggle — AddComponent<MMCameraShaker>
            // auto-adds it via [RequireComponent]
            cam.gameObject.AddComponent<MMCameraShaker>();
        }

        // ── 7. GameFeelManager MonoBehaviour ─────────────────────────────
        // Find ChartLineView (active component) so trade particles can emit at chart head position
        var chartLineView = Object.FindFirstObjectByType<ChartLineView>();

        var manager = rootGo.AddComponent<GameFeelManager>();
        manager.Initialize(
            greenFlash, redFlash, amberFlash, whiteFlash, goldFlash,
            tradeParticles, celebrationParticles, currencyParticles, fullScreenParticles,
            dustParticles,
            floatingTexts, canvasGo.GetComponent<RectTransform>(), chartLineView,
            accumulatorPool);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[GameFeel] Setup complete: canvas(sort=15), 5 flash overlays, 4 particle emitters, " +
                  "camera shaker, floating pool(8), accumulator pool(24)");
        #endif
    }

    private static Image CreateFlashOverlay(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = ColorPalette.WithAlpha(color, 0f);
        img.raycastTarget = false;

        return img;
    }

    private static AssetKits.ParticleImage.ParticleImage CreateParticleEmitter(
        string name, Transform parent, Vector2 anchoredPos, Color color, float size, float lifetime, float speed)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(10f, 10f);

        var pi = go.AddComponent<AssetKits.ParticleImage.ParticleImage>();
        pi.PlayMode = AssetKits.ParticleImage.Enumerations.PlayMode.None;
        pi.loop = false;
        pi.rateOverTime = 0f;
        pi.startColor = new ParticleSystem.MinMaxGradient(color);
        pi.startSize = new AssetKits.ParticleImage.SeparatedMinMaxCurve(size);
        pi.lifetime = new ParticleSystem.MinMaxCurve(lifetime);
        pi.startSpeed = new ParticleSystem.MinMaxCurve(speed);
        pi.shape = AssetKits.ParticleImage.Enumerations.EmitterShape.Circle;
        pi.space = AssetKits.ParticleImage.Enumerations.Simulation.Local;
        // Unscaled time: particles continue animating during Time.timeScale=0 announcement pauses
        pi.timeScale = AssetKits.ParticleImage.Enumerations.TimeScale.Unscaled;

        // Color over lifetime: start bright, fade to transparent
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        pi.colorOverLifetime = new ParticleSystem.MinMaxGradient(gradient);

        // Size over lifetime: shrink to 20%
        pi.sizeOverLifetime = new AssetKits.ParticleImage.SeparatedMinMaxCurve(
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

        // Pre-add a single burst slot at index 0 so we can use SetBurst later (no list growth)
        pi.AddBurst(0f, 1);

        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[GameFeel] Emitter '{name}': size={size}px, life={lifetime}s, speed={speed}px/s");
        #endif

        return pi;
    }
}
