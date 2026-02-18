using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen visual effects for dramatic market events.
/// MarketCrash: screen shake + red pulse. BullRun: green tint. FlashCrash: red flash.
/// Subscribes to EventPopupCompletedEvent (effects start after popup) and MarketEventEndedEvent (effects stop).
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    public static readonly Color CrashRedPulse = ColorPalette.WithAlpha(ColorPalette.Red, 0.3f);
    public static readonly Color BullGreenTint = ColorPalette.WithAlpha(ColorPalette.Green, 0.15f);
    public static readonly Color FlashRed = ColorPalette.WithAlpha(ColorPalette.Red, 0.5f);
    public static readonly float ShakeDuration = 3f;
    public static readonly float ShakeIntensity = 40f;
    public static readonly float RedPulseDuration = 5f;
    public static readonly float FlashDuration = 0.5f;

    private Image _redPulseImage;
    private Image _greenTintImage;
    private Image _flashImage;
    private RectTransform _shakeTransform;
    private Vector3 _originalPosition;

    // Effect state
    private float _shakeElapsed;
    private bool _shaking;
    private float _redPulseElapsed;
    private bool _redPulsing;
    private float _greenTintElapsed;
    private bool _greenTinting;
    private float _flashElapsed;
    private bool _flashing;

    public bool IsShaking => _shaking;
    public bool IsRedPulsing => _redPulsing;
    public bool IsGreenTinting => _greenTinting;
    public bool IsFlashing => _flashing;

    public void Initialize(RectTransform shakeTransform, Image redPulseImage, Image greenTintImage, Image flashImage)
    {
        _shakeTransform = shakeTransform;
        _originalPosition = _shakeTransform.localPosition;
        _redPulseImage = redPulseImage;
        _greenTintImage = greenTintImage;
        _flashImage = flashImage;

        // Start invisible
        SetImageAlpha(_redPulseImage, 0f);
        SetImageAlpha(_greenTintImage, 0f);
        SetImageAlpha(_flashImage, 0f);

        // Subscribe to EventPopupCompletedEvent so effects start AFTER the popup
        // flies away and timeScale resumes (not during the popup pause)
        EventBus.Subscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Subscribe<MarketEventEndedEvent>(OnMarketEventEnded);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EventPopupCompletedEvent>(OnEventPopupCompleted);
        EventBus.Unsubscribe<MarketEventEndedEvent>(OnMarketEventEnded);
    }

    private void OnEventPopupCompleted(EventPopupCompletedEvent evt)
    {
        switch (evt.EventType)
        {
            case MarketEventType.MarketCrash:
                StartShake();
                StartRedPulse();
                break;
            case MarketEventType.BullRun:
                StartGreenTint();
                break;
            case MarketEventType.FlashCrash:
                StartFlash();
                break;
        }
    }

    private void OnMarketEventEnded(MarketEventEndedEvent evt)
    {
        switch (evt.EventType)
        {
            case MarketEventType.MarketCrash:
                StopShake();
                StopRedPulse();
                break;
            case MarketEventType.BullRun:
                StopGreenTint();
                break;
            case MarketEventType.FlashCrash:
                StopFlash();
                break;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_shaking)
        {
            _shakeElapsed += dt;
            if (_shakeElapsed < ShakeDuration)
            {
                float decay = 1f - (_shakeElapsed / ShakeDuration);
                Vector2 offset = Random.insideUnitCircle * ShakeIntensity * decay;
                _shakeTransform.localPosition = _originalPosition + (Vector3)offset;
            }
            else
            {
                StopShake();
            }
        }

        if (_redPulsing)
        {
            _redPulseElapsed += dt;
            if (_redPulseElapsed < RedPulseDuration)
            {
                // Pulse: 0 → 0.3 → 0 over duration
                float t = _redPulseElapsed / RedPulseDuration;
                float alpha = CrashRedPulse.a * Mathf.Sin(t * Mathf.PI);
                SetImageAlpha(_redPulseImage, alpha);
            }
            else
            {
                StopRedPulse();
            }
        }

        if (_greenTinting)
        {
            // Maintain steady tint (fades on event end)
            _greenTintElapsed += dt;
        }

        if (_flashing)
        {
            _flashElapsed += dt;
            if (_flashElapsed < FlashDuration)
            {
                float alpha = FlashRed.a * (1f - (_flashElapsed / FlashDuration));
                SetImageAlpha(_flashImage, alpha);
            }
            else
            {
                StopFlash();
            }
        }
    }

    private void StartShake()
    {
        if (!_shaking)
            _originalPosition = _shakeTransform.localPosition;
        _shaking = true;
        _shakeElapsed = 0f;
    }

    private void StopShake()
    {
        _shaking = false;
        if (_shakeTransform != null)
            _shakeTransform.localPosition = _originalPosition;
    }

    private void StartRedPulse()
    {
        _redPulsing = true;
        _redPulseElapsed = 0f;
        if (_redPulseImage != null)
            _redPulseImage.color = CrashRedPulse;
    }

    private void StopRedPulse()
    {
        _redPulsing = false;
        SetImageAlpha(_redPulseImage, 0f);
    }

    private void StartGreenTint()
    {
        _greenTinting = true;
        _greenTintElapsed = 0f;
        if (_greenTintImage != null)
            _greenTintImage.color = BullGreenTint;
    }

    private void StopGreenTint()
    {
        _greenTinting = false;
        SetImageAlpha(_greenTintImage, 0f);
    }

    private void StartFlash()
    {
        _flashing = true;
        _flashElapsed = 0f;
        if (_flashImage != null)
            _flashImage.color = FlashRed;
    }

    private void StopFlash()
    {
        _flashing = false;
        SetImageAlpha(_flashImage, 0f);
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        var c = image.color;
        c.a = alpha;
        image.color = c;
    }

    /// <summary>
    /// Maps event type to screen effect type.
    /// Static for testability.
    /// </summary>
    public static ScreenEffectType GetEffectType(MarketEventType eventType)
    {
        switch (eventType)
        {
            case MarketEventType.MarketCrash: return ScreenEffectType.ShakeAndPulse;
            case MarketEventType.BullRun: return ScreenEffectType.GreenTint;
            case MarketEventType.FlashCrash: return ScreenEffectType.RedFlash;
            default: return ScreenEffectType.None;
        }
    }
}

public enum ScreenEffectType
{
    None,
    ShakeAndPulse,
    GreenTint,
    RedFlash
}
