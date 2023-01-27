using Lombiq.OrchardCoreApiClient.Models;
using System;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tester;

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
            DefaultTenantUri = new Uri("https://localhost:44335"),
        });

        await apiClient.CreateAndSetupTenantAsync(
                new TenantApiModel
                {
                    Description = "Tenant created by API Client",
                    Name = "ApiClientTenant",
                    DatabaseProvider = "Sqlite",
                    RequestUrlPrefix = "api-client-tenant",
                    RequestUrlHost = string.Empty,
                    ConnectionString = string.Empty,
                    TablePrefix = "apiClientTenant",
                    RecipeName = "Blog",
                    Category = "API Client Tenants",
                },
                new TenantSetupApiModel
                {
                    Name = "ApiClientTenant",
                    DatabaseProvider = "Sqlite",
                    ConnectionString = string.Empty,
                    RecipeName = "Blog",
                    UserName = "admin",
                    Email = "admin@example.com",
                    Password = "Password1!",
                    SiteName = "Api Client Tenant Site",
                    SiteTimeZone = "Europe/Budapest",
                    TablePrefix = "apiclienttenant",
                }
            );

#pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("Creating and setting up the tenant succeeded.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters

        var editModel = new TenantApiModel
        {
            Description = "Tenant edited by API Client",
            Name = "ApiClientTenant",
            RequestUrlPrefix = "api-client-tenant-edited",
            RequestUrlHost = "https://orcharddojo.net/",
            Category = "API Client - Edited Tenants",
        };

        await apiClient.OrchardCoreApi.EditAsync(editModel);

#pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("Editing the tenant succeeded.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    }
}
