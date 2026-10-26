using UnityEngine;

/// <summary>
/// Screen.safeArea에 맞춰 RectTransform 앵커를 조정한다.
/// 노치·펀치홀·라운드 코너 등 안전 영역 밖으로 UI가 나가지 않게 한다.
/// Canvas 직하위 전체 스트레치 패널에 붙이는 것을 권장한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    [SerializeField] private bool _conformX = true;
    [SerializeField] private bool _conformY = true;

    [Tooltip("가로/세로 비율 상한. 0이면 제한하지 않는다. 초광폭 화면에서 양 끝 UI가 너무 벌어지지 않게 가운데로 모은다.")]
    [SerializeField] private float _maxAspect;

    private RectTransform _rt;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private ScreenOrientation _lastOrientation = (ScreenOrientation)(-1);

    private void Awake()
    {
        TryGetComponent(out _rt);
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (HasSafeAreaChanged())
            ApplySafeArea();
    }

    private bool HasSafeAreaChanged()
    {
        return _lastSafeArea != Screen.safeArea
            || _lastScreenSize.x != Screen.width
            || _lastScreenSize.y != Screen.height
            || _lastOrientation != Screen.orientation;
    }

    private void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
        ScreenOrientation orientation = Screen.orientation;

        _lastSafeArea = safeArea;
        _lastScreenSize = screenSize;
        _lastOrientation = orientation;

        if (screenSize.x <= 0 || screenSize.y <= 0)
            return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        if (!_conformX)
        {
            anchorMin.x = 0f;
            anchorMax.x = 1f;
        }

        if (!_conformY)
        {
            anchorMin.y = 0f;
            anchorMax.y = 1f;
        }

        if (_maxAspect > 0f)
        {
            float width = (anchorMax.x - anchorMin.x) * screenSize.x;
            float maxWidth = (anchorMax.y - anchorMin.y) * screenSize.y * _maxAspect;
            if (width > maxWidth)
            {
                float inset = (width - maxWidth) * 0.5f / screenSize.x;
                anchorMin.x += inset;
                anchorMax.x -= inset;
            }
        }

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
