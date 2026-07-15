using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

namespace Util
{
    public static class ProgressBarUtil
    {
        private static readonly Dictionary<int, MotionHandle> _dicTween = new();

        private static void AddTween(MonoBehaviour monoBehaviour, MotionHandle handle)
        {
            int instanceId = monoBehaviour.GetInstanceID();

            if (!_dicTween.TryAdd(instanceId, handle))
            {
                Debug.LogError($"ProgressBarUtil : 해당 instance({instanceId}, {monoBehaviour.name})의 Tween이 이미 추가되어 있음.");
            }
        }

        private static void CancelTween(MonoBehaviour monoBehaviour)
        {
            int instanceId = monoBehaviour.GetInstanceID();

            if (_dicTween.TryGetValue(instanceId, out var handle))
            {
                handle.TryCancel();
                _dicTween.Remove(instanceId);
            }
        }

        public static void CancelProgress(this Image img)
        {
            CancelTween(img);
        }

        public static float SetProgressWithAnimation(this Image img, float fillAmount, float animDuration = 0)
        {
            CancelTween(img);

            if (animDuration > 0)
            {
                var currValue = img.fillAmount;
                var duration = Mathf.Abs(fillAmount - img.fillAmount) * animDuration;

                if (duration == 0)
                {
                    img.fillAmount = fillAmount;
                    return 0;
                }

                var handle = LMotion.Create(currValue, fillAmount, duration)
                    .Bind(newVal => img.fillAmount = newVal);
                AddTween(img, handle);

                return duration;
            }
            else
            {
                img.fillAmount = fillAmount;

                return 0;
            }
        }

        public static float SetProgressWhileTime(this Image img, float fillAmount, float animDuration)
        {
            CancelTween(img);

            if (animDuration > 0)
            {
                var currValue = img.fillAmount;

                if (animDuration == 0)
                {
                    img.fillAmount = fillAmount;
                    return 0;
                }

                var handle = LMotion.Create(currValue, fillAmount, animDuration)
                    .Bind(newVal => img.fillAmount = newVal);
                AddTween(img, handle);

                return animDuration;
            }
            else
            {
                img.fillAmount = fillAmount;

                return 0;
            }
        }

        public static async UniTask SetProgressAsync(this Image img, float fillAmount, float animDuration, CancellationToken token)
        {
            CancelTween(img);

            var currValue = img.fillAmount;
            var duration = Mathf.Abs(fillAmount - img.fillAmount) * animDuration;

            if (duration <= 0)
            {
                img.fillAmount = fillAmount;
                return;
            }

            var handle = LMotion.Create(currValue, fillAmount, duration)
                .Bind(newVal => img.fillAmount = newVal);
            AddTween(img, handle);

            try
            {
                await handle.ToUniTask(cancellationToken: token);
            }
            finally
            {
                _dicTween.Remove(img.GetInstanceID());
            }
        }
    }
}
