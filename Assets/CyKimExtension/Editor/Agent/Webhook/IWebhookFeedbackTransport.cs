#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace WebhookFeedbackSystem
{
    interface IWebhookFeedbackTransport
    {
        void SendText(string title, string description);

        void Send(IReadOnlyList<(string AttachmentName, byte[] Bytes)> files, string title, string description);
    }
}
#endif
