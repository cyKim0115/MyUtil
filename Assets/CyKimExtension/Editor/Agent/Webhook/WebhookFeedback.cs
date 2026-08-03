#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WebhookFeedbackSystem
{
    /// <summary>
    /// 웹훅 피드백 송신 허브. 활성 프로바이더로 Send/SendText를 위임한다.
    /// </summary>
    public static class WebhookFeedback
    {
        public const int MaxFilesPerMessage = 10;

        [MenuItem("Tools/Agent/Webhook/Send Feedback")]
        static void SendFeedbackMenuStub()
        {
            Debug.Log("[WebhookFeedback] Agent 전용. execute_code로 WebhookFeedback.Send / SendText / SetActiveProvider 를 호출하세요.");
        }

        [MenuItem("Tools/Agent/Webhook/Send Feedback", true)]
        static bool ValidateSendFeedbackMenuStub() => false; // Agent 전용. 사용자 Tools 메뉴에서는 비활성.

        public static WebhookFeedbackProvider GetActiveProvider() => WebhookFeedbackSettings.GetActiveProvider();

        public static void SetActiveProvider(WebhookFeedbackProvider provider) =>
            WebhookFeedbackSettings.SetActiveProvider(provider);

        public static void SendText(string title, string description = null) =>
            SendText(GetActiveProvider(), title, description);

        public static void SendText(WebhookFeedbackProvider provider, string title, string description = null)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            {
                Debug.LogError("[WebhookFeedback] title 또는 description 중 하나는 필요합니다.");
                return;
            }

            ResolveTransport(provider).SendText(title, description);
        }

        public static void Send(string screenshotPath, string title, string description = null) =>
            Send(new[] { screenshotPath }, title, description);

        public static void Send(string[] screenshotPaths, string title, string description = null) =>
            Send(GetActiveProvider(), screenshotPaths, title, description);

        public static void Send(
            WebhookFeedbackProvider provider,
            string[] screenshotPaths,
            string title,
            string description = null)
        {
            if (screenshotPaths == null || screenshotPaths.Length == 0)
            {
                Debug.LogError("[WebhookFeedback] 전송할 스크린샷 경로가 없습니다. 텍스트만 보내려면 SendText를 사용하세요.");
                return;
            }

            if (screenshotPaths.Length > MaxFilesPerMessage)
            {
                Debug.LogError($"[WebhookFeedback] 한 메시지당 최대 {MaxFilesPerMessage}장까지 전송할 수 있습니다. (요청: {screenshotPaths.Length})");
                return;
            }

            var files = new List<(string AttachmentName, byte[] Bytes)>(screenshotPaths.Length);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < screenshotPaths.Length; i++)
            {
                var absolutePath = WebhookFeedbackSettings.ToAbsolutePath(screenshotPaths[i]);
                if (!File.Exists(absolutePath))
                {
                    Debug.LogError($"[WebhookFeedback] 스크린샷 파일이 없습니다: {absolutePath}");
                    return;
                }

                var attachmentName = MakeUniqueFileName(Path.GetFileName(absolutePath), usedNames);
                files.Add((attachmentName, File.ReadAllBytes(absolutePath)));
            }

            ResolveTransport(provider).Send(files, title, description);
        }

        static IWebhookFeedbackTransport ResolveTransport(WebhookFeedbackProvider provider)
        {
            return provider switch
            {
                WebhookFeedbackProvider.Discord => new DiscordWebhookTransport(),
                WebhookFeedbackProvider.Slack => new SlackWebhookTransport(),
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "지원하지 않는 웹훅 프로바이더"),
            };
        }

        static string MakeUniqueFileName(string fileName, HashSet<string> usedNames)
        {
            if (usedNames.Add(fileName))
                return fileName;

            var stem = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var index = 2;
            string candidate;
            do
            {
                candidate = $"{stem}_{index}{ext}";
                index++;
            } while (!usedNames.Add(candidate));

            return candidate;
        }
    }
}
#endif
