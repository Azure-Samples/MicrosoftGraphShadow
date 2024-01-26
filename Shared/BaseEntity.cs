namespace Shared
{
    public abstract class BaseEntity {
        public string Id { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty; 
        public string OdataType { get; set; } = String.Empty;
        public string TenantId { get; set; } = String.Empty;
        public IDictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();
    }
}