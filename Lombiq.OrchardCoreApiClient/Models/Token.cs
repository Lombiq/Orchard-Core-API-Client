using Newtonsoft.Json;

namespace Lombiq.OrchardCoreApiClient.Models;

public class Token
{
    [JsonProperty("token_type")]
    public string TokenType { get; set; }

    [JsonProperty("access_token")]
    public string AccessToken { get; set; }

    [JsonProperty("error")]
    public string Error { get; set; }

    [JsonProperty("expires_in")]
    public string ExpiresIn { get; set; }
}
