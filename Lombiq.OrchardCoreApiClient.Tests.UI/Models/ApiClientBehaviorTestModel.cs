using Lombiq.OrchardCoreApiClient.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.OrchardCoreApiClient.Tests.UI.Models;

public class ApiClientBehaviorTestModel
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string FeatureProfile { get; set; }
    public string RequestUrlHost { get; set; }
    public string RequestUrlPrefix { get; set; }
    public string TenantName { get; set; } = "UITestTenantForOrchardCoreApiClientBehavior";
    public Func<UITestContext, TenantApiModel, TenantSetupApiModel, Task> StepsInTenantContext { get; set; }
}
