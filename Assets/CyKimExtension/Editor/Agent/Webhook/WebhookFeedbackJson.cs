#if UNITY_EDITOR
namespace WebhookFeedbackSystem
{
    static class WebhookFeedbackJson
    {
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "");
        }
    }
}
#endif
