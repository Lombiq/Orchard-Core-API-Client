using System;

namespace Lombiq.OrchardCoreApiClient.Exceptions;

public class ApiClientException : Exception
{
    public ApiClientException()
    {
    }

    public ApiClientException(string message)
        : base(message)
    {
    }

    public ApiClientException(string message, Exception exception)
        : base(message, exception)
    {
    }
}
