namespace ResumeDownload.Core
{
    public class DownloadProgressBar
    {
        public string Id { get; internal set; }
        /// <summary>
        /// 下载速度
        /// </summary>
        public string DownloadRate { get; internal set; }
        /// <summary>
        /// 已下载多少兆
        /// </summary>
        public string DownloadSize { get; internal set; }
        /// <summary>
        /// 共多少兆
        /// </summary>
        public string TotalSize { get; internal set; }
        /// <summary>
        /// 百分比
        /// </summary>
        public int Percentage { get; internal set; }
    }
}
