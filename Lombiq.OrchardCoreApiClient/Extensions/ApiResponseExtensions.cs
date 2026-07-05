using Refit;

namespace Lombiq.OrchardCoreApiClient.Extensions;

public static class ApiResponseExtensions
{
    /// <summary>
    /// Gets the error content from an <see cref="ApiResponse{T}"/> if the error is of type <see cref="ApiException"/>.
    /// </summary>
    public static string GetApiErrorContent<T>(this ApiResponse<T> response) =>
        response.Error is ApiException apiException ? apiException.Content : null;
}
