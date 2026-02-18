using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns temporary floating text popups that drift upward and fade out.
/// Created by UISetup as a full-stretch child of the HUD Canvas.
/// Used for trade profit/loss (AC 3) and REP earned (AC 9) feedback.
/// </summary>
public class FloatingTextService : MonoBehaviour
{
    public static readonly float FloatDuration = 0.8f;
    public static readonly float FloatDistance = 60f;
    public static readonly float FadeStartFraction = 0.6f;

    private Font _font;

    public void Initialize(Font font)
    {
        _font = font;
    }

    /// <summary>
    /// Spawns a floating text popup originating from the world position of sourceRect.
    /// Converts world space → canvas local space so the popup appears at the source element
    /// regardless of where the source sits in the UI hierarchy.
    /// </summary>
    public void Spawn(string text, RectTransform sourceRect, Color color)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, sourceRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, screenPoint, cam, out Vector2 localPos);
        StartCoroutine(FloatText(text, localPos, color));
    }

    /// <summary>
    /// Spawns a floating text popup at a 3D world-space position (e.g. the chart head indicator).
    /// Uses Camera.main to project world → screen, then converts to canvas local space.
    /// </summary>
    public void SpawnAtWorldPos(string text, Vector3 worldPos, Color color)
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        Camera uiCam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Camera worldCam = Camera.main;
        Vector2 screenPoint = worldCam != null
            ? (Vector2)worldCam.WorldToScreenPoint(worldPos)
            : (Vector2)worldPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, screenPoint, uiCam, out Vector2 localPos);
        StartCoroutine(FloatText(text, localPos, color));
    }

    private IEnumerator FloatText(string text, Vector2 startPos, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.SetParent(transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = startPos;
        rect.sizeDelta = new Vector2(200f, 40f);

        var canvasGroup = go.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.color = color;
        txt.fontSize = 20;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;
        txt.font = _font != null ? _font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        float elapsed = 0f;
        Vector2 endPos = startPos + Vector2.up * FloatDistance;
        float fadeStartTime = FloatDuration * FadeStartFraction;

        while (elapsed < FloatDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FloatDuration);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            if (elapsed >= fadeStartTime)
            {
                float fadeT = (elapsed - fadeStartTime) / (FloatDuration - fadeStartTime);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeT);
            }

            yield return null;
        }

        Destroy(go);
    }
}
