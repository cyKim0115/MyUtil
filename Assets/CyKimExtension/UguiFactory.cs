using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Util
{
    /// <summary>
    /// 코드로 uGUI를 세우는 기본 부품. 개발용 하네스·샘플·런타임에 개수가 정해지는 목록처럼 프리팹 없이 UI를 만들 때 쓴다.
    /// 글자는 레거시 <see cref="Text"/>다. TMP 폰트 에셋 없이 바로 그려지고, 한글처럼 글리프가 많아도 아틀라스를 따로 굽지 않는다.
    /// 폰트를 넘기지 않으면 <see cref="DefaultFont"/>(내장 LegacyRuntime.ttf)를 쓰고, 프로젝트 폰트로 바꿔 둘 수 있다.
    /// 가로 줄 높이를 고정할 때는 <c>gameObject.SetPreferredHeight(높이)</c>(UILayoutUtil)를 쓴다.
    /// </summary>
    public static class UguiFactory
    {
        private const string BUILTIN_FONT = "LegacyRuntime.ttf";

        private static Font _defaultFont;

        /// <summary>폰트를 생략했을 때 쓰는 폰트. 프로젝트 폰트를 넣어 두면 이후 만드는 글자가 모두 그 폰트를 쓴다.</summary>
        public static Font DefaultFont
        {
            get
            {
                if (_defaultFont == null)
                {
                    _defaultFont = Resources.GetBuiltinResource<Font>(BUILTIN_FONT);
                }

                return _defaultFont;
            }
            set => _defaultFont = value;
        }

        // 도메인 리로드를 끈 Enter Play Mode에서도 Play마다 기본 폰트를 다시 찾는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _defaultFont = null;
        }

        /// <summary>Screen Space Overlay 캔버스. CanvasScaler는 기준 해상도에 맞춰 늘어난다.</summary>
        public static Canvas CreateCanvas(string name, Vector2 referenceResolution, float matchWidthOrHeight = 0.5f,
            int sortingOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>EventSystem이 없으면 만든다. Input System을 쓰는 프로젝트면 InputSystemUIInputModule을 붙인다.</summary>
        public static EventSystem EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return EventSystem.current;
            }

            var go = new GameObject("EventSystem");
            var eventSystem = go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            return eventSystem;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        /// <summary>부모를 꽉 채우는 사각형. <paramref name="inset"/>만큼 네 변을 안으로 줄인다.</summary>
        public static RectTransform CreateStretched(string name, Transform parent, float inset = 0f)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect, inset);
            return rect;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>부모 기준 비율 영역(0~1)에 맞춘다.</summary>
        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>부모 위쪽에 높이 <paramref name="height"/>인 띠로 붙인다.</summary>
        public static void PlaceTop(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -height);
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>색 면. 스프라이트를 넘기면 9-slice로 그린다. 레이캐스트는 기본으로 받지 않는다(버튼 면만 받는다).</summary>
        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite = null,
            bool raycastTarget = false)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            return image;
        }

        /// <summary>레거시 Text. 영역이 글자 높이보다 조금 작아도 잘려 사라지지 않게 세로 넘침을 허용한다.</summary>
        public static Text CreateText(string name, Transform parent, string value, int fontSize, Color color,
            TextAnchor alignment = TextAnchor.MiddleCenter, Font font = null)
        {
            RectTransform rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font != null ? font : DefaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// 라벨이 붙은 버튼. 면 색(<paramref name="color"/>)에 상태 색(하이라이트·눌림)을 곱하고, 라벨은 버튼을 꽉 채운다.
        /// </summary>
        public static Button CreateButton(string label, Transform parent, Color color, Color labelColor, int fontSize,
            UnityAction onClick = null, Sprite sprite = null, Font font = null)
        {
            Image background = CreateImage($"Btn_{label}", parent, color, sprite, raycastTarget: true);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            // 누른 게 눌린 것처럼 보여야 한다.
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            Text text = CreateText("Label", background.transform, label, fontSize, labelColor, TextAnchor.MiddleCenter, font);
            Stretch(text.rectTransform);

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        /// <summary>세로로 쌓는 컨테이너. 자식 폭은 채우고, 높이는 자식이 정한다.</summary>
        public static VerticalLayoutGroup CreateVerticalGroup(string name, Transform parent, float spacing,
            RectOffset padding = null)
        {
            RectTransform rect = CreateRect(name, parent);
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return group;
        }

        /// <summary>가로로 늘어놓는 컨테이너. 자식이 폭을 나눠 갖는다. 세로로 쌓을 때는 줄마다 높이를 고정한다(SetPreferredHeight).</summary>
        public static HorizontalLayoutGroup CreateHorizontalGroup(string name, Transform parent, float spacing,
            RectOffset padding = null)
        {
            RectTransform rect = CreateRect(name, parent);
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding ?? new RectOffset();
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            return group;
        }
    }
}
