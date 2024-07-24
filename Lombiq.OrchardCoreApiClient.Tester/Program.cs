using Lombiq.OrchardCoreApiClient.Clients;
using Lombiq.OrchardCoreApiClient.Models;
using OrchardCore.ContentManagement;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tester;

public static class Program
{
    private const string ClientId = "Console";
    private const string ClientSecret = "Password";

    [SuppressMessage(
        "Design",
        "CA1303:Do not pass literals as localized parameters",
        Justification = "It is not a localization issue")]
    public static async Task Main(string[] arguments)
    {
        var port = int.TryParse(arguments.FirstOrDefault(), out var customPort) ? customPort : 44335;
        var apiClientSettings = new ApiClientSettings
        {
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            DefaultTenantUri = new Uri("https://localhost:" + port.ToTechnicalString()),
        };

        using var tenantsApiClient = new TenantsApiClient(apiClientSettings);

        // A suffix is used to avoid name clashes on an existing site, as this test doesn't delete tenants.
        var suffix = DateTime.Now.Ticks.ToTechnicalString();
        var name = "ApiClientTenant" + suffix;

        await tenantsApiClient.CreateAndSetupTenantAsync(
            new TenantApiModel
            {
                Description = "Tenant created by API Client",
                Name = name,
                DatabaseProvider = "Sqlite",
                RequestUrlPrefix = "api-client-tenant-" + suffix,
                RequestUrlHost = string.Empty,
                ConnectionString = string.Empty,
                TablePrefix = "apiclienttenant" + suffix, // #spell-check-ignore-line
                RecipeName = "Blog",
                Category = "API Client Tenants",
            },
            new TenantSetupApiModel
            {
                Name = name,
                DatabaseProvider = "Sqlite",
                ConnectionString = string.Empty,
                UserName = "admin",
                Email = "admin@example.com",
                Password = "Password1!",
                SiteName = "Api Client Tenant Site",
                SiteTimeZone = "Europe/Budapest",
                TablePrefix = "apiclienttenant" + suffix, // #spell-check-ignore-line
            }
        );

        Console.WriteLine("Creating and setting up the tenant succeeded.");

        var editModel = new TenantApiModel
        {
            Description = "Tenant edited by API Client",
            Name = name,
            RequestUrlPrefix = "api-client-tenant-edited-" + suffix,
            RequestUrlHost = "https://orcharddojo.net/",
            Category = "API Client - Edited Tenants",
        };

        // Requires Orchard Core 1.6.0 or newer, on a 1.5.0 server this returns a 404 error.
        await tenantsApiClient.OrchardCoreApi.EditAsync(editModel);

        Console.WriteLine("Editing the tenant succeeded.");

        using var contentsApiClient = new ContentsApiClient(apiClientSettings);

        var taxonomy = new ContentItem
        {
            ContentType = "Taxonomy",
            DisplayText = "Taxonomy created by API Client",
        };

        var response = await contentsApiClient.OrchardCoreApi.CreateOrUpdateAsync(taxonomy);
        var contentItemIdFromApi = JsonSerializer.Deserialize<ContentItem>(response.Content).ContentItemId;

        Console.WriteLine("Creating the taxonomy succeeded.");

        taxonomy.ContentItemId = contentItemIdFromApi;
        taxonomy.DisplayText = "Taxonomy edited by API Client";

        await contentsApiClient.OrchardCoreApi.CreateOrUpdateAsync(taxonomy);

        Console.WriteLine("Editing the taxonomy succeeded.");
    }
}
