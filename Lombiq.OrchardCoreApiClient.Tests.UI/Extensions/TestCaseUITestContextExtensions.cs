using Atata;
using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tests.UI.Extensions;

public static class TestCaseUITestContextExtensions
{
    // Note that the OrchardCore:OrchardCore_Tenants:TenantRemovalAllowed config is deliberately not set here, because
    // the parent app needs that to be configured if tenant removal should work.
    public static async Task TestOrchardCoreApiClientBehaviorAsync(
        this UITestContext context,
        string clientId = null,
        string clientSecret = null,
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
            ClientId = clientId ?? "UITest",
            ClientSecret = clientSecret ?? "Password",
            DefaultTenantUri = context.Scope.BaseUri,
            DisableCertificateValidation = true,
        });

        if (string.IsNullOrEmpty(clientId))
        {
            await context.ExecuteRecipeDirectlyAsync("Lombiq.OrchardCoreApiClient.Tests.UI.OpenId");
        }

        await context.SignInDirectlyAsync();

        await TestTenantCreateAsync(context, apiClient, createApiModel);
        await TestTenantSetupAsync(context, apiClient, createApiModel, setupApiModel);
        await TestTenantEditAsync(context, apiClient, editModel, setupApiModel);
        await TestTenantDisableAsync(context, apiClient, editModel);
        await TestTenantRemoveAsync(context, apiClient, editModel);
    }

    private static async Task TestTenantCreateAsync(
        UITestContext context,
        ApiClient apiClient,
        TenantApiModel createApiModel)
    {
        using (var response = await apiClient.OrchardCoreApi.CreateAsync(createApiModel))
        {
            response.IsSuccessStatusCode.ShouldBeTrue(
                $"Tenant creation failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");

            new Uri(response.Content).AbsolutePath.ShouldBe($"/{createApiModel.Name}", StringCompareShould.IgnoreCase);
        }

        // This is necessary because only on GitHub + Windows it will not find the tenant on the first try.
        var found = false;
        for (int retries = 0; !found && retries < 5; retries++)
        {
            await context.GoToAdminRelativeUrlAsync("/Tenants");
            found = context.Exists(By.LinkText(createApiModel.Name).Safely());
        }

        found.ShouldBeTrue($"Couldn't find \"{createApiModel.Name}\" in the Tenants page.");

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

        await GoToTenantUrlAndAssertHeaderAsync(context, createApiModel, setupApiModel);

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
        await GoToTenantUrlAndAssertHeaderAsync(context, editModel, setupApiModel);

        var originalPrefix = editModel.RequestUrlPrefix;
        var originalHost = editModel.RequestUrlHost;

        editModel.RequestUrlPrefix = string.Empty;
        editModel.RequestUrlHost = "https://example.com";
        await apiClient.OrchardCoreApi.EditAsync(editModel);
        await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);

        editModel.RequestUrlPrefix = originalPrefix;
        editModel.RequestUrlHost = originalHost;
        await apiClient.OrchardCoreApi.EditAsync(editModel);
        await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);
        await GoToTenantUrlAndAssertHeaderAsync(context, editModel, setupApiModel);

        context.Configuration.TestOutputHelper.WriteLine("Editing the tenant succeeded.");
    }

    private static async Task TestTenantDisableAsync(
        UITestContext context,
        ApiClient apiClient,
        TenantApiModel editModel)
    {
        await apiClient.OrchardCoreApi.DisableAsync(editModel.Name);
        await context.GoToAdminRelativeUrlAsync("/Tenants");
        var href = context.GetAbsoluteAdminUri($"/OrchardCore.Tenants/Admin/Enable/{editModel.Name}").ToString();
        context.GetAll(By.LinkText("Enable")).ShouldContain(element => element.GetAttribute("href") == href);

        context.Configuration.TestOutputHelper.WriteLine("Disabling the tenant succeeded.");
    }

    private static async Task TestTenantRemoveAsync(
        UITestContext context,
        ApiClient apiClient,
        TenantApiModel editModel)
    {
        await apiClient.OrchardCoreApi.RemoveAsync(editModel.Name);
        await context.GoToAdminRelativeUrlAsync("/Tenants", onlyIfNotAlreadyThere: false);
        context.Missing(By.LinkText(editModel.Name));

        context.Configuration.TestOutputHelper.WriteLine("Removing the tenant succeeded.");
    }

    private static async Task GoToTenantUrlAndAssertHeaderAsync(
        UITestContext context,
        TenantApiModel apiModel,
        TenantSetupApiModel setupApiModel)
    {
        // Intentionally not switching tenants because API requests need to continue to go to the Default tenant.
        await context.GoToRelativeUrlAsync(apiModel.RequestUrlPrefix, onlyIfNotAlreadyThere: false);

        context.Get(By.ClassName("navbar-brand")).Text
            .ShouldBe(setupApiModel.SiteName);
    }

    private static Task GoToTenantEditorAsync(UITestContext context, TenantApiModel apiModel) =>
        context.GoToAdminRelativeUrlAsync("/Tenants/Edit/" + apiModel.Name, onlyIfNotAlreadyThere: false);

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
