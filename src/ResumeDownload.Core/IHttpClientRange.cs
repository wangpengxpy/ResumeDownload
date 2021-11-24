using System;
using System.Threading.Tasks;

namespace ResumeDownload.Core
{
    public interface IHttpClientRange : IDisposable
    {
        /// <summary>
        /// 分片下载
        /// </summary>
        /// <param name="start"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        Task<HttpClientRangeResponse> DownloadChunk(long start, long length);
    }
}
