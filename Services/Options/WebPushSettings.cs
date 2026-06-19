namespace AutoStock.Services.Options
{
    public class WebPushSettings
    {
        public bool Enabled { get; set; }
        public string? Subject { get; set; }
        public string? PublicKey { get; set; }
        public string? PrivateKey { get; set; }
    }
}
