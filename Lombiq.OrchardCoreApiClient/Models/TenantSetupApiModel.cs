using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.OrchardCoreApiClient.Models;

public class TenantSetupApiModel
{
    [Required]
    public string Name { get; set; }

    [Required]
    public string SiteName { get; set; }
    public string DatabaseProvider { get; set; }
    public string ConnectionString { get; set; }
    public string TablePrefix { get; set; }

    [Required]
    public string UserName { get; set; }

    [Required]
    public string Email { get; set; }

    [DataType(DataType.Password)]
    public string Password { get; set; }
    public string RecipeName { get; set; }

    [SuppressMessage("Maintainability", "CA1507:Use nameof to express symbol names", Justification = "The other property will be removed.")]
    [JsonProperty("Recipe")]
    public JToken RecipeJson { get; set; }

    public string SiteTimeZone { get; set; }
}
