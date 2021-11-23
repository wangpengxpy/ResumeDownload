using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ResumeDownload.Core
{
    /// <summary>
    /// 缓冲区队列管理器
    /// </summary>
    public class BufferManager : IDisposable
    {
        private readonly ConcurrentDictionary<int, BufferQueue> queues =
            new ConcurrentDictionary<int, BufferQueue>();

        public static readonly byte[] EmptyBuffer = Array.Empty<byte>();

        /// <summary>
        /// 控制所有缓冲区的分配和取消分配的类。
        /// </summary>
        /// <param name="buffers"></param>
        public BufferManager(IEnumerable<BufferQueueSetting> buffers)
        {
            AllocateBuffers(buffers);
        }

        /// <summary>
        ///从池中分配一个现有缓冲区
        /// </summary>
        /// <param name="buffer">需要分配缓冲区大小</param>
        /// <param name="chunkSize">缓冲区大小</param>
        private void GetBuffer(ref byte[] buffer, int chunkSize)
        {
            //如果缓冲区很小，而池中没有缓冲区，则只需返回一个以缓冲区大小的新缓冲区，否则从池中返回。
            if (queues.TryGetValue(chunkSize, out BufferQueue queue))
            {
                if (queue.Count == 0)
                {
                    AllocateBuffers(new[] { new BufferQueueSetting(chunkSize, 5) });
                }
                if (queues[chunkSize].TryDequeue(out buffer))
                {
                    return;
                }
            }
            buffer = new byte[chunkSize];
        }

        /// <summary>
        /// 获取指定大小缓冲区
        /// </summary>
        /// <param name="minCapacity"></param>
        /// <returns></returns>
        public byte[] GetBuffer(int minCapacity)
        {
            var buffer = EmptyBuffer;
            GetBuffer(ref buffer, minCapacity);
            return buffer;
        }

        /// <summary>
        /// 从缓冲池中释放缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="clearContent"></param>
        public void FreeBuffer(byte[] buffer, bool clearContent = false)
        {
            FreeBuffer(ref buffer);
        }

        public void FreeBuffer(ref byte[] buffer, bool clearContent = false)
        {
            if (buffer == EmptyBuffer)
            {
                return;
            }

            if (queues.TryGetValue(buffer.Length, out BufferQueue queue))
            {
                if (clearContent)
                {
                    Array.Clear(buffer, 0, buffer.Length);
                }
                queue.Enqueue(buffer);
            }

            //缓冲区恢复后，将EmptyBuffer作为占位符返回
            buffer = EmptyBuffer;
        }


        /// <summary>
        /// 在池中分配缓冲区或返回已存在缓冲区
        /// </summary>
        /// <param name="settings"></param>
        private void AllocateBuffers(IEnumerable<BufferQueueSetting> settings)
        {
            foreach (var setting in settings)
            {
                BufferQueue queue;
                if (queues.ContainsKey(setting.ChunkSize))
                {
                    queue = queues[setting.ChunkSize];
                }
                else
                {
                    queue = new BufferQueue();

                    queues.AddOrUpdate(setting.ChunkSize, queue, (i, bufferQueue) => bufferQueue);
                }

                for (int i = 0; i < setting.InitialPoolSize; i++)
                {
                    queue.Enqueue(new byte[setting.ChunkSize]);
                }
            }
        }

        /// <summary>
        /// 释放缓冲池
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var queue in queues)
                {
                    //这里直接从缓冲队列将其取出理论上应该就会释放
                    //若取出后将其置为空，但sonalr提示没有必要
                    queue.Value.TryDequeue(out var _);
                }
            }
        }
    }
}
