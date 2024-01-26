namespace Common.Models
{
    public class GraphSettings
    {
        public string NotificationUrl { get; set; } = String.Empty;
        public string ClientId { get; set; } = String.Empty;
        public string Secret { get; set; } = String.Empty;
        public string Scopes { get; set; } = String.Empty;
        public string[] UserAttributeSelection { get; set; } = Array.Empty<string>();
        public string[] UserExpands { get; set; } = Array.Empty<string>();
    }
}
