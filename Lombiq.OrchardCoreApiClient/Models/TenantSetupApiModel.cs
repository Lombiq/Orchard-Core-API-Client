using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;

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

    [Obsolete($"This property relies on a deprecated NuGet package and will be removed in the next major version. " +
              $"Please use the {nameof(RecipeJson)} property instead and serialize the value yourself in a project " +
              $"that doesn't use the .NET Standard framework.")]
    [JsonIgnore]
    public IFormFile Recipe
    {
        get => RecipeJson.ToObject<IFormFile>();
        set => JToken.FromObject(value);
    }

#pragma warning disable CS0618 // Use of Obsolete symbol. But only for reference.
    [JsonProperty(nameof(Recipe))]
#pragma warning restore CS0618 // Use of Obsolete symbol. But only for reference.
    public JToken RecipeJson { get; set; }

    public string SiteTimeZone { get; set; }
}
