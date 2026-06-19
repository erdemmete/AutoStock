namespace AutoStock.Services.Dtos.WebPush
{
    public class WebPushSubscriptionRequestDto
    {
        public string Endpoint { get; set; } = null!;
        public WebPushSubscriptionKeysDto Keys { get; set; } = new();
        public string? UserAgent { get; set; }
    }

    public class WebPushSubscriptionKeysDto
    {
        public string P256dh { get; set; } = null!;
        public string Auth { get; set; } = null!;
    }

    public class WebPushSubscriptionStatusDto
    {
        public bool IsSupported { get; set; } = true;
        public bool IsEnabled { get; set; }
        public bool HasActiveSubscription { get; set; }
        public string? PublicKey { get; set; }
    }

    public class WebPushPayloadDto
    {
        public string Title { get; set; } = null!;
        public string Body { get; set; } = null!;
        public string Url { get; set; } = "/Notifications";
        public int NotificationId { get; set; }
        public string Tag { get; set; } = null!;
    }
}
