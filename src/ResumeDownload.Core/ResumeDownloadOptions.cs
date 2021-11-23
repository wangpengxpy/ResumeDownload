namespace Microsoft.Extensions.DependencyInjection
{
    public class ResumeDownloadOptions
    {
        /// <summary>
        /// 下载文件输出路径
        /// </summary>
        public string OutputFilePath { get; set; }
        /// <summary>
        /// 下载文件最大线程数
        /// </summary>
        public int? MaxThreads { get; set; }
        /// <summary>
        /// 下载文件最大分片大小（单位：字节）
        /// </summary>
        public int? MaxChunkSize { get; set; }
        /// <summary>
        /// 下载文件失败（IO或服务异常）最大尝试次数，默认为5
        /// </summary>
        public int MaxRetries { get; set; } = 5;
    }
}
