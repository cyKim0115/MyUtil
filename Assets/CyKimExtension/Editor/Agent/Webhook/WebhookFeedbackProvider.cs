#if UNITY_EDITOR
namespace WebhookFeedbackSystem
{
    public enum WebhookFeedbackProvider
    {
        Discord = 0,
        Slack = 1,
        /// <summary>Discord와 Slack 양쪽에 동일 메시지를 전송한다.</summary>
        Both = 2,
    }
}
#endif
