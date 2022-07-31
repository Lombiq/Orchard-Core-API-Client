using System;

namespace Lombiq.OrchardCoreApiClient.Models
{
    public class ApiClientSettings
    {
        public Uri DefaultTenantUri { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}