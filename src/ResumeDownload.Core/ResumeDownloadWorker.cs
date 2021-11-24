using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeDownload.Core
{
    public sealed class ResumeDownloadWorker : IResumeDownloadWorker
    {
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

        private Func<IServiceProvider, IHttpClientRange> _clientFactory { get; set; }

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
        public Task Start(string url, string outputFilePath = "", string id = "", IAsyncProgress<DownloadProgressChangedEventArgs> progress = null)
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            var _serviceProvider = scope.ServiceProvider;

            _bufferManager = _serviceProvider.GetRequiredService<BufferManager>();

            _resumeDownload = _serviceProvider.GetRequiredService<IResumeDlownload>();

            _progress = progress;

            _resumeDownload.InitialParameters(id, url, outputFilePath);

            var stream = _resumeDownload.GetFileStream();
            if (stream == null)
            {
                return Task.CompletedTask;
            }

            long totalBytesWritten = 0;

            double byteWriteRate = 0.0;

            var readStack = _resumeDownload.ReadStack;

            var chunkCount = _resumeDownload.ChunkCount;

            var numberOfThreads = _resumeDownload.NumberOfThreads;

            var chunksWritten = readStack.ToDictionary(k => k, v => false);

            var writeQueue = new ConcurrentQueue<DownloadChunkedFilePart>();

            bool downloadThrottle(int c) => writeQueue.Count > WRITE_QUEUE_DELAY_COUNT;

            var resumeDownloadTask = new ResumeDownloadTask();

            Download.Workers.TryAdd(_resumeDownload.Id, resumeDownloadTask);

            try
            {
                for (int i = 0; i < numberOfThreads; i++)
                {
                    _clientFactory ??= ((p) => _serviceProvider.GetRequiredService<IHttpClientRange>());

                    _ = Task.Run(async () =>
                    {
                        var _client = _clientFactory(_serviceProvider);

                        readStack.TryPop(out int currentChunk);

                        var delayThrottle = 1;

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

                                var response = await _client.DownloadChunk(part.FileOffset, part.Length);

                                if (response != null && response.Successed)
                                {
                                    // 当前分片数
                                    part.Chunk = currentChunk;
                                    // 下载字节流
                                    part.Content = response.Content;

                                    // 下载对应写入队列
                                    writeQueue.Enqueue(part);

                                    // 下载成功，重置重试或异常下载阻塞次数
                                    delayThrottle = 1;

                                    // 下载成功，将当前分片数修改以便跳出循环
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
                                    break;
                                }
                            }
                        }
                        catch (Exception ex) when (ex is IOException)
                        {
                            _logger.LogWarning($@"【{_resumeDownload.Id}】 {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}");

                            _client.Dispose();
                        }
                        catch (Exception ex) when (ex is OperationCanceledException)
                        {
                            _logger.LogWarning($"【{_resumeDownload.Id}】 is cancelled");
                        }
                        finally
                        {
                            if (resumeDownloadTask.CancellationTokenSource.IsCancellationRequested)
                            {
                                resumeDownloadTask.CancellationTokenSource.Dispose();
                            }
                            else
                            {
                                if (currentChunk >= 0)
                                {
                                    readStack.Push(currentChunk);
                                }

                                if (_client != null)
                                {
                                    _client.Dispose();
                                }
                            }
                        }

                    }, resumeDownloadTask.CancellationTokenSource.Token);
                }

                var watch = new Stopwatch();
                watch.Start();

                var oldElapsedMilliSeconds = watch.ElapsedMilliseconds;
                var lastWriteTime = DateTime.MaxValue;
                long lastPointInFile = 0;

                //循环将分片文件流写入磁盘文件
                while (chunksWritten.Any(kvp => !kvp.Value))
                {
                    if (Download.Workers.TryGetValue(_resumeDownload.Id, out var cancelledTask))
                    {
                        if (cancelledTask.CancellationTokenSource.IsCancellationRequested)
                        {
                            chunksWritten = new Dictionary<int, bool>();

                            writeQueue = new ConcurrentQueue<DownloadChunkedFilePart>();

                            readStack = new ConcurrentStack<int>();

                            if (_progress != null)
                            {
                                _progress.Report(new DownloadProgressChangedEventArgs(_resumeDownload.FileSize,
                                ComputeProgressIndicator(0, _resumeDownload.FileSize), 0, 0, 0, 0, null, id, null, false, "任务已被取消"));
                            }

                            break;
                        }
                    }

                    while (writeQueue.TryDequeue(out DownloadChunkedFilePart part))
                    {
                        if (Download.Workers.TryGetValue(_resumeDownload.Id, out var pasuseTask))
                        {
                            if (pasuseTask.PauseTokenSource.Token.CanBePaused && pasuseTask.PauseTokenSource.IsPaused)
                            {
                                readStack.Push(part.Chunk);

                                _logger.LogDebug($"【{_resumeDownload.Id}】 paused write chunk: {part.Chunk}");

                                continue;
                            }
                        }

                        _logger.LogDebug($"【{_resumeDownload.Id}】 writing chunk: {part.Chunk}");

                        stream.Position = part.FileOffset;

                        stream.Write(part.Content, 0, part.Length);

                        totalBytesWritten += part.Length;

                        _bufferManager.FreeBuffer(part.Content);

                        chunksWritten[part.Chunk] = true;

                        lastWriteTime = DateTime.Now;

                        if (_progress != null)
                        {
                            var elapsed = watch.ElapsedMilliseconds;
                            var diff = elapsed - oldElapsedMilliSeconds;

                            var bytesDownloaded = (long)chunksWritten.Count(kvp => kvp.Value) * _resumeDownload.MaxChunkSize;

                            var interimReads = bytesDownloaded + part.Length - lastPointInFile;
                            byteWriteRate = (interimReads / (diff / (double)1000));

                            lastPointInFile += interimReads;
                            oldElapsedMilliSeconds = elapsed;

                            ReportProgress(totalBytesWritten, (long)byteWriteRate, _resumeDownload.Id);
                        }
                    }

                    if (_resumeDownload.NeedToCheckForUnwrittenChunks(readStack, lastWriteTime, STALE_WRITE_CHECK_MINUTES))
                    {
                        // 如果还有剩余部分需要写且读堆栈为空
                        var unreadParts = chunksWritten.Where(kvp => !kvp.Value);

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

                stream.Close();
            }

            return Task.CompletedTask;
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
            catch (Exception ex) when(ex is ObjectDisposedException)
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
