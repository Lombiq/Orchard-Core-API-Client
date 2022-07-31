using Lombiq.OrchardCoreApiClient.Models;
using System;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient
{
    public static class Program
    {
        private const string ClientId = "Console";
        private const string ClientSecret = "Password";

        public static async Task Main()
        {
            var apiClient = new ApiClient(new ApiClientSettings
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                DefaultTenantUri = new Uri("https://localhost:44300"),
            });
        }
    }
}
