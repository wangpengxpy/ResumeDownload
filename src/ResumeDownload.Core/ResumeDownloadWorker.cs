using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ResumeDownload.Core
{
    public sealed class ResumeDownloadWorker : IResumeDownloadWorker
    {
        private static readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        /// <summary>
        /// 写入队列超过指定数值则阻塞下载
        /// </summary>
        private const int WRITE_QUEUE_DELAY_COUNT = 30;
        /// <summary>
        /// 下载阻塞延迟时间（ms）
        /// </summary>
        private const int DWONLOAD_Throttle_DELAY = 1000;
        /// <summary>
        /// 
        /// </summary>
        private const int STALE_WRITE_CHECK_MINUTES = 5;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        private Func<ILogger<HttpClientRangeRequest>, IResumeDlownload, IHttpClientRange> _clientFactory { get; set; }

        private readonly ILogger<ResumeDownloadWorker> _logger;

        private IAsyncProgress<DownloadProgressChangedEventArgs> _progress;

        private BufferManager _bufferManager;
        private IResumeDlownload _resumeDownload;

        public ResumeDownloadWorker(
            ILogger<ResumeDownloadWorker> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="outputFilePath"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task Start(string url, string id, string outputFilePath = "", IAsyncProgress<DownloadProgressChangedEventArgs> progress = null)
        {
            _ = Task.Run(() =>
            {
                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                var _serviceProvider = scope.ServiceProvider;

                _bufferManager = _serviceProvider.GetRequiredService<BufferManager>();

                _resumeDownload = _serviceProvider.GetRequiredService<IResumeDlownload>();

                _progress = progress;

                _resumeDownload.InitialParameters(id, url, outputFilePath);

                long totalBytesWritten = 0;

                double byteWriteRate = 0.0;

                var readStack = _resumeDownload.ReadStack;

                var chunksWrittens = readStack.ToDictionary(k => k, v => false);

                var writeQueues = new ConcurrentQueue<DownloadChunkedFilePart>();

                var chunkCount = _resumeDownload.ChunkCount;

                bool downloadThrottle(int c) => writeQueues.Count > WRITE_QUEUE_DELAY_COUNT;

                var resumeDownloadTask = new ResumeDownloadTask();

                Download.Workers.TryAdd(_resumeDownload.Id, resumeDownloadTask);

                try
                {
                    for (int i = 0; i < _resumeDownload.NumberOfThreads; i++)
                    {
                        _clientFactory ??= ((l, r) => new HttpClientRangeRequest(l, r));

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _semaphoreSlim.WaitAsync();

                                await HandleHttpRequest(_serviceProvider, readStack, chunkCount, resumeDownloadTask, writeQueues, downloadThrottle);
                            }
                            finally
                            {
                                _semaphoreSlim.Release();
                            }

                        }, resumeDownloadTask.CancellationTokenSource.Token);
                    }

                    var watch = new Stopwatch();
                    watch.Start();

                    var oldElapsedMilliSeconds = watch.ElapsedMilliseconds;
                    var lastWriteTime = DateTime.MaxValue;
                    long lastPointInFile = 0;

                    while (chunksWrittens.Any(kvp => !kvp.Value))
                    {
                        if (Download.Workers.TryGetValue(_resumeDownload.Id, out var cancelledTask))
                        {
                            if (cancelledTask.CancellationTokenSource.IsCancellationRequested)
                            {
                                chunksWrittens = new Dictionary<int, bool>();

                                writeQueues = new ConcurrentQueue<DownloadChunkedFilePart>();

                                readStack = new ConcurrentStack<int>();

                                if (_progress != null)
                                {
                                    _progress.Report(new DownloadProgressChangedEventArgs(_resumeDownload.FileSize,
                                    ComputeProgressIndicator(0, _resumeDownload.FileSize), 0, 0, 0, 0, null, id, null, false, "任务已被取消"));
                                }

                                break;
                            }
                        }

                        while (writeQueues.TryDequeue(out DownloadChunkedFilePart part))
                        {
                            if (Download.Workers.TryGetValue(_resumeDownload.Id, out var pasuseTask))
                            {
                                if (pasuseTask.PauseTokenSource.Token.CanBePaused && pasuseTask.PauseTokenSource.IsPaused)
                                {
                                    readStack.Push(part.Chunk);

                                    _logger.LogInformation($"【{_resumeDownload.Id}】 paused write chunk: {part.Chunk}");

                                    continue;
                                }
                            }

                            _resumeDownload.GetFileStream().Position = part.FileOffset;

                            _resumeDownload.GetFileStream().Write(part.Content, 0, part.Length);

                            totalBytesWritten += part.Length;

                            _bufferManager.FreeBuffer(part.Content);

                            chunksWrittens[part.Chunk] = true;

                            lastWriteTime = DateTime.Now;

                            if (_progress != null)
                            {
                                var elapsed = watch.ElapsedMilliseconds;
                                var diff = elapsed - oldElapsedMilliSeconds;

                                var bytesDownloaded = (long)chunksWrittens.Count(kvp => kvp.Value) * _resumeDownload.MaxChunkSize;

                                var interimReads = bytesDownloaded + part.Length - lastPointInFile;
                                byteWriteRate = (interimReads / (diff / (double)1000));

                                lastPointInFile += interimReads;
                                oldElapsedMilliSeconds = elapsed;

                                ReportProgress(totalBytesWritten, (long)byteWriteRate, _resumeDownload.Id);
                            }
                        }

                        if (_resumeDownload.NeedToCheckForUnwrittenChunks(_resumeDownload.ReadStack, lastWriteTime, STALE_WRITE_CHECK_MINUTES))
                        {
                            // 如果还有剩余部分需要写且读堆栈为空
                            var unreadParts = chunksWrittens.Where(kvp => !kvp.Value);

                            if (readStack.IsEmpty && unreadParts.Any())
                            {
                                _logger.LogDebug($"read stack is empty, but there remains unwritten parts!  Adding {unreadParts.Count()} parts back to read stack.");

                                readStack.Push(unreadParts.Select(kvp => kvp.Key).First());
                            }

                            lastWriteTime = DateTime.Now;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"【{_resumeDownload.Id}】 Exception: downloading failed. Message：${ex.Message},StackTrace:{ex.StackTrace}");

                    if (_progress != null)
                    {
                        ReportProgress(_resumeDownload.FileSize, totalBytesWritten, _resumeDownload.Id, true, ex.Message);
                    };
                }
                finally
                {
                    _logger.LogWarning($"【{_resumeDownload.Id}】 autoClosing stream");

                    if (_progress != null)
                    {
                        ReportProgress(totalBytesWritten, (long)byteWriteRate, _resumeDownload.Id);
                    };

                    _resumeDownload.GetFileStream().Close();
                }
            });

            return Task.CompletedTask;
        }

        private async Task HandleHttpRequest(IServiceProvider _serviceProvider,
            ConcurrentStack<int> readStack,
            int chunkCount, ResumeDownloadTask resumeDownloadTask,
            ConcurrentQueue<DownloadChunkedFilePart> writeQueues,
            Func<int, bool> downloadThrottle)
        {
            var delayThrottle = 1;

            var _client = _clientFactory(_serviceProvider.GetRequiredService<ILogger<HttpClientRangeRequest>>(), _resumeDownload);

            readStack.TryPop(out int currentChunk);

            try
            {
                while (currentChunk >= 0)
                {
                    var part = new DownloadChunkedFilePart
                    {
                        FileOffset = _resumeDownload.GetChunkStart(currentChunk),
                        Length = _resumeDownload.GetChunkSizeForCurrentChunk(currentChunk, chunkCount)
                    };

                    var cancellationTokenSource = resumeDownloadTask.CancellationTokenSource;

                    await resumeDownloadTask.PauseTokenSource.Token.WaitWhilePausedAsync(cancellationTokenSource.Token);

                    var response = await _client.DownloadChunk(part);

                    if (response != null && response.Successed)
                    {
                        // 当前分片数
                        part.Chunk = currentChunk;

                        // 下载字节流
                        part.Content = response.Content;

                        // 下载对应写入队列
                        writeQueues.Enqueue(part);

                        // 下载成功，重置重试或异常下载阻塞次数
                        delayThrottle = 1;

                        // 每一个任务下载成功后，将当前任务分片数置为-1，最终跳出循环
                        if (!readStack.TryPop(out currentChunk))
                        {
                            currentChunk = -1;
                        }

                        // 当前片数超出队列数量（下载过快，但还未来得及写入磁盘）延迟下载时间
                        while (downloadThrottle(currentChunk))
                        {
                            await Task.Delay(DWONLOAD_Throttle_DELAY);
                        }
                    }
                    else if (response == null || response.IsRetry)
                    {
                        // 若读取异常或失败，则根据全局配置尝试重试
                        var sleepSecond = TimeSpan.FromSeconds(Math.Pow(2, delayThrottle));

                        await Task.Delay(sleepSecond);

                        delayThrottle++;

                        if (delayThrottle > _resumeDownload.MaxRetries)
                        {
                            break;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($@"【{_resumeDownload.Id}】 break :【{currentChunk}】");
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException)
            {
                _logger.LogWarning($@"【{_resumeDownload.Id}】 {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}");

                _client.Dispose();
            }
            finally
            {
                if (resumeDownloadTask.CancellationTokenSource.IsCancellationRequested)
                {
                    resumeDownloadTask.CancellationTokenSource.Dispose();
                }
                else if (currentChunk >= 0)
                {
                    readStack.Push(currentChunk);

                    await HandleHttpRequest(_serviceProvider, readStack, chunkCount, resumeDownloadTask, writeQueues, downloadThrottle);
                }

                if (_client != null)
                {
                    _client.Dispose();
                }

                _semaphoreSlim.Release();
            }
        }

        public void Continue(string id)
        {
            if (!Download.Workers.TryGetValue(id, out var resumeDownloadTask))
            {
                return;
            }

            resumeDownloadTask.PauseTokenSource.IsPaused = false;
        }

        public void Pause(string id)
        {
            if (!Download.Workers.TryGetValue(id, out var resumeDownloadTask))
            {
                return;
            }

            resumeDownloadTask.PauseTokenSource.IsPaused = true;
        }

        public void Cancell(string id)
        {
            if (!Download.Workers.TryGetValue(id, out var resumeDownloadTask))
            {
                return;
            }

            try
            {
                resumeDownloadTask.CancellationTokenSource.Cancel();
            }
            catch (Exception ex) when (ex is ObjectDisposedException)
            {
                _logger.LogWarning("任务已被取消");
            }
        }

        private void ReportProgress(long totalBytesWritten, long byteWriteRate, string id, bool isFailed = false, string message = "")
        {
            _progress.Report(new DownloadProgressChangedEventArgs(_resumeDownload.FileSize,
                         ComputeProgressIndicator(totalBytesWritten, _resumeDownload.FileSize), byteWriteRate, byteWriteRate, totalBytesWritten, totalBytesWritten, null, id, null, isFailed, message));
        }

        /// <summary>
        /// 计算进度百分比
        /// </summary>
        /// <param name="bytesWritten"></param>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        private int ComputeProgressIndicator(long bytesWritten, long fileSize)
        {
            return (int)((fileSize != 0) ? ((bytesWritten / (double)fileSize) * 100.0) : 100);
        }
    }
}
