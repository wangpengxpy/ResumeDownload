using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ResumeDownload.Core
{
    public class HttpClientRangeRequest : IHttpClientRange
    {
        private Stream stream;

        private readonly ILogger<HttpClientRangeRequest> _logger;
        private readonly HttpClient _client;
        private readonly BufferManager _bufferManager;
        private readonly IResumeDlownload _resumeDlownload;

        public const int BUFFER_SIZE = 36000;

        public HttpClientRangeRequest(ILogger<HttpClientRangeRequest> logger, IResumeDlownload resumeDlownload)
        {
            _logger = logger;

            _resumeDlownload = resumeDlownload;

            _bufferManager = new BufferManager(new[]
            {
                new BufferQueueSetting(BUFFER_SIZE, _resumeDlownload.NumberOfThreads),
                new BufferQueueSetting(_resumeDlownload.MaxChunkSize,  _resumeDlownload.NumberOfThreads)
            });
            _client = new HttpClient();
        }

        public HttpClientRangeResponse DownloadChunk(long start, long length)
        {
            HttpClientRangeResponse response;

            byte[] buffer = null;

            byte[] initialReadBytes = null;

            try
            {
                _client.DefaultRequestHeaders.Range = new RangeHeaderValue(start, start + length - 1);

                var httpResponse = _client.GetAsync(_resumeDlownload.Uri, HttpCompletionOption.ResponseHeadersRead)
                    .GetAwaiter().GetResult();

                httpResponse.EnsureSuccessStatusCode();

                stream = httpResponse.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                buffer = _bufferManager.GetBuffer(BUFFER_SIZE);

                var bytesread = stream.Read(buffer, 0, buffer.Length);

                initialReadBytes = _bufferManager.GetBuffer(bytesread);

                Buffer.BlockCopy(buffer, 0, initialReadBytes, 0, bytesread);

                var headers = httpResponse.Content.Headers;

                var statusCode = httpResponse.StatusCode;

                if (statusCode >= HttpStatusCode.OK
                    && statusCode <= HttpStatusCode.Ambiguous)
                {
                    var contentLength = headers.ContentLength;

                    var dest = _bufferManager.GetBuffer((int)contentLength);

                    using var outputStream = new MemoryStream(dest);

                    var left = contentLength.Value - bytesread;

                    outputStream.Write(initialReadBytes, 0, initialReadBytes.Length);

                    while (left > 0)
                    {
                        if (bytesread == 0)
                        {
                            throw new IOException("The stream is not returning any more data");
                        }

                        bytesread = stream.Read(buffer, 0, (int)(left < buffer.Length ? left : buffer.Length));

                        outputStream.Write(buffer, 0, bytesread);
                        left -= bytesread;
                    }

                    response = new HttpClientRangeResponse(statusCode, dest, headers);

                }
                else
                {
                    response = new HttpClientRangeResponse(statusCode, null, headers);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException)
            {
                _logger.LogError($"workId({_resumeDlownload.Id})-request exception：{ex}");
                return null;
            }
            finally
            {
                if (buffer != null && buffer.Length > 0)
                {
                    _bufferManager.FreeBuffer(buffer);
                }

                if (initialReadBytes != null && initialReadBytes.Length > 0)
                {
                    _bufferManager.FreeBuffer(initialReadBytes);
                }
            }
            return response;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_client != null)
                {
                    _client.Dispose();
                }

                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
        }
    }
}
