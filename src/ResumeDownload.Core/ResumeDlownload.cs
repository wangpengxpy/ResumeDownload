using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ResumeDlownload : IResumeDlownload
    {
        private readonly IDownloadParameters _parameters;
        public ResumeDlownload(IDownloadParameters parameters)
        {
            _parameters = parameters;
        }

        public void InitialParameters(string id, string url, string outputFilePath)
        {
            _parameters.Initial(id, url, outputFilePath);
        }

        public long FileSize => _parameters.FileSize;

        public int ChunkCount => GetChunkCount();

        public int MaxChunkSize => _parameters.MaxChunkSize;

        public int MaxRetries => _parameters.MaxRetries > 5 ? 5 : _parameters.MaxRetries;

        public Uri Uri => _parameters.Uri;

        public string Id => _parameters.Id;

        public string OutputPath => _parameters.OuputFilePath;

        public int NumberOfThreads => Math.Min(_parameters.MaxThreads, ChunkCount);

        public ConcurrentStack<int> ReadStack
        {
            get
            {
                var rangeArray = Enumerable.Range(0, ChunkCount).Reverse().ToArray();

                var readStack = new ConcurrentStack<int>();

                readStack.PushRange(rangeArray);

                return readStack;
            }
        }

        public Stream GetFileStream()
        {
            return _parameters.GetOutputStream();
        }

        private int GetChunkCount()
        {
            var fileSize = _parameters.FileSize;
            var chunkSize = _parameters.MaxChunkSize;

            if (_parameters.MaxChunkSize == 0)
            {
                return 0;
            }

            //文件长度 = 分片大小 * 分片数量 + 剩余长度
            //剩余长度 = 文件长度 % 分片大小（取余）
            //分片数量从0开始
            var chunkCount = (int)((fileSize - 1 - (fileSize - 1) % chunkSize) / chunkSize);
            return chunkCount + 1;
        }

        public long GetChunkStart(int currentChunk)
        {
            return currentChunk * (long)MaxChunkSize;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentChunk"></param>
        /// <param name="chunkCount"></param>
        /// <returns></returns>
        public int GetChunkSizeForCurrentChunk(int currentChunk, int chunkCount)
        {
            if (currentChunk + 1 < chunkCount)
            {
                return MaxChunkSize;
            }

            if (currentChunk >= chunkCount)
            {
                return 0;
            }

            var remainder = (int)(FileSize % MaxChunkSize);
            return remainder > 0 ? remainder : MaxChunkSize;
        }

        public bool NeedToCheckForUnwrittenChunks(
            ConcurrentStack<int> readStack,
            DateTime lastWriteTime,
            int minutesToWait)
        {
            return readStack != null && readStack.IsEmpty
                   && (DateTime.Now - lastWriteTime).TotalMinutes > minutesToWait;
        }
    }
}
