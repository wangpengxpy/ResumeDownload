using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Headers;

namespace ResumeDownload.Core
{
    /// <summary>
    /// Range响应简要信息
    /// </summary>
    public class HttpClientRangeResponse
    {
        private static readonly ICollection<HttpStatusCode> RetryStateCodes =
            new ReadOnlyCollection<HttpStatusCode>(new[]
            {
                HttpStatusCode.RequestEntityTooLarge,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.ServiceUnavailable,
                HttpStatusCode.GatewayTimeout
            });

        public HttpClientRangeResponse(HttpStatusCode statusCode, byte[] content, HttpContentHeaders headers)
        {
            Content = content;

            StatusCode = statusCode;

            ContentLength = headers.ContentLength ?? 0;

            var contentRange = headers.ContentRange;

            if (headers.ContentRange != null)
            {
                From = contentRange.From;
                To = contentRange.To;
                Length = contentRange.Length;
            }

            Location = headers.ContentLocation?.ToString();

            Headers = headers;
        }

        public HttpStatusCode StatusCode { get; private set; }
        public HttpContentHeaders Headers { get; set; }
        public string Location { get; private set; }

        public long? From { get; private set; }
        public long? To { get; private set; }
        public long? Length { get; private set; }
        public long ContentLength { get; private set; }
        public byte[] Content { get; private set; }

        public bool Successed => StatusCode >= HttpStatusCode.OK && StatusCode <= HttpStatusCode.Ambiguous;

        public bool IsRetry
        {
            get
            {
                if (!Successed)
                {
                    return RetryStateCodes.Contains(StatusCode);
                }

                return true;
            }
        }
    }
}
