using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Util
{
    public static class ScrollRectUtil
    {
        private static readonly Dictionary<int, MotionHandle> _dicTween = new();

        private static void AddTween(ScrollRect scrollRect, MotionHandle handle)
        {
            int instanceId = scrollRect.GetInstanceID();

            if (!_dicTween.TryAdd(instanceId, handle))
            {
                Debug.LogError($"ScrollRectUtil : 해당 instance({instanceId}, {scrollRect.name})의 Tween이 이미 추가되어 있음.");
            }
        }

        private static void CancelTween(ScrollRect scrollRect)
        {
            int instanceId = scrollRect.GetInstanceID();

            if (_dicTween.TryGetValue(instanceId, out var handle))
            {
                handle.TryCancel();
                _dicTween.Remove(instanceId);
            }
        }

        public static void CancelScrollAnimation(this ScrollRect scrollRect)
        {
            CancelTween(scrollRect);
        }

        public static async UniTask ScrollToPositionAsync(this ScrollRect scrollRect, Vector2 targetPosition, float duration, CancellationToken token = default)
        {
            CancelTween(scrollRect);

            var currentPosition = scrollRect.normalizedPosition;

            if (currentPosition == targetPosition)
            {
                return;
            }

            var handle = LMotion.Create(currentPosition, targetPosition, duration)
                .Bind(newVal => scrollRect.normalizedPosition = newVal);

            AddTween(scrollRect, handle);

            try
            {
                await handle.ToUniTask(cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                CancelTween(scrollRect);
                throw;
            }
            finally
            {
                _dicTween.Remove(scrollRect.GetInstanceID());
            }
        }

        public static float ScrollToPosition(this ScrollRect scrollRect, Vector2 targetPosition, float duration)
        {
            CancelTween(scrollRect);

            var currentPosition = scrollRect.normalizedPosition;

            if (currentPosition == targetPosition)
            {
                return 0;
            }

            var handle = LMotion.Create(currentPosition, targetPosition, duration)
                .Bind(newVal => scrollRect.normalizedPosition = newVal);

            AddTween(scrollRect, handle);

            return duration;
        }

        public static async UniTask ScrollToTopAsync(this ScrollRect scrollRect, float duration, CancellationToken token = default)
        {
            await scrollRect.ScrollToPositionAsync(new Vector2(0, 1), duration, token);
        }

        public static async UniTask ScrollToBottomAsync(this ScrollRect scrollRect, float duration, CancellationToken token = default)
        {
            await scrollRect.ScrollToPositionAsync(new Vector2(0, 0), duration, token);
        }

        public static float ScrollToTop(this ScrollRect scrollRect, float duration)
        {
            return scrollRect.ScrollToPosition(new Vector2(0, 1), duration);
        }

        public static float ScrollToBottom(this ScrollRect scrollRect, float duration)
        {
            return scrollRect.ScrollToPosition(new Vector2(0, 0), duration);
        }
    }
}
