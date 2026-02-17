using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// panel-ui-bug: Temporary debug component that logs panel widths every second
/// to diagnose layout shifting when positions appear/disappear.
/// </summary>
public class LayoutDebugMonitor : MonoBehaviour
{
    private RectTransform _left;
    private RectTransform _center;
    private RectTransform _right;
    private RectTransform _parent;
    private float _logTimer;
    private int _logCount;

    public void Initialize(RectTransform left, RectTransform center, RectTransform right, RectTransform parent)
    {
        _left = left;
        _center = center;
        _right = right;
        _parent = parent;
    }

    private void LateUpdate()
    {
        if (_left == null) return;

        _logTimer += Time.deltaTime;
        if (_logTimer >= 1f)
        {
            _logTimer = 0f;
            _logCount++;

            var leftLE = _left.GetComponent<LayoutElement>();
            var centerLE = _center.GetComponent<LayoutElement>();
            var rightLE = _right.GetComponent<LayoutElement>();

            Debug.Log($"[panel-ui-bug] #{_logCount} Parent width={_parent.rect.width:F1}" +
                $" | Left: rect={_left.rect.width:F1} min={leftLE.minWidth} flex={leftLE.flexibleWidth} pref={leftLE.preferredWidth}" +
                $" | Center: rect={_center.rect.width:F1} min={centerLE.minWidth} flex={centerLE.flexibleWidth} pref={centerLE.preferredWidth}" +
                $" | Right: rect={_right.rect.width:F1} min={rightLE.minWidth} flex={rightLE.flexibleWidth} pref={rightLE.preferredWidth}");

            // Also log child count of position container to see when positions appear
            var posContainer = _right.GetComponentInChildren<VerticalLayoutGroup>();
            if (posContainer != null)
            {
                Debug.Log($"[panel-ui-bug] #{_logCount} Right wing children: {_right.childCount}" +
                    $" | PosContainer children: {posContainer.transform.childCount}" +
                    $" | PosContainer rect: {((RectTransform)posContainer.transform).rect.width:F1}x{((RectTransform)posContainer.transform).rect.height:F1}");
            }

            // Log the parent HLG settings
            var hlg = _parent.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null && _logCount == 1)
            {
                Debug.Log($"[panel-ui-bug] HLG: forceExpandW={hlg.childForceExpandWidth} forceExpandH={hlg.childForceExpandHeight}" +
                    $" spacing={hlg.spacing} padding=({hlg.padding.left},{hlg.padding.right},{hlg.padding.top},{hlg.padding.bottom})");
            }
        }
    }
}
