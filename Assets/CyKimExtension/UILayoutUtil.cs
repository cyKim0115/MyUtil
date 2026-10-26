using UnityEngine;
using UnityEngine.UI;

namespace cyKimUnityExtensions
{
    namespace UnityEngine
    {
        namespace UI
        {
            public static class LayoutUtil
            {   
                /// <summary>
                /// 지정된 Transform 내에서 LayoutGroup과 ContentSizeFitter를 하위 자식부터 순차적으로 Rebuild합니다.
                /// </summary>
                /// <param name="root">Rebuild를 수행할 Transform의 루트</param>
                public static void RebuildLayoutsFromBottom(this Transform root)
                {
                    if (root == null) return;

                    // 하위 자식부터 재귀적으로 탐색하며 Rebuild 실행
                    foreach (Transform child in root)
                    {
                        RebuildLayoutsFromBottom(child);
                    }

                    // LayoutGroup과 ContentSizeFitter를 가진 경우 Rebuild 실행
                    if (root.TryGetComponent<LayoutGroup>(out var _) ||
                        root.TryGetComponent<ContentSizeFitter>(out var _))
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(root as RectTransform);
                    }
                }

                /// <summary>
                /// LayoutElement로 줄 높이를 고정한다. 없으면 붙인다.
                /// flexibleHeight를 0으로 같이 눌러야 한다. 그냥 두면 -1(미지정)이라 LayoutElement가 아무것도 덮지 않는데,
                /// HorizontalLayoutGroup은 childForceExpandHeight가 켜져 있으면 자기 flexibleHeight를 1로 보고한다.
                /// 그러면 그 줄이 부모의 남는 세로 공간을 전부 가져가 96px로 잡은 버튼 줄이 화면 절반이 된다.
                /// </summary>
                public static LayoutElement SetPreferredHeight(this GameObject target, float height)
                {
                    if (!target.TryGetComponent(out LayoutElement element))
                    {
                        element = target.AddComponent<LayoutElement>();
                    }

                    element.preferredHeight = height;
                    element.flexibleHeight = 0f;
                    return element;
                }
            }
        }
    }
}