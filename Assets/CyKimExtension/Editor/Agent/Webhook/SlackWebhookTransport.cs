#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;

namespace WebhookFeedbackSystem
{
    /// <summary>
    /// Incoming Webhook은 로컬 파일 직접 첨부를 지원하지 않으므로,
    /// 이미지를 단기 공개 호스트에 올린 뒤 image block URL로 포함한다.
    /// </summary>
    sealed class SlackWebhookTransport : IWebhookFeedbackTransport
    {
        const string EphemeralUploadUrl = "https://litterbox.catbox.moe/resources/internals/api.php";
        const string EphemeralUploadLifetime = "1h";
        const string IconEmoji = ":pepe_dance:";

        public void SendText(string title, string description)
        {
            var webhookUrl = WebhookFeedbackSettings.ReadWebhookUrl(WebhookFeedbackProvider.Slack);
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Debug.LogError($"[WebhookFeedback] URL 없음: {WebhookFeedbackSettings.SlackWebhookUrlFilePath}");
                return;
            }

            PostJson(webhookUrl, BuildPayloadJson(title, description, imageUrls: null), title ?? "(텍스트)");
        }

        public void Send(IReadOnlyList<(string AttachmentName, byte[] Bytes)> files, string title, string description)
        {
            var webhookUrl = WebhookFeedbackSettings.ReadWebhookUrl(WebhookFeedbackProvider.Slack);
            if (string.IsNullOrEmpty(webhookUrl))
            {
                Debug.LogError($"[WebhookFeedback] URL 없음: {WebhookFeedbackSettings.SlackWebhookUrlFilePath}");
                return;
            }

            var imageUrls = new List<string>(files.Count);
            try
            {
                using var httpClient = new HttpClient();
                for (var i = 0; i < files.Count; i++)
                {
                    var url = UploadEphemeralImage(httpClient, files[i].AttachmentName, files[i].Bytes);
                    if (string.IsNullOrEmpty(url))
                    {
                        Debug.LogError($"[WebhookFeedback] 이미지 임시 업로드 실패: {files[i].AttachmentName}");
                        return;
                    }

                    imageUrls.Add(url);
                }

                PostJson(webhookUrl, BuildPayloadJson(title, description, imageUrls), title, $"{files.Count}장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebhookFeedback] 전송 예외: {e.Message}");
            }
        }

        static string UploadEphemeralImage(HttpClient httpClient, string fileName, byte[] bytes)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent("fileupload"), "reqtype");
            form.Add(new StringContent(EphemeralUploadLifetime), "time");

            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(fileContent, "fileToUpload", fileName);

            var response = httpClient.PostAsync(EphemeralUploadUrl, form).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()?.Trim();
            if (!response.IsSuccessStatusCode || string.IsNullOrEmpty(body) || !body.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[WebhookFeedback] 임시 호스트 응답 실패 ({(int)response.StatusCode}): {body}");
                return null;
            }

            return body;
        }

        static void PostJson(string webhookUrl, string payloadJson, string title, string detail = "텍스트")
        {
            try
            {
                using var httpClient = new HttpClient();
                using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(webhookUrl, content).GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[WebhookFeedback] 전송 완료 ({WebhookFeedbackProvider.Slack}): {title} ({detail})");
                    return;
                }

                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Debug.LogError($"[WebhookFeedback] 전송 실패 ({(int)response.StatusCode}): {body}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebhookFeedback] 전송 예외: {e.Message}");
            }
        }

        static string BuildPayloadJson(string title, string description, List<string> imageUrls)
        {
            var sb = new StringBuilder();
            sb.Append('{');

            var fallback = string.IsNullOrEmpty(title) ? description : title;
            if (string.IsNullOrEmpty(fallback))
                fallback = "webhook feedback";
            sb.Append("\"text\":\"").Append(WebhookFeedbackJson.Escape(fallback)).Append('"');
            sb.Append(",\"icon_emoji\":\"").Append(IconEmoji).Append('"');
            sb.Append(",\"blocks\":[");

            var hasBlock = false;
            if (!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(description))
            {
                sb.Append("{\"type\":\"header\",\"text\":{\"type\":\"plain_text\",\"text\":\"");
                sb.Append(WebhookFeedbackJson.Escape(string.IsNullOrEmpty(title) ? "Feedback" : title));
                sb.Append("\"}}");
                hasBlock = true;

                if (!string.IsNullOrEmpty(description))
                {
                    sb.Append(",{\"type\":\"section\",\"text\":{\"type\":\"mrkdwn\",\"text\":\"");
                    sb.Append(WebhookFeedbackJson.Escape(description));
                    sb.Append("\"}}");
                }
            }

            if (imageUrls != null)
            {
                for (var i = 0; i < imageUrls.Count; i++)
                {
                    if (hasBlock || i > 0)
                        sb.Append(',');
                    sb.Append("{\"type\":\"image\",\"image_url\":\"");
                    sb.Append(WebhookFeedbackJson.Escape(imageUrls[i]));
                    sb.Append("\",\"alt_text\":\"screenshot_").Append(i + 1).Append("\"}");
                    hasBlock = true;
                }
            }

            sb.Append("]}");
            return sb.ToString();
        }
    }
}
#endif
