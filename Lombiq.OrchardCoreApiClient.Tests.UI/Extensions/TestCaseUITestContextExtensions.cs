using Atata;
using Lombiq.OrchardCoreApiClient.Clients;
using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.OrchardCoreApiClient.Tests.UI.Models;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
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
using Xunit;

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
        await context.TestTenantsOrchardCoreApiClientBehaviorAsync(
            new ApiClientBehaviorTestModel
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                FeatureProfile = featureProfile,
            });
        await context.TestContentsOrchardCoreApiClientBehaviorAsync(clientId, clientSecret);
    }

    public static async Task TestTenantsOrchardCoreApiClientBehaviorAsync(
        this UITestContext context,
        ApiClientBehaviorTestModel apiClientBehaviorTestModel)
    {
        // Tenant technical name must be lowercase, so we convert it here.
#pragma warning disable CA1308 // CA1308: Replace the call to 'ToLowerInvariant' with 'ToUpperInvariant'
        var technicalName = apiClientBehaviorTestModel.TenantName.ToLowerInvariant();
#pragma warning restore CA1308

        var createApiModel = new TenantApiModel
        {
            Description = "Tenant created by UI test",
            Name = apiClientBehaviorTestModel.TenantName,
            RequestUrlPrefix = apiClientBehaviorTestModel.RequestUrlPrefix ?? technicalName,
            RequestUrlHost = apiClientBehaviorTestModel.RequestUrlHost,
            TablePrefix = technicalName,
            RecipeName = "Blog",
            Category = "UI Test Tenants",
            FeatureProfiles = string.IsNullOrEmpty(apiClientBehaviorTestModel.FeatureProfile)
                ? null
                : new[] { apiClientBehaviorTestModel.FeatureProfile },
        };

        var setupApiModel = new TenantSetupApiModel
        {
            Name = apiClientBehaviorTestModel.TenantName,
            RecipeName = "Blog",
            UserName = DefaultUser.UserName,
            Email = DefaultUser.Email,
            Password = DefaultUser.Password,
            SiteName = "UI Test Tenant Site",
            SiteTimeZone = "Europe/Budapest",
            TablePrefix = technicalName,
        };

        var editModel = new TenantApiModel
        {
            Description = "Tenant edited by UI test",
            Name = apiClientBehaviorTestModel.TenantName,
            Category = "UI Test Tenants - Edited",
        };

        var isLocalTest = context.IsLocalUITest();

        // If the RequestUrlPrefix is not empty or not set we assume we don't want to change it, so we just append
        // "edited" to the RequestUrlHost.
        if (string.IsNullOrEmpty(apiClientBehaviorTestModel.RequestUrlPrefix) && !isLocalTest)
        {
            editModel.RequestUrlPrefix = apiClientBehaviorTestModel.RequestUrlPrefix;

            // Must be preceded by "edited" to ensure it does not affect subdomains e.g. "editedTestTenant.example.com".
            editModel.RequestUrlHost = "edited" + apiClientBehaviorTestModel.RequestUrlHost;
        }
        else
        {
            editModel.RequestUrlPrefix = technicalName + "edited";
            editModel.RequestUrlHost = apiClientBehaviorTestModel.RequestUrlHost;
        }

        // In case of remote tests these values can be different, or coming from environment variables.
        if (isLocalTest)
        {
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Using local test settings for creating tenant.");
            var databaseProvider = context.Configuration.UseSqlServer
                ? "SqlConnection"
                : "Sqlite";
            createApiModel.DatabaseProvider = databaseProvider;
            createApiModel.ConnectionString = context.SqlServerRunningContext?.ConnectionString;
            createApiModel.RequestUrlHost = string.Empty;
            editModel.RequestUrlHost = string.Empty;
        }

        var apiClientSettings = CreateApiClientSettings(context, apiClientBehaviorTestModel.ClientId, apiClientBehaviorTestModel.ClientSecret);
        using var tenantsApiClient = new TenantsApiClient(apiClientSettings);

        var isDefaultClient = string.IsNullOrEmpty(apiClientBehaviorTestModel.ClientId);

        const string defaultClientRecipe = "Lombiq.OrchardCoreApiClient.Tests.UI.OpenId";
        context.Scope.AtataContext.Log.Info($"Executing the default client recipe \"{defaultClientRecipe}\": {isDefaultClient}");

        if (isDefaultClient)
        {
            await context.ExecuteRecipeDirectlyAsync(defaultClientRecipe);

            // Verify that the recipe has successfully created the application.
            await context.SignInDirectlyAsync();
            await context.GoToAdminRelativeUrlAsync("/OpenId/Application");
            await context.ClickReliablyOnAsync(
                By.XPath($"//li[contains(@class, 'list-group-item') and contains(., '{apiClientSettings.ClientId}')]" +
                         $"//a[normalize-space(.) = 'Edit']"));
            context.Get(By.Name("ClientId")).GetAttribute("value").ShouldBe(apiClientSettings.ClientId);
        }
        else if (isLocalTest)
        {
            await context.SignInDirectlyAsync();
        }

        // Ensure that the tenant does not exist before starting the tests.
        await tenantsApiClient.OrchardCoreApi.DisableAsync(editModel.Name);
        await tenantsApiClient.OrchardCoreApi.RemoveAsync(editModel.Name);

        await TestTenantCreateAsync(context, tenantsApiClient, createApiModel, isLocalTest);
        await TestTenantSetupAsync(context, tenantsApiClient, createApiModel, setupApiModel);

        // If there are additional steps to execute in the tenant context, do that now.
        if (apiClientBehaviorTestModel.StepsInTenantContext != null)
        {
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Executing additional steps in the tenant context...");

            // Switch to the tenant context.
            if (string.IsNullOrEmpty(createApiModel.RequestUrlHost))
            {
                context.SwitchCurrentTenant(createApiModel.Name, createApiModel.RequestUrlPrefix);
            }
            else
            {
                var uriBuilder = new UriBuilder
                {
                    Scheme = "https",
                    Host = createApiModel.RequestUrlHost,
                    Path = createApiModel.RequestUrlPrefix,
                };
                context.SwitchCurrentTenant(createApiModel.Name, uriBuilder.Uri);
            }

            // Execute additional steps in the tenant context.
            await apiClientBehaviorTestModel.StepsInTenantContext(context, createApiModel, setupApiModel);

            // Switch back to the Default tenant.
            context.SwitchCurrentTenantToDefault();
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Additional steps in the tenant context were done.");
        }

        await TestTenantEditAsync(context, tenantsApiClient, editModel, setupApiModel, isLocalTest);
        await TestTenantDisableAsync(context, tenantsApiClient, editModel, isLocalTest);
        await TestTenantRemoveAsync(context, tenantsApiClient, editModel, isLocalTest);
    }

    public static async Task TestContentsOrchardCoreApiClientBehaviorAsync(
        this UITestContext context,
        string clientId = null,
        string clientSecret = null)
    {
        var taxonomy = new ContentItem
        {
            ContentType = "Taxonomy",
            DisplayText = "Taxonomy created by UI test",
        };

        var taxonomyPart = new TaxonomyPart { TermContentType = "Tag" };
        var autoRoutePart = new AutoroutePart { RouteContainedItems = true };
        taxonomy.Apply(taxonomyPart);
        taxonomy.Apply(autoRoutePart);

        using var contentsApiClient = new ContentsApiClient(CreateApiClientSettings(context, clientId, clientSecret));

        taxonomy.ContentItemId = await TestContentCreateAsync(context, contentsApiClient, taxonomy);
        await TestContentGetAsync(contentsApiClient, taxonomy);
        await TestContentRemoveAsync(context, contentsApiClient, taxonomy);
    }

    public static Task GoToTenantLandingPageAsync(
        this UITestContext context,
        string requestUrlPrefix,
        string requestUrlHost = null)
    {
        if (!string.IsNullOrEmpty(requestUrlHost))
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = requestUrlHost,
                Path = requestUrlPrefix,
            };

            return context.GoToAbsoluteUrlAsync(uriBuilder.Uri, onlyIfNotAlreadyThere: false);
        }

        return context.GoToRelativeUrlAsync(requestUrlPrefix, onlyIfNotAlreadyThere: false);
    }

    private static async Task TestTenantCreateAsync(
        UITestContext context,
        TenantsApiClient apiClient,
        TenantApiModel createApiModel,
        bool checkOnAdmin)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Creating the tenant...");
        using (var response = await apiClient.OrchardCoreApi.CreateAsync(createApiModel))
        {
            await context.AssertLogsAsync();
            response.Error.ShouldBeNull(
                $"Tenant creation failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");

            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Tenant creation response had no errors.");

            // Check if response URL is valid, and visit it (should be the tenant setup page and not 404 error).
            var responseUrl = new Uri(response.Content);
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Trying to go to the tenant setup page: " + responseUrl);
            await context.GoToAbsoluteUrlAsync(responseUrl);
        }

        if (checkOnAdmin)
        {
            context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Asserting tenant creation on the admin page...");
            await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, createApiModel);

            context.Get(By.CssSelector("#RecipeName option[selected]")).Text
                .ShouldBe(createApiModel.RecipeName);

            if (createApiModel.DatabaseProvider != null)
            {
                context.Get(By.CssSelector("#DatabaseProvider option[selected]")).GetValue()
                    .ShouldBe(createApiModel.DatabaseProvider);
            }

            if (createApiModel.FeatureProfiles != null)
            {
                context.Get(By.CssSelector("#FeatureProfiles option[selected]")).Text
                    .ShouldBe(createApiModel.FeatureProfiles.First());
            }
        }

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Creating the tenant succeeded.");
    }

    private static async Task TestTenantSetupAsync(
        UITestContext context,
        TenantsApiClient apiClient,
        TenantApiModel createApiModel,
        TenantSetupApiModel setupApiModel)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Initiating tenant setup...");
        using (var response = await apiClient.OrchardCoreApi.SetupAsync(setupApiModel))
        {
            response.Error.ShouldBeNull(
                $"Tenant setup failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");
        }

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Now going to the tenant landing page to assert the setup.");

        await context.GoToTenantLandingPageAsync(createApiModel.RequestUrlPrefix, createApiModel.RequestUrlHost);

        context.Exists(By.LinkText(setupApiModel.SiteName));
        context.Missing(By.ClassName("validation-summary-errors"));

        await GoToTenantUrlAndAssertHeaderAsync(context, createApiModel, setupApiModel);

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Setting up the tenant succeeded.");
    }

    private static async Task TestTenantEditAsync(
        UITestContext context,
        TenantsApiClient apiClient,
        TenantApiModel editModel,
        TenantSetupApiModel setupApiModel,
        bool checkOnAdmin)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Editing the tenant...");
        using (var response = await apiClient.OrchardCoreApi.EditAsync(editModel))
        {
            response.Error.ShouldBeNull(
                $"Tenant edit failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");
        }

        if (checkOnAdmin)
        {
            await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);
        }

        await GoToTenantUrlAndAssertHeaderAsync(context, editModel, setupApiModel);

        var originalPrefix = editModel.RequestUrlPrefix;
        var originalHost = editModel.RequestUrlHost;

        if (!string.IsNullOrEmpty(originalPrefix))
        {
            editModel.RequestUrlPrefix = "edit" + originalPrefix;
        }
        else
        {
            editModel.RequestUrlHost = "edit" + originalHost;
        }

        using (var response = await apiClient.OrchardCoreApi.EditAsync(editModel))
        {
            response.Error.ShouldBeNull(
                $"Tenant edit (with name change) failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");
        }

        if (checkOnAdmin)
        {
            await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);
        }
        else
        {
            await GoToTenantUrlAndAssertHeaderAsync(context, editModel, setupApiModel);
        }

        editModel.RequestUrlPrefix = originalPrefix;
        editModel.RequestUrlHost = originalHost;
        await apiClient.OrchardCoreApi.EditAsync(editModel);
        if (checkOnAdmin)
        {
            await GoToTenantEditorAndAssertCommonTenantFieldsAsync(context, editModel);
        }

        await GoToTenantUrlAndAssertHeaderAsync(context, editModel, setupApiModel);

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Editing the tenant succeeded.");
    }

    private static async Task TestTenantDisableAsync(
        UITestContext context,
        TenantsApiClient apiClient,
        TenantApiModel editModel,
        bool checkOnAdmin)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Disabling the tenant...");

        using (var response = await apiClient.OrchardCoreApi.DisableAsync(editModel.Name))
        {
            response.Error.ShouldBeNull(
                $"Tenant disable failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");
        }

        if (checkOnAdmin)
        {
            await context.GoToAdminRelativeUrlAsync("/Tenants");
            await context.FilterOnAdminWithSearchBoxAsync(editModel.Name);
            context.Exists(By.XPath($"//a[contains(., 'Enable') and contains(@href, '{editModel.Name}')]"));
        }

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Disabling the tenant succeeded.");
    }

    private static async Task TestTenantRemoveAsync(
        UITestContext context,
        TenantsApiClient apiClient,
        TenantApiModel editModel,
        bool checkOnAdmin)
    {
        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Removing the tenant...");

        await ReliabilityHelper.DoWithRetriesOrFailAsync(
            async () =>
            {
                using var response = await apiClient.OrchardCoreApi.RemoveAsync(editModel.Name);

                // The tenant can remain running for a while even after having been disabled. Waiting a bit here to see
                // if it gets unstuck.
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                    response.Error?.Content?.Contains($"The tenant '{editModel.Name}' should be 'Disabled' or 'Uninitialized'.") == true)
                {
                    context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                        "The tenant is still running, despite being disabled, and thus can't be removed. Attempting again.");

                    return false;
                }

                response.Error.ShouldBeNull(
                    $"Tenant remove failed with status code {response.StatusCode}. Content: {response.Error?.Content}\n" +
                    $"Request: {response.RequestMessage}\nDriver URL: {context.Driver.Url}");

                return true;
            },
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5),
            context.Configuration.TestCancellationToken);

        if (checkOnAdmin)
        {
            await context.GoToAdminRelativeUrlAsync("/Tenants", onlyIfNotAlreadyThere: false);
            await context.FilterOnAdminWithSearchBoxAsync(editModel.Name);
            context.Missing(By.LinkText(editModel.Name));
        }

        context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug("Removing the tenant succeeded.");
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

        await context.RefreshAsync();
        context.Missing(By.XPath($"//a[contains(text(), '{contentItem.DisplayText}')]"));
    }

    private static async Task GoToTenantUrlAndAssertHeaderAsync(
        UITestContext context,
        TenantApiModel apiModel,
        TenantSetupApiModel setupApiModel)
    {
        // Intentionally not switching tenants because API requests need to continue to go to the Default tenant.
        await context.GoToTenantLandingPageAsync(apiModel.RequestUrlPrefix, apiModel.RequestUrlHost);

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

    private static ApiClientSettings CreateApiClientSettings(UITestContext context, string clientId, string clientSecret)
    {
        var isDefaultClient = string.IsNullOrEmpty(clientId);
        if (isDefaultClient)
        {
            // This isn't an issue in this small method.
#pragma warning disable S1226 // Introduce a new variable instead of reusing the parameter.
            clientId = "UITest";
            clientSecret = "Password";
#pragma warning restore S1226 // Introduce a new variable instead of reusing the parameter.
        }

        return new()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DefaultTenantUri = context.Scope.BaseUri,
            DisableCertificateValidation = true,
        };
    }
}
