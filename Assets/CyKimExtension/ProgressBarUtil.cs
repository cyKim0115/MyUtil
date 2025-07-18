using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Util
{
    public static class ProgressBarUtil
    {
        private static readonly Dictionary<int, Tween> _dicTween = new();

        private static void AddTween(MonoBehaviour monoBehaviour, Tween tween)
        {
            int instanceId = monoBehaviour.GetInstanceID();

            if (!_dicTween.TryAdd(instanceId, tween))
            {
                Debug.LogError($"ProgressBarUtil : 해당 instance({instanceId}, {monoBehaviour.name})의 Tween이 이미 추가되어 있음.");
            }
        }

        private static void CancelTween(MonoBehaviour monoBehaviour)
        {
            int instanceId = monoBehaviour.GetInstanceID();

            if (_dicTween.TryGetValue(instanceId, out var tween))
            {
                tween.Stop();
                _dicTween.Remove(instanceId);
            }
        }

        public static void SetProgress(this Image img, float fillAmount, float animDuration = 0)
        {
            CancelTween(img);

            if (animDuration > 0)
            {
                var currValue = img.fillAmount;
                var duration = Mathf.Abs(fillAmount - img.fillAmount) * animDuration;

                var tween = Tween.Custom(currValue, fillAmount, duration: duration,
                    onValueChange: newVal => img.fillAmount = newVal);
                AddTween(img, tween);
            }
            else
            {
                img.fillAmount = fillAmount;
            }
        }

        public static async UniTask SetProgressAsync(this Image img, float fillAmount, float animDuration, CancellationToken token)
        {
            var currValue = img.fillAmount;
            var duration = Mathf.Abs(fillAmount - img.fillAmount) * animDuration;

            await Tween.Custom(currValue, fillAmount, duration: duration,
                    onValueChange: newVal => img.fillAmount = newVal)
                .WithCancellation(token);
        }
    }
}