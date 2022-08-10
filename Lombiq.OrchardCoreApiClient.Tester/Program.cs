using Lombiq.OrchardCoreApiClient.Constants;
using Lombiq.OrchardCoreApiClient.Exceptions;
using Lombiq.OrchardCoreApiClient.Interfaces;
using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.OrchardCoreApiClient.Tester.Interfaces;
using Lombiq.OrchardCoreApiClient.Tester.Models;
using RestEase;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tester;

public class ApiClientTester
{
    private readonly IOrchardCoreApiTester _orchardCoreApi;
    private readonly ApiClientSettings _apiClientSettings;

    public ApiClientTester(ApiClientSettings apiClientSettings)
    {
        _apiClientSettings = apiClientSettings;
        _orchardCoreApi = GetOrchardCoreApi(_apiClientSettings.DefaultTenantUri);
    }

    public async Task CreateAndSetupTenantAsync(
        CreateApiViewModel createApiViewModel,
        SetupApiViewModel setupApiViewModel)
    {
        if (!Timezones.GetTimezoneIds.Contains(setupApiViewModel.SiteTimeZone))
        {
            throw new ApiClientException(
                $"Invalid timezone ID {setupApiViewModel.SiteTimeZone}. For the list of all valid timezone IDs " +
                "follow this list: https://gist.github.com/jrolstad/5ca7d78dbfe182d7c1be");
        }

        try
        {
            (await _orchardCoreApi.
                CreateAsync(createApiViewModel).
                ConfigureAwait(false)).
                ResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ApiClientException("Tenant creation failed.", ex);
        }

        try
        {
            (await _orchardCoreApi.
                SetupAsync(setupApiViewModel).
                ConfigureAwait(false)).
                ResponseMessage.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new ApiClientException("Tenant setup failed.", ex);
        }
    }

    public IOrchardCoreApiTester GetOrchardCoreApi(Uri defaultTenantUri) =>
        RestClient.For<IOrchardCoreApiTester>(defaultTenantUri, async (request, _) =>
        {
            if (request.Headers.Authorization != null)
            {
                var tokenResponse = await RestClient.
                For<IOrchardCoreAuthorizatonApi>(defaultTenantUri).
                TokenAsync(
                    new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _apiClientSettings.ClientId,
                        ["client_secret"] = _apiClientSettings.ClientSecret,
                    });

                tokenResponse.ResponseMessage.EnsureSuccessStatusCode();

                if (tokenResponse.GetContent().Error != null)
                {
                    throw new ApiClientException(
                        $"API client setup failed. An error occurred while retrieving an access token: " +
                        tokenResponse.GetContent().Error);
                }

                if (tokenResponse.GetContent().ExpiresIn == "0")
                {
                    throw new ApiClientException($"The token is expired.");
                }

                request.Headers.Authorization = new AuthenticationHeaderValue(
                    request.Headers.Authorization.Scheme, tokenResponse.GetContent().AccessToken);
            }
        });
}

public static class Program
{
    private const string ClientId = "Console";
    private const string ClientSecret = "Password";

    public static async Task Main()
    {
        var apiClient = new ApiClientTester(new ApiClientSettings
        {
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            DefaultTenantUri = new Uri("https://localhost:44335"),
        });

        await apiClient.CreateAndSetupTenantAsync(
                new CreateApiViewModel
                {
                    Description = "Tenant created by API Client",
                    Name = "ApiClientTenant",
                    DatabaseProvider = "Sqlite",
                    RequestUrlPrefix = "apiClientTenant",
                    RequestUrlHost = string.Empty,
                    ConnectionString = string.Empty,
                    TablePrefix = "apiClientTenant",
                    RecipeName = "Blog",
                },
                new SetupApiViewModel
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
                    TablePrefix = "apiClientTenant",
                }
            );

        await apiClient.CreateAndSetupTenantAsync(
                new CreateApiViewModel
                {
                    Description = "Tenant created by API Client2",
                    Name = "ApiClientTenant2",
                    DatabaseProvider = "Sqlite",
                    RequestUrlPrefix = "apiClientTenant2",
                    RequestUrlHost = string.Empty,
                    ConnectionString = string.Empty,
                    TablePrefix = "apiClientTenant2",
                    RecipeName = "Blog",
                },
                new SetupApiViewModel
                {
                    Name = "ApiClientTenant2",
                    DatabaseProvider = "Sqlite",
                    ConnectionString = string.Empty,
                    RecipeName = "Blog",
                    UserName = "admin",
                    Email = "admin@example.com",
                    Password = "Password1!",
                    SiteName = "Api Client Tenant Site2",
                    SiteTimeZone = "Europe/Budapest",
                    TablePrefix = "apiClientTenant2",
                }
            );

#pragma warning disable CA1303 // Do not pass literals as localized parameters
        Console.WriteLine("Creating and setting up the tenants succeeded.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
    }
}
