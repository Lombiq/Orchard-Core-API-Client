using System;
using System.Runtime.Serialization;

namespace Lombiq.OrchardCoreApiClient.Exceptions
{
    [Serializable]
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

        protected ApiClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}