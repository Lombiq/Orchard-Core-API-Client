using Atata;
using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tests.UI.Extensions;

public static class TestCaseUITestContextExtensions
{
    public static async Task TestOrchardCoreApiClientBehaviorAsync(
        this UITestContext context,
        string featureProfile = null)
    {
        const string tenantName = "UITestTenant";
        const string prefix = "uitesttenant"; // #spell-check-ignore-line
        var databaseProvider = context.Configuration.UseSqlServer
            ? "SqlConnection"
            : "Sqlite";

        var createApiModel = new TenantApiModel
        {
            Description = "Tenant created by UI test",
            Name = tenantName,
            DatabaseProvider = databaseProvider,
            RequestUrlPrefix = prefix,
            RequestUrlHost = string.Empty,
            ConnectionString = string.Empty,
            TablePrefix = prefix,
            RecipeName = "Blog",
            Category = "UI Test Tenants",
            FeatureProfiles = string.IsNullOrEmpty(featureProfile) ? null : new[] { featureProfile },
        };

        var setupApiModel = new TenantSetupApiModel
        {
            Name = tenantName,
            DatabaseProvider = databaseProvider,
            ConnectionString = string.Empty,
            RecipeName = "Blog",
            UserName = "admin",
            Email = "admin@example.com",
            Password = "Password1!",
            SiteName = "UI Test Tenant Site",
            SiteTimeZone = "Europe/Budapest",
            TablePrefix = prefix,
        };

        var editModel = new TenantApiModel
        {
            Description = "Tenant edited by UI test",
            Name = tenantName,
            RequestUrlPrefix = prefix + "edited",
            RequestUrlHost = string.Empty,
            Category = "UI Test Tenants - Edited",
        };

        using var apiClient = new ApiClient(new ApiClientSettings
        {
            ClientId = "UITest",
            ClientSecret = "Password",
            DefaultTenantUri = context.Scope.BaseUri,
            DisableCertificateValidation = true,
        });

        await context.ExecuteRecipeDirectlyAsync("Lombiq.OrchardCoreApiClient.Tests.UI.OpenId");

        await context.SignInDirectlyAsync();

        await TestTenantCreateAsync(context, apiClient, createApiModel);
        await TestTenantSetupAsync(context, apiClient, createApiModel, setupApiModel);
        await TestTenantEditAsync(context, apiClient, editModel, setupApiModel);
    }

    private static async Task TestTenantCreateAsync(
        UITestContext context,
        ApiClient apiClient,
        TenantApiModel createApiModel)
    {
        await apiClient.OrchardCoreApi.CreateAsync(createApiModel);

        await context.GoToAdminRelativeUrlAsync("/Tenants");
        context.Exists(By.LinkText(createApiModel.Name));

        await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, createApiModel);

        context.Get(By.CssSelector("#RecipeName option[selected]")).Text
            .ShouldBe(createApiModel.RecipeName);

        context.Get(By.CssSelector("#DatabaseProvider option[selected]")).Text
            .ShouldBe(createApiModel.DatabaseProvider);

        if (createApiModel.FeatureProfiles != null)
        {
            context.Get(By.CssSelector("#FeatureProfiles option[selected]")).Text
                .ShouldBe(createApiModel.FeatureProfiles.First());
        }

        context.Configuration.TestOutputHelper.WriteLine("Creating the tenant succeeded.");
    }

    private static async Task TestTenantSetupAsync(
        UITestContext context,
        ApiClient apiClient,
        TenantApiModel createApiModel,
        TenantSetupApiModel setupApiModel)
    {
        await apiClient.OrchardCoreApi.SetupAsync(setupApiModel);

        await context.GoToRelativeUrlAsync(createApiModel.RequestUrlPrefix);

        context.Exists(By.LinkText(setupApiModel.SiteName));

        context.Missing(By.ClassName("validation-summary-errors"));

        // Intentionally not switching tenants because API requests need to continue to go to the Default tenant.
        await context.GoToRelativeUrlAsync(createApiModel.RequestUrlPrefix);

        context.Get(By.ClassName("navbar-brand")).Text
            .ShouldBe(setupApiModel.SiteName);

        context.Configuration.TestOutputHelper.WriteLine("Setting up the tenant succeeded.");
    }

    private static async Task TestTenantEditAsync(
        UITestContext context,
        ApiClient apiClient,
        TenantApiModel editModel,
        TenantSetupApiModel setupApiModel)
    {
        await apiClient.OrchardCoreApi.EditAsync(editModel);

        await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);

        await context.GoToRelativeUrlAsync(editModel.RequestUrlPrefix);

        context.Get(By.ClassName("navbar-brand")).Text
            .ShouldBe(setupApiModel.SiteName);

        editModel.RequestUrlPrefix = string.Empty;
        editModel.RequestUrlHost = "https://example.com";

        await apiClient.OrchardCoreApi.EditAsync(editModel);

        await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);

        context.Configuration.TestOutputHelper.WriteLine("Editing the tenant succeeded.");
    }

    private static Task GoToTenantEditorAsync(UITestContext context, TenantApiModel apiModel) =>
        context.GoToAdminRelativeUrlAsync("/Tenants/Edit/" + apiModel.Name);

    private static async Task GoToTenantEditorAndAssertCommonTenantFieldsAsync(UITestContext context, TenantApiModel apiModel)
    {
        await GoToTenantEditorAsync(context, apiModel);

        context.Get(By.CssSelector("#Description")).GetValue()
            .ShouldBe(apiModel.Description);

        context.Get(By.CssSelector("#RequestUrlPrefix")).GetValue()
            .ShouldBe(apiModel.RequestUrlPrefix);

        context.Get(By.CssSelector("#RequestUrlHost")).GetValue()
            .ShouldBe(apiModel.RequestUrlHost);

        context.Get(By.CssSelector("#Category")).GetValue()
            .ShouldBe(apiModel.Category);
    }
}
