#if UNITY_EDITOR
using System.IO;

namespace WebhookFeedbackSystem
{
    static class WebhookFeedbackMime
    {
        public static string FromFileName(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                _ => "application/octet-stream",
            };
        }

        public static bool IsImage(string fileName)
        {
            var mime = FromFileName(fileName);
            return mime.StartsWith("image/", System.StringComparison.Ordinal);
        }

        public static bool IsVideo(string fileName)
        {
            var mime = FromFileName(fileName);
            return mime.StartsWith("video/", System.StringComparison.Ordinal);
        }
    }
}
#endif
