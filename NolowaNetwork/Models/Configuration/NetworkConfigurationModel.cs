namespace NolowaNetwork.Models.Configuration
{
    public class NetworkConfigurationModel
    {
        public string Address { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string VirtualHostName { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
        public int Port { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
