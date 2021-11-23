using System.Collections.Concurrent;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 缓冲区队列配置
    /// </summary>
    public class BufferQueueSetting
    {
        public BufferQueueSetting(int chunkSize, int poolSize = 10)
        {
            ChunkSize = chunkSize;
            InitialPoolSize = poolSize;
        }
        public int ChunkSize { get; private set; }
        public int InitialPoolSize { get; private set; }
    }

    public class BufferQueue : ConcurrentQueue<byte[]>
    {

    }
}
