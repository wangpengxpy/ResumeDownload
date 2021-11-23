using System;
using System.IO;

namespace ResumeDownload.Core
{
    public interface IDownloadParameters
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="outputFilePath"></param>
        void Initial(string id, string url, string outputFilePath);
        /// <summary>
        /// 文件标识
        /// </summary>
        string Id { get; }
        /// <summary>
        /// 
        /// </summary>
        Uri Uri { get; }
        /// <summary>
        /// 
        /// </summary>
        string OuputFilePath { get; }
        /// <summary>
        /// 文件输出流
        /// </summary>
        /// <returns></returns>
        Stream GetOutputStream();
        /// <summary>
        /// 最大分片大小
        /// </summary>
        int MaxChunkSize { get; }
        /// <summary>
        /// 最大尝试次数
        /// </summary>
        int MaxRetries { get; }
        /// <summary>
        /// 文件大小
        /// </summary>
        long FileSize { get; }
        /// <summary>
        /// 线程大小
        /// </summary>
        int MaxThreads { get; }
    }
}
