using Atata;
using Lombiq.OrchardCoreApiClient.Clients;
using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement;
using OrchardCore.Taxonomies.Models;
using Shouldly;
using System;
using System.Linq;
using System.Text.Json;
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

        var isDefaultClient = string.IsNullOrEmpty(clientId);
        if (isDefaultClient)
        {
            // If the client ID is not set, change both ID and secret to the default.
#pragma warning disable S1226 // Introduce a new variable instead of reusing the parameter.
            clientId = "UITest";
            clientSecret = "Password";
#pragma warning restore S1226 // Introduce a new variable instead of reusing the parameter.
        }

        var createApiModel = new TenantApiModel
        {
            Description = "Tenant created by UI test",
            Name = tenantName,
            DatabaseProvider = databaseProvider,
            RequestUrlPrefix = prefix,
            RequestUrlHost = string.Empty,
            ConnectionString = context.SqlServerRunningContext?.ConnectionString,
            TablePrefix = prefix,
            RecipeName = "Blog",
            Category = "UI Test Tenants",
            FeatureProfiles = string.IsNullOrEmpty(featureProfile) ? null : new[] { featureProfile },
        };

        var setupApiModel = new TenantSetupApiModel
        {
            Name = tenantName,
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

        var apiClientSettings = new ApiClientSettings
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DefaultTenantUri = context.Scope.BaseUri,
            DisableCertificateValidation = true,
        };

        using var tenantsApiClient = new TenantsApiClient(apiClientSettings);

        const string defaultClientRecipe = "Lombiq.OrchardCoreApiClient.Tests.UI.OpenId";
        context.Scope.AtataContext.Log.Info($"Executing the default client recipe \"{defaultClientRecipe}\": {isDefaultClient}");
        if (isDefaultClient)
        {
            await context.ExecuteRecipeDirectlyAsync(defaultClientRecipe);

            // Verify that the recipe has successfully created the application.
            await context.SignInDirectlyAsync();
            await context.GoToAdminRelativeUrlAsync("/OpenId/Application/Edit/1");
            context.Get(By.Name("ClientId")).GetAttribute("value").ShouldBe(clientId);
        }
        else
        {
            await context.SignInDirectlyAsync();
        }

        await TestTenantCreateAsync(context, tenantsApiClient, createApiModel);
        await TestTenantSetupAsync(context, tenantsApiClient, createApiModel, setupApiModel);
        await TestTenantEditAsync(context, tenantsApiClient, editModel, setupApiModel);
        await TestTenantDisableAsync(context, tenantsApiClient, editModel);
        await TestTenantRemoveAsync(context, tenantsApiClient, editModel);

        var taxonomy = new ContentItem
        {
            ContentType = "Taxonomy",
            DisplayText = "Taxonomy created by UI test",
        };

        var taxonomyPart = new TaxonomyPart { TermContentType = "Tag" };
        var autoRoutePart = new AutoroutePart { RouteContainedItems = true };
        taxonomy.Apply(taxonomyPart);
        taxonomy.Apply(autoRoutePart);

        using var contentsApiClient = new ContentsApiClient(apiClientSettings);

        taxonomy.ContentItemId = await TestContentCreateAsync(context, contentsApiClient, taxonomy);
        await TestContentGetAsync(contentsApiClient, taxonomy);
        await TestContentRemoveAsync(context, contentsApiClient, taxonomy);
    }

    private static async Task TestTenantCreateAsync(
        UITestContext context,
        TenantsApiClient apiClient,
        TenantApiModel createApiModel)
    {
        using (var response = await apiClient.OrchardCoreApi.CreateAsync(createApiModel))
        {
            await context.AssertLogsAsync();
            response.Error.ShouldBeNull(
                $"Tenant creation failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");

            // Check if response URL is valid, and visit it (should be the tenant setup page and not 404 error).
            var responseUrl = new Uri(response.Content);
            responseUrl.AbsolutePath.ShouldBe($"/{createApiModel.Name}", StringCompareShould.IgnoreCase);
            await context.GoToAbsoluteUrlAsync(responseUrl);
        }

        await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, createApiModel);

        context.Get(By.CssSelector("#RecipeName option[selected]")).Text
            .ShouldBe(createApiModel.RecipeName);

        context.Get(By.CssSelector("#DatabaseProvider option[selected]")).GetValue()
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
        TenantsApiClient apiClient,
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
        TenantsApiClient apiClient,
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
        TenantsApiClient apiClient,
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
        TenantsApiClient apiClient,
        TenantApiModel editModel)
    {
        await apiClient.OrchardCoreApi.RemoveAsync(editModel.Name);
        await context.GoToAdminRelativeUrlAsync("/Tenants", onlyIfNotAlreadyThere: false);
        context.Missing(By.LinkText(editModel.Name));

        context.Configuration.TestOutputHelper.WriteLine("Removing the tenant succeeded.");
    }

    private static async Task<string> TestContentCreateAsync(
        UITestContext context,
        ContentsApiClient apiClient,
        ContentItem contentItem)
    {
        var response = await apiClient.OrchardCoreApi.CreateOrUpdateAsync(contentItem);
        var contentItemIdFromApi = JsonSerializer.Deserialize<ContentItem>(response.Content).ContentItemId;
        await context.GoToContentItemEditorByIdAsync(contentItemIdFromApi);

        context.Get(By.Id("TitlePart_Title")).GetValue().ShouldBe(contentItem.DisplayText);

        context.Get(By.Id("AutoroutePart_RouteContainedItems")).GetValue()
            .ShouldBe(contentItem.As<AutoroutePart>().RouteContainedItems.ToString().ToLowerFirstLetter());

        context.Get(By.CssSelector("#TaxonomyPart_TermContentType option[selected]")).Text
            .ShouldBe(contentItem.As<TaxonomyPart>().TermContentType);

        return contentItemIdFromApi;
    }

    private static async Task TestContentGetAsync(
        ContentsApiClient apiClient,
        ContentItem contentItem)
    {
        var response = await apiClient.OrchardCoreApi.GetAsync(contentItem.ContentItemId);
        var contentItemFromApi = JsonSerializer.Deserialize<ContentItem>(response.Content);

        var document = JsonDocument.Parse(response.Content);

        var contentItemFromApiAutoroutePart = JsonSerializer.Deserialize<AutoroutePart>(
            document.RootElement.GetProperty("AutoroutePart").GetRawText());
        var contentItemFromApiTaxonomyPart = JsonSerializer.Deserialize<TaxonomyPart>(
            document.RootElement.GetProperty("TaxonomyPart").GetRawText());

        contentItemFromApi.DisplayText.ShouldBe(contentItem.DisplayText);
        contentItemFromApi.ContentType.ShouldBe(contentItem.ContentType);
        contentItemFromApiAutoroutePart.RouteContainedItems.ShouldBe(contentItem.As<AutoroutePart>().RouteContainedItems);
        contentItemFromApiTaxonomyPart.TermContentType.ShouldBe(contentItem.As<TaxonomyPart>().TermContentType);
    }

    private static async Task TestContentRemoveAsync(
        UITestContext context,
        ContentsApiClient apiClient,
        ContentItem contentItem)
    {
        await context.GoToContentItemListAsync();
        context.Exists(By.XPath($"//a[contains(text(), '{contentItem.DisplayText}')]"));

        await apiClient.OrchardCoreApi.RemoveAsync(contentItem.ContentItemId);

        context.Refresh();
        context.Missing(By.XPath($"//a[contains(text(), '{contentItem.DisplayText}')]"));
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
