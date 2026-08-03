#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace WebhookFeedbackSystem
{
    /// <summary>
    /// 활성 웹훅 프로바이더와 Secrets URL 경로를 관리한다.
    /// </summary>
    public static class WebhookFeedbackSettings
    {
        public const string ActiveProviderFilePath = "Secrets/webhook_active_provider.txt";
        public const string DiscordWebhookUrlFilePath = "Secrets/discord_webhook_url.txt";
        public const string SlackWebhookUrlFilePath = "Secrets/slack_webhook_url.txt";

        const WebhookFeedbackProvider DefaultProvider = WebhookFeedbackProvider.Discord;

        public static WebhookFeedbackProvider GetActiveProvider()
        {
            var absolutePath = ToAbsolutePath(ActiveProviderFilePath);
            if (!File.Exists(absolutePath))
                return DefaultProvider;

            var raw = File.ReadAllText(absolutePath).Trim();
            if (Enum.TryParse(raw, ignoreCase: true, out WebhookFeedbackProvider provider))
                return provider;

            Debug.LogWarning($"[WebhookFeedbackSettings] 알 수 없는 프로바이더 '{raw}'. 기본값 {DefaultProvider} 사용.");
            return DefaultProvider;
        }

        public static void SetActiveProvider(WebhookFeedbackProvider provider)
        {
            var absolutePath = ToAbsolutePath(ActiveProviderFilePath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(absolutePath, provider.ToString());
            Debug.Log($"[WebhookFeedbackSettings] 활성 프로바이더: {provider}");
        }

        public static string GetWebhookUrlFilePath(WebhookFeedbackProvider provider)
        {
            return provider switch
            {
                WebhookFeedbackProvider.Discord => DiscordWebhookUrlFilePath,
                WebhookFeedbackProvider.Slack => SlackWebhookUrlFilePath,
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
            };
        }

        public static string ReadWebhookUrl(WebhookFeedbackProvider provider)
        {
            var absolutePath = ToAbsolutePath(GetWebhookUrlFilePath(provider));
            return File.Exists(absolutePath) ? File.ReadAllText(absolutePath).Trim() : null;
        }

        public static string ToAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, path);
        }
    }
}
#endif
