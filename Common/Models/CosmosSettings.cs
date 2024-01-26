namespace Common.Models
{
    public class CosmosSettings
    {
        public string ConnectionString { get; set; } = String.Empty;
        public string CosmosEndpoint { get; set; } = String.Empty;  
        public string DatabaseId { get; set; } = String.Empty;
        public string ApplicationName { get; set; } = String.Empty;
    }
}
