#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace WebhookFeedbackSystem
{
    /// <summary>
    /// 웹훅 피드백 송신 허브. 활성 프로바이더로 Send/SendText/SendRecording을 위임한다.
    /// </summary>
    public static class WebhookFeedback
    {
        public const int MaxFilesPerMessage = 10;
        public const string ScreenshotsFolderPath = "Assets/Screenshots";

        private const string ClearScreenshotsMenuPath = "Tools/Agent/Webhook/Clear Screenshots Folder";

        [MenuItem("Tools/Agent/Webhook/Send Feedback")]
        static void SendFeedbackMenuStub()
        {
            Debug.Log("[WebhookFeedback] Agent 전용. execute_code로 WebhookFeedback.Send / SendText / SendRecording / SetActiveProvider / ClearScreenshotsFolder 를 호출하세요.");
        }

        [MenuItem("Tools/Agent/Webhook/Send Feedback", true)]
        static bool ValidateSendFeedbackMenuStub() => false; // Agent 전용. 사용자 Tools 메뉴에서는 비활성.

        [MenuItem(ClearScreenshotsMenuPath)]
        public static void ClearScreenshotsFolderMenu() => ClearScreenshotsFolder();

        [MenuItem(ClearScreenshotsMenuPath, true)]
        static bool ValidateClearScreenshotsFolderMenu() => false; // Agent 전용. 사용자 Tools 메뉴에서는 비활성.

        public static WebhookFeedbackProvider GetActiveProvider() => WebhookFeedbackSettings.GetActiveProvider();

        public static void SetActiveProvider(WebhookFeedbackProvider provider) =>
            WebhookFeedbackSettings.SetActiveProvider(provider);

        /// <summary>
        /// 웹훅/플레이테스트 캡처가 끝난 뒤 Assets/Screenshots 안의 파일을 전부 삭제한다.
        /// 폴더와 Screenshots.meta 는 유지한다.
        /// </summary>
        public static int ClearScreenshotsFolder()
        {
            var absoluteFolder = WebhookFeedbackSettings.ToAbsolutePath(ScreenshotsFolderPath);
            if (!Directory.Exists(absoluteFolder))
            {
                Debug.Log($"[WebhookFeedback] Screenshots 폴더 없음: {ScreenshotsFolderPath}");
                return 0;
            }

            var deleted = 0;
            foreach (var filePath in Directory.GetFiles(absoluteFolder))
            {
                var fileName = Path.GetFileName(filePath);
                if (string.Equals(fileName, ".gitkeep", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    File.Delete(filePath);
                    deleted++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WebhookFeedback] 스크린샷 삭제 실패: {filePath} ({ex.Message})");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[WebhookFeedback] Screenshots 폴더 정리 완료: {deleted}개 삭제 ({ScreenshotsFolderPath})");
            return deleted;
        }

        public static void SendText(string title, string description = null) =>
            SendText(GetActiveProvider(), title, description);

        public static void SendText(WebhookFeedbackProvider provider, string title, string description = null)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
            {
                Debug.LogError("[WebhookFeedback] title 또는 description 중 하나는 필요합니다.");
                return;
            }

            foreach (var target in ExpandProviders(provider))
                ResolveTransport(target).SendText(title, description);
        }

        public static void Send(string screenshotPath, string title, string description = null) =>
            Send(new[] { screenshotPath }, title, description);

        public static void Send(string[] screenshotPaths, string title, string description = null) =>
            Send(GetActiveProvider(), screenshotPaths, title, description);

        /// <summary>
        /// 녹화 영상(MP4 등)을 활성 프로바이더로 전송한다. Discord는 첨부, Slack은 임시 URL 링크.
        /// </summary>
        public static void SendRecording(string recordingPath, string title, string description = null) =>
            SendRecording(GetActiveProvider(), recordingPath, title, description);

        public static void SendRecording(
            WebhookFeedbackProvider provider,
            string recordingPath,
            string title,
            string description = null)
        {
            if (string.IsNullOrWhiteSpace(recordingPath))
            {
                Debug.LogError("[WebhookFeedback] 전송할 녹화 경로가 없습니다.");
                return;
            }

            Send(provider, new[] { recordingPath }, title, description);
        }

        public static void Send(
            WebhookFeedbackProvider provider,
            string[] screenshotPaths,
            string title,
            string description = null)
        {
            if (screenshotPaths == null || screenshotPaths.Length == 0)
            {
                Debug.LogError("[WebhookFeedback] 전송할 파일 경로가 없습니다. 텍스트만 보내려면 SendText를 사용하세요.");
                return;
            }

            if (screenshotPaths.Length > MaxFilesPerMessage)
            {
                Debug.LogError($"[WebhookFeedback] 한 메시지당 최대 {MaxFilesPerMessage}개까지 전송할 수 있습니다. (요청: {screenshotPaths.Length})");
                return;
            }

            var files = new List<(string AttachmentName, byte[] Bytes)>(screenshotPaths.Length);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < screenshotPaths.Length; i++)
            {
                var absolutePath = WebhookFeedbackSettings.ToAbsolutePath(screenshotPaths[i]);
                if (!File.Exists(absolutePath))
                {
                    Debug.LogError($"[WebhookFeedback] 파일이 없습니다: {absolutePath}");
                    return;
                }

                var attachmentName = MakeUniqueFileName(Path.GetFileName(absolutePath), usedNames);
                files.Add((attachmentName, File.ReadAllBytes(absolutePath)));
            }

            foreach (var target in ExpandProviders(provider))
                ResolveTransport(target).Send(files, title, description);
        }

        static IEnumerable<WebhookFeedbackProvider> ExpandProviders(WebhookFeedbackProvider provider)
        {
            if (provider == WebhookFeedbackProvider.Both)
            {
                yield return WebhookFeedbackProvider.Discord;
                yield return WebhookFeedbackProvider.Slack;
                yield break;
            }

            yield return provider;
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
