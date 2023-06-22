using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tests.UI.Extensions;

public static class TestCaseUITestContextExtensions
{
    public static async Task TestOrchardCoreApiClientBehaviorAsync(this UITestContext context)
    {
        await context.ExecuteRecipeDirectlyAsync("Lombiq.OrchardCoreApiClient.Tests.UI.OpenId");

        using var apiClient = new ApiClient(new ApiClientSettings
        {
            ClientId = "UITest",
            ClientSecret = "Password",
            DefaultTenantUri = context.Scope.BaseUri,
        });

        const string tenantName = "UITestTenant";
        const string prefix = "uitesttenant";

        await apiClient.CreateAndSetupTenantAsync(
            new TenantApiModel
            {
                Description = "Tenant created by UI test",
                Name = tenantName,
                DatabaseProvider = "Sqlite",
                RequestUrlPrefix = prefix,
                RequestUrlHost = string.Empty,
                ConnectionString = string.Empty,
                TablePrefix = prefix,
                RecipeName = "Blog",
                Category = "UI Test Tenants",
            },
            new TenantSetupApiModel
            {
                Name = tenantName,
                DatabaseProvider = "Sqlite",
                ConnectionString = string.Empty,
                RecipeName = "Blog",
                UserName = "admin",
                Email = "admin@example.com",
                Password = "Password1!",
                SiteName = "UI Test Tenant Site",
                SiteTimeZone = "Europe/Budapest",
                TablePrefix = prefix,
            }
        );

        context.Configuration.TestOutputHelper.WriteLine("Creating and setting up the tenant succeeded.");

        var editModel = new TenantApiModel
        {
            Description = "Tenant edited by UI test",
            Name = tenantName,
            RequestUrlPrefix = prefix + "edited",
            Category = "UI Test Tenants - Edited",
        };

        await apiClient.OrchardCoreApi.EditAsync(editModel);

        context.Configuration.TestOutputHelper.WriteLine("Editing the tenant succeeded.");
    }
}
