#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;

namespace WebhookFeedbackSystem
{
    sealed class DiscordWebhookTransport : IWebhookFeedbackTransport
    {
        const int EmbedColor = 0x5865F2;

        public void SendText(string title, string description)
        {
            var webhookUrl = WebhookFeedbackSettings.ReadWebhookUrl(WebhookFeedbackProvider.Discord);
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Debug.LogError($"[WebhookFeedback] URL 없음: {WebhookFeedbackSettings.DiscordWebhookUrlFilePath}");
                return;
            }

            PostJson(webhookUrl, BuildTextPayloadJson(title, description), title ?? "(텍스트)");
        }

        public void Send(IReadOnlyList<(string AttachmentName, byte[] Bytes)> files, string title, string description)
        {
            var webhookUrl = WebhookFeedbackSettings.ReadWebhookUrl(WebhookFeedbackProvider.Discord);
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Debug.LogError($"[WebhookFeedback] URL 없음: {WebhookFeedbackSettings.DiscordWebhookUrlFilePath}");
                return;
            }

            var disposables = new List<IDisposable>(files.Count + 2);
            try
            {
                using var httpClient = new HttpClient();
                var form = new MultipartFormDataContent();
                disposables.Add(form);

                // embed에는 제목/설명만, 이미지는 일반 첨부로 올려 갤러리(가로) 배치를 유도한다.
                var jsonContent = new StringContent(BuildTextPayloadJson(title, description), Encoding.UTF8, "application/json");
                disposables.Add(jsonContent);
                form.Add(jsonContent, "payload_json");

                for (var i = 0; i < files.Count; i++)
                {
                    var fileContent = new ByteArrayContent(files[i].Bytes);
                    disposables.Add(fileContent);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                    form.Add(fileContent, $"files[{i}]", files[i].AttachmentName);
                }

                var response = httpClient.PostAsync(webhookUrl, form).GetAwaiter().GetResult();
                LogResponse(response, title, $"{files.Count}장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebhookFeedback] 전송 예외: {e.Message}");
            }
            finally
            {
                for (var i = disposables.Count - 1; i >= 0; i--)
                    disposables[i].Dispose();
            }
        }

        static void PostJson(string webhookUrl, string payloadJson, string logLabel)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(webhookUrl, content).GetAwaiter().GetResult();
                LogResponse(response, logLabel, "텍스트");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebhookFeedback] 전송 예외: {e.Message}");
            }
        }

        static void LogResponse(HttpResponseMessage response, string title, string detail)
        {
            if (response.IsSuccessStatusCode)
            {
                Debug.Log($"[WebhookFeedback] 전송 완료 ({WebhookFeedbackProvider.Discord}): {title} ({detail})");
                return;
            }

            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Debug.LogError($"[WebhookFeedback] 전송 실패 ({(int)response.StatusCode}): {body}");
        }

        static string BuildTextPayloadJson(string title, string description)
        {
            var sb = new StringBuilder();
            sb.Append("{\"embeds\":[{");
            sb.Append("\"color\":").Append(EmbedColor);

            if (!string.IsNullOrEmpty(title))
                sb.Append(",\"title\":\"").Append(WebhookFeedbackJson.Escape(title)).Append('"');
            if (!string.IsNullOrEmpty(description))
                sb.Append(",\"description\":\"").Append(WebhookFeedbackJson.Escape(description)).Append('"');

            sb.Append("}]}");
            return sb.ToString();
        }
    }
}
#endif
