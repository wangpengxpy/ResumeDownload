using System;
using System.Collections.Concurrent;
using System.IO;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 指定文件断点续传下载全局参数获取或计算
    /// </summary>
    public interface IResumeDlownload
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="url"></param>
        /// <param name="outputFilePath"></param>
        void InitialParameters(string id, string url, string outputFilePath);
        /// <summary>
        /// 
        /// </summary>
        Uri Uri { get; }
        /// <summary>
        /// 
        /// </summary>
        string Id { get; }
        /// <summary>
        /// 
        /// </summary>
        string OutputPath { get; }
        /// <summary>
        /// 
        /// </summary>
        long FileSize { get; }
        /// <summary>
        /// 分片总数
        /// </summary>
        int ChunkCount { get; }
        /// <summary>
        /// 
        /// </summary>
        int MaxChunkSize { get; }
        /// <summary>
        /// 
        /// </summary>
        int MaxRetries { get; }
        /// <summary>
        /// 下载线程数
        /// </summary>
        int NumberOfThreads { get; }
        /// <summary>
        /// 读栈
        /// </summary>
        ConcurrentStack<int> ReadStack { get; }
        /// <summary>
        /// 获取当前分片起始长度
        /// </summary>
        /// <param name="currentChunk"></param>
        /// <returns></returns>
        long GetChunkStart(int currentChunk);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentChunk"></param>
        int GetChunkSizeForCurrentChunk(int currentChunk, int chunkCount);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="readStack"></param>
        /// <param name="lastWriteTime"></param>
        /// <param name="minutesToWait"></param>
        /// <returns></returns>
        bool NeedToCheckForUnwrittenChunks(ConcurrentStack<int> readStack, DateTime lastWriteTime, int minutesToWait);
        /// <summary>
        /// 写入文件流
        /// </summary>
        /// <returns></returns>
        Stream GetFileStream();
    }
}
